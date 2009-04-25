using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using jabber.client;
using jabber.protocol.client;

namespace Saxx.Carmine {
    public partial class Bot : MarshalByRefObject, IBot {

        private JabberClient _client;
        private RosterManager _rosterManager;
        private PresenceManager _presenceManager;
        private PluginManager _pluginManager;

        private IEnumerable<Plugin> Plugins {
            get;
            set;
        }

        public Bot() {
            _client = new JabberClient();
            _client.AutoRoster = true;
            _client.AutoReconnect = 10;

            _rosterManager = new RosterManager();
            _rosterManager.Stream = _client;
            _rosterManager.AutoAllow = AutoSubscriptionHanding.AllowAll;
            _rosterManager.AutoSubscribe = true;

            _presenceManager = new PresenceManager();
            _presenceManager.Stream = _client;
       
            _client.OnInvalidCertificate += new RemoteCertificateValidationCallback(client_OnInvalidCertificate);
            _client.OnError += new bedrock.ExceptionHandler(client_OnError);
            _client.OnMessage += new MessageHandler(client_OnMessage);
            _client.OnDisconnect += new bedrock.ObjectHandler(client_OnDisconnect);
            
            InitPlugins();
        }

        private void InitPlugins() {
            Log(LogType.Info, "Initiating plugins");

            _pluginManager = new PluginManager(Settings.PluginsDirectory.Substring(Settings.PluginsDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1), true);
            _pluginManager.OnPluginsReloaded += delegate() {
                 Log(LogType.Info, "Loading plugins");

                Plugins = new List<Plugin>();
                foreach (var pluginName in _pluginManager.GetSubClasses(typeof(Plugin).FullName)) {
                    var plugin = _pluginManager.CreateInstance(pluginName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance, new object[] { });
                    Log(LogType.Info, "Plugin '" + plugin + "' loaded");
                    ((List<Plugin>)Plugins).Add((Plugin)plugin);
                }

                foreach (var compilerError in _pluginManager.CompilerErrors)
                    Log(LogType.Error, "A compiler error has occured: " + compilerError);

                foreach (var plugin in Plugins)
                    try {
                        plugin.Initialize(this);
                    }
                    catch (Exception ex) {
                        Log(LogType.Error, "A plugin threw an exception: ", ex);
                    }

            };
        }

        public void Connect(string user, string server, string password) {
            _pluginManager.Start();

            Log(LogType.Info, "Connecting");
            _client.User = user;
            _client.Server = server;
            _client.Password = password;
            _client.Connect();

            while (!_client.IsAuthenticated)
                Thread.Sleep(500);

            SetStatus("I'm online.");

            foreach (var plugin in Plugins)
                try {
                    plugin.Connected();
                }
                catch (Exception ex) {
                    Log(LogType.Error, "A plugin threw an exception: ", ex);
                }
        }

        public void Disconnect() {
            Log(LogType.Info, "Disconnecting");
            _client.Close();
            _pluginManager.Stop();
        }

        private void client_OnDisconnect(object sender) {
            foreach (var plugin in Plugins)
                try {
                    plugin.Disconnect();
                }
                catch (Exception ex) {
                    Log(LogType.Error, "A plugin threw an exception: ", ex);
                }
        }

        private void client_OnMessage(object sender, Message msg) {
            Log(LogType.Info, "Message received from " + msg.From.User + "@" + msg.From.Server + ": " + msg.Body);
            foreach (var plugin in Plugins)
                try {
                    plugin.Message(msg.From.User + "@" + msg.From.Server, msg.Body);
                }
                catch (Exception ex) {
                    Log(LogType.Error, "A plugin threw an exception: ", ex);
                }
        }

        private bool client_OnInvalidCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            return true;
        }

        private void client_OnError(object sender, Exception ex) {
            Log(LogType.Fatal, "The client threw an exception: ", ex);
        }

        //required for the pass-through to the other appdomain
        public override object InitializeLifetimeService() {
            return null;
        }

    }
}
