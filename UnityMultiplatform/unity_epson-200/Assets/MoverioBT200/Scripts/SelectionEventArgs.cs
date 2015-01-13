using UnityEngine;
using System.Collections;
using System;

public class SelectionEventArgs : EventArgs
{
  public enum SelectionEventType { Hovered, Unhovered, Selected };

  public SelectionEventType Type;
  public System.DateTime Time;
  public GameObject Target;
  public ControllerType Device;
  public bool Conflit;

  public SelectionEventArgs()
  {
    Time = System.DateTime.Now;
  }
}
