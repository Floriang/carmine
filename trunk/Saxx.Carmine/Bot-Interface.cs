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
