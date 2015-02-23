using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class WandSceneUI : MonoBehaviour
  {

    private static WandSceneUI instance;

    public static WandSceneUI Instance
    {
      get
      {
        if (instance == null)
          instance = new WandSceneUI();
        return instance;
      }
    }

    public bool ShowGUI;

    public float[] targetWidths = null;
    public float[] targetDistances = null;

    //The prefab for the target objects
    public GameObject TargetPrefab = null;
    public GameObject TargetSet = null;

    public WandSceneUI()
    {
      instance = this;
      instance.ShowGUI = false;
    }

    ~WandSceneUI()
    {
      Debug.Log("Destroying the WandSceneUI");
    }

    void Start()
    {
      IndexInitialTargets();
      CreateInitialLayout();
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

      if (ShowGUI)
      {
        GUILayout.BeginArea(new Rect(0, 200, 100, 30));
        if (GUILayout.Button("Start", GUILayout.Width(100), GUILayout.Height(30)))
        {
          StartExperiment();
        }
        GUILayout.EndArea();
      }
    }

    void IndexInitialTargets()
    {
      if (TargetSet == null)
        return;

      Random.seed = (int)(System.DateTime.Now.Ticks % 1000);

			targets = new List<Target> ();
			targets.AddRange(TargetSet.GetComponentsInChildren<Target> ());
			targets.Sort(new TargetComparer());
    }

    private int currentW = 0;
    private int currentD = 0;
    private int trialNr = 0;
    private int currentTarget = -1;
    private System.DateTime startTime = System.DateTime.MinValue;
    private List<Target> targets;

    private void StartExperiment()
    {
      trialNr = 0;
      currentW = currentD = 0;
      currentTarget = Random.Range(0, targets.Count);
      startTime = System.DateTime.Now;

      CreateLayout(targetWidths[currentW], targetDistances[currentD]);
      Target targetScript = (Target)targets[currentTarget].GetComponent(typeof(Target));
      targetScript.Highlighted = true;
    }

    private void CreateInitialLayout()
    {
      CreateLayout(targetWidths[0], targetDistances[0]);
    }

    private void CreateLayout(float tWidth, float tDistance)
    {
      float distanceFromZero = tDistance / 2.0f;
      float angle = 360.0f / targets.Count;

      for (int index = 0; index < targets.Count; index++)
      {
        targets[index].transform.localScale = new Vector3(tWidth, tWidth, tWidth);
        float currentAngle = angle * index * Mathf.Deg2Rad;
        Vector3 position = new Vector3(Mathf.Sin(currentAngle) * distanceFromZero, 0.0f, Mathf.Cos(currentAngle) * distanceFromZero);
        targets[index].transform.localPosition = position;
      }
    }

    public void OnHovered(SelectionEventArgs args)
    {
    }

    public void OnUnhovered(SelectionEventArgs args)
    {
    }

    public void OnSelected(SelectionEventArgs args)
    {
      Debug.Log("Selected: " + (args.Target == null ? "void" : args.Target.name) + " Device: " + args.ControllerEvent.Device);

      if (startTime == System.DateTime.MinValue)
        return; //the experiment has not started yet

      if (args.Target == targets[currentTarget].gameObject)
      {
        Target targetScript = (Target)targets[currentTarget].GetComponent(typeof(Target));
        targetScript.Highlighted = false;

        int nextTarget = currentTarget + 8;
        if (trialNr % 2 == 1)
          nextTarget++;
        nextTarget = nextTarget % 16;
        currentTarget = nextTarget;

        trialNr = ++trialNr % 16;
        if (trialNr == 0)
        {
          currentTarget = Random.Range(0, targets.Count);

          currentD++;
          if (currentD == 2)
          {
            currentD = 0;
            currentW++;
            if (currentW == 2)
            {
              //experiment ends
              currentW = 0;
              currentD = 0;
              CreateLayout(targetWidths[currentW], targetDistances[currentD]);
              startTime = System.DateTime.MinValue;
              return;
            }
          }
          CreateLayout(targetWidths[currentW], targetDistances[currentD]);
        }

        targetScript = (Target)targets[currentTarget].GetComponent(typeof(Target));
        targetScript.Highlighted = true;
      }
    }

  }

}