using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class GyroMouseController : MonoBehaviour
  {

    public bool ShowGUI;
    public bool IsActive
    {
      get { return gameObject.activeSelf; }
      set
      {
        this.enabled = value;
        gameObject.SetActive(value);
        HideSelectionRay();
      }
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

    ~GyroMouseController()
    {
      Debug.Log("Destroying the GyroMouseController");
    }

    private Quaternion lastRotationInv;
    private GUITexture GyroPointer;

    private GameObject EPSONcamera;
    private Camera RightCamera;
    private Ray SelectionRay;
    private LineRenderer SelectionRayRenderer;

    // Use this for initialization
    void Start()
    {
      GyroPointer = gameObject.GetComponent<GUITexture>();

      EPSONcamera = GameObject.Find("EPSON Moverio BT-200");
      RightCamera = EPSONcamera.transform.FindChild("rightCam").gameObject.GetComponent<Camera>();

      PrepareSelectionRay();
    }

		//readings below this number will be considered zero
		public float IMUNoiseFactor = 0.00125f;
		//the following are the multipliers for width and height
		public float MultiplierX = 540f * 2.0f;
		public float MultiplierY = 540f * 2.0f;

    // Update is called once per frame
    void FixedUpdate()
    {
      if (SystemInfo.deviceType == DeviceType.Handheld)
      {
        Quaternion rotationDiff = lastRotationInv * RotationProvider.Instance.Rotation;
        lastRotationInv = Quaternion.Inverse(RotationProvider.Instance.Rotation);

        Vector3 rotPosNeg = RotationProvider.RotAsPosNeg(rotationDiff);

				Rect pointerLocation = GyroPointer.pixelInset;
				pointerLocation.x += MultiplierX * CDFunction(rotPosNeg.y);
				pointerLocation.y += MultiplierY * CDFunction(rotPosNeg.x * -1);

        //limits
        Vector2 minValues = new Vector2(-1 * (Screen.width / 4 + GyroPointer.pixelInset.width / 2),
                                        -1 * (Screen.height / 2 + GyroPointer.pixelInset.height / 2));
        Vector2 maxValues = new Vector2(Screen.width / 4 - GyroPointer.pixelInset.width / 2,
                                        Screen.height / 2 - GyroPointer.pixelInset.height / 2);

        Rect boundedPointerLocation = pointerLocation;
        boundedPointerLocation.x = Mathf.Max(minValues.x, Mathf.Min(maxValues.x, pointerLocation.x));
        boundedPointerLocation.y = Mathf.Max(minValues.y, Mathf.Min(maxValues.y, pointerLocation.y));

        GyroPointer.pixelInset = boundedPointerLocation;

        if (Network.isServer)
          networkView.RPC("SynchPointer", RPCMode.Others, boundedPointerLocation.x, boundedPointerLocation.y);
      }

      CheckHovers();
      DrawSelectionRay();
    }

    [RPC]
    void SynchPointer(float x, float y)
    {
      Rect pointer = GyroPointer.pixelInset;
      pointer.x = x;
      pointer.y = y;
      GyroPointer.pixelInset = pointer;
    }

    float CDFunction(float angleDiff)
    {
      float sign = Mathf.Sign(angleDiff);
      float val = Mathf.Abs(angleDiff);
			float cdCorrectedVal = Mathf.Atan(val * Mathf.Deg2Rad - IMUNoiseFactor);

      return sign * Mathf.Max(0f, cdCorrectedVal);
    }

    void OnGUI()
    {
      if (!ShowGUI || Network.isClient)
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

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(null);
      args.Device = ControllerType.GyroMouse;
      args.Conflict = false;
      args.Pointer = GetScreenPoint();

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
        args.Device = ControllerType.GyroMouse;
        args.Conflict = false;
        args.Pointer = GetScreenPoint();

        targetObj.SendMessage("NotHovered", args, SendMessageOptions.DontRequireReceiver);
        hoveredTargets.Remove(targetObj);
      }

      List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
      foreach (GameObject targetObj in keys)
        hoveredTargets[targetObj] = false;
    }

    void OnTouchStarted(MoverioInputEventArgs args)
    {
      if (!this.enabled)
        return;

      CheckSelections(args);
    }

    void OnRotationBaselineSet()
    {
      if (!this.enabled)
        return;

      SetDefaultPointerPosition(); 
    }

    public void SetDefaultPointerPosition()
    {
      lastRotationInv = Quaternion.Inverse(RotationProvider.Instance.Rotation);

      Rect boundedPointerLocation = GyroPointer.pixelInset;
      boundedPointerLocation.x = -1 * boundedPointerLocation.width / 2;
      boundedPointerLocation.y = -1 * boundedPointerLocation.height / 2;
      GyroPointer.pixelInset = boundedPointerLocation;
    }

    void CheckSelections(MoverioInputEventArgs mieArgs)
    {
      Collider target = GetFirstAffectedTarget();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(mieArgs);
      args.Device = ControllerType.GyroMouse;
      args.Conflict = false;
      args.Pointer = GetScreenPoint();

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
      origin.x = origin.x + GyroPointer.pixelInset.center.x;
      origin.y = origin.y + GyroPointer.pixelInset.center.y;
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