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

    //private static Client MyClient;
    private bool isInitialized = false;

    public int MulticastProgramID = 1;
    public String IPofMulticastGroup = null;
    public int MulticastPort = 5000;
    public int TTL = 10;

    //These ones are to be used in the desktop machine
    Vector3 desktopPosition;
    Quaternion desktopRotation;

    //These ones are to be used in the android client
    CircularList<Vector3> clientPositions = new CircularList<Vector3>(5);
    CircularList<Quaternion> clientRotations = new CircularList<Quaternion>(5);
    bool receivedData = false;

    public GameObject Camera = null;

    public WindowsViconConnector()
    {
    }

    void Update()
    {
      if(isInitialized && Network.isServer)
      {
        lock (mutex)
        {
          Camera.transform.localPosition = desktopPosition;
          Camera.transform.localRotation = desktopRotation;
        }
        networkView.RPC("ReceiveTrackingData", RPCMode.AllBuffered, desktopPosition, desktopRotation);
      }
      else if (Network.isClient && receivedData)
      {
        Camera.transform.localPosition = GetFilteredPosition();
        Camera.transform.localRotation = GetFilteredRotation();
      }
    }

    public void Initialize()
    {
      TransportComponent.Instance.MulticastGroupAddress = IPAddress.Parse(IPofMulticastGroup);
      TransportComponent.Instance.Port = MulticastPort;
      TransportComponent.Instance.UDPTTL = TTL;
      TransportComponent.Instance.TransportListeners.Add(MulticastProgramID, this);
      TransportMessageExporter.Exporters.Add(MulticastProgramID, new TestExporter());
      TransportMessageImporter.Importers.Add(MulticastProgramID, new TestImporter());
      TransportComponent.Instance.Init();

      isInitialized = true;
    }

    System.Object mutex = new System.Object();
    void ITransportListener.MessageReceived(TransportMessage message, string rawMessage)
    {
      ViconMessage msg = (message.MessageData as ViconMessage);

      //Receives the data in millimeters and converts it to meters
      Vector3 position = new Vector3();
      position.x = (float)(msg.Position[0] / 1000);
      position.y = (float)(msg.Position[1] / 1000);
      position.z = -(float)(msg.Position[2] / 1000);

      Quaternion quat = new Quaternion();
      quat.x = -(float)msg.OrientationQuat[0];
      quat.y = -(float)msg.OrientationQuat[1];
      quat.z = (float)msg.OrientationQuat[2];
      quat.w = (float)msg.OrientationQuat[3];

      lock (mutex)
      {
        desktopPosition = position;
        desktopRotation = quat;
      }
    }

    private Quaternion GetFilteredRotation()
    {
      if (clientRotations == null || clientRotations.Count == 0)
        return Quaternion.identity;

      float accumX, accumY, accumZ, accumW, count;
      accumX = accumY = accumZ = accumW = 0.0f;
      count = clientRotations.Count;
      for (int index = 0; index < count; index++)
      {
        Quaternion quat = clientRotations[index];
        accumX += quat.x;
        accumY += quat.y;
        accumZ += quat.z;
        accumW += quat.w;
      }

      Quaternion filtered = new Quaternion(accumX / count, accumY / count, accumZ / count, accumW / count);
      return filtered;
    }

    private Vector3 GetFilteredPosition()
    {
      if (clientPositions == null || clientPositions.Count == 0)
        return Vector3.one;

      float accumX, accumY, accumZ, count;
      accumX = accumY = accumZ = 0.0f;
      count = clientPositions.Count;
      for (int index = 0; index < count; index++)
      {
        Vector3 pos = clientPositions[index];
        accumX += pos.x;
        accumY += pos.y;
        accumZ += pos.z;
      }

      Vector3 filtered = new Vector3(accumX / count, accumY / count, accumZ / count);
      return filtered;
    }

    [RPC]
    void ReceiveTrackingData(Vector3 cameraPosition, Quaternion cameraRotation)
    {
      if (Network.isServer)
        return;

      clientPositions.Value = cameraPosition;
      clientPositions.Next();
      clientRotations.Value = cameraRotation;
      clientRotations.Next();
      receivedData = true;
    }

  }
