using UnityEngine;
using System.Collections;

namespace UnityMoverioBT200.Scripts
{
  public class WandLocationAlternatives : MonoBehaviour
  {

    public int StartLocationIndex;
    private int currentLocationIndex;

    public GameObject Wand;

    private static WandLocationAlternatives instance;
    public static WandLocationAlternatives Instance
    {
      get
      {
        if (instance == null)
          instance = new WandLocationAlternatives();
        return instance;
      }
    }

    public bool ShowGUI;

    public GameObject[] locations;

    public WandLocationAlternatives()
    {
      instance = this;
      instance.ShowGUI = false;
    }

    ~WandLocationAlternatives()
    {
      Debug.Log("Destroying the WandLocationAlternatives");
    }

    void Start()
    {
      if (locations.Length > 0)
      {
        if (StartLocationIndex >= 0 && StartLocationIndex < locations.Length)
        {
          //Sets the initial location
          currentLocationIndex = StartLocationIndex;
          UpdateWandParameters(locations[currentLocationIndex].transform, true);
        }
      }
      else
      {
        //do nothing, leave the wand wherever it was placed in the editor
      }
    }

    // Update is called once per frame
    void OnGUI()
    {
      if (!ShowGUI || ControllerSettings.Instance.Controller != ControllerType.Wand)
        return;

      GUILayout.BeginArea(new Rect(Screen.width - 100, 140, 100, 25 + 25 * locations.Length + 10));
      GUILayout.Label("Wand Location", GUILayout.Width(100), GUILayout.Height(25));
      for (int index = 0; index < locations.Length; index++)
      {
        Transform location = locations[index].transform;
        bool isCurrent = Wand.transform.position == location.transform.position;
        if (GUILayout.Toggle(isCurrent,
                             location.name,
                             GUILayout.Width(100), GUILayout.Height(25)) && !isCurrent)  //-- this last piece is VERY important
        {
          currentLocationIndex = index;
          UpdateWandParameters(locations[currentLocationIndex].transform, true);

          if (Network.isClient || Network.isServer)
            networkView.RPC("SynchWandLocation", RPCMode.OthersBuffered, currentLocationIndex);
        }
      }
      GUILayout.EndArea();
    }

    void Update()
    {
      if (Network.isClient)
        return;

      UpdateWandParameters(locations[currentLocationIndex].transform);
    }

    void UpdateWandParameters(Transform newLocation, bool setDefaults = false)
    {
      if (Wand != null)
        Wand.transform.position = newLocation.position;

      if (setDefaults)
      {
        RotationProvider.Instance.SetBaselineRotation();
        WandController.Instance.SetDefaultRotation();
        WandController.Instance.SetDefaultWandLength();
      }
    }

    [RPC]
    void SynchWandLocation(int location)
    {
      currentLocationIndex = location;
      UpdateWandParameters(locations[currentLocationIndex].transform, true);
    }

  }

}