using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TouchMouseController : MonoBehaviour
{

  public bool ShowGUI { get; set; }
  public bool IsActive
  {
    get { return this.enabled; }
    set { this.enabled = value; }
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

  public GameObject UIController;

  private Camera LeftCamera;
  private Camera RightCamera;
  private Ray SelectionRay;
  private LineRenderer SelectionRayRenderer;

  public TouchMouseController()
  {
    instance = this;
    instance.ShowGUI = false;
  }

  void Start()
  {
    LeftCamera = GameObject.Find("leftCam").GetComponent<Camera>();
    RightCamera = GameObject.Find("rightCam").GetComponent<Camera>();
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

  void Update()
  {
    CheckHovers();
    //DrawSelectionRay();
  }

  private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

  void CheckHovers()
  {
    Collider target = GetFirstAffectedTarget();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.TouchPad;
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
      args.Device = ControllerType.TouchPad;
      args.Conflict = false;

      targetObj.SendMessage("NotHovered", args, SendMessageOptions.DontRequireReceiver);
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

  void CheckSelections()
  {
    Collider target = GetFirstAffectedTarget();

    SelectionControllerEventArgs args = new SelectionControllerEventArgs();
    args.Device = ControllerType.TouchPad;
    args.Conflict = false;

    if (target != null)
      target.SendMessage("Selected", args);
    else
    {
      SelectionEventArgs seArgs = new SelectionEventArgs();
      seArgs.Device = ControllerType.TouchPad;
      seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
      seArgs.Conflit = args.Conflict;

      UIController.SendMessage("Selected", seArgs);
    }
  }

  Collider GetFirstAffectedTarget()
  {
    Camera cam = LeftCamera;
    Vector2 position = Input.mousePosition;
    if (position.x > (Screen.width / 2))
      cam = RightCamera;

    SelectionRay = cam.ScreenPointToRay(position);
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

  void OnGUI()
  {
    if (!ShowGUI || !this.enabled)
      return;

    GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, 30));
    if (GUILayout.Button("TouchMouse", GUILayout.Width(100), GUILayout.Height(30)))
    {
    }
    GUILayout.EndArea();
  }
}
