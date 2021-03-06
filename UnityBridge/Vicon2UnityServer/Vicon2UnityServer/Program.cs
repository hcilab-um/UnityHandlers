﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Ubicomp.Utils.NET.MulticastTransportFramework;
using Vicon2Unity;
using System.Net.Sockets;
using ViconDataStreamSDK.DotNET;
using Vicon2UnityServer.Properties;

namespace Vicon2UnityServer
{
  class Program : ITransportListener
  {
    private static ViconDataStreamSDK.DotNET.Client MyClient;
    public const int ProgramID = 1;

    public Guid localHostGuid = Guid.NewGuid();

    public Program(String groupIP, int port, int TTL)
    {
      TransportComponent.Instance.MulticastGroupAddress = IPAddress.Parse(groupIP);
      TransportComponent.Instance.Port = port;
      TransportComponent.Instance.UDPTTL = TTL;
    }

    public void Config()
    {
      TransportComponent.Instance.TransportListeners.Add(Program.ProgramID, this);

      TransportMessageExporter.Exporters.Add(Program.ProgramID, new TestExporter());
      TransportMessageImporter.Importers.Add(Program.ProgramID, new TestImporter());

      TransportComponent.Instance.Init();
    }

    private void SendMessages(ViconMessage message)
    {
      EventSource eventSource = new EventSource(localHostGuid, Environment.MachineName, Environment.MachineName);

      TransportMessage tMessage1 = new TransportMessage(eventSource, Program.ProgramID, message);

      String json1 = TransportComponent.Instance.Send(tMessage1);
    }

    void ITransportListener.MessageReceived(TransportMessage message, string rawMessage)
    {
      Console.WriteLine("MessageReceived: {0}", (message.MessageData as ViconMessage));
    }

    static void Main(string[] args)
    {
      Program testObj = new Program("225.4.5.6", 5000, 10);
      testObj.Config();
      testObj.ConnectToVicon(Settings.Default.ViconServerIP, Settings.Default.ViconServerPort);
      Console.Write("Waiting for new frame...");
      Console.WriteLine();
      while (!Console.KeyAvailable)
      {
        ViconMessage message = LoadViconMessage(Settings.Default.Camera1Name, Settings.Default.Camera2Name, Settings.Default.FingerIndexName, Settings.Default.FingerThumbName, Settings.Default.RayName);
        if (message != null)
        {
          testObj.SendMessages(message);
        }
      }
      Console.WriteLine("Press Enter to finish.");
      Console.Read();
    }

    private void ConnectToVicon(String ipOfViconServer, int port)
    {
      Socket viconSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
      string HostName = ipOfViconServer + ":" + port;

      // Make a new client
      MyClient = new ViconDataStreamSDK.DotNET.Client();
      while (!MyClient.IsConnected().Connected)
      {
        // Direct connection
        MyClient.Connect(HostName);
        System.Threading.Thread.Sleep(200);
        Console.Write(".");
      }

      Console.WriteLine();
      MyClient.EnableSegmentData();
      //ServerPush have no latency
      MyClient.SetStreamMode(ViconDataStreamSDK.DotNET.StreamMode.ServerPush);

      // Set the global up axis
      MyClient.SetAxisMapping(ViconDataStreamSDK.DotNET.Direction.Forward, ViconDataStreamSDK.DotNET.Direction.Up, ViconDataStreamSDK.DotNET.Direction.Right); // Y-up
    }

    private static ViconMessage LoadViconMessage(string camera1Name, string camera2Name, string fingerIndexName, string fingerThumbName, string rayName)
    {
      // Get a frame
      ViconDataStreamSDK.DotNET.Result a = MyClient.GetFrame().Result;
      while (MyClient.GetFrame().Result != ViconDataStreamSDK.DotNET.Result.Success)
      {
        System.Threading.Thread.Sleep(200);
        Console.Write(".");
      }
      Console.WriteLine();

      // Get the global segment translation
      ViconObject camera1 = GetViconObjectFromFrame(camera1Name);
      ViconObject camera2 = GetViconObjectFromFrame(camera2Name);
      ViconObject fingerIndex = fingerIndexName != null ? GetViconObjectFromFrame(fingerIndexName) : ViconObject.Empty;
      ViconObject fingerThumb = fingerThumbName != null ? GetViconObjectFromFrame(fingerThumbName) : ViconObject.Empty;
      ViconObject ray = rayName != null ? GetViconObjectFromFrame(rayName) : ViconObject.Empty;

      ViconMessage message = new ViconMessage();
      message.Camera1 = camera1;
      message.Camera2 = camera2;
      message.FingerIndex = fingerIndex;
      message.FingerThumb = fingerThumb;
      message.Ray = ray;

      if (message.Camera1 != null)
        return message;
      return null;
    }

    private static ViconObject GetViconObjectFromFrame(string objectName)
    {
      Output_GetSegmentGlobalTranslation _Output_GetSegmentGlobalTranslation = MyClient.GetSegmentGlobalTranslation(objectName, objectName);
      Output_GetSegmentGlobalRotationQuaternion _Output_GetSegmentGlobalRotationQuaternion = MyClient.GetSegmentGlobalRotationQuaternion(objectName, objectName);

      if (Result.Success == _Output_GetSegmentGlobalTranslation.Result
          && Result.Success == _Output_GetSegmentGlobalRotationQuaternion.Result)
      {
        ViconObject returnV = new ViconObject()
        {
          SubjectName = objectName,
          Occluded = _Output_GetSegmentGlobalTranslation.Occluded,
        };

        returnV.Position[0] = _Output_GetSegmentGlobalTranslation.Translation[0];
        returnV.Position[1] = _Output_GetSegmentGlobalTranslation.Translation[1];
        returnV.Position[2] = _Output_GetSegmentGlobalTranslation.Translation[2];

        returnV.RotationQuat[0] = _Output_GetSegmentGlobalRotationQuaternion.Rotation[0];
        returnV.RotationQuat[1] = _Output_GetSegmentGlobalRotationQuaternion.Rotation[1];
        returnV.RotationQuat[2] = _Output_GetSegmentGlobalRotationQuaternion.Rotation[2];
        returnV.RotationQuat[3] = _Output_GetSegmentGlobalRotationQuaternion.Rotation[3];

        Console.WriteLine("{0}: {1},{2},{3}", objectName, returnV.Position[0], returnV.Position[1], returnV.Position[2]);
        return returnV;
      }

      Console.WriteLine("{0}: Not Defined in Tracker", objectName);
      return ViconObject.Empty;
    }
  }
}
