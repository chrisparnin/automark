using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace automark.Util
{
    class Config
    {
        static Config()
        {
            //GitExectuable = @"C:\Program Files (x86)\Git\bin\git";
            string local = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().FullName);
            GitExectuable = System.IO.Path.Combine(local, "git");
        }
        public static string GitExectuable {get;set;}
    }
}