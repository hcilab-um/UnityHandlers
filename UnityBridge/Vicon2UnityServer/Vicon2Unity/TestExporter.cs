using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json.Conversion;

namespace Vicon2Unity
{
  public class TestExporter : IExporter
  {
    public void Export(ExportContext context, object value, Jayrock.Json.JsonWriter writer)
    {
      ViconMessage mMessage = (ViconMessage)value;
      context.Export(mMessage, writer);
    }

    public Type InputType
    {
      get { return typeof(ViconMessage); }
    }
  }
}
