using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityMoverioBT200.Scripts
{

  /// <summary>
  /// Taken from http://msdn.microsoft.com/en-us/library/vstudio/234b841s(v=vs.90).aspx
  /// </summary>
  class TargetComparer : IComparer<Target>
  {
		public int Compare(Target x, Target y)
    {
      if (x == null)
      {
        if (y == null)
        {
          // If x is null and y is null, they're 
          // equal.  
          return 0;
        }
        else
        {
          // If x is null and y is not null, y 
          // is greater.  
          return -1;
        }
      }
      else
      {
        // If x is not null... 
        // 
        if (y == null)
        // ...and y is null, x is greater.
        {
          return 1;
        }
        else
        {
          return x.name.CompareTo(y.name);
        }
      }
    }
  }

}