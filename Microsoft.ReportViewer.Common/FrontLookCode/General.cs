using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ReportViewer.Common.FrontLookCode
{
    public static class General
    {

        public static string ConsoleWriteDebug(this string gString)
        {
            Console.WriteLine(gString);
            Debug.WriteLine(gString);
            return gString;
        }
    }
}
