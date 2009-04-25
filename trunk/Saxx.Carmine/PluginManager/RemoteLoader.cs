using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Saxx.Carmine {
    public class RemoteLoader : MarshalByRefObject {
        protected List<Type> _types = new List<Type>();
        protected List<Assembly> _assemblies = new List<Assembly>();

        public void LoadAssembly(string fullName) {
            var assembly = Assembly.Load(Path.GetFileNameWithoutExtension(fullName));
            _assemblies.Add(assembly);
            foreach (var type in assembly.GetTypes())
                _types.Add(type);
        }

        public IEnumerable<string> LoadFiles(IEnumerable<string> files) {
            var compiler = new Compiler();
            var assembly = compiler.GetAssembly(files);

            if (assembly != null) {
                _assemblies.Add(assembly);
                foreach (var type in assembly.GetTypes())
                    _types.Add(type);
            }

            var errors = new List<string>();
            if (compiler.CompilerErrors != null)
                foreach (CompilerError error in compiler.CompilerErrors)
                    errors.Add(error.ErrorText);
            return errors;
        }

        public IEnumerable<string> Types {
            get {
                return _types.Select(x => x.FullName);
            }
        }

        public IEnumerable<string> Assemblies {
            get {
                return _assemblies.Select(x => x.FullName);
            }
        }

        public IEnumerable<string> GetSubClasses(string baseClass) {
            var baseClassType = Type.GetType(baseClass);
            if (baseClassType == null) 
                baseClassType = GetTypeByName(baseClass);
            if (baseClassType == null) 
                throw new ArgumentException("There is no type '" + baseClass + "' within the plugins or the common library.");

            return _types.Where(x => x.IsSubclassOf(baseClassType)).Select(x => x.FullName).ToList();
        }

        public MarshalByRefObject CreateInstance(string typeName, BindingFlags bindingFlags, object[] constructorParams) {
            var owningAssembly = _assemblies.FirstOrDefault(x => x.GetType(typeName) != null);
            if (owningAssembly == null)
                throw new InvalidOperationException("There is no assembly for type '" + typeName + "'.");


            var instance = owningAssembly.CreateInstance(typeName, false, bindingFlags, null, constructorParams, null, null) as MarshalByRefObject;
            if (instance == null)
                throw new ArgumentException("Unable to create an instance for type '" + typeName + "'. Make sure it derives from MarshalByRefObject.");
            return instance;
        }

        private Type GetTypeByName(string typeName) {
            return _types.FirstOrDefault(x => x.FullName == typeName);
        }

    }
}
