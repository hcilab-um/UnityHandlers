using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ubicomp.Utils.NET.MulticastTransportFramework;

namespace Vicon2Unity
{

  public class ViconMessage : ITransportMessageContent
  {
    public double[] Position;
    public double[] OrientationQuat;
    public bool Occluded;
    public string SubjectName;

    public ViconMessage() 
    {
    }
  }

}
