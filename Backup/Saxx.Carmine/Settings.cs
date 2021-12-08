using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace Saxx.Carmine {
    public static class Settings {

        public static string Get(string key, string defaultValue) {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings[key]))
                return defaultValue;
            return ConfigurationManager.AppSettings[key];
        }

        public static int Get(string key, int defaultValue) {
            return int.Parse(Get(key, defaultValue.ToString()));
        }

        public static bool Get(string key, bool defaultValue) {
            return bool.Parse(Get(key, defaultValue.ToString()));
        }

        public static string JabberUser {
            get {
                return Get("user", "Carmine");
            }
        }

        public static string JabberServer {
            get {
                return Get("server", "jabber.org");
            }
        }

        public static string JabberPassword {
            get {
                return Get("password", "");
            }
        }

        public static string PluginsDirectory {
            get {
                return Path.Combine(RootDirectory, "Plugins");
            }
        }

        public static string RootDirectory {
            get {
                return Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            }
        }

        public static int TickInterval {
            get {
                return Get("tick-interval", 30000);
            }
        }
    }
}
