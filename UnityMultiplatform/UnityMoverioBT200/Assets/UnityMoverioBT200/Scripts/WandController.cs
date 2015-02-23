using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class WandController : MonoBehaviour
  {
    public enum WandFunctionType { Ray, TouchDrag, Twist, TouchPush }

    public enum TwistDirectionType { ClockWise, CounterClockWise }

    /** These are the public properties set on the Unity editor **/
    public GameObject InitialPointing;
    public float WandGain = 0.05f;
    public WandFunctionType WandType;
    public WandFunctionType RayDissambiguationMode;
    public TwistDirectionType TwistDirection;

    public int SelectionFeedbackMillis = 150; //milliseconds
    public Color BaseColor = Color.white;
    public Color SelectionColor = Color.blue;
    /** End **/

    public bool ShowGUI;
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

    ~WandController()
    {
      Debug.Log("Destroying the WandController");
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
    void FixedUpdate()
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
        networkView.RPC("SynchState", RPCMode.Others, new Vector3(shaftColor.r, shaftColor.g, shaftColor.b));
    }

    [RPC]
    void SynchState(Vector3 shaftColor)
    {
      Shaft.renderer.material.SetColor("_Color", new Color(shaftColor.x, shaftColor.y, shaftColor.z));
    }

    void ProcessOrientationUpdate()
    {
      if (Network.isClient) //the desktop simply gets its values from the Android server
        return;

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

    void OnTouchStarted(MoverioInputEventArgs args)
    {
      isFingerDown = true;
      switch (WandType)
      {
        case WandFunctionType.Ray:
          CheckSelectionWithDesambiguation(args);
          break;
        case WandFunctionType.Twist:
          CheckSelections(args);
          break;
        case WandFunctionType.TouchPush:
          pushRotation = RotationProvider.Instance.Rotation;
          invPushRotation = Quaternion.Inverse(pushRotation);
          break;
        default:
          break;
      }
    }

    void OnTouchMoved(MoverioInputEventArgs args)
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

    void OnTouchEnded(MoverioInputEventArgs args)
    {
      isFingerDown = false;
      switch (WandType)
      {
        case WandFunctionType.TouchDrag:
        case WandFunctionType.TouchPush:
          CheckSelections(args);
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
        SelectionControllerEventArgs args = new SelectionControllerEventArgs(null);
        args.Device = ControllerType.Wand;
        args.Conflict = targets.Length > 1;
        args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

        if (hoveredTargets.ContainsKey(target.gameObject))
          hoveredTargets[target.gameObject] = true;
        else
          hoveredTargets.Add(target.gameObject, true);
        target.SendMessage("Hovered", args, SendMessageOptions.DontRequireReceiver);
      }

      List<GameObject> notHovered = new List<GameObject>();
      foreach (GameObject targetObj in hoveredTargets.Keys)
      {
        if (!hoveredTargets[targetObj])
          notHovered.Add(targetObj);
      }

      foreach (GameObject targetObj in notHovered)
      {
        SelectionControllerEventArgs args = new SelectionControllerEventArgs(null);
        args.Device = ControllerType.Wand;
        args.Conflict = false;
        args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

        targetObj.SendMessage("NotHovered", args, SendMessageOptions.DontRequireReceiver);
        hoveredTargets.Remove(targetObj);
      }

      List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
      foreach (GameObject targetObj in keys)
        hoveredTargets[targetObj] = false;
    }

    private System.DateTime selectionTime = System.DateTime.MinValue;

    void CheckSelections(MoverioInputEventArgs mieArgs)
    {
      Collider[] targets = GetAffectedTargets();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(mieArgs);
      args.Device = ControllerType.Wand;
      args.Conflict = targets.Length > 1;
      args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

      foreach (Collider target in targets)
        target.SendMessage("Selected", args, SendMessageOptions.DontRequireReceiver);

      if (targets.Length == 0 || args.Conflict)
      {
        SelectionEventArgs seArgs = new SelectionEventArgs(args);
        seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
        seArgs.Conflict = args.Conflict;

        MessageBroker.BroadcastAll("OnSelected", seArgs);
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

    void CheckSelectionWithDesambiguation(MoverioInputEventArgs mieArgs)
    {
      Collider[] targets = GetAffectedTargets();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(mieArgs);
      args.Device = ControllerType.Wand;
      args.Conflict = false;
      args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

      if (targets.Length == 0)
      {
        SelectionEventArgs seArgs = new SelectionEventArgs(args);
        seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
        seArgs.Conflict = false;

        MessageBroker.BroadcastAll("OnSelected", seArgs);
        selectionTime = System.DateTime.Now;
      }
      else if (targets.Length == 1)
      {
        targets[0].SendMessage("Selected", args, SendMessageOptions.DontRequireReceiver);
        selectionTime = System.DateTime.Now;
      }
      else
      {
        //disambiguate here -- for exp 1
        SelectionControllerEventArgs argsC = new SelectionControllerEventArgs(mieArgs);
        argsC.Device = ControllerType.Wand;
        argsC.Conflict = targets.Length > 1;
        argsC.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

        foreach (Collider target in targets)
          target.SendMessage("Selected", args, SendMessageOptions.DontRequireReceiver);
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
      tipRadius = Mathf.Abs(tipRadius) / 10;
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
      if (Shaft == null || Tip == null)
        return;

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
      if (!ShowGUI)
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

          if (Network.isClient || Network.isServer)
            networkView.RPC("SynchExtensionMethodSettings", RPCMode.OthersBuffered, WandType.ToString());
        }
      }
      GUILayout.EndArea();
    }

    [RPC]
    void SynchExtensionMethodSettings(string method)
    {
      WandType = (WandFunctionType)System.Enum.Parse(typeof(WandFunctionType), method);
      SetExtensionMethodSettings();
    }

    void OnRotationBaselineSet()
    {
      if (!this.enabled)
        return;

      SetDefaultRotation();
    }

    void OnReferenceFrameUpdated(ReferenceFrame refFrame)
    {
      SetDefaultRotation();
      SetDefaultWandLength();
    }
  }

}