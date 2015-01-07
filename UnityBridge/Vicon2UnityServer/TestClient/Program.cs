using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ubicomp.Utils.NET.MulticastTransportFramework;

namespace TestClient
{
  public class Program : ITransportListener
  {
    public const int ProgramID = 1;
    public int port = 5000;
    public int TTL = 10;
    public string groupIP = "225.4.5.6";

    static void Main(string[] args)
    {
    }
  }
}
