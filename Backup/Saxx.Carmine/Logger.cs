using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Config;

[assembly: XmlConfigurator(Watch = true)]
namespace Saxx.Carmine {
    public static class Logger {

        private static readonly ILog log = LogManager.GetLogger(typeof(Bot));

        public static void Log(LogType type, string message) {
            Log(type, message, null);
        }

        public static void Log(LogType type, string message, Exception ex) {
            switch (type) {
                case LogType.Debug:
                    log.Debug(message, ex);
                    break;
                case LogType.Error:
                    log.Error(message, ex);
                    break;
                case LogType.Fatal:
                    log.Fatal(message, ex);
                    break;
                case LogType.Info:
                    log.Info(message, ex);
                    break;
                case LogType.Warn:
                    log.Warn(message, ex);
                    break;
            }
        }

    }
}
