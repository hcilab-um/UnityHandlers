using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UnityMoverioBT200.Scripts
{

  public class OffScreenMarkers : MonoBehaviour
  {

    List<Target> targets;

    // Use this for initialization
    void Start()
    {
      //1- gets all the scripts from the scene
      targets = new List<Target>();
      targets.AddRange((Target[])FindObjectsOfType<Target>());
    }

    // Update is called once per frame
    void Update()
    {
      foreach (Target target in targets)
      {
        if (target.renderer.isVisible)
          continue;
      }
    }
  }

}