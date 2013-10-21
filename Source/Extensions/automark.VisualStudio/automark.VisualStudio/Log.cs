using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ninlabs.automark.VisualStudio
{
    public class Log
    {
        public static string LogPath { get; set; }
        private List<string> Buffer = new List<string>();

        DateTime lastWrite = DateTime.MinValue;

        public void WriteMessage(string message)
        {
            // Throttled write.
            if ((DateTime.Now - lastWrite ).TotalMinutes > 1 || Buffer.Count > 500)
            {
                WriteBuffer();
                Write(message);
            }
            else
            {
                Buffer.Add(message);
            }
            lastWrite = DateTime.Now;
        }

        private void Write(string message)
        {
            try
            {
                using (var writer = System.IO.File.AppendText(LogPath))
                {
                    writer.WriteLine(message);
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
            }
        
        }


        private void WriteBuffer()
        {
            try
            {
                using (var writer = System.IO.File.AppendText(LogPath))
                {
                    foreach (var message in Buffer)
                    {
                        writer.WriteLine(message);
                    }
                    Buffer.Clear();
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
            }

        }


        public void Flush()
        {
            if (Buffer.Count > 0)
            {
                WriteBuffer();
            }
        }
    }
}
