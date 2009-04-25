using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Saxx.Carmine {
    public class LocalLoader : MarshalByRefObject {
        AppDomain _appDomain;
        RemoteLoader _remoteLoader;

        public LocalLoader(string pluginDirectory) {
            var setup = new AppDomainSetup() {
                ApplicationName = "Plugins",
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                PrivateBinPath = Path.GetDirectoryName(pluginDirectory).Substring(Path.GetDirectoryName(pluginDirectory).LastIndexOf(Path.DirectorySeparatorChar) + 1),
                CachePath = Path.Combine(pluginDirectory, "Cache" + Path.DirectorySeparatorChar),
                ShadowCopyFiles = "true",
                ShadowCopyDirectories = pluginDirectory
            };

            _appDomain = AppDomain.CreateDomain("Plugins", null, setup);
            _remoteLoader = (RemoteLoader)_appDomain.CreateInstanceAndUnwrap("Saxx.Carmine", "Saxx.Carmine.RemoteLoader");
        }


        public void LoadAssembly(string filename) {
            _remoteLoader.LoadAssembly(filename);
        }

        public IEnumerable<string> LoadFiles(IEnumerable<string> files) {
            return _remoteLoader.LoadFiles(files);
        }

        public void Unload() {
            if (_appDomain != null)
                AppDomain.Unload(_appDomain);
            _appDomain = null;
        }

        public IEnumerable<string> GetSubClasses(string baseClass) {
            return _remoteLoader.GetSubClasses(baseClass);
        }

        public MarshalByRefObject CreateInstance(string typeName, BindingFlags bindingFlags, object[] constructorParams) {
            return _remoteLoader.CreateInstance(typeName, bindingFlags, constructorParams);
        }

    }
}
