using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Ubicomp.Utils.NET.MulticastTransportFramework;
using Vicon2Unity;
using System.Net.Sockets;
using ViconDataStreamSDK.DotNET;

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
      testObj.ConnectToVicon();
      Console.Write("Waiting for new frame...");
      Console.WriteLine();
      while (!Console.KeyAvailable)
      {
        ViconMessage message = LoadViconMessage("bt100");
        if (message != null)
        {
          testObj.SendMessages(message);
        }
      }
      Console.WriteLine("Press Enter to finish.");
      Console.Read();
    }

    private void ConnectToVicon()
    {
      Socket viconSocket = new Socket(AddressFamily.InterNetwork,
              SocketType.Dgram, ProtocolType.Udp);
      string HostName = "localhost:801";
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
      MyClient.SetAxisMapping(ViconDataStreamSDK.DotNET.Direction.Forward,
                         ViconDataStreamSDK.DotNET.Direction.Left,
                         ViconDataStreamSDK.DotNET.Direction.Up); // Z-up
      //MyClient.StartTransmittingMulticast("localhost", "224.0.0.0");
    }

    private static ViconMessage LoadViconMessage(string subjectName)
    {
      // Get a frame
      Console.Write("Waiting for new frame...");
      ViconDataStreamSDK.DotNET.Result a = MyClient.GetFrame().Result;
      while (MyClient.GetFrame().Result != ViconDataStreamSDK.DotNET.Result.Success)
      {
        System.Threading.Thread.Sleep(200);
        Console.Write(".");
      }
      Console.WriteLine();

      // Get the global segment translation
      Output_GetSegmentGlobalTranslation _Output_GetSegmentGlobalTranslation =
        MyClient.GetSegmentGlobalTranslation(subjectName, subjectName);
      // Get the global segment rotation in quaternion co-ordinates
      Output_GetSegmentGlobalRotationQuaternion _Output_GetSegmentGlobalRotationQuaternion =
        MyClient.GetSegmentGlobalRotationQuaternion(subjectName, subjectName);

      if (Result.Success ==_Output_GetSegmentGlobalTranslation.Result
          && Result.Success == _Output_GetSegmentGlobalRotationQuaternion.Result)
      {
        return new ViconMessage() 
        { 
          SubjectName = subjectName,
          Occluded = _Output_GetSegmentGlobalTranslation.Occluded, 
          Position = _Output_GetSegmentGlobalTranslation.Translation, 
          OrientationQuat = _Output_GetSegmentGlobalRotationQuaternion.Rotation };
      }

      Console.WriteLine("Fail To get Vicon message: {0} - {1}", 
                         _Output_GetSegmentGlobalTranslation.Result.ToString(), 
                         _Output_GetSegmentGlobalRotationQuaternion.Result.ToString());
      return null;
    }
  }
}
