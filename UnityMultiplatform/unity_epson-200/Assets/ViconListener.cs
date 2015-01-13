using UnityEngine;
using System.Collections;
using Ubicomp.Utils.NET.MulticastTransportFramework;
using System.Net;
using Vicon2Unity;
using System;
using UnityEngine.UI;

public class ViconListener : MonoBehaviour, ITransportListener 
{
  public const int ProgramID = 1;
  public int port = 5000;
  public int TTL = 10;
  public string groupIP = "225.4.5.6";

  public string subjectName;
  public Vector3 position = new Vector3();
  public Quaternion quat = new Quaternion();

  private bool messageReceived = false;

  // Use this for initialization
  void Start()
  {
    TransportComponent.Instance.MulticastGroupAddress = IPAddress.Parse(groupIP);
    TransportComponent.Instance.Port = port;
    TransportComponent.Instance.UDPTTL = TTL;

    TransportComponent.Instance.TransportListeners.Add(ViconListener.ProgramID, this);
    TransportMessageExporter.Exporters.Add(ViconListener.ProgramID, new TestExporter());
    TransportMessageImporter.Importers.Add(ViconListener.ProgramID, new TestImporter());
    TransportComponent.Instance.Init();
  }

  void ITransportListener.MessageReceived(TransportMessage message, string rawMessage)
  {
    ViconMessage msg = (message.MessageData as ViconMessage);
    Console.WriteLine("MessageReceived: {0}", (message.MessageData as ViconMessage));
    subjectName = msg.SubjectName;
    position.x = (float)msg.Position[0] / 1000.0f;
    position.y = (float)msg.Position[1] / 1000.0f;
    position.z = -(float)msg.Position[2] / 1000.0f;

    quat.x = -(float)msg.OrientationQuat[0];
    quat.y = -(float)msg.OrientationQuat[1]; 
    quat.z = (float)msg.OrientationQuat[2];
    quat.w = (float)msg.OrientationQuat[3];

    messageReceived = true;
  }

  // Update is called once per frame
  void Update()
  {
    if (!messageReceived)
      return;

    transform.position = position;
    transform.rotation = quat;
    messageReceived = false;
  }
}
