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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.VSNet.Tasks;
using NAnt.VSNet.Types;

namespace NAnt.VSNet {
    public class Solution {
        #region Public Instance Constructors

        public Solution(string solutionFileName, ArrayList additionalProjects, ArrayList referenceProjects, TempFileCollection tfc, SolutionTask solutionTask, WebMapCollection webMappings, FileSet excludesProjects, string outputDir) {
            _fileName = solutionFileName;
            _htProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htProjectDirectories = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htOutputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htProjectFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htProjectDependencies = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htReferenceProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _tfc = tfc;
            _solutionTask = solutionTask;
            _outputDir = outputDir;
            _excludesProjects = excludesProjects;
            _webMaps = webMappings;

            string fileContents;

            using (StreamReader sr = new StreamReader(solutionFileName)) {
                fileContents = sr.ReadToEnd();
            }

            Regex re = new Regex(@"Project\(\""(?<package>\{.*?\})\"".*?\""(?<name>.*?)\"".*?\""(?<project>.*?)\"".*?\""(?<guid>.*?)\""");
            MatchCollection mc = re.Matches(fileContents);
            FileInfo fiSolution = new FileInfo(solutionFileName);

            foreach (Match m in mc) {
                string project = m.Groups["project"].Value;
                string guid = m.Groups["guid"].Value;
                string fullPath;

                // only C#, VB.NET and C++ projects are supported at this moment
                if (!ProjectFactory.IsSupportedProjectType(project)) {

                    // output a warning message in the build log
                    Log(Level.Warning, LogPrefix + "Only C#, VB.NET and C++ projects" +
                        " are supported.  Skipping project '{0}'.", project);

                    // skip the project
                    continue;
                }

                // translate URLs to physical paths if using a webmap
                WebMap map = _webMaps[project];
                if (map != null && map.IfDefined && !map.UnlessDefined) {
                    project = map.Path;
                }

                try {
                    Uri uri = new Uri(project);
                    if (uri.Scheme == Uri.UriSchemeFile) {
                        fullPath = Path.Combine(fiSolution.DirectoryName, uri.LocalPath);
                    } else {
                        fullPath = project;
                    }
                } catch (UriFormatException) {
                    fullPath = Path.Combine(fiSolution.DirectoryName, project);
                }
                
                if (Project.IsEnterpriseTemplateProject(fullPath)) {
                    RecursiveLoadTemplateProject(fullPath);
                } else {
                    _htProjectFiles[guid] = fullPath;
                }
            }

            Regex reDependencies = new Regex(@"^\s+(?<guid>\{[0-9a-zA-Z]{8}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{12}\}).\d+\s+=\s+(?<dep>\{[0-9a-zA-Z]{8}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{12}\})", RegexOptions.Multiline);
            mc = reDependencies.Matches(fileContents);

            foreach (Match m in mc) {
                string guid = m.Groups["guid"].Value;
                string dependency = m.Groups["dep"].Value;
                AddProjectDependency(guid, dependency);
            }

            LoadProjectGUIDs(additionalProjects, false);
            LoadProjectGUIDs(referenceProjects, true);
            LoadProjects();
            GetDependenciesFromProjects();
        }

        public Solution(ArrayList projects, ArrayList referenceProjects, TempFileCollection tfc, SolutionTask solutionTask, WebMapCollection webMaps, FileSet excludesProjects, string outputDir) {
            _htProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htProjectDirectories = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htOutputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htProjectFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htProjectDependencies = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htReferenceProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _tfc = tfc;
            _solutionTask = solutionTask;
            _excludesProjects = excludesProjects;
            _webMaps = webMaps;
            _outputDir = outputDir;

            LoadProjectGUIDs(projects, false);
            LoadProjectGUIDs(referenceProjects, true);
            LoadProjects();
            GetDependenciesFromProjects();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string FileName {
            get { return _fileName; }
        }

        #endregion Public Instance Properties

        #region Private Instance Properties

        private string LogPrefix {
            get { 
                if (_solutionTask != null) {
                    return _solutionTask.LogPrefix;
                }

                return string.Empty;
            }
        }

        #endregion Private Instance Properties

        #region Public Instance Methods

        public void RecursiveLoadTemplateProject(string fileName) {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            foreach (XmlNode node in doc.SelectNodes("//Reference")) {
                string subProjectFilename = node.SelectSingleNode("FILE").InnerText;
                string guid = node.SelectSingleNode("GUIDPROJECTID").InnerText;

                string fullPath = Path.Combine(Path.GetDirectoryName(fileName), subProjectFilename);
                if (Project.IsEnterpriseTemplateProject(fullPath)) {
                    RecursiveLoadTemplateProject(fullPath);
                } else {
                    _htProjectFiles[guid] = fullPath;
                }
            }
        }

        public string GetProjectFileFromGUID(string projectGUID) {
            return (string) _htProjectFiles[projectGUID];
        }

        public ProjectBase GetProjectFromGUID(string projectGUID) {
            return (ProjectBase) _htProjects[projectGUID];
        }

        public bool Compile(string configuration, ArrayList compilerArguments, string logFile, bool verbose, bool showCommands) {
            Hashtable htDeps = (Hashtable) _htProjectDependencies.Clone();
            Hashtable htProjectsDone = CollectionsUtil.CreateCaseInsensitiveHashtable();
            Hashtable htFailedProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            bool success = true;

            while (true) {
                bool compiledThisRound = false;

                foreach (ProjectBase p in _htProjects.Values) {
                    if (htProjectsDone.Contains(p.GUID)) {
                        continue;
                    }

                    if (GetProjectDependencies(p.GUID).Length == 0) {
                        bool failed = htFailedProjects.Contains(p.GUID);

                        if (!failed) {
                            // Fixup references
                            Log(Level.Verbose, LogPrefix + "Fixing up references...");

                            foreach (Reference reference in p.References) {
                                // store original reference filename
                                string originalReference = reference.Filename;

                                // resolving path, where reference file is (find that file in search paths)
                                reference.ResolveFolder();

                                if (reference.IsProjectReference) {
                                    ProjectBase pRef = GetProjectFromGUID(reference.Project.GUID);
                                    if (pRef == null) {
                                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                            "Unable to locate referenced project '{0}' while loading '{1}'.",
                                            reference.Name, p.Name), Location.UnknownLocation);
                                    }
                                    string outputFile = pRef.GetOutputFile(configuration);
                                    if (outputFile == null) {
                                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                            "Unable to find '{0}' configuration for project '{1}'.",
                                            configuration, pRef.Name), Location.UnknownLocation);
                                    }
                                    reference.Filename = outputFile;
                                } else if (_htOutputFiles.Contains(reference.Filename)) {
                                    ProjectBase pRef = (ProjectBase) _htProjects[(string) _htOutputFiles[reference.Filename]];
                                    if (pRef != null) {
                                        reference.Filename = pRef.GetOutputFile(configuration);
                                    }
                                }

                                // only output message when reference has actually been fixed up
                                if (originalReference != reference.Filename) {
                                    Log(Level.Verbose, LogPrefix + "Fixed reference '{0}': {1} -> {2}.", 
                                        reference.Name, originalReference, reference.Filename);
                                }
                            }
                        }

                        if (!_htReferenceProjects.Contains(p.GUID) && (failed || !p.Compile(configuration, compilerArguments, logFile, verbose, showCommands))) {
                            if (!failed) {
                                Log(Level.Error, LogPrefix + "Project '{0}' failed!", p.Name);
                                Log(Level.Error, LogPrefix + "Continuing build with non-dependent projects.");
                            }

                            success = false;
                            htFailedProjects[p.GUID] = null;

                            // mark the projects referencing this one as failed
                            foreach (ProjectBase pFailed in _htProjects.Values) {
                                if (HasProjectDependency(pFailed.GUID, p.GUID)) {
                                    htFailedProjects[pFailed.GUID] = null;
                                }
                            }
                        }

                        compiledThisRound = true;

                        // remove all references to this project
                        foreach (ProjectBase pRemove in _htProjects.Values) {
                            RemoveProjectDependency(pRemove.GUID, p.GUID);
                        }
                        htProjectsDone[p.GUID] = null;
                    }
                }

                if (_htProjects.Count == htProjectsDone.Count) {
                    break;
                }
                if (!compiledThisRound) {
                    throw new BuildException("Circular dependency detected.", Location.UnknownLocation);
                }
            }

            return success;
        }

        #endregion Public Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        private void Log(Level messageLevel, string message) {
            if (_solutionTask != null) {
                _solutionTask.Log(messageLevel, message);
            }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        private void Log(Level messageLevel, string message, params object[] args) {
            if (_solutionTask != null) {
                _solutionTask.Log(messageLevel, message, args);
            }
        }

        #endregion Private Static Methods

        #region Private Instance Methods

        private void LoadProjectGUIDs(ArrayList projects, bool isReferenceProject) {
            foreach (string projectFileName in projects) {
                string projectGUID = ProjectFactory.LoadGUID(projectFileName, _tfc);
                _htProjectFiles[projectGUID] = projectFileName;
                if (isReferenceProject) {
                    _htReferenceProjects[projectGUID] = null;
                }
            }
        }

        private void AddProjectDependency(string projectGUID, string dependencyGUID) {
            if (!_htProjectDependencies.Contains(projectGUID)) {
                _htProjectDependencies[projectGUID] = CollectionsUtil.CreateCaseInsensitiveHashtable();
            }

            ((Hashtable) _htProjectDependencies[projectGUID])[dependencyGUID] = null;
        }

        private void RemoveProjectDependency(string projectGUID, string dependencyGUID) {
            if (!_htProjectDependencies.Contains(projectGUID)) {
                return;
            }

            ((Hashtable) _htProjectDependencies[projectGUID]).Remove(dependencyGUID);
        }

        private bool HasProjectDependency(string projectGUID, string dependencyGUID) {
            if (!_htProjectDependencies.Contains(projectGUID)) {
                return false;
            }

            return ((Hashtable) _htProjectDependencies[projectGUID]).Contains(dependencyGUID);
        }

        private string[] GetProjectDependencies(string projectGUID) {
            if (!_htProjectDependencies.Contains(projectGUID)) {
                return new string[0];
            }

            return (string[]) new ArrayList(((Hashtable) _htProjectDependencies[projectGUID]).Keys).ToArray(typeof(string));
        }

        private void LoadProjects() {
            Log(Level.Verbose, LogPrefix + "Loading projects...");

            FileSet excludes = _solutionTask.ExcludeProjects;
            foreach (DictionaryEntry de in _htProjectFiles) {
                string projectPath = (string) de.Value;
                if (!excludes.FileNames.Contains(projectPath)) {
                    Log(Level.Verbose, LogPrefix + "Loading project '{0}'.", projectPath);
                    ProjectBase p = ProjectFactory.LoadProject(this, _solutionTask, _tfc, _outputDir, projectPath);
                    if (p.GUID == null || p.GUID == String.Empty) {
                        p.GUID = FindGUIDFromPath(projectPath);
                    }
                    _htProjects[de.Key] = p;
                } else {
                    Log(Level.Verbose, LogPrefix + "Excluding project '{0}'.", (string) de.Value);
                }
            }
        }

        private string FindGUIDFromPath(string projectPath) {
            foreach(DictionaryEntry de in _htProjectFiles) {
                string guid = (string) de.Key;
                string path = (string) de.Value;
                if (String.Compare(path, projectPath, true, CultureInfo.InvariantCulture) == 0) {
                    return guid;
                }
            }
            return "";
        }

        private void GetDependenciesFromProjects() {
            Log(Level.Verbose, LogPrefix + "Gathering additional dependencies...");

            // first get all of the output files
            foreach (DictionaryEntry de in _htProjects) {
                string projectGuid = (string) de.Key;
                ProjectBase p = (ProjectBase) de.Value;

                foreach (string configuration in p.Configurations) {
                    _htOutputFiles[p.GetOutputFile(configuration)] = projectGuid;
                }
            }

            // if one of output files resides in reference search path - circle began
            // we must build project with that outputFile before projects referencing it
            // (similar to project dependency) VS.NET 7.0/7.1 do not address this problem

            // build list of output which reside in such folders
            Hashtable outputsInAssemblyFolders = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach (DictionaryEntry de in _htOutputFiles) {
                string outputfile = (string)de.Key;
                string folder = Path.GetDirectoryName(outputfile);

                if (_solutionTask.AssemblyFolders.DirectoryNames.Contains(folder) || _solutionTask.DefaultAssemlyFolders.DirectoryNames.Contains(folder)) {
                    outputsInAssemblyFolders[Path.GetFileName(outputfile)] = de.Value;
                }
            }

            // build the dependency list
            foreach (DictionaryEntry de in _htProjects) {
                string projectGUID = (string) de.Key;
                ProjectBase project = (ProjectBase) de.Value;

                foreach (Reference reference in project.References) {
                    if (reference.IsProjectReference) {
                        AddProjectDependency(projectGUID, reference.Project.GUID);
                    } else if (_htOutputFiles.Contains(reference.Filename)) {
                        AddProjectDependency(projectGUID, (string) _htOutputFiles[reference.Filename]);
                    } else if (outputsInAssemblyFolders.Contains(Path.GetFileName(reference.Filename))) {
                        AddProjectDependency(projectGUID, (string) outputsInAssemblyFolders[Path.GetFileName(reference.Filename)]);
                    }
                }
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private string _fileName;
        private Hashtable _htProjectFiles;
        private Hashtable _htProjects;
        private Hashtable _htProjectDirectories;
        private Hashtable _htProjectDependencies;
        private Hashtable _htOutputFiles;
        private Hashtable _htReferenceProjects;
        private SolutionTask _solutionTask;
        private WebMapCollection _webMaps;
        private FileSet _excludesProjects;
        private string _outputDir;
        private TempFileCollection _tfc;

        #endregion Private Instance Fields
    }
}
