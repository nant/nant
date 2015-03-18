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
using System.Xml;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;
using NAnt.VSNet.Types;

namespace NAnt.VSNet {
    public abstract class SolutionBase {
        #region Protected Instance Constructors

        protected SolutionBase(SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver) : this(tfc, solutionTask) {
            if (solutionTask.SolutionFile != null) {
                _file = solutionTask.SolutionFile;
            } else {
                LoadProjectGuids(new ArrayList(solutionTask.Projects.FileNames), false);
                LoadProjectGuids(new ArrayList(solutionTask.ReferenceProjects.FileNames), true);
                LoadProjects(gacCache, refResolver, CollectionsUtil.CreateCaseInsensitiveHashtable());
            }
        }

        #endregion Protected Instance Constructors

        #region Private Instance Constructors

        private SolutionBase(TempFileCollection tfc, SolutionTask solutionTask) {
            _htOutputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _projectEntries = new ProjectEntryCollection();
            _htReferenceProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _tfc = tfc;
            _solutionTask = solutionTask;
            _outputDir = solutionTask.OutputDir;
            _webMaps = solutionTask.WebMaps;
        }

        #endregion Private Instance Constructors

        #region Public Instance Properties

        public FileInfo File {
            get { return _file; }
        }

        public TempFileCollection TemporaryFiles {
            get { return _tfc; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties {

        protected WebMapCollection WebMaps {
            get { return _webMaps; }
        }

        public ProjectEntryCollection ProjectEntries {
            get { return _projectEntries; }
        }

        #endregion Protected Instance Properties {

        #region Public Instance Methods

        public void RecursiveLoadTemplateProject(string fileName) {
            XmlDocument doc = _solutionTask.ProjectFactory.LoadProjectXml(fileName);

            foreach (XmlNode node in doc.SelectNodes("//Reference")) {
                XmlNode projectGuidNode = node.SelectSingleNode("GUIDPROJECTID");
                XmlNode fileNode = node.SelectSingleNode("FILE");

                if (fileNode == null) {
                    Log(Level.Warning, "Reference with missing <FILE> node. Skipping.");
                    continue;
                }

                // check if we're dealing with project or assembly reference
                if (projectGuidNode != null) {
                    string subProjectFilename = node.SelectSingleNode("FILE").InnerText;
                    string fullPath;

                    // translate URLs to physical paths if using a webmap
                    string map = _webMaps.FindBestMatch(subProjectFilename);
                    if (map != null) {
                        Log(Level.Debug, "Found webmap match '{0}' for '{1}.", 
                            map, subProjectFilename);
                        subProjectFilename = map;
                    }

                    try {
                        Uri uri = new Uri(subProjectFilename);
                        if (uri.Scheme == Uri.UriSchemeFile) {
                            fullPath = FileUtils.GetFullPath(FileUtils.CombinePaths(
                                Path.GetDirectoryName(fileName), uri.LocalPath));
                        } else {
                            fullPath = subProjectFilename;

                            if (!_solutionTask.EnableWebDav) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                    "Cannot build web project '{0}'.  Please use" 
                                    + " <webmap> to map the given URL to a project-relative" 
                                    + " path, or specify enablewebdav=\"true\" on the" 
                                    + " <solution> task element to use WebDAV.", fullPath));
                            }
                        }
                    } catch (UriFormatException) {
                        fullPath = FileUtils.GetFullPath(FileUtils.CombinePaths(
                            Path.GetDirectoryName(fileName), subProjectFilename));
                    }

                    // check if project file actually exists
                    if (!System.IO.File.Exists(fullPath)) {
                        throw CreateProjectDoesNotExistException(fullPath);
                    }

                    if (ManagedProjectBase.IsEnterpriseTemplateProject(fullPath)) {
                        RecursiveLoadTemplateProject(fullPath);
                    } else {
                        ProjectEntries.Add(new ProjectEntry(projectGuidNode.InnerText, fullPath));
                    }
                } else {
                    Log(Level.Verbose, "Skipping file reference '{0}'.", 
                        fileNode.InnerText);
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
            // locate project entry using the project guid
            ProjectEntry projectEntry = ProjectEntries[projectGuid];

            // TODO : as an emergency patch throw a build error when a GUID fails
            // to return a project file. This should be sanity checked when the 
            // HashTable is populated and not at usage time to avoid internal 
            // errors during build.
            if (projectEntry == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project with GUID '{0}' must be included for the build to" 
                    + " work.", projectGuid), Location.UnknownLocation);
            }

            return projectEntry.Path;
        }

