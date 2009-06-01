using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using bedrock;
using jabber;
using jabber.client;
using jabber.connection.sasl;
using jabber.protocol.client;
using jabber.protocol.iq;
using System.Linq;

namespace Saxx.Carmine {
    public partial class Bot : MarshalByRefObject, IBot {

        private JabberClient _client;
        private RosterManager _rosterManager;
        private PresenceManager _presenceManager;
        private PluginManager _pluginManager;
        private Thread _thread;
        private bool _stopThread;

        private IEnumerable<Plugin> Plugins {
            get;
            set;
        }

        public Bot() {
            _client = new JabberClient();
            _client.AutoRoster = true;
            _client.AutoLogin = false;
            _client.Resource = "Carmine " + Environment.Version.ToString();

            _rosterManager = new RosterManager();
            _rosterManager.Stream = _client;
            _rosterManager.AutoAllow = AutoSubscriptionHanding.AllowAll;
            _rosterManager.AutoSubscribe = true;
            _rosterManager.OnRosterItem += new RosterItemHandler(_rosterManager_OnRosterItem);

            _presenceManager = new PresenceManager();
            _presenceManager.Stream = _client;
            _presenceManager.OnPrimarySessionChange += new PrimarySessionHandler(_presenceManager_OnPrimarySessionChange);

            _client.OnInvalidCertificate += new RemoteCertificateValidationCallback(client_OnInvalidCertificate);
            _client.OnError += new bedrock.ExceptionHandler(client_OnError);
            _client.OnMessage += new MessageHandler(client_OnMessage);
            _client.OnDisconnect += new ObjectHandler(client_OnDisconnect);

            _client.OnLoginRequired += new ObjectHandler(client_OnLoginRequired);
            _client.OnRegisterInfo += new RegisterInfoHandler(client_OnRegisterInfo);
            _client.OnRegistered += new IQHandler(client_OnRegistered);

            InitPlugins();

            _thread = new Thread(new ThreadStart(delegate() {
                var timeLeft = Settings.TickInterval;
                while (!_stopThread) {
                    if (timeLeft <= 0) {
                        foreach (var plugin in Plugins)
                            try {
                                plugin.Tick();
                            }
                            catch (Exception ex) {
                                Log(LogType.Error, "A plugin threw an exception: ", ex);
                            }
                        timeLeft = Settings.TickInterval;
                    }
                    timeLeft -= 1000;
                    Thread.Sleep(1000); //we don't sleep the entire tick interval, because ending the bot would take too long then.
                }
            }));
        }

        void _presenceManager_OnPrimarySessionChange(object sender, JID bare) {
            if (bare.Bare.Equals(_client.JID.Bare, StringComparison.InvariantCultureIgnoreCase))
                return;

            if (_presenceManager[bare] == null) {
                foreach (var plugin in Plugins)
                    try {
                        plugin.ContactWentOffline(bare.ToString());
                    }
                    catch (Exception ex) {
                        Log(LogType.Error, "A plugin threw an exception: ", ex);
                    }
            }
            else {
                foreach (var plugin in Plugins)
                    try {
                        plugin.ContactWentOnline(bare.ToString());
                    }
                    catch (Exception ex) {
                        Log(LogType.Error, "A plugin threw an exception: ", ex);
                    }
            }
        }

        void _rosterManager_OnRosterItem(object sender, Item ri) {
            if (ri.Subscription == Subscription.from) //to fix some strange issue with GTalk contacts not showing fully subscribed
                _client.Subscribe(ri.JID, ri.Nickname, ri.GetGroups().Select(x => x.Name).ToArray());
        }

        public bool IsAvailable(string jid) {
            return _presenceManager.IsAvailable(jid);
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

        public void Connect() {
            _pluginManager.Start();

            Log(LogType.Info, "Connecting");
            _client.User = Settings.JabberUser;
            _client.Server = Settings.JabberServer;
            _client.Password = Settings.JabberPassword;
            _client.Connect();

            var retryCount = 50;
            while (!_client.IsAuthenticated && retryCount-- > 0)
                Thread.Sleep(500);

            if (_client.IsAuthenticated) {
                Log(LogType.Info, "Authenticated");
                SetStatus("I'm online.");

                foreach (var plugin in Plugins)
                    try {
                        plugin.Connected();
                    }
                    catch (Exception ex) {
                        Log(LogType.Error, "A plugin threw an exception: ", ex);
                    }

                _stopThread = false;
                _thread.Start();
            }
        }

        public void Disconnect() {
            Log(LogType.Info, "Disconnecting");
            _client.Close();
            _pluginManager.Stop();

            _stopThread = true;
        }

        private void client_OnDisconnect(object sender) {
            foreach (var plugin in Plugins)
                try {
                    plugin.Disconnect();
                }
                catch (Exception ex) {
                    Log(LogType.Error, "A plugin threw an exception: ", ex);
                }

            Log(LogType.Info, "Disconnected");
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
            if (ex is AuthenticationFailedException) {
                Log(LogType.Fatal, "Authentication failed. Check your Jabber credentials. The user you want to register probably already exists on the server.");
                Environment.Exit(-1);
            }
            Log(LogType.Fatal, "The Jabber client threw an exception: ", ex);
        }

        void client_OnRegistered(object sender, IQ iq) {
            Log(LogType.Info, "Logging in");
            _client.Login();
        }

        bool client_OnRegisterInfo(object sender, Register register) {
            return true;
        }

        void client_OnLoginRequired(object sender) {
            Log(LogType.Info, "Registering");
            _client.Register(new JID(Settings.JabberUser, Settings.JabberServer, null));
        }

        //required for the pass-through to the other appdomain
        public override object InitializeLifetimeService() {
            return null;
        }

    }
}
