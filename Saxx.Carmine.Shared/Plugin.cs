using System;

namespace Saxx.Carmine {
    public abstract class Plugin : MarshalByRefObject, IDisposable {

        public virtual void Initialize(IBot bot) {
            Bot = bot;
        }

        public virtual void Connected() {
        }

        public virtual void Disconnect() {
        }

        public virtual void Message(string from, string message) {
        }

        public virtual void ContactWentOnline(string from) {
        }

        public virtual void ContactWentOffline(string from) {
        }

        public virtual void Dispose() {
        }

        public virtual void Tick() {
        }

        protected IBot Bot {
            get;
            set;
        }

        #region Convenience methods just used to pass-through to the Bot
        public IDatabase GetDatabase(string fileName) {
            return Bot.GetDatabase(fileName);
        }

        public void SendMessage(string to, string message) {
            Bot.SendMessage(to, message);
        }

        public void SendMessageXHTML(string to, string message) {
            Bot.SendMessageXHTML(to, message);
        }

        public void SetStatus(string newStatus) {
            Bot.SetStatus(newStatus);
        }

        public bool IsOperator(string jid) {
            return Bot.IsOperator(jid);
        }

        public void Log(LogType type, string message, Exception ex) {
            Bot.Log(type, message, ex);
        }

        public void Log(LogType type, string message) {
            Bot.Log(type, message);
        }

        public bool IsAvailable(string jid) {
            return Bot.IsAvailable(jid);
        }
        #endregion

        //required, or the plugins will be removed from the remoting server after a while
        public override object InitializeLifetimeService() {
            return null;
        }
    }
}
