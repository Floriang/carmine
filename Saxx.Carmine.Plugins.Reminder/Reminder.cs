using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace Saxx.Carmine.Plugins {
    public class Reminder : Plugin {

        private Thread _thread;

        public override void Connected() {
            _thread = new Thread(new ThreadStart(delegate() {
                while (true) {
                    var reminderInfos = LoadReminderInfos().ToList();
                    var q = reminderInfos.Where(x => x.Date <= DateTime.Now);
                    if (q.Count() > 0) {
                        foreach (var reminderInfo in q)
                            Bot.SendMessage(reminderInfo.To, "Hey, don't forget: " + reminderInfo.Message);
                        SaveReminderInfos(reminderInfos.Where(x => !q.Contains(x)));
                    }
                    Thread.Sleep(30000);
                }
            }));
            _thread.Start();
        }

        public override void Disconnect() {
            if (_thread != null)
                _thread.Abort();
        }

        public override void Dispose() {
            if (_thread != null)
                _thread.Abort();
        }

        public override string ToString() {
            return "The Reminder plugin";
        }

        public override void Message(string from, string message) {
            var date = DateTime.Now;
            var reminderMessage = "";
            var reminderInfos = LoadReminderInfos().ToList();


            if (message.Equals("get-reminders", StringComparison.InvariantCultureIgnoreCase)) {
                PrintReminders(from);
                return;
            }
            else if (message.Equals("clear-all-reminders", StringComparison.InvariantCultureIgnoreCase) && Bot.IsOperator(from)) {
                SaveReminderInfos(new List<ReminderInfo>());
                Bot.SendMessage(from, "I deleted all reminders.");
                return;
            }
            else if (message.Equals("clear-reminders", StringComparison.InvariantCultureIgnoreCase)) {
                ClearReminders(from);
                return;
            }
            else if (message.Equals("syntax", StringComparison.InvariantCultureIgnoreCase)) {
                PrintSyntax(from);
                return;
            }

            var patterns = new string[] { 
                @"^remind me in( (?<hours>\d+):(?<minutes>\d+)){1}(?<message>.*?)$",
                @"^remind me in((?<days> \d+)d)?((?<hours> \d+)h)?((?<minutes> \d+)m)?(?<message>.*?)$", 
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
                SaveReminderInfos(reminderInfos);

                if (date.Date == DateTime.Now.Date)
                    Bot.SendMessage(from, "I'll remind you at " + date.ToString("HH:mm", CultureInfo.InvariantCulture) + " about: " + reminderMessage);
                else if (date.Year != DateTime.Now.Year)
                    Bot.SendMessage(from, "I'll remind you on " + date.ToString("dd. MMMM yyyy", CultureInfo.InvariantCulture) + " at " + date.ToString("HH:mm", CultureInfo.InvariantCulture) + " about: " + reminderMessage);
                else
                    Bot.SendMessage(from, "I'll remind you on " + date.ToString("dddd, dd. MMMM", CultureInfo.InvariantCulture) + " at " + date.ToString("HH:mm", CultureInfo.InvariantCulture) + " about: " + reminderMessage);
            }
        }

        private void PrintReminders(string from) {
            var result = "";
            var reminders = LoadReminderInfos();
            if (Bot.IsOperator(from)) {
                result += "*All reminders*" + Environment.NewLine;
            }
            else {
                reminders = reminders.Where(x => x.To.Equals(from, StringComparison.InvariantCultureIgnoreCase));
                result += "*Your reminders*" + Environment.NewLine;
            }

            foreach (var reminder in reminders)
                result += reminder.Date.ToString("dd.MM.yyyy HH:mm") + " - " + reminder.Message + (!reminder.To.Equals(from, StringComparison.InvariantCultureIgnoreCase) ? " (" + reminder.To + ")" : "") + Environment.NewLine;

            Bot.SendMessage(from, result);
        }

        private void ClearReminders(string from) {
            SaveReminderInfos(LoadReminderInfos().Where(x => !x.To.Equals(from, StringComparison.InvariantCultureIgnoreCase)));
            Bot.SendMessage(from, "I deleted all reminders of yours.");
        }

        private void PrintSyntax(string from) {
            var result = "*Reminder Syntax*" + Environment.NewLine + Environment.NewLine;

            result += "remind me in [<days>d] [<hours>h] [<minutes>m] [<message>]" + Environment.NewLine + "_adds a reminder to fire in the specified days, hours and minutes._" + Environment.NewLine + Environment.NewLine;
            result += "remind me in <hours>:<minutes> [<message>]" + Environment.NewLine + "_adds a reminder to fire in the specified hours and minutes._" + Environment.NewLine + Environment.NewLine;
            result += "remind me [on <day>.<month>.<year>] at <hour>:<minute> [<message>]" + Environment.NewLine + "_adds a reminder to fire at the specified date and time._" + Environment.NewLine + Environment.NewLine;
            result += "get-reminders" + Environment.NewLine + "_prints all reminders._" + Environment.NewLine + Environment.NewLine;
            result += "clear-reminders" + Environment.NewLine + "_deletes all reminders of yours._" + Environment.NewLine + Environment.NewLine;
            result += "clear-all-reminders" + Environment.NewLine + "_deletes all reminders (operators only)._" + Environment.NewLine + Environment.NewLine;

            Bot.SendMessage(from, result);
        }

        #region Read and write XML
        private void SaveReminderInfos(IEnumerable<ReminderInfo> reminderInfos) {
            var xml = new XDocument();
            xml.Add(
                    new XElement("reminders", from x in reminderInfos
                                              select new XElement("reminder", 
                                                  new XElement("to", x.To), 
                                                  new XElement("message", new XCData(x.Message)), 
                                                  new XElement("date", x.Date.ToString(CultureInfo.InvariantCulture))
                                              )
                                )
                    );
            xml.Save(XmlPath);
        }

        private IEnumerable<ReminderInfo> LoadReminderInfos() {
            var result = new List<ReminderInfo>();
            if (File.Exists(XmlPath)) {
                var xml = XDocument.Load(XmlPath);
                foreach (var node in xml.Element("reminders").Elements("reminder"))
                    result.Add(new ReminderInfo(node.Element("to").Value, node.Element("message").Value, DateTime.Parse(node.Element("date").Value, CultureInfo.InvariantCulture)));
            }
            return result.OrderBy(x => x.Date);
        }

        private string XmlPath {
            get {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Plugins\Reminder.xml");
            }
        }
        #endregion

    }
}
