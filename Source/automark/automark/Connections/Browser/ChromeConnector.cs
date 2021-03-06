﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using automark.Models;
using automark.Util;
using System.Data.SQLite;

namespace automark.Connections.Browser
{
    class ChromeHistory : SqlLiteConnector
    {

        public override List<WebVisit> RecentStackoverflow(string dbPath)
        {
            var list = new List<WebVisit>();
            using (var connection = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbPath + ";Version=3;Read Only=True"))
            {
                connection.Open();
                
                var command =
                    @"SELECT urls.url, visits.visit_time, urls.title
                  FROM visits, urls
                  WHERE visits.url = urls.id AND urls.url LIKE '%stackoverflow%'
                ";

                using (var c = connection.CreateCommand())
                {
                    c.CommandText = command;
                    var reader = c.ExecuteReader();
                    var urls = new HashSet<string>();
                    while (reader.Read())
                    {
                        var url = reader.GetString(0);
                        var timeEpoch = reader.GetInt64(1) / 1000;
                        var visitTime = FromGoogleTime(timeEpoch);
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



        public DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public DateTime FromGoogleTime(long unixTime)
        {
            var epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(unixTime).ToLocalTime();
        }


        public void RecentGoogleSearches()
        {

        }
    }
}
