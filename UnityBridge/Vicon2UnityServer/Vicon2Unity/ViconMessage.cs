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

    public ViconObject() 
    { 
      Position = new double[3];
      OrientationQuat = new double[4];
      Occluded = false;
      SubjectName = String.Empty;
    }

    private static ViconObject empty;
    public static ViconObject Empty 
    { 
      get 
      {
        if (empty == null)
          empty = new ViconObject();
        return empty;
      } 
    }
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
