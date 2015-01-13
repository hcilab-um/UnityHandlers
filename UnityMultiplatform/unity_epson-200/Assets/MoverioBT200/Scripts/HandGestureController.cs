using UnityEngine;
using System.Collections;

public class HandGestureController : MonoBehaviour
{

  public bool ShowGUI { get; set; }
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

  public HandGestureController()
  {
    instance = this;
    instance.ShowGUI = false;
  }

  // Use this for initialization
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }

  void OnGUI()
  {
    if (!ShowGUI || !this.enabled)
      return;

    GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, 30));
    if (GUILayout.Button("HandGesture", GUILayout.Width(100), GUILayout.Height(30)))
    {
    }
    GUILayout.EndArea();
  }

}
