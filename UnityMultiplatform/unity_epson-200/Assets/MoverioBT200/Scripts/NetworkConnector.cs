using UnityEngine;
using System.Collections;
using System;

public class NetworkConnector : MonoBehaviour
{

  public string IPofDesktop = string.Empty;

  private bool connected;

  private static NetworkConnector instance;
  public static NetworkConnector Instance
  {
    get
    {
      if (instance == null)
        instance = new NetworkConnector();
      return instance;
    }
  }

  public bool ShowGUI;

  public NetworkConnector()
  {
    instance = this;
    instance.ShowGUI = false;
  }

  void OnGUI()
  {
    if (!ShowGUI)
      return;

    GUILayout.Space(240);
    if (!connected)
    {
      if (GUILayout.Button("Desktop", GUILayout.Width(100), GUILayout.Height(30)))
      {
        connected = true;
        Network.InitializeServer(2, 8080, true);
        gameObject.SendMessage("Initialize", SendMessageOptions.DontRequireReceiver); //calls the WindowsViconConnector.Initialize method
      }

      if (GUILayout.Button("Android", GUILayout.Width(100), GUILayout.Height(30)))
      {
        connected = true;
        Network.Connect(IPofDesktop, 8080);
      }
    }
    else if (Network.isServer)
    {
      GUILayout.Label(Network.player.ipAddress, GUILayout.Width(100), GUILayout.Height(50));
    }
  }
}
