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

                try {
                    // translate URLs to physical paths if using a webmap
                    WebMap map = _webMaps[project];
                    if (map != null && map.IfDefined && !map.UnlessDefined) {
                        project = map.Path;
                    }

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

            //Console.WriteLine( "Loading project GUIDs..." );
            LoadProjectGUIDs(additionalProjects, false);
            LoadProjectGUIDs(referenceProjects, true);
            //Console.WriteLine( "Loading projects..." );
            LoadProjects();
            //Console.WriteLine( "Gathering additional dependencies..." );
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

            //Console.WriteLine( "Loading project GUIDs..." );
            LoadProjectGUIDs(projects, false);
            LoadProjectGUIDs(referenceProjects, true);
            //Console.WriteLine( "Loading projects..." );
            LoadProjects();
            //Console.WriteLine( "Gathering additional dependencies..." );
            GetDependenciesFromProjects();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public string FileName {
            get { return _fileName; }
        }

        #endregion Public Instance Properties

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

        public Project GetProjectFromGUID(string projectGUID) {
            return (Project) _htProjects[projectGUID];
        }

        public bool Compile(string configuration, ArrayList compilerArguments, string logFile, bool verbose, bool showCommands) {
            Hashtable htDeps = (Hashtable) _htProjectDependencies.Clone();
            Hashtable htProjectsDone = CollectionsUtil.CreateCaseInsensitiveHashtable();
            Hashtable htFailedProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();

            bool success = true;
            while (true) {
                bool compiledThisRound = false;

                foreach (Project p in _htProjects.Values) {
                    if (htProjectsDone.Contains(p.GUID)) {
                        continue;
                    }

                    //Console.WriteLine( "{0} {1}: {2} dep(s)", p.Name, p.GUID, GetProjectDependencies( p.GUID ).Length );
                    //foreach ( string strDep in GetProjectDependencies( p.GUID ) )
                    //    Console.WriteLine( "  " + ( ( Project )_htProjects[ strDep ] ).Name );
                    if (GetProjectDependencies(p.GUID).Length == 0) {
                        bool failed = htFailedProjects.Contains(p.GUID);

                        if (!failed) {
                            // Fixup references
                            //Console.WriteLine( "Fixing up references..." );
                            foreach (Reference r in p.References) {
                                //Console.WriteLine( "Original: {0}", r.Filename );
                                if (r.IsProjectReference) {
                                    Project pRef = GetProjectFromGUID(r.Project.GUID);
                                    if (pRef == null)
                                        throw new Exception("Unable to locate referenced project while loading " + p.Name + ".");
                                    if (pRef.GetConfigurationSettings(configuration) == null)
                                        throw new Exception("Unable to find appropriate configuration " + configuration + " for project reference " + p.Name + ".");
                                    if (pRef != null)
                                        r.Filename = pRef.GetConfigurationSettings(configuration).FullOutputFile;
                                } else if (_htOutputFiles.Contains(r.Filename)) {
                                    Project pRef = (Project) _htProjects[(string) _htOutputFiles[r.Filename]];
                                    if (pRef != null && pRef.GetConfigurationSettings(configuration) != null) {
                                        r.Filename = pRef.GetConfigurationSettings(configuration).FullOutputFile;
                                    }
                                }
                                
                                //Console.WriteLine( "   Now: {0}", r.Filename );
                            }
                        }

                        if (!_htReferenceProjects.Contains(p.GUID) && (failed || !p.Compile(configuration, compilerArguments, logFile, verbose, showCommands))) {
                            if (!failed) {
                                Console.WriteLine( "*** Project {0} failed!", p.Name );
                                Console.WriteLine( "*** Continuing build with non-dependent projects:" );
                            }

                            success = false;
                            htFailedProjects[p.GUID] = null;

                            // Mark the projects referencing this one as failed
                            foreach (Project pFailed in _htProjects.Values) {
                                if (HasProjectDependency(pFailed.GUID, p.GUID)) {
                                    htFailedProjects[pFailed.GUID] = null;
                                }
                            }
                        }

                        compiledThisRound = true;

                        // Remove all references to this project
                        foreach (Project pRemove in _htProjects.Values) {
                            RemoveProjectDependency(pRemove.GUID, p.GUID);
                        }
                        htProjectsDone[p.GUID] = null;
                    }
                }

                if (_htProjects.Count == htProjectsDone.Count) {
                    break;
                }
                if (!compiledThisRound) {
                    throw new Exception("Circular dependency detected");
                }
            }

            return success;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void LoadProjectGUIDs(ArrayList projects, bool isReferenceProject) {
            foreach (string projectFileName in projects) {
                //Console.WriteLine( "{0} -> {1}", strProjectFilename, Project.LoadGUID( strProjectFilename, _tfc ) );
                string projectGUID = Project.LoadGUID(projectFileName, _tfc);
                _htProjectFiles[projectGUID] = projectFileName;
                if (isReferenceProject) {
                    _htReferenceProjects[projectGUID] = null;
                }
            }
        }

        private void AddProjectDependency(string projectGUID, string dependencyGUID) {
            //Console.WriteLine( "{0}->{1}", strProjectGUID, strDependencyGUID );
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
            FileSet excludes = _solutionTask.ExcludeProjects;
            foreach (DictionaryEntry de in _htProjectFiles) {
                if (!excludes.FileNames.Contains((string)de.Value)) {
                    Project p = new Project(_solutionTask, _tfc, _outputDir);
                    p.Load(this, (string) de.Value);
                    _htProjects[de.Key] = p;
                } else {
                    _solutionTask.Log(Level.Verbose, _solutionTask.LogPrefix + "Excluding project {0}.", (string)de.Value);
                }
            }
        }

        private void GetDependenciesFromProjects() {
            // First get all of the output files
            foreach (DictionaryEntry de in _htProjects) {
                string strGUID = (string) de.Key;
                Project p = (Project) de.Value;

                foreach (string strConfiguration in p.Configurations) {
                    //Console.WriteLine( "{0} [{1}] -> {2}", p.Name, strConfiguration, p.GetConfigurationSettings( strConfiguration ).FullOutputFile.ToLower() );
                    _htOutputFiles[p.GetConfigurationSettings(strConfiguration).FullOutputFile] = strGUID;
                }
            }

            // build the dependency list
            foreach (DictionaryEntry de in _htProjects) {
                string projectGUID = (string) de.Key;
                Project project = (Project) de.Value;

                foreach (Reference reference in project.References) {
                    if (reference.IsProjectReference) {
                        AddProjectDependency(projectGUID, reference.Project.GUID);
                    } else if (_htOutputFiles.Contains(reference.Filename)) {
                        AddProjectDependency(projectGUID, (string) _htOutputFiles[reference.Filename]);
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
