using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jayrock.Json.Conversion;

namespace Vicon2Unity
{
  public class TestImporter : IImporter
  {
    public object Import(ImportContext context, Jayrock.Json.JsonReader reader)
    {
      ViconMessage mMessage = context.Import<ViconMessage>(reader);
      return mMessage;
    }

    public Type OutputType
    {
      get { return typeof(ViconMessage); }
    }
  }
}
