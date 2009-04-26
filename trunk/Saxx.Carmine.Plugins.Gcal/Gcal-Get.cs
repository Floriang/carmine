﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Google.GData.Calendar;
using Google.GData.Client;

namespace Saxx.Carmine.Plugins {
    public partial class Gcal {

        private void PrintEvents(string from) {
            Log(LogType.Info, "Printing events for " + from);
            var userData = GetUserData(from);
            if (userData == null) {
                SendMessage(from, "No Google credentials found for you :(");
                Log(LogType.Info, "No Google credentials for " + from + " in the database");
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
            var calendars = GetCalendars(calendarService);


            foreach (var uri in calendars.Select(x => x.Uri)) {
                var eventQuery = new EventQuery(uri.ToString());
                eventQuery.StartTime = DateTime.Now.ToUniversalTime().Date;
                eventQuery.EndTime = DateTime.Now.ToUniversalTime().Date.AddDays(1);
                eventQuery.SortOrder = CalendarSortOrder.ascending;
                var eventFeed = calendarService.Query(eventQuery);
                foreach (EventEntry eventEntry in eventFeed.Entries) {
                    var evnt = new Event();
                    result.Add(evnt);

                    var eventTime = eventEntry.Times.FirstOrDefault();
                    if (eventTime != null && !eventTime.AllDay)
                        evnt.Time = eventTime.StartTime.ToString("HH:mm") + " - " + eventTime.EndTime.ToString("HH:mm");

                    evnt.Title = eventEntry.Title.Text;
                }
            }

            return result.OrderBy(x => x.Time);
        }
    }
}