using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Saxx.Carmine.Plugins {
    public partial class Twitter : Plugin {

        public Twitter() {
            ServicePointManager.Expect100Continue = false; //we'd get a 417 error otherwise
        }

        public override void Message(string from, string message) {
            var match = Regex.Match(message, "^twitter register (?<username>.*?) (?<password>.*?)$", RegexOptions.IgnoreCase);
            if (match.Success) {
                var user = new UserData() {
                    Jid = from,
                    UserName = match.Groups["username"].Value,
                    Password = match.Groups["password"].Value,
                    LastId = 0
                };

                var userData = GetUserData().ToList();
                userData = userData.Where(x => !x.Jid.Equals(user.Jid, StringComparison.InvariantCultureIgnoreCase)).ToList();
                userData.Add(user);
                WriteTimeline(user, true);
                SetUserData(userData);
                return;
            }

            match = Regex.Match(message, "^twitter (?<status>.*?)$", RegexOptions.IgnoreCase);
            if (match.Success) {
                var status = match.Groups["status"].Value.Trim();
                if (!string.IsNullOrEmpty(status)) {
                    var userData = GetUserData();
                    var user = userData.FirstOrDefault(x => x.Jid.Equals(from, StringComparison.InvariantCultureIgnoreCase));
                    if (user != null) {
                        SetStatus(user, status);
                        WriteTimeline(user, false);
                        SetUserData(userData);
                    }
                    else
                        SendMessage(from, "You have to register your twitter credentials first.");
                }
                return;
            }

            if (message.Equals("twitter")) {
                var userData = GetUserData();
                var user = userData.FirstOrDefault(x => x.Jid.Equals(from, StringComparison.InvariantCultureIgnoreCase));
                if (user != null) {
                    WriteTimeline(user, true);
                    SetUserData(userData);
                }
                else
                    SendMessage(from, "You have to register your twitter credentials first.");
                return;
            }
        }

        public override void Tick() {
            var userData =  GetUserData().ToList();
            bool updateUserData = false;

            foreach (var user in userData) 
                if (IsAvailable(user.Jid)) {
                    WriteTimeline(user, false);
                    updateUserData = true;
                }

            if (updateUserData)
                SetUserData(userData);
        }

        private void SetStatus(UserData user, string status) {
            try {
                var request = WebRequest.Create("http://twitter.com/statuses/update.xml");
                request.Credentials = new NetworkCredential(user.UserName, user.Password);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                using (var writer = new StreamWriter(request.GetRequestStream()))
                    writer.Write("status=" + HttpUtility.UrlEncode(status));

                using (var reader = new StreamReader(request.GetResponse().GetResponseStream()))
                    reader.ReadToEnd();
            }
            catch (Exception ex) {
                SendMessage(user.Jid, "Error while updating your Twitter status: " + ex.Message);
                Log(LogType.Error, "Error while updating Twitter status.", ex);
            }
        }

        private void WriteTimeline(UserData user, bool answerAlways) {
            try {
                var request = WebRequest.Create("http://twitter.com/statuses/friends_timeline.xml" + (user.LastId > 0 ? "?since_id=" + user.LastId : ""));
                request.Credentials = new NetworkCredential(user.UserName, user.Password);

                var message = "";
                using (var reader = new StreamReader(request.GetResponse().GetResponseStream())) {
                    var xml = XDocument.Load(reader);
                    foreach (var element in xml.Root.Elements("status")) {
                        var text = element.Element("text").Value;
                        var from = element.Element("user").Element("screen_name").Value;
                        var id = Convert.ToInt32(element.Element("id").Value);

                        message = "*@" + from + ":* " + text + Environment.NewLine + Environment.NewLine + message;

                        user.LastId = Math.Max(user.LastId, id);
                    }
                }

                message = message.Trim();

                if (!string.IsNullOrEmpty(message))
                    SendMessage(user.Jid, message);
                else if (answerAlways)
                    SendMessage(user.Jid, "No new statuses available.");
            }
            catch (Exception ex) {
                SendMessage(user.Jid, "Error while downloading your Twitter timeline: " + ex.Message);
                Log(LogType.Error, "Error while downloading Twitter timeline.", ex);
            }
        }

        public override string ToString() {
            return "The Twitter plugin";
        }

    }
}
