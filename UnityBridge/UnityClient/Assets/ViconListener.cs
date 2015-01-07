using UnityEngine;
using System.Collections;
using Ubicomp.Utils.NET.MulticastTransportFramework;
using System.Net;
using Vicon2Unity;
using System;

public class ViconListener : MonoBehaviour, ITransportListener 
{
	public const int ProgramID = 1;
	public int port = 5000;
	public int TTL = 10;
	public string groupIP = "225.4.5.6";

	public string name = "";
	public Vector3 position = new Vector3();
	// Use this for initialization
	void Start () 
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
		name = msg.SubjectName;
		position.x = (float)msg.Position [0];
		position.y = (float)msg.Position [1];
		position.z = (float)msg.Position [2];
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}
}
