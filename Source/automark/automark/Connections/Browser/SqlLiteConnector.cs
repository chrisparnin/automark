using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using automark.Models;
using automark.Util;

namespace automark.Connections.Browser
{
    class SqlLiteConnector
    {
        static SqlLiteConnector()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Environment.Is64BitProcess) // .NET 4.0
            {
                path = Path.Combine(path, "SQLite.Native", "x64", "SQLite.Interop.dll");
            }
            else
            {
                // X32
                path = Path.Combine(path, "SQLite.Native", "x86", "SQLite.Interop.dll");
            }
            NativeMethods.LoadLibrary(path);
        }


        public virtual List<WebVisit> RecentStackoverflow(string temp)
        {
            return null;
        }

    }
}
