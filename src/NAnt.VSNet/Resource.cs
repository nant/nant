// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.DotNet.Tasks;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class Resource {
        #region Public Instance Constructors

        public Resource(Project project, string resourceSourceFile, string resourceSourceFileRelativePath, string dependentFile, SolutionTask solutionTask) {
            _project = project;
            _projectSettings = project.ProjectSettings;
            _resourceSourceFile = resourceSourceFile;
            _resourceSourceFileRelativePath = resourceSourceFileRelativePath;
            _dependentFile = dependentFile;
            _solutionTask = solutionTask;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string Setting {
            get { return @"/res:""" + _resourceFile + @""""; }
        }

        public string InputFile {
            get { return _resourceSourceFile; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public void Compile(ConfigurationSettings configurationSettings, bool showCommands) {
            _configurationSettings = configurationSettings;

            FileInfo fiResource = new FileInfo(_resourceSourceFile);

            switch (fiResource.Extension.ToLower()) {
                case ".resx":
                    _resourceFile = CompileResx();
                    break;
                case ".licx":
                    _resourceFile = CompileLicx();
                    break;
                default:
                    _resourceFile = CompileResource();
                    break;
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private string GetDependentResourceName(string dependentFile) {
            switch (Path.GetExtension(dependentFile).ToLower()) {
                case ".cs":
                    return GetDependentResourceNameCSharp(dependentFile);
                case ".vb":
                    return GetDependentResourceNameVB(dependentFile);
                default:
                    throw new ArgumentException("Unknown file extension");
            }
        }

        private string GetDependentResourceNameCSharp(string dependentFile) {
            Regex re = new Regex(@"
                (?>namespace(?<ns>(.|\s)*?){)
                    |
                (?>class(?<class>.*?):)
                    |
                }
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);

            Match m;
            using (StreamReader sr = new StreamReader(dependentFile)) {
                m = re.Match(sr.ReadToEnd());
            }

            Stack st = new Stack();

            while (m.Success) {
                string strValue = m.Value;
                if (strValue.StartsWith( "namespace")) {
                    st.Push(m.Result("${ns}").Trim());
                } else if (strValue.StartsWith("class")) {
                    st.Push(m.Result("${class}").Trim());
                    break;
                } else if (strValue == "}") {
                    if (st.Count > 0) {
                        st.Pop();
                    }
                }
                
                m = m.NextMatch();
            }
        
            Stack stReverse = new Stack();
            while (st.Count > 0) {
                stReverse.Push(st.Pop());
            }

            ArrayList al = new ArrayList(stReverse.ToArray());

            string className = string.Join(".", (string[]) al.ToArray(typeof(string)));
            return className + ".resources";
        }

        private string GetDependentResourceNameVB(string dependentFile) {
            Regex re = new Regex(@"
                (?>^\s*?(?!End)\s*Namespace\s*(?<ns>.*)\s*?$)
                    |
                (?>^(?>\s*)(?!End)([\w\s](?=(?!$)))*Class\s*(?<class>.*?)\s*?$)
                    |
                ^\s*End\s*(?:(Class|Namespace))\s*?$
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled);

            Match m;
            using (StreamReader sr = new StreamReader(dependentFile)) {
                m = re.Match(sr.ReadToEnd());
            }

            Stack st = new Stack();

            while (m.Success) {
                string strValue = m.Value.Trim();
                if (strValue.StartsWith("End ")) {
                    if (st.Count > 0) {
                        st.Pop();
                    }
                } else if (strValue.StartsWith("Namespace")) {
                    st.Push(m.Result("${ns}").Trim());
                } else if (strValue.IndexOf("Class") >= 0) {
                    st.Push(m.Result("${class}").Trim());
                    break;
                }
                
                m = m.NextMatch();
            }
        
            Stack stReverse = new Stack();
            while (st.Count > 0) {
                stReverse.Push(st.Pop());
            }

            ArrayList al = new ArrayList(stReverse.ToArray());

            string className = string.Join(".", (string[]) al.ToArray(typeof(string)));
            return _projectSettings.RootNamespace + "." + className + ".resources";
        }

        private string CompileResource() {
            string outputFile = _projectSettings.GetTemporaryFilename(_projectSettings.RootNamespace + "." + _resourceSourceFileRelativePath.Replace("\\", "."));
            
            if (File.Exists(outputFile)) {
                File.SetAttributes(outputFile, FileAttributes.Normal);
                File.Delete(outputFile);
            }

            File.Copy(_resourceSourceFile, outputFile);
            return outputFile;
        }

        private string CompileLicx() {
            string outputFile = _projectSettings.OutputFile;

            LicenseTask lt = new LicenseTask();
            lt.Input = _resourceSourceFile;
            lt.Output = _projectSettings.GetTemporaryFilename(outputFile + ".licenses");
            lt.Target = outputFile;
            lt.Verbose = _solutionTask.Verbose;
            lt.Project = _solutionTask.Project;
            lt.Assemblies = new FileSet();

            foreach (Reference r in _project.References) {
                if (r.IsSystem) {
                    lt.Assemblies.AsIs.Add(r.Name);
                } else {
                    lt.Assemblies.Includes.Add(r.Filename);
                }
            }

            lt.Project.Indent();
            lt.Execute();
            lt.Project.Unindent();

            return lt.Output;
        }

        private string CompileResx() {
            string strInFile = _resourceSourceFile;
            string strOutFile;
            
            if (_dependentFile != null) {
                strOutFile = GetDependentResourceName(_dependentFile);
            } else {
                strOutFile = _projectSettings.RootNamespace + "." + Path.GetDirectoryName(_resourceSourceFileRelativePath).Replace("\\", ".") + "." + Path.GetFileNameWithoutExtension(_resourceSourceFile) + ".resources";
            }
            strOutFile = _projectSettings.GetTemporaryFilename(strOutFile);

            _solutionTask.Project.Indent();
            _solutionTask.Log(Level.Verbose, _solutionTask.LogPrefix + "ResGenTask Input: {0} Output: {1}", strInFile, strOutFile);
            _solutionTask.Project.Unindent();

            ResGenTask rt = new ResGenTask();
            rt.Input = strInFile;
            rt.Output = Path.GetFileName(strOutFile);
            rt.ToDirectory = Path.GetDirectoryName(strOutFile);
            rt.Verbose = false;
            rt.Project = _solutionTask.Project;
            rt.BaseDirectory = Path.GetDirectoryName(strInFile);
            rt.Project.Indent();
            rt.Execute();
            rt.Project.Unindent();

            return strOutFile;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _resourceFile;
        private string _resourceSourceFile;
        private string _dependentFile;
        private string _resourceSourceFileRelativePath;
        private ProjectSettings _projectSettings;
        private Project _project;
        private ConfigurationSettings _configurationSettings;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
