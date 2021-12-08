using System;
using System.Collections.Generic;
using System.Linq;
using jabber.protocol.client;

namespace Saxx.Carmine {
    public partial class Bot : IBot {

        public void SendMessage(string to, string message) {
            Log(LogType.Info, "Sending message to " + to + ": " + message + "");
            _client.Message(to, message);
        }

        public void SendMessageXHTML(string to, string message) {
            Log(LogType.Info, "Sending XHTML message to " + to + ": " + message);

            Message msg = new Message(_client.Document);
            msg.Type = MessageType.chat; // we send a chat message, not a normal message
            msg.Body = System.Text.RegularExpressions.Regex.Replace(message, @"(<\/?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)\/?>)|()", ""); // deleting XHTML tags => Convert XHTML to text
            msg.To = to;
            msg.Html = message;

            _client.Write(msg);
        }

        public void SetStatus(string status) {
            Log(LogType.Info, "Setting status to " + status + "");
            _client.Presence(PresenceType.available, status, null, 0);
        }

        public bool IsOperator(string from) {
            return Settings.Get("operators", "").Split(new char[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries).Any(x => x.Equals(from, System.StringComparison.InvariantCultureIgnoreCase));
        }

        public void Log(LogType type, string message) {
            Logger.Log(type, message);
        }

        public void Log(LogType type, string message, Exception ex) {
            Logger.Log(type, message, ex);
        }

        public IDatabase GetDatabase(string fileName) {
            return new Database(fileName);
        }


    }

}