        public ProjectBase GetProjectFromGuid(string projectGuid) {
            ProjectEntry projectEntry = ProjectEntries[projectGuid];
            if (projectEntry == null || projectEntry.Project == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project with GUID '{0}' is not loaded.", projectGuid), 
                    Location.UnknownLocation);
            }
            return projectEntry.Project;
        }

        public bool Compile(Configuration solutionConfiguration) {
            Hashtable htProjectsDone = CollectionsUtil.CreateCaseInsensitiveHashtable();
            Hashtable htFailedProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            ArrayList failedProjects = new ArrayList();
            bool success = true;

            GetDependenciesFromProjects(solutionConfiguration);

            while (true) {
                bool compiledThisRound = false;

                foreach (ProjectEntry projectEntry in ProjectEntries) {
                    ProjectBase project = projectEntry.Project;

                    if (project == null) {
                        // mark project done
                        htProjectsDone[projectEntry.Guid] = null;

                        // skip projects that are not loaded/supported
                        continue;
                    }

                    if (htProjectsDone.Contains(project.Guid)) {
                        continue;
                    }

                    bool failed = htFailedProjects.Contains(project.Guid);
                    if (!failed) {
                        // attempt to convert assembly references to project 
                        // references
                        //
                        // this might affect the build order as it can add 
                        // project dependencies
                        if (FixProjectReferences(project, solutionConfiguration, htProjectsDone, htFailedProjects)) {
                            // mark project failed if it references a project that
                            // failed to build
                            //
                            // this can only happen when assembly reference was
                            // was fixed to a project reference (that already failed
                            // to build before the fix-up)
                            failed = true;

                            // avoid running through the fix-up next time
                            htFailedProjects[project.Guid] = null;
                        }
                    }

                    if (!HasDirtyProjectDependency(project, htProjectsDone)) {
                        try {
                            if (!_htReferenceProjects.Contains(project.Guid) && (failed || !project.Compile(solutionConfiguration))) {
                                if (!failed) {
                                    Log(Level.Error, "Project '{0}' failed!", project.Name);
                                    Log(Level.Error, "Continuing build with non-dependent projects.");
                                    failedProjects.Add(project.Name);
                                }

                                success = false;
                                htFailedProjects[project.Guid] = null;

                                // mark the projects referencing this one as failed
                                foreach (ProjectEntry entry in ProjectEntries) {
                                    ProjectBase dependentProject = entry.Project;
                                    if (dependentProject == null) {
                                        // skip projects that are not loaded/supported
                                    }
                                    // if the project depends on the failed
                                    // project, then also mark it failed
                                    if (dependentProject.ProjectDependencies.Contains(project)) {
                                        htFailedProjects[dependentProject.Guid] = null;
                                    }
                                }
                            }
                        } catch (BuildException) {
                            // Re-throw build exceptions
                            throw;
                        } catch (Exception e) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "Unexpected error while compiling project '{0}'", project.Name), 
                                Location.UnknownLocation, e);
                        }

                        compiledThisRound = true;

                        // mark project done
                        htProjectsDone[project.Guid] = null;
                    }
                }

                if (ProjectEntries.Count == htProjectsDone.Count) {
                    break;
                }
                if (!compiledThisRound) {
                    throw new BuildException("Circular dependency detected.", Location.UnknownLocation);
                }
            }

            if (failedProjects.Count > 0) {
                Log(Level.Error, string.Empty);
                Log(Level.Error, "Solution failed to build!  Failed projects were:" );
                foreach (string projectName in failedProjects)
                    Log(Level.Error, "  - " + projectName );
            }

            return success;
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        protected void Log(Level messageLevel, string message) {
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
        protected void Log(Level messageLevel, string message, params object[] args) {
            if (_solutionTask != null) {
                _solutionTask.Log(messageLevel, message, args);
            }
        }

        protected void LoadProjectGuids(ArrayList projects, bool isReferenceProject) {
            foreach (string projectFileName in projects) {
                string projectGuid = _solutionTask.ProjectFactory.LoadGuid(projectFileName);

                // locate project entry using the project guid
                ProjectEntry projectEntry = ProjectEntries[projectGuid];
                if (projectEntry != null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Error loading project {0}. " 
                        + " Project GUID {1} already exists! Conflicting project is {2}.", 
                        projectFileName, projectGuid, projectEntry.Path));
                }
                ProjectEntries.Add(new ProjectEntry(projectGuid, projectFileName));
                if (isReferenceProject)
                    _htReferenceProjects[projectGuid] = null;
            }
        }

        /// <summary>
        /// Loads the projects from the file system and stores them in an 
        /// instance variable.
        /// </summary>
        /// <param name="gacCache"><see cref="GacCache" /> instance to use to determine whether an assembly is located in the Global Assembly Cache.</param>
        /// <param name="refResolver"><see cref="ReferencesResolver" /> instance to use to determine location and references of assemblies.</param>
        /// <param name="explicitProjectDependencies">TODO</param>
        /// <exception cref="BuildException">A project GUID in the solution file does not match the actual GUID of the project in the project file.</exception>
        protected void LoadProjects(GacCache gacCache, ReferencesResolver refResolver, Hashtable explicitProjectDependencies) {
            Log(Level.Verbose, "Loading projects...");

            FileSet excludes = _solutionTask.ExcludeProjects;

            foreach (ProjectEntry projectEntry in ProjectEntries) {
                string projectPath = projectEntry.Path;
                string projectGuid = projectEntry.Guid;

                // determine whether project is on case-sensitive filesystem,
                bool caseSensitive = PlatformHelper.IsVolumeCaseSensitive(projectPath);

                // indicates whether the project should be skipped (excluded)
                bool skipProject = false;

                // check whether project should be excluded from build
                foreach (string excludedProjectFile in excludes.FileNames) {
                    if (string.Compare(excludedProjectFile, projectPath, !caseSensitive, CultureInfo.InvariantCulture) == 0) {
                        Log(Level.Verbose, "Excluding project '{0}'.", 
                            projectPath);
                        // do not load project
                        skipProject = true;
                        // we have a match, so quit looking
                        break;
                    }
                }

                if (skipProject) {
                    // remove dependencies for excluded projects
                    if (explicitProjectDependencies.ContainsKey(projectGuid)) {
                        explicitProjectDependencies.Remove(projectGuid);
                    }

                    // project was excluded, move on to next project
                    continue;
                }

                Log(Level.Verbose, "Loading project '{0}'.", projectPath);
                ProjectBase p = _solutionTask.ProjectFactory.LoadProject(this, _solutionTask, 
                    _tfc, gacCache, refResolver, _outputDir, projectPath);
                if (p == null) {
                    Log(Level.Warning, "Project '{0}' is of unsupported type. Skipping.", projectPath);
                    // skip the project
                    continue;
                }
                if (p.Guid == null || p.Guid.Length == 0) {
                    p.Guid = FindGuidFromPath(projectPath);
                }

                // add project to entry
                projectEntry.Project = p;

                // set project build configuration
                SetProjectBuildConfiguration(projectEntry);
            }

            // add explicit dependencies (as set in VS.NET) to individual projects
            foreach (DictionaryEntry dependencyEntry in explicitProjectDependencies) {
                string projectGuid = (string) dependencyEntry.Key;
                Hashtable dependencies = (Hashtable) dependencyEntry.Value;

                ProjectEntry projectEntry = ProjectEntries[projectGuid];
                if (projectEntry == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Dependencies for project \'{0}\' could not be analyzed."
                        + " Project is not included.", projectGuid), 
                        Location.UnknownLocation);
                }

                ProjectBase project = projectEntry.Project;

                // make sure project is loaded
                if (project == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Dependencies for project \'{0}\' could not be analyzed."
                        + " Project is not loaded.", projectGuid), 
                        Location.UnknownLocation);
                }

                foreach (string dependentProjectGuid in dependencies.Keys) {
                    ProjectEntry dependentEntry = ProjectEntries[dependentProjectGuid];
                    if (dependentEntry == null || dependentEntry.Project == null) {
                        Log(Level.Warning, "Project \"{0}\": ignored dependency"
                            + " on project \"{1}\", which is not included.", 
                            project.Name, dependentProjectGuid);
                        continue;
                    }

                    project.ProjectDependencies.Add(dependentEntry.Project);
                }
            }
        }

        protected void GetDependenciesFromProjects(Configuration solutionConfiguration) {
            Log(Level.Verbose, "Gathering additional dependencies...");

            // first get all of the output files
            foreach (ProjectEntry projectEntry in ProjectEntries) {
                ProjectBase project = projectEntry.Project;

                if (project == null) {
                    // skip projects that are not loaded/supported
                    continue;
                }

                foreach (ConfigurationBase projectConfig in project.ProjectConfigurations.Values) {
                    string projectOutputFile = projectConfig.OutputPath;
                    if (projectOutputFile != null) {
                        _htOutputFiles[projectOutputFile] = project.Guid;
                    }
                }
            }

            // if one of output files resides in reference search path - circle began
            // we must build project with that outputFile before projects referencing it
            // (similar to project dependency) VS.NET 7.0/7.1 do not address this problem

            // build list of output which reside in such folders
            Hashtable outputsInAssemblyFolders = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach (DictionaryEntry de in _htOutputFiles) {
                string outputfile = (string) de.Key;
                string folder = Path.GetDirectoryName(outputfile);

                if (_solutionTask.AssemblyFolderList.Contains(folder)) {
                    outputsInAssemblyFolders[Path.GetFileName(outputfile)] = 
                        (string) de.Value;
                }
            }

            // build the dependency list
            foreach (ProjectEntry projectEntry in ProjectEntries) {
                ProjectBase project = projectEntry.Project;

                if (project == null) {
                    // skip projects that are not loaded/supported
                    continue;
                }

                // check if project actually supports the build configuration
                ConfigurationBase projectConfig = project.BuildConfigurations[solutionConfiguration];
                if (projectConfig == null) {
                    continue;
                }

                // ensure output directory exists. VS creates output directories
                // before it starts compiling projects
                if (!projectConfig.OutputDir.Exists) {
                    projectConfig.OutputDir.Create();
                    projectConfig.OutputDir.Refresh();
                }

                foreach (ReferenceBase reference in project.References) {
                    ProjectReferenceBase projectReference = reference as ProjectReferenceBase;
                    if (projectReference != null) {
                        project.ProjectDependencies.Add(projectReference.Project);
                    } else {
                        string outputFile = reference.GetPrimaryOutputFile(
                            solutionConfiguration);
                        // if we reference an assembly in an AssemblyFolder
                        // that is an output directory of another project, 
                        // then add dependency on that project
                        if (outputFile == null) {
                            continue;
                        }

                        string dependencyGuid = (string) outputsInAssemblyFolders[Path.GetFileName(outputFile)];
                        if (dependencyGuid == null) {
                            continue;
                        }

                        ProjectEntry dependencyEntry = ProjectEntries[dependencyGuid];
                        if (dependencyEntry != null && dependencyEntry.Project != null) {
                            project.ProjectDependencies.Add(dependencyEntry.Project);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Translates a project path, in the form of a relative file path or
        /// a URL, to an absolute file path.
        /// </summary>
        /// <param name="solutionDir">The directory of the solution.</param>
        /// <param name="projectPath">The project path to translate to an absolute file path.</param>
        /// <returns>
        /// The project path translated to an absolute file path.
        /// </returns>
        protected string TranslateProjectPath(string solutionDir, string projectPath) {
            if (solutionDir == null) {
                throw new ArgumentNullException("solutionDir");
            }
            if (projectPath == null) {
                throw new ArgumentNullException("projectPath");
            }

            string translatedPath = null;

            // translate URLs to physical paths if using a webmap
            string map = WebMaps.FindBestMatch(projectPath);
            if (map != null) {
                Log(Level.Debug, "Found webmap match '{0}' for '{1}.", 
                    map, projectPath);
                translatedPath = map;
            } else {
                translatedPath = projectPath;
            }

            try {
#if NET_2_0
                Uri uri = null;
                Uri.TryCreate(translatedPath, UriKind.Absolute, out uri);
#else
                Uri uri = new Uri(translatedPath);
#endif
                if(uri==null) {
                    translatedPath = FileUtils.GetFullPath(FileUtils.CombinePaths( 
                        solutionDir, translatedPath));
                } else if (uri.Scheme == Uri.UriSchemeFile) {
                    translatedPath = FileUtils.GetFullPath(FileUtils.CombinePaths(
                        solutionDir, uri.LocalPath));
                } else {
                    if (!_solutionTask.EnableWebDav) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Cannot build web project '{0}'.  Please use" 
                            + " <webmap> to map the given URL to a project-relative" 
                            + " path, or specify enablewebdav=\"true\" on the" 
                            + " <solution> task element to use WebDAV.", translatedPath));
                    }
                }
            } catch (UriFormatException) {
                translatedPath = FileUtils.GetFullPath(FileUtils.CombinePaths(
                    solutionDir, translatedPath));
            }

            return translatedPath;
        }

        /// <summary>
        /// Converts assembly references to projects to project references, adding
        /// a build dependency.c
        /// </summary>
        /// <param name="project">The <see cref="ProjectBase" /> to analyze.</param>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <param name="builtProjects"><see cref="Hashtable" /> containing list of projects that have been built.</param>
        /// <param name="failedProjects"><see cref="Hashtable" /> containing list of projects that failed to build.</param>
        protected bool FixProjectReferences(ProjectBase project, Configuration solutionConfiguration, Hashtable builtProjects, Hashtable failedProjects) {
            // check if the project still has dependencies that have not been 
            // built
            if (HasDirtyProjectDependency(project, builtProjects)) {
                return false;
            }

            ConfigurationBase projectConfig = project.BuildConfigurations[solutionConfiguration];

            // check if the project actually supports the build configuration
            if (projectConfig == null) {
                return false;
            }

            Log(Level.Verbose, "Fixing up references...");

            ArrayList projectReferences = (ArrayList) 
                project.References.Clone();

            bool referencesFailedProject = false;

            foreach (ReferenceBase reference in projectReferences) {
                AssemblyReferenceBase assemblyReference = reference as 
                    AssemblyReferenceBase;
                if (assemblyReference == null) {
                    // project references and wrappers don't 
                    // need to be fixed
                    continue;
                }

                ProjectBase projectRef = null;

                string outputFile = assemblyReference.GetPrimaryOutputFile(
                    solutionConfiguration);
                if (outputFile == null) {
                    continue;
                }

                if (_htOutputFiles.Contains(outputFile)) {
                    // if the reference is an output file of
                    // another build configuration of a project
                    // and this output file wasn't built before
                    // then use the output file for the current 
                    // build configuration 
                    //
                    // eg. a project file might be referencing the
                    // the debug assembly of a given project as an
                    // assembly reference, but the projects are now 
                    // being built in release configuration, so
                    // instead of failing the build we use the 
                    // release assembly of that project

                    // Note that this was designed to intentionally 
                    // deviate from VS.NET's building strategy.

                    // See "Reference Configuration Matching" at http://nant.sourceforge.net/wiki/index.php/SolutionTask
                    // for why we must always convert file references to project references

                    // If we want a different behaviour, this 
                    // should be controlled by a flag

                    projectRef = ProjectEntries[(string) _htOutputFiles[outputFile]].Project;
                } else if (_outputDir != null) {
                    // if an output directory is set, then the 
                    // assembly reference might not have been 
                    // resolved during Reference initialization, 
                    // as the output file of the project might 
                    // not have existed at that time
                    //
                    // this will perform matching on file name
                    // only, so its really tricky (VS.NET does
                    // not support this)

                    string projectOutput = FileUtils.CombinePaths(
                        _outputDir.FullName, Path.GetFileName(
                        outputFile));
                    if (_htOutputFiles.Contains(projectOutput)) {
                        projectRef = (ProjectBase) ProjectEntries[
                            (string) _htOutputFiles[projectOutput]].Project;
                    }
                }

                // try matching assembly reference and project on assembly name
                // if the assembly file does not exist
                if (projectRef == null && !System.IO.File.Exists(outputFile)) {
                    foreach (ProjectEntry projectEntry in ProjectEntries) {
                        // we can only do this for managed projects, as we only have
                        // an assembly name for these
                        ManagedProjectBase managedProject = projectEntry.Project as ManagedProjectBase;
                        if (managedProject == null) {
                            continue;
                        }
                        // check if the assembly names match
                        if (assemblyReference.Name == managedProject.ProjectSettings.AssemblyName) {
                            projectRef = managedProject;
                            break;
                        }
                    }
                }

                if (projectRef != null) {
                    if (!referencesFailedProject && failedProjects.ContainsKey(projectRef.Guid)) {
                        referencesFailedProject = true;
                    }

                    ProjectReferenceBase projectReference = assemblyReference.
                        CreateProjectReference(projectRef);
                    Log(Level.Verbose, "Converted assembly reference to project reference: {0} -> {1}", 
                        assemblyReference.Name, projectReference.Name);

                    // remove assembly reference from project
                    project.References.Remove(assemblyReference);

                    // add project reference instead
                    project.References.Add(projectReference);

                    // unless referenced project has already been build, add
                    // referenced project as project dependency
                    if (!builtProjects.Contains(projectReference.Project.Guid)) {
                        project.ProjectDependencies.Add(projectReference.Project);
                    }
                }
            }

            return referencesFailedProject;
        }

        protected BuildException CreateProjectDoesNotExistException(string projectPath) {
            return new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Project '{0}' does not exist.", projectPath));
        }

        protected virtual void SetProjectBuildConfiguration(ProjectEntry projectEntry) { 
            if (projectEntry.BuildConfigurations == null) {
                // project was not loaded from solution file, and as a result
                // there's no project configuration section available, so we'll
                // consider all project configurations as valid build
                // configurations
                ProjectBase project = projectEntry.Project;
                project.BuildConfigurations.Clear();
                foreach (ConfigurationDictionaryEntry ce in project.ProjectConfigurations) {
                    project.BuildConfigurations[ce.Name] = ce.Config;
                }
            } else {
                // project was loaded from solution file, so only add build
                // configurations that were listed in project configuration
                // section

                ProjectBase project = projectEntry.Project;

                foreach (ConfigurationMapEntry ce in projectEntry.BuildConfigurations) {
                    Configuration solutionConfig = ce.Key;
                    Configuration projectConfig = ce.Value;

                    ConfigurationBase conf = project.ProjectConfigurations [projectConfig];
                    if (conf != null) {
                        project.BuildConfigurations[solutionConfig] = conf;
                    }
                }
            }
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Determines whether any of the project dependencies of the specified
        /// project still needs to be built.
        /// </summary>
        /// <param name="project">The <see cref="ProjectBase" /> to analyze.</param>
        /// <param name="builtProjects"><see cref="Hashtable" /> containing list of projects that have been built.</param>
        /// <returns>
        /// <see langword="true" /> if one of the project dependencies has not
        /// yet been built; otherwise, <see langword="false" />.
        /// </returns>
        private bool HasDirtyProjectDependency(ProjectBase project, Hashtable builtProjects) {
            foreach (ProjectBase projectDependency in project.ProjectDependencies) {
                if (!builtProjects.ContainsKey(projectDependency.Guid)) {
                    return true;
                }
            }
            return false;
        }

        private string FindGuidFromPath(string projectPath) {
            foreach (ProjectEntry projectEntry in ProjectEntries) {
                if (string.Compare(projectEntry.Path, projectPath, true, CultureInfo.InvariantCulture) == 0) {
                    return projectEntry.Guid;
                }
            }
            return string.Empty;
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly FileInfo _file;
        private readonly ProjectEntryCollection _projectEntries;
        private readonly Hashtable _htOutputFiles;
        private readonly Hashtable _htReferenceProjects;
        private readonly SolutionTask _solutionTask;
        private readonly WebMapCollection _webMaps;
        private readonly DirectoryInfo _outputDir;
        private readonly TempFileCollection _tfc;

        #endregion Private Instance Fields
    }
}
