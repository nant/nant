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
//
// Matthew Mastracci (matt@aclaro.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Util;
using NAnt.DotNet.Tasks;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class Resource {
        #region Public Instance Constructors

        public Resource(Project project, string resourceSourceFile, string resourceSourceFileRelativePath, string dependentFile, SolutionTask solutionTask) {
            _project = project;
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

        public Project Project {
            get { return _project; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public void Compile(ConfigurationSettings configurationSettings, bool showCommands) {
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
            string extension = Path.GetExtension(dependentFile);

            switch (extension.ToLower(CultureInfo.InvariantCulture)) {
                case ".cs":
                    return GetDependentResourceNameCSharp(dependentFile);
                case ".vb":
                    return GetDependentResourceNameVB(dependentFile);
                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                        "Unsupported file extension '{0}'.", extension));
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
                string value = m.Value;
                if (value.StartsWith("namespace")) {
                    st.Push(m.Result("${ns}").Trim());
                } else if (value.StartsWith("class")) {
                    st.Push(m.Result("${class}").Trim());
                    break;
                } else if (value == "}") {
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
                string value = m.Value.Trim();
                if (value.StartsWith("End ")) {
                    if (st.Count > 0) {
                        st.Pop();
                    }
                } else if (value.StartsWith("Namespace")) {
                    st.Push(m.Result("${ns}").Trim());
                } else if (value.IndexOf("Class") >= 0) {
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
            return Project.ProjectSettings.RootNamespace + "." + className + ".resources";
        }

        private string CompileResource() {
            string outputFile = Project.ProjectSettings.GetTemporaryFilename(
                Project.ProjectSettings.RootNamespace + "." + 
                _resourceSourceFileRelativePath.Replace("\\", "."));
            
            if (File.Exists(outputFile)) {
                File.SetAttributes(outputFile, FileAttributes.Normal);
                File.Delete(outputFile);
            }

            File.Copy(_resourceSourceFile, outputFile);
            return outputFile;
        }

        private string CompileLicx() {
            string outputFileName = Project.ProjectSettings.OutputFileName;

            // create instance of License task
            LicenseTask lt = new LicenseTask();

            // inherit project from solution task
            lt.Project = _solutionTask.Project;

            // inherit parent from solution task
            lt.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            lt.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            lt.InitializeTaskConfiguration();

            // set parent of child elements
            lt.Assemblies.Parent = lt;

            // inherit project from solution task for child elements
            lt.Assemblies.Project = lt.Project;

            // set task properties
            lt.Input = _resourceSourceFile;
            lt.Output = Project.ProjectSettings.GetTemporaryFilename(outputFileName + ".licenses");
            lt.Target = outputFileName;

            foreach (Reference reference in Project.References) {
                lt.Assemblies.Includes.Add(reference.Filename);
            }

            // execute task
            lt.Project.Indent();
            lt.Execute();
            lt.Project.Unindent();

            return lt.Output;
        }

        private string CompileResx() {
            string inFile = _resourceSourceFile;
            string outFile;
            
            if (!StringUtils.IsNullOrEmpty(_dependentFile)) {
                outFile = GetDependentResourceName(_dependentFile);
            } else {
                StringBuilder sb = new StringBuilder();
                if (!StringUtils.IsNullOrEmpty(Project.ProjectSettings.RootNamespace)) {
                    sb.Append(Project.ProjectSettings.RootNamespace);
                }
                if (!StringUtils.IsNullOrEmpty(Path.GetDirectoryName(_resourceSourceFileRelativePath))) {
                    sb.AppendFormat(".{0}", Path.GetDirectoryName(_resourceSourceFileRelativePath).Replace("\\", "."));
                }
                if (!StringUtils.IsNullOrEmpty(_resourceSourceFile)) {
                    sb.AppendFormat(".{0}", Path.GetFileNameWithoutExtension(_resourceSourceFile));
                }
                sb.Append(".resources");
                outFile = sb.ToString();
            }
            outFile = Project.ProjectSettings.GetTemporaryFilename(outFile);

            // create instance of ResGen task
            ResGenTask rt = new ResGenTask();

            // inherit project from solution task
            rt.Project = _solutionTask.Project;

            // inherit parent from solution task
            rt.Parent = _solutionTask.Parent;

            // inherit verbose setting from solution task
            rt.Verbose = _solutionTask.Verbose;

            // make sure framework specific information is set
            rt.InitializeTaskConfiguration();

            // set task properties
            rt.Input = inFile;
            rt.Output = Path.GetFileName(outFile);
            rt.ToDirectory = Path.GetDirectoryName(outFile);
            rt.BaseDirectory = Path.GetDirectoryName(inFile);

            // execute task
            rt.Project.Indent();
            rt.Execute();
            rt.Project.Unindent();

            return outFile;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _resourceFile;
        private string _resourceSourceFile;
        private string _dependentFile;
        private string _resourceSourceFileRelativePath;
        private Project _project;
        private SolutionTask _solutionTask;

        #endregion Private Instance Fields
    }
}
