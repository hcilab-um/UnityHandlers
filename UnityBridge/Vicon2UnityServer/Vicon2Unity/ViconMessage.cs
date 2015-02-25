using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ubicomp.Utils.NET.MulticastTransportFramework;

namespace Vicon2Unity
{

  public class ViconObject
  {
    public double[] Position;
    public double[] OrientationQuat;
    public bool Occluded;
    public string SubjectName;

    public ViconObject() { }
  }

  public class ViconMessage : ITransportMessageContent
  {
    public ViconObject Camera;
    public ViconObject FingerIndex;
    public ViconObject FingerThumb;
    public ViconObject Ray;

    public ViconMessage() { }
  }

}
