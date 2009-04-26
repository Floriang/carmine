using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.GData.Calendar;
using Google.GData.Client;
using System.Data;

namespace Saxx.Carmine.Plugins {
    public partial class Gcal : Plugin {

        public override void Message(string from, string message) {
            var match = Regex.Match(message, "^gcal register (?<username>.*?) (?<password>.*?)( (?<sendsummary>sendsummary))?$", RegexOptions.IgnoreCase);
            if (match.Success) {
                Log(LogType.Info, "Registering user data for " + from);
                var userData = new UserData() {
                    Id = from,
                    LastCheck = DateTime.Now,
                    UserName = match.Groups["username"].Value,
                    Password = match.Groups["password"].Value,
                    SendSummary = match.Groups["sendsummary"] != null && !string.IsNullOrEmpty(match.Groups["sendsummary"].Value)
                };
                SetUserData(userData);
                PrintEvents(from, 0);
                return;
            }

            match = Regex.Match(message, "^gcal add (?<event>.*)$", RegexOptions.IgnoreCase);
            if (match.Success) {
                AddEvent(from, match.Groups["event"].Value);
                return;
            }

            match = Regex.Match(message, @"^gcal (?<days>\d*)$", RegexOptions.IgnoreCase);
            if (match.Success) {
                PrintEvents(from, Convert.ToInt32(match.Groups["days"].Value));
                return;
            }

            if (message.Equals("gcal tomorrow", StringComparison.InvariantCultureIgnoreCase))
                PrintEvents(from, 1);
            else if (message.Equals("gcal", StringComparison.InvariantCultureIgnoreCase))
                PrintEvents(from, 0);
        }

        private DateTime _lastDate = DateTime.Now.Date;
        public override void Tick() {
            if (_lastDate < DateTime.Now.Date) {
                foreach (var userdata in GetUserData().Where(x => x.SendSummary))
                    PrintEvents(userdata.Id, 0);
                _lastDate = DateTime.Now.Date;
            }
        }

        private IEnumerable<Calendar> GetCalendars(CalendarService calendarService) {
            var result = new List<Calendar>();

            var calendarsQuery = new FeedQuery("http://www.google.com/calendar/feeds/default/allcalendars/full");
            var calendarsFeed = calendarService.Query(calendarsQuery);
            foreach (var calendarsEntry in calendarsFeed.Entries) {
                var calendar = new Calendar();

                var fullUri = calendarsEntry.Links.First(x => x.Rel == "self").HRef.Content;
                var calendarId = fullUri.Substring(fullUri.LastIndexOf("/") + 1);

                calendar.Uri = new Uri("http://www.google.com/calendar/feeds/" + calendarId + "/private/full");
                calendar.Name = calendarsEntry.Title.Text;

                result.Add(calendar);
            }

            return result;
        }

        public override string ToString() {
            return "The Gcal plugin";
        }

        private class Event {
            public string Title {
                get;
                set;
            }

            public string Time {
                get;
                set;
            }
        }

        private class Calendar {
            public string Name {
                get;
                set;
            }

            public Uri Uri {
                get;
                set;
            }
        }

        #region Database
        private class UserData {
            public string Id {
                get;
                set;
            }

            public string UserName {
                get;
                set;
            }

            public string Password {
                get;
                set;
            }

            public DateTime LastCheck {
                get;
                set;
            }

            public bool SendSummary {
                get;
                set;
            }
        }

        private IEnumerable<UserData> GetUserData() {
            var result = new List<UserData>();
            Log(LogType.Info, "Loading all user data");
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);

                var reader = db.ExecuteReader("SELECT [Id], [UserName], [Password], [LastCheck], [SendSummary] FROM [UserData]");
                if (reader.Read())
                    result.Add(ReadUserData(reader));
            }

            return result;
        }

        private UserData GetUserData(string id) {
            Log(LogType.Info, "Loading user data for " + id);
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);

                var reader = db.ExecuteReader("SELECT [Id], [UserName], [Password], [LastCheck], [SendSummary] FROM [UserData] WHERE [Id] = ?", id);
                if (reader.Read())
                    return ReadUserData(reader);
                return null;
            }
        }

        private void SetUserData(UserData userData) {
            Log(LogType.Info, "Saving user data for " + userData.Id);
            using (var db = GetDatabase(DatabaseName)) {
                SetupDatabase(db);
                db.ExecuteCommand("DELETE FROM [UserData] WHERE [Id] = ?;", userData.Id);
                db.ExecuteCommand("INSERT INTO [UserData] ([Id], [UserName], [Password], [LastCheck], [SendSummary]) VALUES (?, ?, ?, ?, ?);", userData.Id, userData.UserName, userData.Password, userData.LastCheck, userData.SendSummary);
            }
        }

        private UserData ReadUserData(IDataReader reader) {
            var userData = new UserData();
            userData.Id = (string)reader["Id"];
            userData.UserName = (string)reader["UserName"];
            userData.Password = (string)reader["Password"];
            userData.LastCheck = (DateTime)reader["LastCheck"];
            userData.SendSummary = (bool)reader["SendSummary"];
            return userData;
        }

        private void SetupDatabase(IDatabase db) {
            try {
                db.ExecuteReader("SELECT Count(*) FROM [UserData];");
            }
            catch {
                db.ExecuteCommand("CREATE TABLE [UserData] ("
                    + "[Id] NVARCHAR(1000) NOT NULL PRIMARY KEY,"
                    + "[UserName] NVARCHAR(1000) NOT NULL,"
                    + "[Password] NVARCHAR(1000) NOT NULL,"
                    + "[LastCheck] TIMESTAMP NOT NULL,"
                    + "[SendSummary] BIT NOT NULL"
                    + ");");
                db.ExecuteCommand("PRAGMA auto_vacuum = 1;");
            }
        }

        private string DatabaseName {
            get {
                return this.GetType().FullName;
            }
        }
        #endregion
    }
}
