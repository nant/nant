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

        public Solution(FileInfo solutionFile, ArrayList additionalProjects, ArrayList referenceProjects, TempFileCollection tfc, SolutionTask solutionTask, WebMapCollection webMappings, FileSet excludesProjects, DirectoryInfo outputDir) {
            _file = solutionFile;
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
            ProjectFactory.ClearCache();

            string fileContents;

            using (StreamReader sr = new StreamReader(solutionFile.FullName)) {
                fileContents = sr.ReadToEnd();
            }

            Regex re = new Regex(@"Project\(\""(?<package>\{.*?\})\"".*?\""(?<name>.*?)\"".*?\""(?<project>.*?)\"".*?\""(?<guid>.*?)\""");
            MatchCollection mc = re.Matches(fileContents);

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
                string map = _webMaps.FindBestMatch(project);
                if (map != null) {
                    Log(Level.Debug, LogPrefix + "Found webmap match: " + map);
                    project = map;
                }

                try {
                    Uri uri = new Uri(project);
                    if (uri.Scheme == Uri.UriSchemeFile) {
                        fullPath = Path.Combine(solutionFile.DirectoryName, uri.LocalPath);
                    } else {
                        fullPath = project;

                        if (!solutionTask.EnableWebDav) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "Cannot build web project '{0}'.  Please use" 
                                + " <webmap> to map the given URL to a project-relative" 
                                + " path, or specify enablewebdav=\"true\" on the" 
                                + " <solution> task element to use WebDAV.", fullPath));
                        }
                    }
                } catch (UriFormatException) {
                    fullPath = Path.Combine(solutionFile.DirectoryName, project);
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

            LoadProjectGuids(additionalProjects, false);
            LoadProjectGuids(referenceProjects, true);
            LoadProjects();
            GetDependenciesFromProjects();
        }

        public Solution(ArrayList projects, ArrayList referenceProjects, TempFileCollection tfc, SolutionTask solutionTask, WebMapCollection webMaps, FileSet excludesProjects, DirectoryInfo outputDir) {
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
            ProjectFactory.ClearCache();

            LoadProjectGuids(projects, false);
            LoadProjectGuids(referenceProjects, true);
            LoadProjects();
            GetDependenciesFromProjects();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public FileInfo File {
            get { return _file; }
        }

        public TempFileCollection TemporaryFiles {
            get { return _tfc; }
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

        /// <summary>
        /// Gets the project file of the project with the given unique identifier.
        /// </summary>
        /// <param name="projectGuid">The unique identifier of the project for which the project file should be retrieves.</param>
        /// <returns>
        /// The project file of the project with the given unique identifier.
        /// </returns>
        /// <exception cref="BuildException">No project with unique identifier <paramref name="projectGuid" /> could be located.</exception>
        public string GetProjectFileFromGuid(string projectGuid) {
            // locate project file using the project guid
            string projectFile = (string) _htProjectFiles[projectGuid];

            // TODO : as an emergency patch throw a build error when a GUID fails
            // to return a project file. This should be sanity checked when the 
            // HashTable is populated and not at usage time to avoid internal 
            // errors during build.
            if (projectFile == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project with GUID '{0}' must be included for the build to" 
                    + " work.", projectGuid), Location.UnknownLocation);
            }

            return projectFile;
        }

        public ProjectBase GetProjectFromGuid(string projectGuid) {
            return (ProjectBase) _htProjects[projectGuid];
        }

        public bool Compile(string configuration, ArrayList compilerArguments, string logFile, bool verbose, bool showCommands) {
            Hashtable htDeps = (Hashtable) _htProjectDependencies.Clone();
            Hashtable htProjectsDone = CollectionsUtil.CreateCaseInsensitiveHashtable();
            Hashtable htFailedProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            bool success = true;

            while (true) {
                bool compiledThisRound = false;

                foreach (ProjectBase p in _htProjects.Values) {
                    if (htProjectsDone.Contains(p.Guid)) {
                        continue;
                    }

                    if (GetProjectDependencies(p.Guid).Length == 0) {
                        bool failed = htFailedProjects.Contains(p.Guid);

                        if (!failed) {
                            // Fixup references
                            Log(Level.Verbose, LogPrefix + "Fixing up references...");

                            foreach (Reference reference in p.References) {
                                // store original reference filename
                                string originalReference = reference.Filename;

                                // resolving path, where reference file is (find that file in search paths)
                                reference.ResolveFolder();

                                if (reference.IsProjectReference) {
                                    ProjectBase pRef = GetProjectFromGuid(reference.Project.Guid);
                                    if (pRef == null) {
                                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                            "Unable to locate referenced project '{0}' while loading '{1}'.",
                                            reference.Name, p.Name), Location.UnknownLocation);
                                    }
                                    string outputPath = pRef.GetOutputPath(configuration);
                                    if (outputPath == null) {
                                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                            "Unable to find '{0}' configuration for project '{1}'.",
                                            configuration, pRef.Name), Location.UnknownLocation);
                                    }
                                    reference.Filename = outputPath;
                                } else if (_htOutputFiles.Contains(reference.Filename)) {
                                    ProjectBase pRef = (ProjectBase) _htProjects[(string) _htOutputFiles[reference.Filename]];
                                    if (pRef != null) {
                                        reference.Filename = pRef.GetOutputPath(configuration);
                                    }
                                }

                                // only output message when reference has actually been fixed up
                                if (originalReference != reference.Filename) {
                                    Log(Level.Verbose, LogPrefix + "Fixed reference '{0}': {1} -> {2}.", 
                                        reference.Name, originalReference, reference.Filename);
                                }
                            }
                        }

                        if (!_htReferenceProjects.Contains(p.Guid) && (failed || !p.Compile(configuration, compilerArguments, logFile, verbose, showCommands))) {
                            if (!failed) {
                                Log(Level.Error, LogPrefix + "Project '{0}' failed!", p.Name);
                                Log(Level.Error, LogPrefix + "Continuing build with non-dependent projects.");
                            }

                            success = false;
                            htFailedProjects[p.Guid] = null;

                            // mark the projects referencing this one as failed
                            foreach (ProjectBase pFailed in _htProjects.Values) {
                                if (HasProjectDependency(pFailed.Guid, p.Guid)) {
                                    htFailedProjects[pFailed.Guid] = null;
                                }
                            }
                        }

                        compiledThisRound = true;

                        // remove all references to this project
                        foreach (ProjectBase pRemove in _htProjects.Values) {
                            RemoveProjectDependency(pRemove.Guid, p.Guid);
                        }
                        htProjectsDone[p.Guid] = null;
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

        private void LoadProjectGuids(ArrayList projects, bool isReferenceProject) {
            foreach (string projectFileName in projects) {
                string projectGuid = ProjectFactory.LoadGuid(projectFileName, _tfc);
                _htProjectFiles[projectGuid] = projectFileName;
                if (isReferenceProject) {
                    _htReferenceProjects[projectGuid] = null;
                }
            }
        }

        private void AddProjectDependency(string projectGuid, string dependencyGuid) {
            if (!_htProjectDependencies.Contains(projectGuid)) {
                _htProjectDependencies[projectGuid] = CollectionsUtil.CreateCaseInsensitiveHashtable();
            }

            ((Hashtable) _htProjectDependencies[projectGuid])[dependencyGuid] = null;
        }

        private void RemoveProjectDependency(string projectGuid, string dependencyGuid) {
            if (!_htProjectDependencies.Contains(projectGuid)) {
                return;
            }

            ((Hashtable) _htProjectDependencies[projectGuid]).Remove(dependencyGuid);
        }

        private bool HasProjectDependency(string projectGuid, string dependencyGuid) {
            if (!_htProjectDependencies.Contains(projectGuid)) {
                return false;
            }

            return ((Hashtable) _htProjectDependencies[projectGuid]).Contains(dependencyGuid);
        }

        private string[] GetProjectDependencies(string projectGuid) {
            if (!_htProjectDependencies.Contains(projectGuid)) {
                return new string[0];
            }

            return (string[]) new ArrayList(((Hashtable) _htProjectDependencies[projectGuid]).Keys).ToArray(typeof(string));
        }

        /// <summary>
        /// Loads the projects from the file system and stores them in an 
        /// instance variable.
        /// </summary>
        /// <exception cref="BuildException">A project GUID in the solution file does not match the actual GUID of the project in the project file.</exception>
        private void LoadProjects() {
            Log(Level.Verbose, LogPrefix + "Loading projects...");

            FileSet excludes = _solutionTask.ExcludeProjects;

            // _htProjectFiles contains project GUIDs read from the sln file as 
            // keys and the corresponding full path to the project file as the 
            // value
            foreach (DictionaryEntry de in _htProjectFiles) {
                string projectPath = (string) de.Value;

                // check whether project should be excluded from build
                if (!excludes.FileNames.Contains(projectPath)) {
                    Log(Level.Verbose, LogPrefix + "Loading project '{0}'.", projectPath);
                    ProjectBase p = ProjectFactory.LoadProject(this, _solutionTask, _tfc, _outputDir, projectPath);
                    if (p.Guid == null || p.Guid == string.Empty) {
                        p.Guid = FindGuidFromPath(projectPath);
                    }

                    // If the project GUID from the sln file doesn't match the project GUID
                    // from the project file we will run into problems. Alert the user to fix this
                    // as it is basically a corruption probably caused by user manipulation of the sln
                    // included projects. I.e. copy and paste issue.
                    if (!p.Guid.Equals(de.Key.ToString())) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "GUID corruption detected for project '{0}'. GUID values" 
                            + " in project file and solution file do not match ('{1}'" 
                            + " and '{2}'). Please correct this manually.", p.Name, 
                            p.Guid, de.Key.ToString()), Location.UnknownLocation);
                    }

                    // add project to hashtable
                    _htProjects[de.Key] = p;
                } else {
                    Log(Level.Verbose, LogPrefix + "Excluding project '{0}'.", 
                        (string) de.Value);
                }
            }
        }

        private string FindGuidFromPath(string projectPath) {
            foreach(DictionaryEntry de in _htProjectFiles) {
                string guid = (string) de.Key;
                string path = (string) de.Value;
                if (string.Compare(path, projectPath, true, CultureInfo.InvariantCulture) == 0) {
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
                    _htOutputFiles[p.GetOutputPath(configuration)] = projectGuid;
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

                if (_solutionTask.AssemblyFolders.DirectoryNames.Contains(folder) || _solutionTask.DefaultAssemblyFolders.DirectoryNames.Contains(folder)) {
                    outputsInAssemblyFolders[Path.GetFileName(outputfile)] = de.Value;
                }
            }

            // build the dependency list
            foreach (DictionaryEntry de in _htProjects) {
                string projectGuid = (string) de.Key;
                ProjectBase project = (ProjectBase) de.Value;

                foreach (Reference reference in project.References) {
                    if (reference.IsProjectReference) {
                        AddProjectDependency(projectGuid, reference.Project.Guid);
                    } else if (_htOutputFiles.Contains(reference.Filename)) {
                        AddProjectDependency(projectGuid, (string) _htOutputFiles[reference.Filename]);
                    } else if (outputsInAssemblyFolders.Contains(Path.GetFileName(reference.Filename))) {
                        AddProjectDependency(projectGuid, (string) outputsInAssemblyFolders[Path.GetFileName(reference.Filename)]);
                    }
                }
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private FileInfo _file;
        private Hashtable _htProjectFiles;
        private Hashtable _htProjects;
        private Hashtable _htProjectDirectories;
        private Hashtable _htProjectDependencies;
        private Hashtable _htOutputFiles;
        private Hashtable _htReferenceProjects;
        private SolutionTask _solutionTask;
        private WebMapCollection _webMaps;
        private FileSet _excludesProjects;
        private DirectoryInfo _outputDir;
        private TempFileCollection _tfc;

        #endregion Private Instance Fields
    }
}
