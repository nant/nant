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
using System.Runtime.CompilerServices;
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
            _htProjectBuildConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _htReferenceProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _tfc = tfc;
            _solutionTask = solutionTask;
            _outputDir = solutionTask.OutputDir;
            _webMaps = solutionTask.WebMaps;
            ProjectFactory.ClearCache();
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

        protected ProjectEntryCollection ProjectEntries {
            get { return _projectEntries; }
        }

        protected Hashtable ProjectBuildConfigurations {
            get { return _htProjectBuildConfigurations; }
        }

        #endregion Protected Instance Properties {

        #region Public Instance Methods

        public void RecursiveLoadTemplateProject(string fileName) {
            XmlDocument doc = ProjectFactory.LoadProjectXml(fileName);

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

        public bool Compile(string solutionConfiguration) {
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
                string projectGuid = ProjectFactory.LoadGuid(projectFileName);

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

                // check if project type is supported
                if (!ProjectFactory.IsSupportedProjectType(projectPath)) {
                    // output a warning message in the build log
                    Log(Level.Warning, "Only C#, J#, VB.NET and C++ projects" +
                        " are supported.  Skipping project '{0}'.", projectPath);
                    // skip the project
                    continue;
                }

                Log(Level.Verbose, "Loading project '{0}'.", projectPath);
                ProjectBase p = ProjectFactory.LoadProject(this, _solutionTask, 
                    _tfc, gacCache, refResolver, _outputDir, projectPath);
                if (p.Guid == null || p.Guid == string.Empty) {
                    p.Guid = FindGuidFromPath(projectPath);
                }

                // add project to entry
                projectEntry.Project = p;

                // set project build configuration
                SetProjectBuildConfiguration(p);
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

        protected void GetDependenciesFromProjects(string solutionConfiguration) {
            Log(Level.Verbose, "Gathering additional dependencies...");

            // first get all of the output files
            foreach (ProjectEntry projectEntry in ProjectEntries) {
                ProjectBase project = projectEntry.Project;

                if (project == null) {
                    // skip projects that are not loaded/supported
                    continue;
                }

                foreach (string configuration in project.Configurations) {
                    ConfigurationBase projectConfig = (ConfigurationBase) project.ProjectConfigurations[configuration];
                    if (projectConfig != null) {
                        string projectOutputFile = projectConfig.OutputPath;
                        if (projectOutputFile != null) {
                            _htOutputFiles[projectOutputFile] = project.Guid;
                        }
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
                ConfigurationBase projectConfig = (ConfigurationBase) 
                    project.BuildConfigurations[solutionConfiguration];
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
                Uri uri = new Uri(translatedPath);
                if (uri.Scheme == Uri.UriSchemeFile) {
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
        protected bool FixProjectReferences(ProjectBase project, string solutionConfiguration, Hashtable builtProjects, Hashtable failedProjects) {
            // check if the project still has dependencies that have not been 
            // built
            if (HasDirtyProjectDependency(project, builtProjects)) {
                return false;
            }

            ConfigurationBase projectConfig = (ConfigurationBase) 
                project.BuildConfigurations[solutionConfiguration];

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

        private void SetProjectBuildConfiguration(ProjectBase project) {
            if (!_htProjectBuildConfigurations.Contains(project.Guid)) {
                // project was not loaded from solution file, so there's no
                // project configuration section available, so we'll consider 
                // all project configurations as valid build configurations
                project.BuildConfigurations.Clear();
                foreach (string configuration in project.ProjectConfigurations.Keys) {
                    project.BuildConfigurations[configuration] = project.ProjectConfigurations[configuration];
                }
            } else {
                // project was loaded from solution file, so only add build
                // configurations that were listed in project configuration
                // section
                Hashtable projectBuildConfigurations = (Hashtable) _htProjectBuildConfigurations[project.Guid];
                foreach (DictionaryEntry de in projectBuildConfigurations) {
                    string solutionConfiguration = (string) de.Key;
                    string projectConfiguration = (string) de.Value;
                    if (project.ProjectConfigurations.ContainsKey(projectConfiguration)) {
                        project.BuildConfigurations[solutionConfiguration] = project.ProjectConfigurations[projectConfiguration];
                    }
                }
            }
        }

        private string FindGuidFromPath(string projectPath) {
            foreach (ProjectEntry projectEntry in ProjectEntries) {
                if (string.Compare(projectEntry.Path, projectPath, true, CultureInfo.InvariantCulture) == 0) {
                    return projectEntry.Guid;
                }
            }
            return "";
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly FileInfo _file;
        private readonly ProjectEntryCollection _projectEntries;
        private readonly Hashtable _htProjectBuildConfigurations;
        private readonly Hashtable _htOutputFiles;
        private readonly Hashtable _htReferenceProjects;
        private readonly SolutionTask _solutionTask;
        private readonly WebMapCollection _webMaps;
        private readonly DirectoryInfo _outputDir;
        private readonly TempFileCollection _tfc;

        #endregion Private Instance Fields

        public class ProjectEntry {
            #region Private Instance Fields

            private string _guid;
            private string _path;
            private ProjectBase _project;

            #endregion Private Instance Fields

            #region Public Instance Constructors
            
            public ProjectEntry(string guid, string path) {
                if (guid == null) {
                    throw new ArgumentNullException("guid");
                }
                if (path == null) {
                    throw new ArgumentNullException("path");
                }

                _guid = guid;
                _path = path;
            }

            #endregion Public Instance Constructors

            #region Public Instance Properties

            public string Guid {
                get { return _guid; }
            }

            public string Path {
                get { return _path; }
            }

            /// <summary>
            /// Gets or sets the in memory representation of the project.
            /// </summary>
            /// <value>
            /// The in memory representation of the project, or <see langword="null" />
            /// if the project is not (yet) loaded.
            /// </value>
            /// <remarks>
            /// This property will always be <see langword="null" /> for
            /// projects that are not supported.
            /// </remarks>
            public ProjectBase Project {
                get { return _project; }
                set {
                    if (value != null) {
                        // if the project GUID from the solution file doesn't match the 
                        // project GUID from the project file we will run into problems. 
                        // Alert the user to fix this as it is basically a corruption 
                        // probably caused by user manipulation of the solution file
                        // i.e. copy and paste
                        if (string.Compare(Guid, value.Guid, true, CultureInfo.InvariantCulture) != 0) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "GUID corruption detected for project '{0}'. GUID values" 
                                + " in project file and solution file do not match ('{1}'" 
                                + " and '{2}'). Please correct this manually.", value.Name, 
                                value.Guid, Guid), Location.UnknownLocation);
                        }
                    }
                _project = value; 
                }
            }

            #endregion Public Instance Properties
        }

        /// <summary>
        /// Contains a collection of <see cref="ProjectEntry" /> elements.
        /// </summary>
        [Serializable()]
            public class ProjectEntryCollection : CollectionBase {
            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ProjectEntryCollection"/> class.
            /// </summary>
            public ProjectEntryCollection() {
            }
        
            /// <summary>
            /// Initializes a new instance of the <see cref="ProjectEntryCollection"/> class
            /// with the specified <see cref="ProjectEntryCollection"/> instance.
            /// </summary>
            public ProjectEntryCollection(ProjectEntryCollection value) {
                AddRange(value);
            }
        
            /// <summary>
            /// Initializes a new instance of the <see cref="ProjectEntryCollection"/> class
            /// with the specified array of <see cref="ProjectEntry"/> instances.
            /// </summary>
            public ProjectEntryCollection(ProjectEntry[] value) {
                AddRange(value);
            }

            #endregion Public Instance Constructors
        
            #region Public Instance Properties

            /// <summary>
            /// Gets or sets the element at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index of the element to get or set.</param>
            [IndexerName("Item")]
            public ProjectEntry this[int index] {
                get { return (ProjectEntry) base.List[index]; }
                set { base.List[index] = value; }
            }

            /// <summary>
            /// Gets the <see cref="ProjectEntry"/> with the specified GUID.
            /// </summary>
            /// <param name="guid">The GUID of the <see cref="ProjectEntry"/> to get.</param>
            /// <remarks>
            /// Performs a case-insensitive lookup.
            /// </remarks>
            [IndexerName("Item")]
            public ProjectEntry this[string guid] {
                get {
                    if (guid != null) {
                        // try to locate instance by guid (case-insensitive)
                        foreach (ProjectEntry projectEntry in base.List) {
                            if (string.Compare(projectEntry.Guid, guid, true, CultureInfo.InvariantCulture) == 0) {
                                return projectEntry;
                            }
                        }
                    }
                    return null;
                }
                set {
                    bool insert = true;

                    for (int i = 0; i < base.Count; i++) {
                        ProjectEntry projectEntry = (ProjectEntry) base.List[i];
                        if (string.Compare(projectEntry.Guid, guid, true, CultureInfo.InvariantCulture) == 0) {
                            base.List[i] = value;
                            insert = false;
                        }
                    }

                    if (insert) {
                        Add(value);
                    }
                }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods
        
            /// <summary>
            /// Adds a <see cref="ProjectEntry"/> to the end of the collection.
            /// </summary>
            /// <param name="item">The <see cref="ProjectEntry"/> to be added to the end of the collection.</param> 
            /// <returns>
            /// The position into which the new element was inserted.
            /// </returns>
            public int Add(ProjectEntry item) {
                if (item == null) {
                    throw new ArgumentNullException("item");
                }

                // fail if a project with the same GUID exists in the collection
                ProjectEntry existingEntry = this[item.Guid];
                if (existingEntry != null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "The GUIDs of projects \"{0}\" and \"{1}\" are identical."
                        + " Please correct this manually.", item.Path,
                        existingEntry.Path), Location.UnknownLocation);
                }

                return base.List.Add(item);
            }

            /// <summary>
            /// Adds the elements of a <see cref="ProjectEntry"/> array to the end of the collection.
            /// </summary>
            /// <param name="items">The array of <see cref="ProjectEntry"/> elements to be added to the end of the collection.</param> 
            public void AddRange(ProjectEntry[] items) {
                for (int i = 0; (i < items.Length); i = (i + 1)) {
                    Add(items[i]);
                }
            }

            /// <summary>
            /// Adds the elements of a <see cref="ProjectEntryCollection"/> to the end of the collection.
            /// </summary>
            /// <param name="items">The <see cref="ProjectEntryCollection"/> to be added to the end of the collection.</param> 
            public void AddRange(ProjectEntryCollection items) {
                for (int i = 0; (i < items.Count); i = (i + 1)) {
                    Add(items[i]);
                }
            }
        
            /// <summary>
            /// Determines whether a <see cref="ProjectEntry"/> is in the collection.
            /// </summary>
            /// <param name="item">The <see cref="ProjectEntry"/> to locate in the collection.</param> 
            /// <returns>
            /// <see langword="true" /> if <paramref name="item"/> is found in the 
            /// collection; otherwise, <see langword="false" />.
            /// </returns>
            public bool Contains(ProjectEntry item) {
                return base.List.Contains(item);
            }

            /// <summary>
            /// Determines whether a <see cref="ProjectEntry"/> with the specified
            /// GUID is in the collection, using a case-insensitive lookup.
            /// </summary>
            /// <param name="value">The GUID to locate in the collection.</param> 
            /// <returns>
            /// <see langword="true" /> if a <see cref="ProjectEntry" /> with GUID 
            /// <paramref name="value"/> is found in the collection; otherwise, 
            /// <see langword="false" />.
            /// </returns>
            public bool Contains(string value) {
                return this[value] != null;
            }
        
            /// <summary>
            /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
            /// </summary>
            /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
            /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            public void CopyTo(ProjectEntry[] array, int index) {
                base.List.CopyTo(array, index);
            }
        
            /// <summary>
            /// Retrieves the index of a specified <see cref="ProjectEntry"/> object in the collection.
            /// </summary>
            /// <param name="item">The <see cref="ProjectEntry"/> object for which the index is returned.</param> 
            /// <returns>
            /// The index of the specified <see cref="ProjectEntry"/>. If the <see cref="ProjectEntry"/> is not currently a member of the collection, it returns -1.
            /// </returns>
            public int IndexOf(ProjectEntry item) {
                return base.List.IndexOf(item);
            }
        
            /// <summary>
            /// Inserts a <see cref="ProjectEntry"/> into the collection at the specified index.
            /// </summary>
            /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
            /// <param name="item">The <see cref="ProjectEntry"/> to insert.</param>
            public void Insert(int index, ProjectEntry item) {
                base.List.Insert(index, item);
            }
        
            /// <summary>
            /// Returns an enumerator that can iterate through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="ProjectEntryEnumerator"/> for the entire collection.
            /// </returns>
            public new ProjectEntryEnumerator GetEnumerator() {
                return new ProjectEntryEnumerator(this);
            }
        
            /// <summary>
            /// Removes a member from the collection.
            /// </summary>
            /// <param name="item">The <see cref="ProjectEntry"/> to remove from the collection.</param>
            public void Remove(ProjectEntry item) {
                base.List.Remove(item);
            }
        
            #endregion Public Instance Methods
        }

        /// <summary>
        /// Enumerates the <see cref="ProjectEntry"/> elements of a <see cref="ProjectEntryCollection"/>.
        /// </summary>
        public class ProjectEntryEnumerator : IEnumerator {
            #region Internal Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ProjectEntryEnumerator"/> class
            /// with the specified <see cref="ProjectEntryCollection"/>.
            /// </summary>
            /// <param name="arguments">The collection that should be enumerated.</param>
            internal ProjectEntryEnumerator(ProjectEntryCollection arguments) {
                IEnumerable temp = (IEnumerable) (arguments);
                _baseEnumerator = temp.GetEnumerator();
            }

            #endregion Internal Instance Constructors

            #region Implementation of IEnumerator
            
            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <returns>
            /// The current element in the collection.
            /// </returns>
            public ProjectEntry Current {
                get { return (ProjectEntry) _baseEnumerator.Current; }
            }

            object IEnumerator.Current {
                get { return _baseEnumerator.Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// <see langword="true" /> if the enumerator was successfully advanced 
            /// to the next element; <see langword="false" /> if the enumerator has 
            /// passed the end of the collection.
            /// </returns>
            public bool MoveNext() {
                return _baseEnumerator.MoveNext();
            }

            bool IEnumerator.MoveNext() {
                return _baseEnumerator.MoveNext();
            }
            
            /// <summary>
            /// Sets the enumerator to its initial position, which is before the 
            /// first element in the collection.
            /// </summary>
            public void Reset() {
                _baseEnumerator.Reset();
            }
            
            void IEnumerator.Reset() {
                _baseEnumerator.Reset();
            }

            #endregion Implementation of IEnumerator

            #region Private Instance Fields
    
            private IEnumerator _baseEnumerator;

            #endregion Private Instance Fields
        }
    }
}
