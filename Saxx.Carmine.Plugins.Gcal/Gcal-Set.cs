using System;
using System.Linq;
using System.Text.RegularExpressions;
using Google.GData.Calendar;
using Google.GData.Extensions;

namespace Saxx.Carmine.Plugins {
    public partial class Gcal {

        private void AddEvent(string from, string evnt) {
            Log(LogType.Info, "Adding event " + evnt + " for " + from);
            var userData = GetUserData(from);
            if (userData == null) {
                SendMessage(from, "I don't know any Google credentials from you :(");
                Log(LogType.Info, "No Google credentials for " + from + " in the database");
            }
            else {
                var calendarService = new CalendarService("Carmine");
                calendarService.setUserCredentials(userData.UserName, userData.Password);
                var calendars = GetCalendars(calendarService);

                var match = Regex.Match(evnt,
                    /* calendar */ @"^(to (?<calendar>[A-Za-z0-9\._-]*) )?"
                    /* date     */ + @"(on (?<date>.*?) )?"
                    /* time     */ + @"(at (?<startTime>\d{1,2}:\d{1,2})(-(?<endTime>\d{1,2}:\d{1,2}))? )?"
                    /* title    */ + @"(?<title>.*?)"
                    /* location */ + @"( in (?<location>.*?))?$");

                if (match.Success) {
                    var calendarUri = calendars.First().Uri;
                    var title = match.Groups["title"].Value;
                    var location = match.Groups["location"] == null || string.IsNullOrEmpty(match.Groups["location"].Value) ? "" : match.Groups["location"].Value;

                    if (match.Groups["calendar"] != null && !string.IsNullOrEmpty(match.Groups["calendar"].Value)) {
                        var cal = calendars.FirstOrDefault(x => x.Name.Equals(match.Groups["calendar"].Value, StringComparison.InvariantCultureIgnoreCase));
                        if (cal != null)
                            calendarUri = cal.Uri;
                    }

                    var startDate = DateTime.Now.Date;
                    if (match.Groups["date"] != null && !string.IsNullOrEmpty(match.Groups["date"].Value)) {
                        DateTime parsedDate;
                        if (DateTime.TryParse(match.Groups["date"].Value, out parsedDate))
                            startDate = parsedDate;
                    }

                    var endDate = startDate;
                    var allDay = false;
                    if (match.Groups["startTime"] != null && !string.IsNullOrEmpty(match.Groups["startTime"].Value)) {
                        var startTime = match.Groups["startTime"].Value;
                        startDate = startDate.AddHours(int.Parse(startTime.Substring(0, startTime.IndexOf(":"))));
                        startDate = startDate.AddMinutes(int.Parse(startTime.Substring(startTime.LastIndexOf(":") + 1, startTime.Length - startTime.LastIndexOf(":") - 1)));

                        if (match.Groups["endTime"] != null && !string.IsNullOrEmpty(match.Groups["endTime"].Value)) {
                            var endTime = match.Groups["endTime"].Value;
                            endDate = endDate.AddHours(int.Parse(endTime.Substring(0, endTime.IndexOf(":"))));
                            endDate = endDate.AddMinutes(int.Parse(endTime.Substring(endTime.LastIndexOf(":") + 1, endTime.Length - endTime.LastIndexOf(":") - 1)));
                        }
                        else
                            endDate = startDate.AddHours(1);
                    }
                    else {
                        startDate = startDate.Date;
                        endDate = endDate.Date.AddDays(1);
                        allDay = true;
                    }

                    var eventEntry = new EventEntry(title, "", location);
                    eventEntry.Times.Add(new When(startDate, endDate, allDay));
                    eventEntry = calendarService.Insert(calendarUri, eventEntry);

                    SendMessage(from, "I added your event :)");
                }
                else
                    SendMessage(from, "Sorry, I don't understand what you're talking about :(");
            }
        }

    }
}
