using System;

namespace Saxx.Carmine.Plugins {
    internal class ReminderInfo {

        public ReminderInfo(string to, string message, DateTime date) {
            To = to;
            Message = message;
            Date = date;
        }
        
        public string To {
            get;
            private set;
        }

        public string Message {
            get;
            private set;
        }

        public DateTime Date {
            get;
            private set;
        }

    }
}
