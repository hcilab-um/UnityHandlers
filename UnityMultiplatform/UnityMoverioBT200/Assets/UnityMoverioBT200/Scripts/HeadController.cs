using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Vicon2Unity.ViconConnector.Scripts;

namespace UnityMoverioBT200.Scripts
{

  public class HeadController : MonoBehaviour
  {
    /** Behaviour changes whether it's connected or standalone, and according to the reference frame
     *  -- the change is controlled from the SceneUI script
     * 							ViewCentric		BodyCentric		WorldCentric
     *  Connected			Gyro				RaycastExt		RaycastExt
     *  StandAlone		Gyro				RaycastIMU		RaycastIMU
     */
    public enum BehaviourType { GyroMouse, RaycastIMU, RaycastExternal }
    public BehaviourType Behaviour = BehaviourType.RaycastIMU;

    public GameObject LookAtObject;
    public GUITexture HeadPointer;

    private GameObject NeckJoint;
    private GameObject EPSONcamera;
    private Camera RightCamera;

    private Ray SelectionRay;
    private LineRenderer SelectionRayRenderer;

    public bool ShowGUI;
    public bool IsActive
    {
      get { return gameObject.activeSelf; }
      set
      {
        this.enabled = value;
        HeadPointer.enabled = value;
				SetDefaults();
        HideSelectionRay();
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

    ~HeadController()
    {
      Debug.Log("Destroying the HeadController");
    }

    // Use this for initialization
    void Start()
    {
      NeckJoint = GameObject.Find("NeckJoint");

      EPSONcamera = GameObject.Find("EPSON Moverio BT-200");
      RightCamera = EPSONcamera.transform.FindChild("rightCam").gameObject.GetComponent<Camera>();

			SetDefaults();
      PrepareSelectionRay();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
      //when it's server, the rotation info comes from the optical tracker
      if (Network.isClient || SystemInfo.deviceType == DeviceType.Desktop)
      {
        CheckHovers();
        return;
      }

      switch (Behaviour)
      {
        case BehaviourType.GyroMouse:
          ComputeGyroPointer();
          break;
        case BehaviourType.RaycastIMU:
          NeckJoint.transform.localRotation = RotationProvider.Instance.Rotation;
          break;
        case BehaviourType.RaycastExternal:
          //The neckjoint orientation is set by the optical tracker script (Vicon or OptiTrack)
          break;
      }

      CheckHovers();
      DrawSelectionRay();
    }

    //readings below this number will be considered zero
    public float IMUNoiseFactor = 0.00125f;
    //the following are the multipliers for width and height
    public float MultiplierX = 540f * 2.0f;
    public float MultiplierY = 540f * 2.0f;

    private Quaternion lastRotationInv;

    void ComputeGyroPointer()
    {
      Quaternion rotationDiff = lastRotationInv * RotationProvider.Instance.Rotation;
      lastRotationInv = Quaternion.Inverse(RotationProvider.Instance.Rotation);

      Vector3 rotPosNeg = RotationProvider.RotAsPosNeg(rotationDiff);

      Rect pointerLocation = HeadPointer.pixelInset;
      pointerLocation.x += MultiplierX * CDFunction(rotPosNeg.y);
			pointerLocation.y += MultiplierY * CDFunction(rotPosNeg.x * -1);
      ApplyMinMaxBoundaries (ref pointerLocation);

			HeadPointer.pixelInset = pointerLocation;

      if (Network.isServer)
				networkView.RPC("SynchPointer", RPCMode.Others, pointerLocation.x, pointerLocation.y);
    }

		private void ApplyMinMaxBoundaries(ref Rect pointer)
		{
			Vector2 minValues = new Vector2 (-1 * (Screen.width / 4 + HeadPointer.pixelInset.width / 2),
			                                 -1 * (Screen.height / 2 + HeadPointer.pixelInset.height / 2));
			Vector2 maxValues = new Vector2 (Screen.width / 4 - HeadPointer.pixelInset.width / 2,
			                                 Screen.height / 2 - HeadPointer.pixelInset.height / 2);

			pointer.x = Mathf.Max (minValues.x, Mathf.Min (maxValues.x, pointer.x));
			pointer.y = Mathf.Max (minValues.y, Mathf.Min (maxValues.y, pointer.y));
		}

    [RPC]
    void SynchPointer(float x, float y)
    {
      Rect pointer = HeadPointer.pixelInset;
      pointer.x = x;
      pointer.y = y;
      HeadPointer.pixelInset = pointer;
    }

    float CDFunction(float angleDiff)
    {
      float sign = Mathf.Sign(angleDiff);
      float val = Mathf.Abs(angleDiff);
      float cdCorrectedVal = Mathf.Atan(val * Mathf.Deg2Rad - IMUNoiseFactor);

      return sign * Mathf.Max(0f, cdCorrectedVal);
    }

		public void SetDefaults ()
		{
			SetDefaultRotation ();
			SetDefaultPointerPosition ();
		}

    private void SetDefaultRotation()
    {
      //it orientates the entire head
      GameObject.Find("NeckJoint").transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

			Vector3 tForward = transform.TransformVector (Vector3.forward);
			gameObject.transform.LookAt(transform.position + tForward);
    }

    void OnRotationBaselineSet()
    {
      if (!this.enabled)
        return;

      SetDefaultPointerPosition();
    }

    public void SetDefaultPointerPosition()
    {
      if (Behaviour == BehaviourType.GyroMouse)
        lastRotationInv = Quaternion.Inverse(RotationProvider.Instance.Rotation);

			//Sets it to 0,0 --> the middle of the display
      Vector2 position = new Vector2(0, 0);
      Rect boundedPointerLocation = HeadPointer.pixelInset;
      boundedPointerLocation.x = position.x - boundedPointerLocation.width / 2;
      boundedPointerLocation.y = position.y - boundedPointerLocation.height / 2;

			if(RightCamera != null && LookAtObject != null)
			{
				if(Behaviour == BehaviourType.RaycastIMU)
					NeckJoint.transform.localRotation = RotationProvider.Instance.Rotation;

				//Sets it to the position which is the middle of the two displays according to the target distance
				Ray headDirection = new Ray (EPSONcamera.transform.position, Vector3.left);
				float distanceToTargets = (LookAtObject.transform.position - transform.position).magnitude;
				Vector3 aimingPoint = headDirection.GetPoint (distanceToTargets);
				Vector3 screenPoint = RightCamera.WorldToScreenPoint (aimingPoint);
        Vector3 middlePoint = new Vector3(Screen.width * 3 / 4, Screen.height / 2);
        Vector3 diff = screenPoint - middlePoint;
        boundedPointerLocation.x += diff.x;
        boundedPointerLocation.y += diff.y;

				ApplyMinMaxBoundaries (ref boundedPointerLocation);
			}

      HeadPointer.pixelInset = boundedPointerLocation;
    }

    void OnReferenceFrameUpdated(ReferenceFrame refFrame)
    {
      SetDefaultPointerPosition();

      /** Behaviour changes whether it's connected or standalone, and according to the reference frame
       *  -- the change is controlled from the SceneUI script
       * 							ViewCentric		BodyCentric		WorldCentric
       *  Connected			Gyro				RaycastExt		RaycastExt
       *  StandAlone		Gyro				RaycastIMU		RaycastIMU
       */
      if (refFrame == ReferenceFrame.View)
        Behaviour = BehaviourType.GyroMouse;
      else if (Network.isServer)
        Behaviour = BehaviourType.RaycastExternal;
      else
        Behaviour = BehaviourType.RaycastIMU;
    }

    void OnTouchStarted(MoverioInputEventArgs args)
    {
      if (!this.enabled)
        return;

      CheckSelections(args);
    }

    private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

    void CheckHovers()
    {
      Collider target = GetFirstAffectedTarget();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(null);
      args.Device = ControllerType.Head;
      args.Conflict = false;
      if (Behaviour == BehaviourType.GyroMouse)
        args.Pointer = GetScreenPoint();
      else
        args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

      if (target != null)
      {
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
        args = new SelectionControllerEventArgs(null);
        args.Device = ControllerType.Head;
        args.Conflict = false;
        if (Behaviour == BehaviourType.GyroMouse)
          args.Pointer = GetScreenPoint();
        else
          args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

        targetObj.SendMessage("NotHovered", args, SendMessageOptions.DontRequireReceiver);
        hoveredTargets.Remove(targetObj);
      }

      List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
      foreach (GameObject targetObj in keys)
        hoveredTargets[targetObj] = false;
    }

    void CheckSelections(MoverioInputEventArgs mieArgs)
    {
      Collider target = GetFirstAffectedTarget();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(mieArgs);
      args.Device = ControllerType.Head;
      args.Conflict = false;
      if (Behaviour == BehaviourType.GyroMouse)
        args.Pointer = GetScreenPoint();
      else
        args.Pointer = RotationProvider.Instance.Rotation.eulerAngles;

      if (target != null)
        target.SendMessage("Selected", args, SendMessageOptions.DontRequireReceiver);
      else
      {
        SelectionEventArgs seArgs = new SelectionEventArgs(args);
        seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
        seArgs.Conflict = args.Conflict;

        MessageBroker.BroadcastAll("OnSelected", seArgs);
      }
    }

    Collider GetFirstAffectedTarget()
    {
      Vector3 origin = GetScreenPoint();
      SelectionRay = RightCamera.ScreenPointToRay(origin);

      RaycastHit hit;
      Physics.Raycast(SelectionRay, out hit);
      return hit.collider;
    }

    private Vector3 GetScreenPoint()
    {
      Vector3 origin = new Vector3(Screen.width / 2 + Screen.width / 4, Screen.height / 2);
      origin.x = origin.x + HeadPointer.pixelInset.center.x;
      origin.y = origin.y + HeadPointer.pixelInset.center.y;
      return origin;
    }

    void PrepareSelectionRay()
    {
      if (SystemInfo.deviceType == DeviceType.Handheld)
        return;

      SelectionRayRenderer = gameObject.AddComponent<LineRenderer>();
      SelectionRayRenderer.material = new Material(Shader.Find("Particles/Additive"));
      SelectionRayRenderer.SetColors(Color.red, Color.blue);
      SelectionRayRenderer.SetWidth(0.005f, 0.005f);
      SelectionRayRenderer.SetVertexCount(2);

      HideSelectionRay();
    }

    private void HideSelectionRay()
    {
      if (SelectionRayRenderer == null)
        return;

      SelectionRayRenderer.SetPosition(0, Vector3.zero);
      SelectionRayRenderer.SetPosition(1, Vector3.zero);
    }

    void DrawSelectionRay()
    {
      if (SystemInfo.deviceType == DeviceType.Handheld)
        return;

      var origin = SelectionRay.origin;
      var direction = SelectionRay.direction;
      var endPoint = origin + direction * 100f;
      RaycastHit hit;

      SelectionRayRenderer.SetPosition(0, origin);

      if (Physics.Raycast(origin, direction, out hit))
        endPoint = hit.point;

      SelectionRayRenderer.SetPosition(1, endPoint);
    }

  }

}