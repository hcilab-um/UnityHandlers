using UnityEngine;
using System.Collections;
using System;

public class NetworkConnector : MonoBehaviour
{

  public string IP = string.Empty;

  bool connected;

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

  public bool ShowGUI { get; set; }

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
      if (GUILayout.Button("Android", GUILayout.Width(100), GUILayout.Height(30)))
      {
        connected = true;
        Network.InitializeServer(2, 8080, true);
      }

      if (GUILayout.Button("Desktop", GUILayout.Width(100), GUILayout.Height(30)))
      {
        connected = true;
        Network.Connect(IP, 8080);

        try
        {
          //For the wand controller scene
          WandController.Instance.enabled = false;
          TouchMouseController.Instance.enabled = false;
          GyroMouseController.Instance.enabled = false;
          HandGestureController.Instance.enabled = false;
          HeadController.Instance.enabled = false;
        }
        catch (Exception ex) { Debug.LogException(ex); }

        try
        {
          //For the phone icons scene
          HeadController.Instance.enabled = false;
        }
        catch (Exception ex) { Debug.LogException(ex); }
      }
    }
    else if (Network.isServer)
    {
      GUILayout.Label(Network.player.ipAddress, GUILayout.Width(100), GUILayout.Height(50));
    }
  }
}
