using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading;

namespace Saxx.Carmine {

    public delegate void EmptyDelegate();

    public class PluginManager {
        private string pluginDirectory = null;
        private FileSystemWatcher _fileSystemWatcher = null;
        private DateTime _changeTime = DateTime.MinValue;
        private Thread _reloadThread = null;
        private string _lockObject = "{PLUGINMANAGERLOCK}";
        private bool _beginShutdown = false;
        private bool _active = true;
        private LocalLoader _localLoader = null;

        public event EmptyDelegate OnPluginsReloaded;

        public PluginManager(string pluginRelativePath, bool autoReload) {
            AutoReload = autoReload;

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var currentDirectory = assemblyLocation.Substring(0, assemblyLocation.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            pluginDirectory = Path.Combine(currentDirectory, pluginRelativePath);
            if (!pluginDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
                pluginDirectory = pluginDirectory + Path.DirectorySeparatorChar;

            _localLoader = new LocalLoader(pluginDirectory);
        }

        ~PluginManager() {
            Stop();
        }

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
            var extension = Path.GetExtension(e.FullPath);
            if (extension.Equals(".dll", StringComparison.InvariantCultureIgnoreCase) || extension.Equals(".cs", StringComparison.InvariantCultureIgnoreCase) || extension.Equals(".vb", StringComparison.InvariantCultureIgnoreCase))
                _changeTime = DateTime.Now.AddSeconds(1);
        }

        private void ReloadThreadLoop() {
            while (IsStarted && !_beginShutdown) {
                if (_changeTime != DateTime.MinValue && DateTime.Now > _changeTime)
                    ReloadPlugins();
                Thread.Sleep(1000);
            }
            _active = false;
        }

        public void Start() {
            IsStarted = true;
            if (AutoReload) {
                _fileSystemWatcher = new FileSystemWatcher(pluginDirectory);
                _fileSystemWatcher.EnableRaisingEvents = true;
                _fileSystemWatcher.Changed += new FileSystemEventHandler(fileSystemWatcher_Changed);
                _fileSystemWatcher.Deleted += new FileSystemEventHandler(fileSystemWatcher_Changed);
                _fileSystemWatcher.Created += new FileSystemEventHandler(fileSystemWatcher_Changed);

                _reloadThread = new Thread(new ThreadStart(ReloadThreadLoop));
                _reloadThread.Start();
            }

            ReloadPlugins();
        }

        public void ReloadPlugins() {
            lock (_lockObject) {
                _localLoader.Unload();
                _localLoader = new LocalLoader(pluginDirectory);
                LoadAssemblies();

                _changeTime = DateTime.MinValue;
                if (OnPluginsReloaded != null)
                    OnPluginsReloaded();
            }
        }

        private void LoadAssemblies() {
            var directory = new DirectoryInfo(pluginDirectory);
            foreach (FileInfo file in directory.GetFiles("*.dll"))
                try {
                    _localLoader.LoadAssembly(file.FullName);
                }
                catch (PolicyException e) {
                    throw new PolicyException(String.Format("Unable to load assembly '" + file.Name + "'. The code probably requires privileges to execute."), e);
                }

            var files = new List<string>();
            foreach (FileInfo file in directory.GetFiles("*.cs"))
                files.Add(file.FullName);
            foreach (FileInfo file in directory.GetFiles("*.vb"))
                files.Add(file.FullName);
            LoadFiles(files);
        }

        private void LoadFiles(IEnumerable<string> files) {
            CompilerErrors = _localLoader.LoadFiles(files);
        }

        public void Stop() {
            IsStarted = false;
            _localLoader.Unload();
            _beginShutdown = true;
            while (_active)
                Thread.Sleep(100);
        }

        public IEnumerable<string> GetSubClasses(string baseClass) {
            return _localLoader.GetSubClasses(baseClass);
        }

        public MarshalByRefObject CreateInstance(string typeName, BindingFlags bindingFlags, object[] constructorParams) {
            return _localLoader.CreateInstance(typeName, bindingFlags, constructorParams);
        }

        #region Properties
        public bool AutoReload {
            get;
            private set;
        }

        private bool IsStarted {
            get;
            set;
        }

        public IEnumerable<string> CompilerErrors {
            get;
            private set;
        }
        #endregion

    }

}
