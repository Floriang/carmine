using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.GData.Calendar;
using Google.GData.Client;

namespace Saxx.Carmine.Plugins {
    public class Gcal : Plugin {

        public override void Message(string from, string message) {
            var match = Regex.Match(message, "^gcal register (?<UserName>.*?) (?<Password>.*)$", RegexOptions.IgnoreCase);
            if (match.Success) {
                Log(LogType.Info, "Registering user data for " + from);
                var userData = new UserData() {
                    Id = from,
                    LastCheck = DateTime.Now,
                    UserName = match.Groups["UserName"].Value,
                    Password = match.Groups["Password"].Value
                };
                SetUserData(userData);
                PrintEvents(from);
            }
            else if (message.Equals("gcal", StringComparison.InvariantCultureIgnoreCase)) {
                PrintEvents(from);
            }
        }

        private void PrintEvents(string from) {
            Log(LogType.Info, "Printing events for " + from);
            var userData = GetUserData(from);
            if (userData == null) {
                SendMessage(from, "No Google credentials found for you :(");
                Log(LogType.Info, "No Google credentials " + from + " in the database");
            }
            else {
                try {
                    var events = GetEvents("hannes.sachsenhofer@gmail.com", "sien.Turn").ToList();

                    if (events.Count > 0)
                        SendMessage(from, "Your events for today: ");
                    else
                        SendMessage(from, "No events scheduled for today :)");
                    foreach (var evnt in events)
                        SendMessage(from, (string.IsNullOrEmpty(evnt.Time) ? "" : "_" + evnt.Time + "_  ") + evnt.Title);
                }
                catch (Exception ex) {
                    SendMessage(from, "There was an error while loading your events :(");
                    Log(LogType.Error, "Error while loading events for " + userData.UserName + ": ", ex);
                }
            }
        }

        private IEnumerable<Event> GetEvents(string user, string password) {
            Log(LogType.Info, "Loading Gcal events for " + user);
            var result = new List<Event>();

            var calendarService = new CalendarService("Carmine");
            calendarService.setUserCredentials(user, password);

            var calendarUris = new List<string>();
            var calendarsQuery = new FeedQuery("http://www.google.com/calendar/feeds/default/allcalendars/full");
            var calendarsFeed = calendarService.Query(calendarsQuery);
            foreach (var calendarsEntry in calendarsFeed.Entries) {
                var fullUri = calendarsEntry.Links.First(x => x.Rel == "self").HRef.Content;
                var calendarId = fullUri.Substring(fullUri.LastIndexOf("/") + 1);
                calendarUris.Add("http://www.google.com/calendar/feeds/" + calendarId + "/private/full");
            }

            foreach (var uri in calendarUris) {
                var eventQuery = new EventQuery(uri);
                eventQuery.StartTime = DateTime.Now.ToUniversalTime().Date;
                eventQuery.EndTime = DateTime.Now.ToUniversalTime().Date.AddDays(1);
                eventQuery.SortOrder = CalendarSortOrder.ascending;
                var eventFeed = calendarService.Query(eventQuery);
                foreach (EventEntry eventEntry in eventFeed.Entries) {
                    var evnt = new Event();
                    result.Add(evnt);

                    var eventTime = eventEntry.Times.FirstOrDefault();
                    if (eventTime != null && !eventTime.AllDay)
                        evnt.Time = eventTime.StartTime.ToString("HH:mm");
                    evnt.Title = eventEntry.Title.Text;
                }
            }

            return result.OrderBy(x => x.Time);
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
        }

        private UserData GetUserData(string id) {
            Log(LogType.Info, "Loading user data for " + id);
            using (var db = GetDatabase(DatabaseName)) {
                try {
                    db.ExecuteReader("SELECT Count(*) FROM [UserData];");
                }
                catch {
                    SetupDatabase(db);
                }

                var reader = db.ExecuteReader("SELECT [Id], [UserName], [Password], [LastCheck] FROM [UserData] WHERE [Id] = ?", id);
                if (reader.Read()) {
                    var userData = new UserData();
                    userData.Id = (string)reader["Id"];
                    userData.UserName = (string)reader["UserName"];
                    userData.Password = (string)reader["Password"];
                    userData.LastCheck = (DateTime)reader["LastCheck"];
                    return userData;
                }
                return null;
            }
        }

        private void SetUserData(UserData userData) {
            Log(LogType.Info, "Saving user data for " + userData.Id);
            using (var db = GetDatabase(DatabaseName)) {
                db.ExecuteCommand("DELETE FROM [UserData] WHERE [Id] = ?;", userData.Id);
                db.ExecuteCommand("INSERT INTO [UserData] ([Id], [UserName], [Password], [LastCheck]) VALUES (?, ?, ?, ?);", userData.Id, userData.UserName, userData.Password, userData.LastCheck);
            }
        }

        private void SetupDatabase(IDatabase db) {
            db.ExecuteCommand("CREATE TABLE [UserData] ("
                + "[Id] NVARCHAR(1000) NOT NULL PRIMARY KEY,"
                + "[UserName] NVARCHAR(1000) NOT NULL,"
                + "[Password] NVARCHAR(1000) NOT NULL,"
                + "[LastCheck] TIMESTAMP NOT NULL"
                + ");");
            db.ExecuteCommand("PRAGMA auto_vacuum = 1;");
        }

        private string DatabaseName {
            get {
                return this.GetType().FullName;
            }
        }
        #endregion
    }
}
