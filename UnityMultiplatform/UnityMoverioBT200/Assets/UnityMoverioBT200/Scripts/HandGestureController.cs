using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class HandGestureController : MonoBehaviour
  {

    public bool ShowGUI;

    public bool IsActive
    {
      get { return gameObject.activeSelf; }
      set { this.enabled = value; gameObject.SetActive(value); }
    }

    private static HandGestureController instance;
    public static HandGestureController Instance
    {
      get
      {
        if (instance == null)
          instance = new HandGestureController();
        return instance;
      }
    }

    public GameObject FingerIndex;

    public HandGestureController()
    {
      instance = this;
      instance.ShowGUI = false;
    }

    ~HandGestureController()
    {
      Debug.Log("Destroying the HandGestureController");
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
      if (Network.isClient)
        return;

      //We are moving to an index finger overlap, given that the previous approach suffered from considerable Heisemberg effect. 
      // -- moreover, this approach where selection happens on the TouchPad make is comparable to the other selection methods. 
      gameObject.transform.position = FingerIndex.transform.position;
      CheckHovers();
    }

    void OnTouchStarted(MoverioInputEventArgs args)
    {
      CheckSelections(args);
    }

    void OnGUI()
    {
      if (!ShowGUI || !Network.isClient)
        return;
    }

    private Dictionary<GameObject, bool> hoveredTargets = new Dictionary<GameObject, bool>();

    void CheckHovers()
    {
      Collider[] targets = GetAffectedTargets();
      foreach (Collider target in targets)
      {
        SelectionControllerEventArgs args = new SelectionControllerEventArgs(null);
        args.Device = ControllerType.HandGesture;
        args.Conflict = targets.Length > 1;
        args.Pointer = gameObject.transform.position;

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
        args.Device = ControllerType.HandGesture;
        args.Conflict = false;
        args.Pointer = gameObject.transform.position;

        targetObj.SendMessage("NotHovered", args, SendMessageOptions.DontRequireReceiver);
        hoveredTargets.Remove(targetObj);
      }

      List<GameObject> keys = new List<GameObject>(hoveredTargets.Keys);
      foreach (GameObject targetObj in keys)
        hoveredTargets[targetObj] = false;
    }

    void CheckSelections(MoverioInputEventArgs mieArgs)
    {
      Collider[] targets = GetAffectedTargets();

      SelectionControllerEventArgs args = new SelectionControllerEventArgs(mieArgs);
      args.Device = ControllerType.HandGesture;
      args.Conflict = targets.Length > 1;
      args.Pointer = gameObject.transform.position;

      foreach (Collider target in targets)
        target.SendMessage("Selected", args, SendMessageOptions.DontRequireReceiver);

      if (targets.Length == 0 || args.Conflict)
      {
        SelectionEventArgs seArgs = new SelectionEventArgs(args);
        seArgs.Type = SelectionEventArgs.SelectionEventType.Selected;
        seArgs.Conflict = args.Conflict;

        MessageBroker.BroadcastAll("OnSelected", seArgs);
      }
    }

    Collider[] GetAffectedTargets()
    {
      List<Collider> targets = new List<Collider>();
      Collider[] objects = Physics.OverlapSphere(gameObject.transform.position, GetComponent<SphereCollider>().radius / 100);
      for (int index = 0; index < objects.Length; index++)
      {
        if (objects[index].tag.CompareTo("Target") != 0)
          continue;
        targets.Add(objects[index]);
      }
      return targets.ToArray();
    }

  }

}