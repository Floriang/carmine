using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace Saxx.Carmine {
    public class Compiler {
        private CompilerErrorCollection _errors;

        public Assembly GetAssembly(IEnumerable<string> files) {
            _errors = null;
            if (files.Count() <= 0)
                return null;

            string fileType = null;
            foreach (var fileName in files) {
                var extension = Path.GetExtension(fileName);
                if (fileType == null)
                    fileType = extension;
                else if (fileType != extension)
                    throw new ArgumentException("All files in the file list must be of the same type.");
            }

            CodeDomProvider codeProvider = null;
            switch (fileType) {
                case ".cs":
                    codeProvider = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
                    break;
                case ".vb":
                    codeProvider = new VBCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } });
                    break;
                default:
                    throw new InvalidOperationException("Only .cs or .vb files supported.");
            }

            var compilerParams = new CompilerParameters() {
                CompilerOptions = "/target:library",
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = true
            };

            compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
            compilerParams.ReferencedAssemblies.Add("system.dll");
            compilerParams.ReferencedAssemblies.Add("system.core.dll");
            compilerParams.ReferencedAssemblies.Add("system.configuration.dll");
            compilerParams.ReferencedAssemblies.Add("system.data.dll");
            compilerParams.ReferencedAssemblies.Add("system.design.dll");
            compilerParams.ReferencedAssemblies.Add("system.directoryservices.dll");
            compilerParams.ReferencedAssemblies.Add("system.drawing.design.dll");
            compilerParams.ReferencedAssemblies.Add("system.drawing.dll");
            compilerParams.ReferencedAssemblies.Add("system.enterpriseservices.dll");
            compilerParams.ReferencedAssemblies.Add("system.management.dll");
            compilerParams.ReferencedAssemblies.Add("system.runtime.remoting.dll");
            compilerParams.ReferencedAssemblies.Add("system.runtime.serialization.formatters.soap.dll");
            compilerParams.ReferencedAssemblies.Add("system.security.dll");
            compilerParams.ReferencedAssemblies.Add("system.serviceprocess.dll");
            compilerParams.ReferencedAssemblies.Add("system.web.dll");
            compilerParams.ReferencedAssemblies.Add("system.web.regularexpressions.dll");
            compilerParams.ReferencedAssemblies.Add("system.web.services.dll");
            compilerParams.ReferencedAssemblies.Add("system.windows.forms.dll");
            compilerParams.ReferencedAssemblies.Add("system.xml.dll");
            compilerParams.ReferencedAssemblies.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "saxx.carmine.shared.dll"));

            var compilerResults = codeProvider.CompileAssemblyFromFile(compilerParams, files.ToArray());

            if (compilerResults.Errors.Count > 0)
                _errors = compilerResults.Errors;

            Assembly compiledAssembly = null;
            try {
                compiledAssembly = compilerResults.CompiledAssembly;
            }
            catch {
            }
            return compiledAssembly;
        }

        public CompilerErrorCollection CompilerErrors {
            get {
                return _errors;
            }
        }
    }
}
