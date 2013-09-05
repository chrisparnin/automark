using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace automark.Util
{
    class Config
    {
        static Config()
        {
            GitExectuable = @"C:\Program Files (x86)\Git\bin\git";
        }
        public static string GitExectuable {get;set;}
    }
}