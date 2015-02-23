using UnityEngine;
using System.Collections;

namespace UnityMoverioBT200.Scripts
{
  public class Target : MonoBehaviour
  {

    public enum TargetStateType
    {
      Normal,
      Hovered,
      Conflicted,
      Selected
    }

    public Color BaseColor;
    public Color HoveredColor;
    public Color ConflictedColor; //more than one object in the scene is highlighted
    public Color SelectedColor;
    public Color HighlightedColor;
    public TargetStateType State = TargetStateType.Normal;
    public int SelectionFeedbackInterval = 100; //milliseconds

    public bool Highlighted = false;

    // Update is called once per frame
    void Update()
    {
      double milliselapsed = double.MaxValue;
      if (selectionEvent != null)
        milliselapsed = (double)selectionEvent.TimeEllapsed().TotalMilliseconds;

      TargetStateType drawState = milliselapsed < SelectionFeedbackInterval ? TargetStateType.Selected : State;

      if (Network.isClient)
        drawState = State;

      if (Highlighted)
      {
        renderer.material.SetColor("_Color", HighlightedColor);

        switch (drawState)
        {
          case TargetStateType.Selected:
            renderer.material.SetColor("_Color", SelectedColor);
            break;
          case TargetStateType.Hovered:
            renderer.material.SetColor("_Color", HoveredColor + HighlightedColor);
            break;
          case TargetStateType.Conflicted:
            renderer.material.SetColor("_Color", ConflictedColor + HighlightedColor);
            break;
          default:
            break;
        }
      }
      else
      {
        switch (drawState)
        {
          case TargetStateType.Selected:
            renderer.material.SetColor("_Color", SelectedColor);
            break;
          case TargetStateType.Hovered:
            renderer.material.SetColor("_Color", HoveredColor);
            break;
          case TargetStateType.Conflicted:
            renderer.material.SetColor("_Color", ConflictedColor);
            break;
          case TargetStateType.Normal:
          default:
            renderer.material.SetColor("_Color", BaseColor);
            break;
        }
      }

      if (Network.isServer)
        networkView.RPC("SynchState", RPCMode.Others, drawState.ToString(), Highlighted);
    }

    [RPC]
    void SynchState(string state, bool hl)
    {
      State = (TargetStateType)System.Enum.Parse(typeof(TargetStateType), state);
      Highlighted = hl;
    }

    void Hovered(SelectionControllerEventArgs args)
    {
      TargetStateType prevState = State;
      if (args.Conflict)
        State = TargetStateType.Conflicted;
      else
        State = TargetStateType.Hovered;

      if (prevState != State)
        NotifyEventListeners(SelectionEventArgs.SelectionEventType.Hovered, args);
    }

    void NotHovered(SelectionControllerEventArgs args)
    {
      State = TargetStateType.Normal;
      NotifyEventListeners(SelectionEventArgs.SelectionEventType.Unhovered, args);
    }

    private SelectionControllerEventArgs selectionEvent = null;

    void Selected(SelectionControllerEventArgs args)
    {
      selectionEvent = args;
      NotifyEventListeners(SelectionEventArgs.SelectionEventType.Selected, args);
    }

    void NotifyEventListeners(SelectionEventArgs.SelectionEventType eventType, SelectionControllerEventArgs controllerArgs)
    {
      SelectionEventArgs args = new SelectionEventArgs(controllerArgs);
      args.Type = eventType;
      args.Target = gameObject;
      args.Conflict = false;

      MessageBroker.BroadcastAll("On" + args.Type.ToString(), args);
    }

  }

}