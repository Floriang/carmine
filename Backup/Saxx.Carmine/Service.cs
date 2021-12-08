using System.ServiceProcess;
using System;

namespace Saxx.Carmine {
    public partial class Service : ServiceBase {
        public Service() {
            InitializeComponent();
        }

        private Bot _bot;

        protected override void OnStart(string[] args) {
            Logger.Log(LogType.Info, "Starting in service mode");
            _bot = new Bot();
            _bot.Connect();
        }

        protected override void OnStop() {
            Logger.Log(LogType.Info, "Stopping from service mode");
            _bot.Disconnect();
        }
    }
}
