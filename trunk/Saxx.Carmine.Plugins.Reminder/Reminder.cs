using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saxx.Carmine.Plugins {
    public partial class Reminder : Plugin {

        private DateTime? _nextReminder;
        public override void Tick() {
            if (_nextReminder.HasValue && DateTime.Now < _nextReminder.Value)
                return;

            var reminderInfos = GetReminderInfos().ToList();
            var q = reminderInfos.Where(x => x.Date <= DateTime.Now).ToList();
            if (q.Count() > 0) {
                foreach (var reminderInfo in q)
                    Bot.SendMessage(reminderInfo.Jid, "Hey, don't forget: " + reminderInfo.Message);
                SetReminderInfos(reminderInfos.Where(x => !q.Contains(x)));
            }
        }

        public override string ToString() {
            return "The Reminder plugin";
        }

        public override void Message(string from, string message) {
            var date = DateTime.Now;
            var reminderMessage = "";
            var reminderInfos = GetReminderInfos().ToList();

            if (message.Equals("remind get", StringComparison.InvariantCultureIgnoreCase)) {
                SendMessage(from, PrintReminders(from));
                return;
            }
            else if (message.Equals("remind get all", StringComparison.InvariantCultureIgnoreCase) && Bot.IsOperator(from)) {
                SendMessage(from, PrintReminders());
                return;
            }
            else if (message.Equals("remind clear all", StringComparison.InvariantCultureIgnoreCase) && Bot.IsOperator(from)) {
                SetReminderInfos(new List<ReminderInfo>());
                Bot.SendMessage(from, "I deleted all reminders.");
                return;
            }
            else if (message.Equals("remind clear", StringComparison.InvariantCultureIgnoreCase)) {
                ClearReminders(from);
                return;
            }

            var patterns = new string[] { 
                @"^remind me in( (?<hours>\d+):(?<minutes>\d+)){1}(?<message>.*?)$",
                @"^remind me in((?<days> \d+)d)?((?<hours> \d+)h)?((?<minutes> \d+)m)?(?<message>.*?)$", 
                @"^remind me in (?<minutes>\d{1,4})(?<message>.*?)$",
                @"^remind me on (?<day>\d{1,2}).(?<month>\d{1,2}).(?<year>\d{4}) at (?<hour>\d{1,2}):(?<minute>\d{1,2})(?<message>.*?)$",
                @"^remind me on (?<day>\d{1,2}).(?<month>\d{1,2}). at (?<hour>\d{1,2}):(?<minute>\d{1,2})(?<message>.*?)$",
                @"^remind me at (?<hour>\d{1,2}):(?<minute>\d{1,2})(?<message>.*?)$"
            };

            foreach (var pattern in patterns) {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = regex.Match(message);
                if (regex.IsMatch(message)) {
                    var year = date.Year;
                    var month = date.Month;
                    var day = date.Day;
                    var hour = date.Hour;
                    var minute = date.Minute;
                    var daySpecified = false;
                    var yearSpecified = false;


                    if (match.Groups["minute"] != null && !string.IsNullOrEmpty(match.Groups["minute"].Value))
                        minute = int.Parse(match.Groups["minute"].Value);

                    if (match.Groups["hour"] != null && !string.IsNullOrEmpty(match.Groups["hour"].Value))
                        hour = int.Parse(match.Groups["hour"].Value);

                    if (match.Groups["day"] != null && !string.IsNullOrEmpty(match.Groups["day"].Value)) {
                        daySpecified = true;
                        day = int.Parse(match.Groups["day"].Value);
                    }

                    if (match.Groups["month"] != null && !string.IsNullOrEmpty(match.Groups["month"].Value))
                        month = int.Parse(match.Groups["month"].Value);

                    if (match.Groups["year"] != null && !string.IsNullOrEmpty(match.Groups["year"].Value)) {
                        year = int.Parse(match.Groups["year"].Value);
                        yearSpecified = true;
                    }

                    try {
                        date = new DateTime(year, month, day, hour, minute, 0);
                    }
                    catch {
                        Bot.SendMessage(from, "Can't do, this is not a valid date.");
                        return;
                    }

                    if (match.Groups["minutes"] != null && !string.IsNullOrEmpty(match.Groups["minutes"].Value))
                        date = date.AddMinutes(int.Parse(match.Groups["minutes"].Value));

                    if (match.Groups["hours"] != null && !string.IsNullOrEmpty(match.Groups["hours"].Value))
                        date = date.AddHours(int.Parse(match.Groups["hours"].Value));

                    if (match.Groups["days"] != null && !string.IsNullOrEmpty(match.Groups["days"].Value))
                        date = date.AddDays(int.Parse(match.Groups["days"].Value));
                    
                    if (!daySpecified && date < DateTime.Now)
                        date = date.AddDays(1); //use the next day, when only the time is specified.
                    else if (!yearSpecified && date < DateTime.Now)
                        date = date.AddYears(1); //use the next year, when only day and month are specified.

                    reminderMessage = match.Groups["message"].Value.Trim();
                    if (string.IsNullOrEmpty(reminderMessage))
                        reminderMessage = "Something.";
                    if (reminderMessage.ToLower().StartsWith("about "))
                        reminderMessage = reminderMessage.Substring(6);

                    break;
                }
            }

            if (!string.IsNullOrEmpty(reminderMessage)) {
                reminderInfos.Add(new ReminderInfo(from, reminderMessage, date));
                SetReminderInfos(reminderInfos);

                if (date.Date == DateTime.Now.Date)
                    Bot.SendMessage(from, "I'll remind you at " + date.ToString("HH:mm", CultureInfo.InvariantCulture) + " about: " + reminderMessage);
                else if (date.Year != DateTime.Now.Year)
                    Bot.SendMessage(from, "I'll remind you on " + date.ToString("dd. MMMM yyyy", CultureInfo.InvariantCulture) + " at " + date.ToString("HH:mm", CultureInfo.InvariantCulture) + " about: " + reminderMessage);
                else
                    Bot.SendMessage(from, "I'll remind you on " + date.ToString("dddd, dd. MMMM", CultureInfo.InvariantCulture) + " at " + date.ToString("HH:mm", CultureInfo.InvariantCulture) + " about: " + reminderMessage);
            }
        }

        private string PrintReminders(string from) {
            var reminders = GetReminderInfos().Where(x => x.Jid.Equals(from, StringComparison.InvariantCultureIgnoreCase));
            var result = "*Your reminders:*" + Environment.NewLine;

            foreach (var reminder in reminders)
                result += reminder.Date.ToString("dd.MM.yyyy HH:mm") + " - " + reminder.Message + Environment.NewLine;

            return result;
        }

        private string PrintReminders() {
            var reminders = GetReminderInfos();
            var result = "*All reminders:*" + Environment.NewLine;

            foreach (var reminder in reminders)
                result += reminder.Date.ToString("dd.MM.yyyy HH:mm") + " - " + reminder.Message + " (" + reminder.Jid + ")" + Environment.NewLine;

            return result;
        }

        private void ClearReminders(string from) {
            SetReminderInfos(GetReminderInfos().Where(x => !x.Jid.Equals(from, StringComparison.InvariantCultureIgnoreCase)));
            Bot.SendMessage(from, "I deleted all reminders of yours.");
        }

    }
}
