using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GyroMouseController : MonoBehaviour
{

  public GameObject UIController;

  public bool ShowGUI { get; set; }
  public bool IsActive
  {
    get { return gameObject.activeSelf; }
    set { this.enabled = value; gameObject.SetActive(value); }
  }

  private static GyroMouseController instance;
  public static GyroMouseController Instance
  {
    get
    {
      if (instance == null)
        instance = new GyroMouseController();
      return instance;
    }
  }

  public GyroMouseController()
  {
    instance = this;
    instance.ShowGUI = false;
  }

  private Quaternion lastRotationInv;
  private GUITexture GyroPointer;

  private GameObject EPSONcamera;
  private Camera RightCamera;
  private Ray SelectionRay;
  
  private Rect InitialPointerLocation;    

  // Use this for initialization
  void Start()
  {
    GyroPointer = gameObject.GetComponent<GUITexture>();
    InitialPointerLocation = GyroPointer.pixelInset;

    EPSONcamera = GameObject.Find("EPSON Moverio BT-200");
    RightCamera = EPSONcamera.transform.FindChild("rightCam").gameObject.GetComponent<Camera>();
  }

  // Update is called once per frame
  void Update()
  {
    Quaternion rotationDiff = lastRotationInv * RotationProvider.Instance.Rotation;
    lastRotationInv = Quaternion.Inverse(RotationProvider.Instance.Rotation);

    Vector3 rotPosNeg = RotationProvider.RotAsPosNeg(rotationDiff);

    //rotating the device 40 degress in x moves the pointer the entire width of the display
    // similarly, 30 degrees in y moves the pointer then entire height of the display
    Vector2 multiplier = new Vector2(2f * Screen.height, 2f * Screen.height);

    //limits
    Vector2 minValues = new Vector2(-1 * (Screen.width / 4 + GyroPointer.pixelInset.width / 2),
                                    -1 * (Screen.height / 2 + GyroPointer.pixelInset.height / 2));
    Vector2 maxValues = new Vector2(Screen.width / 4 - GyroPointer.pixelInset.width / 2,
                                    Screen.height / 2 - GyroPointer.pixelInset.height / 2);

    Rect pointerLocation = GyroPointer.pixelInset;
    pointerLocation.x += multiplier.x * CDFunction(rotPosNeg.y);
    pointerLocation.y += multiplier.y * CDFunction(rotPosNeg.x * -1);

    Rect boundedPointerLocation = pointerLocation;
    boundedPointerLocation.x = Mathf.Max(minValues.x, Mathf.Min(maxValues.x, pointerLocation.x));
    boundedPointerLocation.y = Mathf.Max(minValues.y, Mathf.Min(maxValues.y, pointerLocation.y));

    GyroPointer.pixelInset = boundedPointerLocation;

    CheckHovers();
  }
  
  float CDFunction(float angleDiff)
  {
    float sign = Mathf.Sign(angleDiff);
    float val = Mathf.Abs(angleDiff);
    float cdCorrectedVal = Mathf.Atan(val * Mathf.Deg2Rad - 0.00125f);

    return sign * Mathf.Max(0f, cdCorrectedVal);
  }

  void OnGUI()
  {
    if (!ShowGUI || !this.enabled)
      return;

    GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, 30));
    if (GUILayout.Button("GyroMouse", GUILayout.Width(100), GUILayout.Height(30)))
    {
    }
    GUILayout.EndArea();
  }

  private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

  void CheckHovers()
  {
    Collider target = GetFirstAffectedTarget();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.GyroMouse;
    args.Conflict = false;

    if (target != null)
    {
      if (hoveredTargets.ContainsKey(target.gameObject))
        hoveredTargets[target.gameObject] = true;
      else
        hoveredTargets.Add(target.gameObject, true);
      target.SendMessage("Hovered", args);
    }

    List<GameObject> notHovered = new List<GameObject>();
    foreach (GameObject targetObj in hoveredTargets.Keys)
    {
      if (!hoveredTargets[targetObj])
        notHovered.Add(targetObj);
    }

    foreach (GameObject targetObj in notHovered)
    {
      args = new SelectionControllerEventArgs();
      args.Device = ControllerType.GyroMouse;
      args.Conflict = false;

      targetObj.SendMessage("NotHovered", args);
      hoveredTargets.Remove(targetObj);
    }

    List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
    foreach (GameObject targetObj in keys)
      hoveredTargets[targetObj] = false;
  }

  void TouchStarted(MoverioInputEventArgs args)
  {
    if (!this.enabled)
      return;

    CheckSelections();
  }

  void DoubleTap(MoverioInputEventArgs args)
  {
    if (!this.enabled)
      return;
    
    GyroPointer.pixelInset = InitialPointerLocation;
  }

  void CheckSelections()
  {
    Collider target = GetFirstAffectedTarget();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.GyroMouse;
    args.Conflict = false;

    if (target != null)
      target.SendMessage("Selected", args);
    else
    {
      SelectionEventArgs seArgs = new SelectionEventArgs();
      seArgs.Device = ControllerType.GyroMouse;
      seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
      seArgs.Conflit = args.Conflict;

      UIController.SendMessage("Selected", seArgs);
    }
  }

  Collider GetFirstAffectedTarget()
  {
    Vector3 origin = new Vector3(Screen.width / 2 + Screen.width / 4, Screen.height / 2);
    origin.x = origin.x + GyroPointer.pixelInset.center.x;
    origin.y = origin.y + GyroPointer.pixelInset.center.y;

    SelectionRay = RightCamera.ScreenPointToRay(origin);

    RaycastHit hit;
    Physics.Raycast(SelectionRay, out hit);
    return hit.collider;
  }
}
