using UnityEngine;
using System.Collections;
using System;

namespace UnityMoverioBT200.Scripts
{

  public class NetworkController : MonoBehaviour
  {

    public string IPofAndroidServer = string.Empty;
    private bool connected;

    private static NetworkController instance;
    public static NetworkController Instance
    {
      get
      {
        if (instance == null)
          instance = new NetworkController();
        return instance;
      }
    }

    public bool ShowGUI;

    public NetworkController()
    {
      instance = this;
      instance.ShowGUI = false;
      instance.connected = false;
    }

    ~NetworkController()
    {
      Debug.Log("Destroying the NetworkController");
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
      if (!ShowGUI)
        return;

      GUILayout.Space(240);
      if (!connected)
      {
        if (GUILayout.Button("Android", GUILayout.Width(100), GUILayout.Height(30)))
        {
          Network.InitializeServer(2, 8080, true);
          connected = true;

          //calls the WindowsViconConnector.OnNetworkStarted method
          MessageBroker.BroadcastAll("OnNetworkStarted", true);
        }

        if (GUILayout.Button("Desktop", GUILayout.Width(100), GUILayout.Height(30)))
        {
          Network.Connect(IPofAndroidServer, 8080);
          connected = true;

          //calls the WindowsViconConnector.OnNetworkStarted method
          MessageBroker.BroadcastAll("OnNetworkStarted", false);
        }
      }
      else if (Network.isServer)
      {
        GUILayout.Label(Network.player.ipAddress, GUILayout.Width(100), GUILayout.Height(50));
      }
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
      if (Network.isServer)
        Debug.Log("Local server connection disconnected");
      else
        if (info == NetworkDisconnection.LostConnection)
          Debug.Log("Lost connection to the server");
        else
          Debug.Log("Successfully diconnected from the server");
    }
  }
}