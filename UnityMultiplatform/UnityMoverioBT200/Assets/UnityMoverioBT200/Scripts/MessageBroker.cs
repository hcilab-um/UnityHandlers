using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityMoverioBT200.Scripts
{
  class MessageBroker
  {

    //This array contains a reference to all first level GameObjects in the scene
    private static List<GameObject> baseGOs;

    public static void LoadBaseObjects() 
    {
      baseGOs = new List<GameObject>();

      GameObject[] gos = (GameObject[])GameObject.FindObjectsOfType(typeof(GameObject));
      foreach (GameObject go in gos)
      {
        if (go && go.transform.parent == null)
          baseGOs.Add(go);
      }
    }

    public static void BroadcastAll(string methodName, System.Object msg = null)
    {
      if (baseGOs == null)
        return;

      foreach (GameObject go in baseGOs)
          go.BroadcastMessage(methodName, msg, SendMessageOptions.DontRequireReceiver);
    }

  }
}
