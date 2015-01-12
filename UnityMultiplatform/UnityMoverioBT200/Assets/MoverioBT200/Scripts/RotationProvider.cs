using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;

public class RotationProvider : MonoBehaviour
{

  //the Unity method uses only the gyro.attitude and I have noticed considerable drift
  public enum AlgorithmIMU { Unity, MadgwickOnAndroid }; //other options: Android orientation, MadgwickOnUnity

  public enum SensorMode { Controller, Headset };

  private Regex validateData;
  private string dataFromJava = string.Empty;

  private CircularList<float> framerateFilter;
  private System.DateTime lastUpdate;
  private CircularList<Quaternion> orientationFilter;
  private int CurrentFilterSize = 0;
  private bool useMagnetometer = false;

  private Quaternion actualRotation;
  private Quaternion baselineRotation;
  private Quaternion invBaselineRotation;

  public Quaternion Rotation { get { return actualRotation; } }
  public bool IsReceivingDataFromIMU { get; set; }

  public int FilterSize = 5;
  public AlgorithmIMU Algorithm;
  public SensorMode SourceIMU = SensorMode.Controller;
  private int updateCount = 0;
  private bool sensorChangedSetBaseline = false;

  public bool ShowGUI { get; set; }

  private static RotationProvider instance;
  public static RotationProvider Instance
  {
    get
    {
      if (instance == null)
        instance = new RotationProvider();
      return instance;
    }
  }

  public RotationProvider()
  {
    instance = this;
    instance.ShowGUI = false;
    instance.IsReceivingDataFromIMU = false;
  }

  // Use this for initialization
  void Start()
  {
    validateData = new Regex("(-)*([0-9])+.([0-9])+");
    framerateFilter = new CircularList<float>(60);

    lastUpdate = System.DateTime.Now;
    actualRotation = Quaternion.identity;
    baselineRotation = Quaternion.identity;
    invBaselineRotation = Quaternion.identity;

    //Icredibly enough the Unity orientation methods work OK in Japan
    Input.gyro.enabled = true;
    UpdateFilterParameters(FilterSize);

    StartCoroutine(FPS());
    CommToAndroid.CallAndroidMethod("SetGameObject", this.name);

    SensorMode currentSensor = (SensorMode)System.Enum.Parse(typeof(SensorMode), CommToAndroid.CallAndroidMethod<string>("GetSensorMode"));
    if (currentSensor != SourceIMU)
      SetSourceIMU(SourceIMU);
  }

  // Update is called once per frame
  void Update()
  {
    if (dataFromJava == string.Empty)
      return;

    MatchCollection matches = validateData.Matches(dataFromJava);
    if (matches == null || matches.Count != 4)
    {
      Debug.Log("Wrong data received from Java: " + dataFromJava);
      return;
    }

    //updates the size of the filter
    UpdateFilterParameters(FilterSize);
    //String message = String.format("%3.3f;%3.3f;%3.3f;%3.3f", mAHRSState.q0, mAHRSState.q1, mAHRSState.q2, mAHRSState.q3);
    Quaternion rotation = new Quaternion();
    if (SourceIMU == SensorMode.Controller)
    {
      if (Algorithm == AlgorithmIMU.MadgwickOnAndroid)
      {
        rotation.y = float.Parse(matches[0].Value);
        rotation.z = float.Parse(matches[1].Value);
        rotation.x = float.Parse(matches[2].Value) * -1;
        rotation.w = float.Parse(matches[3].Value);
      }
      else if (Algorithm == AlgorithmIMU.Unity)
      {
        rotation.x = Input.gyro.attitude.x * -1;
        rotation.z = Input.gyro.attitude.y * -1;
        rotation.y = Input.gyro.attitude.z * -1;
        rotation.w = Input.gyro.attitude.w;
      }
    }
    else
    {
      if (Algorithm == AlgorithmIMU.MadgwickOnAndroid)
      {
        //this code is not correctly mapped
        rotation.x = float.Parse(matches[0].Value);
        rotation.y = float.Parse(matches[1].Value);
        rotation.z = float.Parse(matches[2].Value);
        rotation.w = float.Parse(matches[3].Value);
      }
      else if (Algorithm == AlgorithmIMU.Unity)
      {
        rotation.x = Input.gyro.attitude.x * -1;
        rotation.y = Input.gyro.attitude.y * -1;
        rotation.z = Input.gyro.attitude.z;
        rotation.w = Input.gyro.attitude.w;
      }
    }

    // Updates the rotation in every update and introduces the settings of a new baseline when a specified lenght of 
    // time has passed (BaselineTouchMillis) without significant movement (5 degrees) or finger displacement (20px).
    actualRotation = invBaselineRotation * GetFilteredRotation(rotation);

    UpdateCount();
  }

  void UpdateCount()
  {
    updateCount++;
    if (!sensorChangedSetBaseline && updateCount >= 60)
    {
      SetBaselineRotation();
      sensorChangedSetBaseline = true;
    }
  }

  void DoubleTap(MoverioInputEventArgs args)
  {
    SetBaselineRotation();
  }

