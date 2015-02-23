using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class TouchMouseController : MonoBehaviour
  {

    public bool ShowGUI;
    public bool IsActive
    {
      get { return this.enabled; }
      set
      {
        this.enabled = value;
        HideSelectionRay();
      }
    }

    private static TouchMouseController instance;
    public static TouchMouseController Instance
    {
      get
      {
        if (instance == null)
          instance = new TouchMouseController();
        return instance;
      }
    }

    private Camera LeftCamera;
    private Camera RightCamera;
    private Ray SelectionRay;
    private LineRenderer SelectionRayRenderer;

    private Vector3 mousePosition;

    public TouchMouseController()
    {
      instance = this;
      instance.ShowGUI = false;
    }

    ~TouchMouseController()
    {
      Debug.Log("Destroying the TouchMouseController");
    }

    void Start()
    {
      LeftCamera = GameObject.Find("leftCam").GetComponent<Camera>();
      RightCamera = GameObject.Find("rightCam").GetComponent<Camera>();
      PrepareSelectionRay();
    }

    void Update()
    {
      if (SystemInfo.deviceType == DeviceType.Handheld || !Network.isClient)
      {
        mousePosition = Input.mousePosition;
        if (Network.isServer)
          networkView.RPC("SynchMousePosition", RPCMode.Others, mousePosition);
      }

      CheckHovers();
      DrawSelectionRay();
    }

    [RPC]
    private void SynchMousePosition(Vector3 position)
    {
      mousePosition = position;
    }

    private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

    void CheckHovers()
    {
      Collider target = GetFirstAffectedTarget();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(null);
      args.Device = ControllerType.TouchPad;
      args.Conflict = false;
      args.Pointer = mousePosition;

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
        args.Device = ControllerType.TouchPad;
        args.Conflict = false;
        args.Pointer = mousePosition;

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

    void CheckSelections(MoverioInputEventArgs mieArgs)
    {
      Collider target = GetFirstAffectedTarget();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(mieArgs);
      args.Device = ControllerType.TouchPad;
      args.Conflict = false;
      args.Pointer = mousePosition;

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
      Camera cam = LeftCamera;
      Vector3 position = mousePosition;
      if (position.x > (Screen.width / 2))
        cam = RightCamera;

      SelectionRay = cam.ScreenPointToRay(position);
      RaycastHit hit;
      Physics.Raycast(SelectionRay, out hit);
      return hit.collider;
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

    void OnGUI()
    {
      if (!ShowGUI || Network.isClient)
        return;

      GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, 30));
      if (GUILayout.Button("TouchMouse", GUILayout.Width(100), GUILayout.Height(30)))
      {
      }
      GUILayout.EndArea();
    }
  }

}