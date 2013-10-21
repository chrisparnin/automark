using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Models;

namespace automark.Connections.Browser
{
    class FirefoxConnector : SqlLiteConnector
    {
        public override List<WebVisit> RecentStackoverflow(string dbPath)
        {
            var list = new List<WebVisit>();
             using (var connection = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbPath + ";Version=3;Read Only=True"))
            {
                connection.Open();

                var command =    
  @"SELECT moz_places.url, datetime(moz_historyvisits.visit_date/1000000,'unixepoch') as visit_time, moz_places.title
	FROM moz_places, moz_historyvisits 
	WHERE moz_places.id = moz_historyvisits.place_id AND moz_places.url LIKE '%stackoverflow%'
                ";

                using (var c = connection.CreateCommand())
                {
                    c.CommandText = command;
                    var reader = c.ExecuteReader();
                    var urls = new HashSet<string>();
                    while (reader.Read())
                    {
                        var url = reader.GetString(0);
                        var visitTime = reader.GetDateTime(1);
                        //var visitTime = FromGoogleTime(timeEpoch);
                        var title = reader.GetString(2);
                        if (!urls.Contains(url))
                        {
                            list.Add(new WebVisit() { Url = url, Timestamp = visitTime, Title = title });
                        }
                        urls.Add(url);
                    }
                }
                connection.Close();
            }

            return list;
        }

        public string FindDbPath()
        {
            try
            {
                string ffPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Mozilla\Firefox\Profiles\");
                // [profile]\places.sqlite
                foreach (var profile in System.IO.Directory.EnumerateDirectories(ffPath))
                {
                    var db = System.IO.Path.Combine(ffPath, profile, "places.sqlite");
                    if (System.IO.File.Exists(db))
                        return db;
                }
            }
            catch (Exception ex)
            {
                Trace.Write(ex.Message);
            }
            return null;
        }
    }
}
