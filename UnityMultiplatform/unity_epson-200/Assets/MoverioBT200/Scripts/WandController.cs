using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class WandController : MonoBehaviour
{
  public enum WandFunctionType { Ray, TouchDrag, Twist, TouchPush }

  public enum TwistDirectionType { ClockWise, CounterClockWise }

  /** These are the public properties set on the Unity editor **/
  public GameObject UIController;
  public GameObject InitialPointing;
  public float WandGain = 0.05f;
  public WandFunctionType WandType;
  public WandFunctionType RayDissambiguationMode;
  public TwistDirectionType TwistDirection;

  public int SelectionFeedbackMillis = 150; //milliseconds
  public Color BaseColor = Color.white;
  public Color SelectionColor = Color.blue;
  /** End **/

  public bool ShowGUI { get; set; }
  public bool IsActive
  {
    get { return gameObject.activeSelf; }
    set { this.enabled = value; gameObject.SetActive(value); }
  }

  private GameObject WandRotation { get; set; }
  private GameObject Tip { get; set; }
  private GameObject Shaft { get; set; }

  private static WandController instance;
  public static WandController Instance
  {
    get
    {
      if (instance == null)
        instance = new WandController();
      return instance;
    }
  }

  public WandController()
  {
    instance = this;
    instance.ShowGUI = false;
  }

  // Use this for initialization 
  void Start()
  {
    WandRotation = GameObject.Find("WandRotation");
    Tip = GameObject.Find("Tip");
    Shaft = GameObject.Find("Shaft");
    CalculateLocalTipParameters();

    SetExtensionMethodSettings();
  }

  // Update is called once per frame
  void Update()
  {
    ProcessOrientationUpdate();
    CheckHovers();
    CheckRayMode(); //whether it needs to go into disambiguation mode

    Color shaftColor = BaseColor;
    System.DateTime now = System.DateTime.Now;
    if ((now - selectionTime).TotalMilliseconds < SelectionFeedbackMillis)
      shaftColor = SelectionColor;

    Shaft.renderer.material.SetColor("_Color", shaftColor);

    if (Network.isServer)
      networkView.RPC("UpdateState", RPCMode.AllBuffered, new Vector3(shaftColor.r, shaftColor.g, shaftColor.b));
  }

  [RPC]
  void UpdateState(Vector3 shaftColor)
  {
    if (Network.isServer)
      return;

    Shaft.renderer.material.SetColor("_Color", new Color(shaftColor.x, shaftColor.y, shaftColor.z));
  }

  void ProcessOrientationUpdate()
  {
    Quaternion rotationToApply = RotationProvider.Instance.Rotation;

    float finalScale = factorToCenter;
    switch (WandType)
    {
      case WandFunctionType.Twist:
        WandRotation.transform.localRotation = rotationToApply;
        float rotationZ = rotationToApply.eulerAngles.z;
        if (rotationZ > 180f)
          rotationZ = -1 * (360 - rotationZ);

        if (TwistDirection == TwistDirectionType.ClockWise)
          finalScale += rotationZ * WandGain;
        else
          finalScale += rotationZ * WandGain * -1;
        SetWandLength(finalScale);
        break;
      case WandFunctionType.TouchPush:
        if (!isFingerDown)
        {
          WandRotation.transform.localRotation = rotationToApply;
          return;
        }

        Quaternion rotDiff = invPushRotation * rotationToApply;

        float rotationX = rotDiff.eulerAngles.x;
        if (rotationX > 180f)
          rotationX = -1 * (360 - rotationX);

        finalScale += rotationX * WandGain;
        SetWandLength(finalScale);
        break;
      default:
        WandRotation.transform.localRotation = rotationToApply;
        break;
    }
  }

  public void SetDefaultRotation()
  {
    SetDefaultRotation(InitialPointing);
  }

  private void SetDefaultRotation(GameObject target)
  {
    if (target == null)
      return;

    this.transform.LookAt(target.transform.position);
  }

  bool isFingerDown = false;
  private Quaternion pushRotation = Quaternion.identity;
  private Quaternion invPushRotation = Quaternion.identity;

  void TouchStarted(MoverioInputEventArgs args)
  {
    isFingerDown = true;
    switch (WandType)
    {
      case WandFunctionType.Ray:
        CheckSelectionWithDesambiguation();
        break;
      case WandFunctionType.Twist:
        CheckSelections();
        break;
      case WandFunctionType.TouchPush:
        pushRotation = RotationProvider.Instance.Rotation;
        invPushRotation = Quaternion.Inverse(pushRotation);
        break;
      default:
        break;
    }
  }

  void TouchMoved(MoverioInputEventArgs args)
  {
    switch (WandType)
    {
      case WandFunctionType.TouchDrag:
        float finalScale = factorToCenter + args.DragFromOrigin.y * WandGain;
        SetWandLength(finalScale);
        break;
      default:
        break;
    }
  }

  void TouchEnded(MoverioInputEventArgs args)
  {
    isFingerDown = false;
    switch (WandType)
    {
      case WandFunctionType.TouchDrag:
      case WandFunctionType.TouchPush:
        CheckSelections();
        SetWandLength(factorToCenter);
        break;
      default:
        break;
    }
  }

  private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

  void CheckHovers()
  {
    Collider[] targets = GetAffectedTargets();
    foreach (Collider target in targets)
    {
      SelectionControllerEventArgs args = new SelectionControllerEventArgs();
      args.Device = ControllerType.Wand;
      args.Conflict = targets.Length > 1;

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
      SelectionControllerEventArgs args = new SelectionControllerEventArgs();
      args.Device = ControllerType.Wand;
      args.Conflict = false;

      targetObj.SendMessage("NotHovered", args);
      hoveredTargets.Remove(targetObj);
    }

    List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
    foreach (GameObject targetObj in keys)
      hoveredTargets[targetObj] = false;
  }

  private System.DateTime selectionTime = System.DateTime.MinValue;

  void CheckSelections()
  {
    Collider[] targets = GetAffectedTargets();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.Wand;
    args.Conflict = targets.Length > 1;

    foreach (Collider target in targets)
      target.SendMessage("Selected", args);

    if (targets.Length == 0 || args.Conflict)
    {
      SelectionEventArgs seArgs = new SelectionEventArgs();
      seArgs.Device = ControllerType.Wand;
      seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
      seArgs.Conflit = args.Conflict;

      UIController.SendMessage("Selected", seArgs);
    }

    selectionTime = System.DateTime.Now;
  }

  void CheckRayMode()
  {
    if (hoveredTargets.Count <= 1)
    {
      return;
    }
  }

  void CheckSelectionWithDesambiguation()
  {
    Collider[] targets = GetAffectedTargets();

    if (targets.Length == 0)
    {
      SelectionEventArgs seArgs = new SelectionEventArgs();
      seArgs.Device = ControllerType.Wand;
      seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
      seArgs.Conflit = false;

      UIController.SendMessage("Selected", seArgs);
      selectionTime = System.DateTime.Now;
    }
    else if (targets.Length == 1)
    {
      SelectionControllerEventArgs args = new SelectionControllerEventArgs();
      args.Device = ControllerType.Wand;
      args.Conflict = false;

      targets[0].SendMessage("Selected", args);
      selectionTime = System.DateTime.Now;
    }
    else
    {
      //disambiguate here -- for exp 1
      SelectionControllerEventArgs args = new SelectionControllerEventArgs();
      args.Device = ControllerType.Wand;
      args.Conflict = targets.Length > 1;

      foreach (Collider target in targets)
        target.SendMessage("Selected", args);
      selectionTime = System.DateTime.Now;
    }
  }

  Collider[] GetAffectedTargets()
  {
    List<Collider> targets = new List<Collider>();
    if (WandType == WandFunctionType.Ray)
    {
      Vector3 origin = gameObject.transform.position;
      Vector3 direction = (Tip.transform.position - origin).normalized;
      RaycastHit[] hits = Physics.RaycastAll(origin, direction, 100f);
      for (int index = 0; index < hits.Length; index++)
      {
        if (hits[index].collider.tag.CompareTo("Target") != 0)
          continue;
        targets.Add(hits[index].collider);
      }
    }
    else
    {
      Collider[] objects = Physics.OverlapSphere(Tip.transform.position, tipRadius);
      for (int index = 0; index < objects.Length; index++)
      {
        if (objects[index].tag.CompareTo("Target") != 0)
          continue;
        targets.Add(objects[index]);
      }
    }
    return targets.ToArray();
  }

  private float tipRadius = 0.0f;

  void CalculateLocalTipParameters()
  {
    float tipLocalRadius = (Tip.collider as SphereCollider).radius;
    tipRadius = (Tip.collider as SphereCollider).transform.TransformPoint(new Vector3(tipLocalRadius, 0f)).x;
  }

  public void SetDefaultWandLength()
  {
    if (WandType == WandFunctionType.Ray)
      SetWandLength(100f, true);
    else
      SetDefaultWandLength(InitialPointing);
  }

  float factorToCenter = 1.0f;
  float factorMinMaxSegment = 0.5f;

  private void SetDefaultWandLength(GameObject target)
  {
    if (target == null)
      return;

    Vector3 centerPoint = target.transform.position;
    Vector3 wandPosition = this.transform.position;
    //Vector3 tipPosition = Tip.transform.position;

    float baseLength = 0.2f;
    float centerLenth = (centerPoint - wandPosition).magnitude;
    factorToCenter = centerLenth / baseLength;
    SetWandLength(factorToCenter);

    //Review this part of the code later, that target.transfor.Find() shouldn't really be there
    Transform closerPointT = target.transform.Find("CloserPoint");
    Transform furtherPointT = target.transform.Find("FurtherPoint");
    if (closerPointT != null && furtherPointT != null)
    {
      //Vector3 closerPoint = closerPointT.position;
      //Vector3 furtherPoint = furtherPointT.position;
    }
  }
  
  void SetWandLength(float finalScale)
  {
	SetWandLength(finalScale, false);
  }

  void SetWandLength(float finalScale, bool ignoreBounds)
  {
    float boundedFinalScale = finalScale;
    if (!ignoreBounds)
    {
      boundedFinalScale = System.Math.Max(finalScale, factorToCenter - factorMinMaxSegment);
      boundedFinalScale = System.Math.Min(boundedFinalScale, factorToCenter + factorMinMaxSegment);
    }

    Shaft.transform.localScale = new Vector3(Shaft.transform.localScale.x, boundedFinalScale, Shaft.transform.localScale.z);

    Vector3 wandPosition = new Vector3(0, 0, boundedFinalScale);
    Shaft.transform.localPosition = wandPosition;

    Tip.transform.localPosition = wandPosition * 2.0f;
  }

  void SetExtensionMethodSettings()
  {
    switch (WandType)
    {
      case WandFunctionType.TouchDrag:
        //Dragging the finger for the whole extension of the screen increases the scale in one point
        WandGain = 1f / Screen.height;
        break;
      case WandFunctionType.Twist:
        //Turning the device 45 degrees changes the scale in one point
        WandGain = 1f / 45;
        break;
      case WandFunctionType.TouchPush:
        //Pushing the device 20 degress changes the scale in one point
        WandGain = 1f / 20;
        break;
      default:
        break;
    }

    SetDefaultWandLength();
  }

  void OnGUI()
  {
    if (!ShowGUI || !this.enabled)
      return;

    System.Array extensionMethods = System.Enum.GetValues(typeof(WandFunctionType));
    float heightEM = 25 + 25 * extensionMethods.Length + 25;
    GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, heightEM));
    GUILayout.Label("Wand Type:", GUILayout.Width(Screen.width), GUILayout.Height(20));
    foreach (WandFunctionType method in extensionMethods)
    {
      bool isCurrent = WandType == method;
      if (GUILayout.Toggle(isCurrent,
                           method.ToString(),
                           GUILayout.Width(100), GUILayout.Height(25)) && !isCurrent)  //-- this last piece is VERY important
      {
        WandType = method;
        SetExtensionMethodSettings();
      }
    }
    GUILayout.EndArea();
  }
}
