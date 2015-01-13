using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeadController : MonoBehaviour
{

  public GameObject LookAtObject;
  public GUITexture HeadPointer;
  public GameObject UIController;

  private GameObject NeckJoint;
  private GameObject EPSONcamera;
  private Camera RightCamera;

  private Ray SelectionRay;
  private LineRenderer SelectionRayRenderer;

  public bool ShowGUI { get; set; }
  public bool IsActive
  {
    get { return gameObject.activeSelf; }
    set
    {
      this.enabled = value;
      HeadPointer.enabled = value;
      SetDefaultRotation();
    }
  }

  private static HeadController instance;
  public static HeadController Instance
  {
    get
    {
      if (instance == null)
        instance = new HeadController();
      return instance;
    }
  }

  public HeadController()
  {
    instance = this;
    instance.ShowGUI = false;
  }

  // Use this for initialization
  void Start()
  {
    NeckJoint = GameObject.Find("NeckJoint");

    EPSONcamera = GameObject.Find("EPSON Moverio BT-200");
    RightCamera = EPSONcamera.transform.FindChild("rightCam").gameObject.GetComponent<Camera>();

    //PrepareSelectionRay();
  }

  void PrepareSelectionRay()
  {
    SelectionRayRenderer = gameObject.AddComponent<LineRenderer>();
    SelectionRayRenderer.material = new Material(Shader.Find("Particles/Additive"));
    SelectionRayRenderer.SetColors(Color.red, Color.blue);
    SelectionRayRenderer.SetWidth(0.005f, 0.005f);
    SelectionRayRenderer.SetVertexCount(2);
  }

  // Update is called once per frame
  void Update()
  {
    //if(!RotationProvider.Instance.IsReceivingDataFromIMU)
    //    return; //keeps the orientation given at startup
    if (!this.enabled)
      return;

    NeckJoint.transform.localRotation = RotationProvider.Instance.Rotation;

    CheckHovers();
    //DrawSelectionRay();
  }

  public void SetDefaultRotation()
  {
    SetDefaultRotation(LookAtObject);
  }

  private void SetDefaultRotation(GameObject target)
  {
    if (target == null)
      return;

    //it orientates the entire head
    GameObject.Find("NeckJoint").transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    gameObject.transform.LookAt(target.transform.position);
  }

  void TouchStarted(MoverioInputEventArgs args)
  {
    if (!this.enabled)
      return;

    CheckSelections();
  }

  private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

  void CheckHovers()
  {
    Collider target = GetFirstAffectedTarget();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.Head;
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
      args.Device = ControllerType.Head;
      args.Conflict = false;

      targetObj.SendMessage("NotHovered", args);
      hoveredTargets.Remove(targetObj);
    }

    List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
    foreach (GameObject targetObj in keys)
      hoveredTargets[targetObj] = false;
  }

  void CheckSelections()
  {
    Collider target = GetFirstAffectedTarget();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.Head;
    args.Conflict = false;

    if (target != null)
      target.SendMessage("Selected", args);
    else
    {
      SelectionEventArgs seArgs = new SelectionEventArgs();
      seArgs.Device = ControllerType.Head;
      seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
      seArgs.Conflit = args.Conflict;

      UIController.SendMessage("Selected", seArgs);
    }
  }

  Collider GetFirstAffectedTarget()
  {
    float eyeWidth = Screen.width / 2;
    SelectionRay = RightCamera.ScreenPointToRay(new Vector3(eyeWidth + eyeWidth * HeadPointer.transform.position.x, Screen.height / 2));
    RaycastHit hit;
    Physics.Raycast(SelectionRay, out hit);
    return hit.collider;
  }

  void DrawSelectionRay()
  {
    var origin = SelectionRay.origin;
    var direction = SelectionRay.direction;
    var endPoint = origin + direction * 1.5f;
    RaycastHit hit;

    SelectionRayRenderer.SetPosition(0, origin);

    if (Physics.Raycast(origin, direction, out hit))
      endPoint = hit.point;

    SelectionRayRenderer.SetPosition(1, endPoint);
  }

}
