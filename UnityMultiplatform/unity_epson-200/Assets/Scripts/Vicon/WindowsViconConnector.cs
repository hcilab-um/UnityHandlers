using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
//using ViconDataStreamSDK.DotNET;
using UnityEngine;
using Ubicomp.Utils.NET.MulticastTransportFramework;
using Vicon2Unity;

  class WindowsViconConnector : MonoBehaviour, ITransportListener
  {

    public GameObject bear;

    //private static Client MyClient;
    private bool isInitialized = false;

    public String IPofViconServer;
    public int PortofViconServer;
    public String TrackingObjectName;
    public String TrackingSegmentName;

    public WindowsViconConnector()
    {
    }

    void Update()
    {
      if(isInitialized && Network.isServer)
      {
        lock (mutex)
        {
          transform.position = position;
          transform.rotation = quat;
        }
      }
    }

    public void Initialize()
    {
      TransportComponent.Instance.MulticastGroupAddress = IPAddress.Parse(groupIP);
      TransportComponent.Instance.Port = port;
      TransportComponent.Instance.UDPTTL = TTL;
      TransportComponent.Instance.TransportListeners.Add(WindowsViconConnector.ProgramID, this);
      TransportMessageExporter.Exporters.Add(WindowsViconConnector.ProgramID, new TestExporter());
      TransportMessageImporter.Importers.Add(WindowsViconConnector.ProgramID, new TestImporter());
      TransportComponent.Instance.Init();

      isInitialized = true;
    }

    public const int ProgramID = 1;

    public int port = 5000;
    public int TTL = 10;
    public string groupIP = "225.4.5.6";

    public Vector3 position = new Vector3();
    public Quaternion quat = new Quaternion();

    System.Object mutex = new System.Object();

    void ITransportListener.MessageReceived(TransportMessage message, string rawMessage)
    {
      ViconMessage msg = (message.MessageData as ViconMessage);

      lock (mutex)
      {
        position.x = (float)msg.Position[0] / 1000.0f;
        position.y = (float)msg.Position[1] / 1000.0f;
        position.z = -(float)msg.Position[2] / 1000.0f;

        quat.x = -(float)msg.OrientationQuat[0];
        quat.y = -(float)msg.OrientationQuat[1];
        quat.z = (float)msg.OrientationQuat[2];
        quat.w = (float)msg.OrientationQuat[3];
      }
    }
  }