  void OnGUI()
  {
    if (!ShowGUI || !this.enabled)
      return;

    GUILayout.Space(310);
    if (GUILayout.Button("Set Baseline", GUILayout.Width(100), GUILayout.Height(30)))
      SetBaselineRotation();

    if (GUILayout.Toggle(useMagnetometer, "Magt", GUILayout.Width(100), GUILayout.Height(25)))
    {
      CommToAndroid.CallAndroidMethod("ToggleMagnetometer");
      useMagnetometer = !useMagnetometer;
    }

    SensorMode otherIMU = SourceIMU == SensorMode.Headset ? SensorMode.Controller : SensorMode.Headset;
    if (GUILayout.Button("Use " + otherIMU.ToString(),
                         GUILayout.Width(100), GUILayout.Height(30)))
    {
      SourceIMU = otherIMU;
      CommToAndroid.CallAndroidMethod("SetSensorMode", otherIMU.ToString());
    }

    System.Array algorithmTypes = System.Enum.GetValues(typeof(AlgorithmIMU));
    GUILayout.Label("Algorithm:", GUILayout.Width(100), GUILayout.Height(20));
    foreach (AlgorithmIMU method in algorithmTypes)
    {
      bool isCurrent = Algorithm == method;
      if (GUILayout.Toggle(isCurrent,
                           method.ToString(),
                           GUILayout.Width(100), GUILayout.Height(25)) && !isCurrent)  //-- this last piece is VERY important
      {
        Algorithm = method;
      }
    }

    GUILayout.BeginArea(new Rect(0, 520, 200, 20));
    string topMessage = string.Format("{0:0.00} dFPS - {1:0.00} gFPS", GetDataUpdateRate(), FramesPerSec);
    GUILayout.TextField(topMessage, GUILayout.Width(Screen.width), GUILayout.Height(20));
    GUILayout.EndArea();
  }

  private void UpdateFilterParameters(int size)
  {
    if (size <= 0)
      size = 1;
    if (size == CurrentFilterSize)
      return;

    CurrentFilterSize = size;
    orientationFilter = new CircularList<Quaternion>(CurrentFilterSize);
  }

  /* **********************************************************************
   * PROPERTIES
   * *********************************************************************/
  public float FramesPerSec { get; protected set; }
  public float FrecuencyToCheckFPS = 0.5f;

  /*
   * EVENT: FPS
   */
  private IEnumerator FPS()
  {
    for (; ; )
    {
      // Capture frame-per-second
      int lastFrameCount = Time.frameCount;
      float lastTime = Time.realtimeSinceStartup;
      yield return new WaitForSeconds(FrecuencyToCheckFPS);
      float timeSpan = Time.realtimeSinceStartup - lastTime;
      int frameCount = Time.frameCount - lastFrameCount;

      // Display it
      FramesPerSec = frameCount / timeSpan;
    }
  }

  // This method is called from the Android code by means of a:
  //  UnityPlayer.UnitySendMessage("Object Name", "Message", "Parameters");
  //  Please note that "Message" is the name of the method.
  void Message(string data)
  {
    dataFromJava = data;

    System.DateTime updateTime = System.DateTime.Now;
    double milliseconds = (updateTime - lastUpdate).TotalMilliseconds;
    double fRate = 1000.0d / milliseconds;
    lastUpdate = updateTime;

    if (milliseconds < 10f) //removes the very short bursts that are corrupting the real value
      return;

    framerateFilter.Value = (float)fRate;
    framerateFilter.Next();
    IsReceivingDataFromIMU = true;
  }

  private float GetDataUpdateRate()
  {
    float accumDFR, count;
    accumDFR = 0.0f;
    count = framerateFilter.Count;
    for (int index = 0; index < count; index++)
      accumDFR += framerateFilter[index];

    return accumDFR / count;
  }

  private Quaternion GetFilteredRotation(Quaternion rotation)
  {
    if (rotation != Quaternion.identity)
    {
      orientationFilter.Value = rotation;
      orientationFilter.Next();
    }

    if (orientationFilter == null || orientationFilter.Count == 0)
      return Quaternion.identity;

    float accumX, accumY, accumZ, accumW, count;
    accumX = accumY = accumZ = accumW = 0.0f;
    count = orientationFilter.Count;
    for (int index = 0; index < count; index++)
    {
      Quaternion quat = orientationFilter[index];
      accumX += quat.x;
      accumY += quat.y;
      accumZ += quat.z;
      accumW += quat.w;
    }

    Quaternion filtered = new Quaternion(accumX / count, accumY / count, accumZ / count, accumW / count);
    return filtered;
  }

  public void SetBaselineRotation()
  {
    baselineRotation = GetFilteredRotation(Quaternion.identity);
    invBaselineRotation = Quaternion.Inverse(baselineRotation);
  }

  public static float RotMagnitude(Quaternion rotationT)
  {
    return RotAsPosNeg(rotationT).magnitude;
  }

  public static Vector3 RotAsPosNeg(Quaternion rotationT)
  {
    Vector3 diffRotVec3 = new Vector3();

    diffRotVec3.x = rotationT.eulerAngles.x;
    if (diffRotVec3.x > 180f)
      diffRotVec3.x = -1 * (360 - diffRotVec3.x);
    diffRotVec3.y = rotationT.eulerAngles.y;
    if (diffRotVec3.y > 180f)
      diffRotVec3.y = -1 * (360 - diffRotVec3.y);
    diffRotVec3.z = rotationT.eulerAngles.z;
    if (diffRotVec3.z > 180f)
      diffRotVec3.z = -1 * (360 - diffRotVec3.z);

    return diffRotVec3;
  }

  public static string Vector3ToString(Vector3 point)
  {
    return string.Format("[{0:0.000}, {1:0.000}, {2:0.000}]", point.x, point.y, point.z);
  }

  public void SetSourceIMU(SensorMode imu)
  {
    SourceIMU = imu;
    CommToAndroid.CallAndroidMethod("SetSensorMode", SourceIMU.ToString());

    updateCount = 0;
    sensorChangedSetBaseline = false;
  }

}
