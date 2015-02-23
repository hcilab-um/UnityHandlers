using UnityEngine;
using System.Collections;

namespace UnityMoverioBT200.Scripts
{

  public class PhoneIconsSceneUI : MonoBehaviour
  {

    private static PhoneIconsSceneUI instance;
    public static PhoneIconsSceneUI Instance
    {
      get
      {
        if (instance == null)
          instance = new PhoneIconsSceneUI();
        return instance;
      }
    }

    public bool ShowGUI;

    public PhoneIconsSceneUI()
    {
      instance = this;
      instance.ShowGUI = false;
    }

    // Use this for initialization
    void Start()
    {
      HeadController.Instance.IsActive = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnGUI()
    {
      if (!ShowGUI && GUILayout.Button("Show Controls", GUILayout.Width(100), GUILayout.Height(30)))
      {
        ShowGUI = true;
      }
      else if (ShowGUI && GUILayout.Button("Hide Controls", GUILayout.Width(100), GUILayout.Height(30)))
      {
        ShowGUI = false;
      }

      ControllerSettings.Instance.ShowGUI = ShowGUI;
      NetworkController.Instance.ShowGUI = ShowGUI;
      RotationProvider.Instance.ShowGUI = ShowGUI;
      WandLocationAlternatives.Instance.ShowGUI = ShowGUI;

      WandController.Instance.ShowGUI = ShowGUI;
      TouchMouseController.Instance.ShowGUI = ShowGUI;
      GyroMouseController.Instance.ShowGUI = ShowGUI;
      HandGestureController.Instance.ShowGUI = ShowGUI;
      HeadController.Instance.ShowGUI = ShowGUI;
    }
  }

}