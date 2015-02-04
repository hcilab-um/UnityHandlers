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
    Vector3 desktopCameraPosition;
    Vector3 desktopFingerIndexPosition;
    Vector3 desktopFingerThumbPosition;

    Quaternion desktopCameraRotation;
    Quaternion desktopFingerIndexRotation;
    Quaternion desktopFingerThumbRotation;

    //These ones are to be used in the android client
    CircularList<Vector3> clientCameraPositions = new CircularList<Vector3>(5);
    CircularList<Quaternion> clientCameraRotations = new CircularList<Quaternion>(5);

    CircularList<Vector3> clientFingerThumbPositions = new CircularList<Vector3>(5);
    CircularList<Quaternion> clientFingerThumbRotations = new CircularList<Quaternion>(5);

    CircularList<Vector3> clientFingerIndexPositions = new CircularList<Vector3>(5);
    CircularList<Quaternion> clientFingerIndexRotations = new CircularList<Quaternion>(5);
    
    bool receivedData = false;

    public GameObject FingerIndex = null;
    public GameObject FingerThumb = null;
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
          Camera.transform.localPosition = desktopCameraPosition;
          Camera.transform.localRotation = desktopCameraRotation;
          
          FingerIndex.transform.localPosition = desktopFingerIndexPosition;
          FingerIndex.transform.localRotation = desktopFingerIndexRotation;

          FingerThumb.transform.localPosition = desktopFingerThumbPosition;
          FingerThumb.transform.localRotation = desktopFingerThumbRotation;
        }
        
        networkView.RPC("ReceiveTrackingDataCamera", RPCMode.AllBuffered, desktopCameraPosition, desktopCameraRotation);
        networkView.RPC("ReceiveTrackingDataFingerIndex", RPCMode.AllBuffered, desktopFingerIndexPosition, desktopFingerIndexRotation);
        networkView.RPC("ReceiveTrackingDataFingerThumb", RPCMode.AllBuffered, desktopFingerThumbPosition, desktopFingerThumbRotation);

      }
      else if (Network.isClient && receivedData)
      {
        Camera.transform.localPosition = GetFilteredPosition(clientCameraPositions);
        Camera.transform.localRotation = GetFilteredRotation(clientCameraRotations);
        
        FingerIndex.transform.localPosition = GetFilteredPosition(clientFingerThumbPositions);
        FingerIndex.transform.localRotation = GetFilteredRotation(clientFingerThumbRotations);

        FingerThumb.transform.localPosition = GetFilteredPosition(clientFingerIndexPositions);
        FingerThumb.transform.localRotation = GetFilteredRotation(clientFingerThumbRotations);
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
      Vector3 cameraPos = new Vector3();
      Vector3 fingerIndexPos = new Vector3();
      Vector3 fingerThumbPos = new Vector3();

      Quaternion cameraRot = new Quaternion();
      Quaternion fingerIndexRot = new Quaternion();
      Quaternion fingerThumbRot = new Quaternion();

      LoadMessageObject(msg.Camera, ref cameraPos, ref cameraRot);
      LoadMessageObject(msg.FingerIndex, ref fingerIndexPos, ref fingerIndexRot);
      LoadMessageObject(msg.FingerThumb, ref fingerThumbPos, ref fingerThumbRot);

      lock (mutex)
      {
        desktopCameraPosition = cameraPos;
        desktopCameraRotation = cameraRot;
        
        desktopFingerIndexPosition = fingerIndexPos;
        desktopFingerIndexRotation = fingerIndexRot;

        desktopFingerThumbPosition = fingerThumbPos;
        desktopFingerThumbRotation = fingerThumbRot;
      }
    }

    private static void LoadMessageObject(ViconObject viconObject, ref Vector3 objectPos, ref Quaternion objectRot)
    {
      if (viconObject != null)
      {
        objectPos.x = (float)(viconObject.Position[0] / 1000);
        objectPos.y = (float)(viconObject.Position[1] / 1000);
        objectPos.z = -(float)(viconObject.Position[2] / 1000);

        objectRot.x = -(float)viconObject.OrientationQuat[0];
        objectRot.y = -(float)viconObject.OrientationQuat[1];
        objectRot.z = (float)viconObject.OrientationQuat[2];
        objectRot.w = (float)viconObject.OrientationQuat[3];
      }
    }

    private Quaternion GetFilteredRotation(CircularList<Quaternion> rotationList)
    {
      if (rotationList == null || rotationList.Count == 0)
        return Quaternion.identity;

      float accumX, accumY, accumZ, accumW, count;
      accumX = accumY = accumZ = accumW = 0.0f;
      count = rotationList.Count;
      for (int index = 0; index < count; index++)
      {
        Quaternion quat = rotationList[index];
        accumX += quat.x;
        accumY += quat.y;
        accumZ += quat.z;
        accumW += quat.w;
      }

      Quaternion filtered = new Quaternion(accumX / count, accumY / count, accumZ / count, accumW / count);
      return filtered;
    }

    private Vector3 GetFilteredPosition(CircularList<Vector3> positionList)
    {
      if (positionList == null || positionList.Count == 0)
        return Vector3.one;

      float accumX, accumY, accumZ, count;
      accumX = accumY = accumZ = 0.0f;
      count = positionList.Count;
      for (int index = 0; index < count; index++)
      {
        Vector3 pos = positionList[index];
        accumX += pos.x;
        accumY += pos.y;
        accumZ += pos.z;
      }

      Vector3 filtered = new Vector3(accumX / count, accumY / count, accumZ / count);
      return filtered;
    }

    [RPC]
    void ReceiveTrackingDataCamera(Vector3 objectPosition, Quaternion objectRotation)
    {
      if (Network.isServer)
        return;

      clientCameraPositions.Value = objectPosition;
      clientCameraPositions.Next();
      clientCameraRotations.Value = objectRotation;
      clientCameraRotations.Next();
      receivedData = true;
    }

    [RPC]
    void ReceiveTrackingDataFingerIndex(Vector3 objectPosition, Quaternion objectRotation)
    {
      if (Network.isServer)
        return;

      clientFingerIndexPositions.Value = objectPosition;
      clientFingerIndexPositions.Next();
      clientFingerIndexRotations.Value = objectRotation;
      clientFingerIndexRotations.Next();
    }

    [RPC]
    void ReceiveTrackingDataFingerThumb(Vector3 objectPosition, Quaternion objectRotation)
    {
      if (Network.isServer)
        return;

      clientFingerThumbPositions.Value = objectPosition;
      clientFingerThumbPositions.Next();
      clientFingerThumbRotations.Value = objectRotation;
      clientFingerThumbRotations.Next();
    }

  }
