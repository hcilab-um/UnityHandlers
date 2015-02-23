using UnityEngine;
using System.Collections;
using System;

namespace UnityMoverioBT200.Scripts
{

  public enum ControllerType { Wand, TouchPad, GyroMouse, HandGesture, Head }

  public class ControllerSettings : MonoBehaviour
  {

    private static ControllerSettings instance;

    public static ControllerSettings Instance
    {
      get
      {
        if (instance == null)
          instance = new ControllerSettings();
        return instance;
      }
    }

    public bool ShowGUI;

    public ControllerType Controller;

    public ControllerSettings()
    {
      instance = this;
      instance.ShowGUI = false;
    }

    ~ControllerSettings()
    {
      Debug.Log("Destroying the ControllerSettings");
    }

    // Use this for initialization
    void Start()
    {
      MessageBroker.LoadBaseObjects();
      SetCurrentController(Controller);
    }

    void OnGUI()
    {
      if (!ShowGUI || !this.enabled)
        return;

      System.Array controllerTypes = System.Enum.GetValues(typeof(ControllerType));
      int cHeight = 25 + 25 * controllerTypes.Length + 110;
      GUILayout.BeginArea(new Rect(0, 35, 100, cHeight));
      GUILayout.Label("Controller:", GUILayout.Width(Screen.width), GUILayout.Height(20));
      foreach (ControllerType controller in controllerTypes)
      {
        bool isCurrent = Controller == controller;
        if (GUILayout.Toggle(isCurrent,
                             controller.ToString(),
                             GUILayout.Width(100), GUILayout.Height(25)) && !isCurrent)  //-- this last piece is VERY important
        {
          //calls both the remote and the local method
          SetCurrentController(controller);

          if (Network.isClient || Network.isServer)
            networkView.RPC("SynchCurrentController", RPCMode.OthersBuffered, controller.ToString());
        }
      }
      GUILayout.EndArea();
    }

    [RPC]
    void SynchCurrentController(String newControllerS)
    {
      ControllerType newController = (ControllerType)System.Enum.Parse(typeof(ControllerType), newControllerS);
      SetCurrentController(newController);
    }

    void SetCurrentController(ControllerType newController)
    {
      Controller = newController;

      WandController.Instance.IsActive = false;
      TouchMouseController.Instance.IsActive = false;
      GyroMouseController.Instance.IsActive = false;
      HandGestureController.Instance.IsActive = false;
      HeadController.Instance.IsActive = false;

			//For all controllers any touch to the moverio touchpad should be understood as a click
			// Except for the one marked below
			MoverioInputProvider.Instance.TreatMovementAsTouch = true;

      switch (Controller)
      {
        case ControllerType.Wand:
          RotationProvider.Instance.SetSourceIMU(RotationProvider.SensorMode.Controller);
          WandController.Instance.IsActive = true;
          break;
        case ControllerType.GyroMouse:
          RotationProvider.Instance.SetSourceIMU(RotationProvider.SensorMode.Controller);
          GyroMouseController.Instance.IsActive = true;
          break;
        case ControllerType.Head:
          RotationProvider.Instance.SetSourceIMU(RotationProvider.SensorMode.Headset);
          HeadController.Instance.IsActive = true;
          break;
        case ControllerType.HandGesture:
          HandGestureController.Instance.IsActive = true;
          break;
        case ControllerType.TouchPad:
					//we desactivate "move as touch" for this provider in order to avoid movement click noise
					MoverioInputProvider.Instance.TreatMovementAsTouch = false;
          TouchMouseController.Instance.IsActive = true;
          break;
        default:
          break;
      }

      HeadController.Instance.SetDefaults();
      MoverioInputProvider.Instance.LoadScripts();
    }

    // Update is called once per frame
    void Update()
    {
    }
  }

}