using UnityEngine;
using System.Collections;

namespace UnityMoverioBT200.Scripts
{

  public class CommToAndroid : MonoBehaviour
  {
    public enum DisplayMode { Display2D, Display3D };

    public DisplayMode DisplayMode2D3D = DisplayMode.Display2D;

    public bool ShowGUI;

    private static CommToAndroid instance = null;
    public static CommToAndroid Instace
    {
      get
      {
        if (instance == null)
          instance = new CommToAndroid();
        return instance;
      }
    }

    public GUIText JNITextOutput = null;

    public CommToAndroid()
    {
      instance = this;
    }

    ~CommToAndroid()
    {
      Debug.Log("Destroying the CommToAndroid");
    }

    // Use this for initialization
    void Start()
    {
      try
      {
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
          DisplayMode currentMode = (DisplayMode)System.Enum.Parse(typeof(DisplayMode), CallAndroidMethod<string>("getDisplayMode"));
          if (currentMode != DisplayMode2D3D)
            CallAndroidMethod("setDisplayMode", DisplayMode2D3D.ToString());
        }

        string messageFromJNI = CallAndroidMethod<string>("getStringFromJNI");
        Debug.Log(messageFromJNI);
        if (JNITextOutput != null)
          JNITextOutput.text = messageFromJNI;
      }
      catch (System.Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    void OnGUI()
    {
      if (!ShowGUI)
        return;

      GUILayout.Space(207);
      if (GUILayout.Button("Set 3D", GUILayout.Width(100), GUILayout.Height(30)))
      {
        DisplayMode2D3D = DisplayMode.Display3D;
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
          CallAndroidMethod("setDisplayMode", DisplayMode.Display2D.ToString());
          CallAndroidMethod("setDisplayMode", DisplayMode2D3D.ToString());
        }

        if (Network.isClient || Network.isServer)
          networkView.RPC("SynchDisplayMode", RPCMode.OthersBuffered, DisplayMode2D3D.ToString());
      }
    }

    [RPC]
    void SynchDisplayMode(string displayMode)
    {
      DisplayMode2D3D = (DisplayMode)System.Enum.Parse(typeof(DisplayMode), displayMode);
      if (SystemInfo.deviceType != DeviceType.Handheld)
        return;

      CallAndroidMethod("setDisplayMode", DisplayMode.Display2D.ToString());
      CallAndroidMethod("setDisplayMode", DisplayMode2D3D.ToString());
    }

    // Update is called once per frame
    void Update()
    {

    }

    public static void CallAndroidMethod(string methodName, params object[] parameters)
    {
      if (SystemInfo.deviceType != DeviceType.Handheld)
        return;

      try
      {
        using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
          using (AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"))
          {
            jo.Call(methodName, parameters);
          }
        }
      }
      catch (System.Exception ex)
      {
        Debug.LogException(ex);
      }
    }

    public static ReturnType CallAndroidMethod<ReturnType>(string methodName, params object[] parameters)
    {
      if (SystemInfo.deviceType != DeviceType.Handheld)
        return default(ReturnType);

      try
      {
        using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
          using (AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"))
          {
            return jo.Call<ReturnType>(methodName, parameters);
          }
        }
      }
      catch (System.Exception ex)
      {
        Debug.LogException(ex);
      }
      return default(ReturnType);
    }

  }

}