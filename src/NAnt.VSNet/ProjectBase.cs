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
// Dmitry Jemerov <yole@yole.ru>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Base class for all project classes.
    /// </summary>
    public abstract class ProjectBase {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectBase" /> class.
        /// </summary>
        protected ProjectBase(SolutionTask solutionTask, TempFileCollection temporaryFiles, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) {
            _projectConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _buildConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _extraOutputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _solutionTask = solutionTask;
            _temporaryFiles = temporaryFiles;
            _outputDir = outputDir;
            _gacCache = gacCache;
            _refResolver = refResolver;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the VS.NET project.
        /// </summary>
        public abstract string Name {
            get;
        }

        /// <summary>
        /// Gets the path of the VS.NET project.
        /// </summary>
        public abstract string ProjectPath {
            get;
        }

        /// <summary>
        /// Gets the directory containing the VS.NET project.
        /// </summary>
        public abstract DirectoryInfo ProjectDirectory {
            get;
        }
        
        /// <summary>
        /// Gets or sets the unique identifier of the VS.NET project.
        /// </summary>
        public abstract string Guid {
            get; 
            set;
        }

        public string[] Configurations {
            get {
                return (string[]) new ArrayList(_projectConfigurations.Keys).ToArray(typeof(string));
            }
        }

        /// <summary>
        /// Gets a case-insensitive list of project configurations.
        /// </summary>
        /// <remarks>
        /// The key of the <see cref="Hashtable" /> is the name of the 
        /// configuration and the value is a <see cref="ConfigurationBase" />
        /// instance.
        /// </remarks>
        public Hashtable ProjectConfigurations {
            get { return _projectConfigurations; }
        }

        /// <summary>
        /// Gets a list of project configurations that can be build.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Project configurations that are not in this list do not need to be 
        /// compiled (unless the project was not loaded through a solution file).
        /// </para>
        /// <para>
        /// The key of the <see cref="Hashtable" /> is the name of the 
        /// configuration and the value is a <see cref="ConfigurationBase" />
        /// instance.
        /// </para>
        /// </remarks>
        public Hashtable BuildConfigurations {
            get { return _buildConfigurations; }
        }

        public abstract Reference[] References {
            get;
        }

        public SolutionTask SolutionTask {
            get { return _solutionTask; }
        }

        public TempFileCollection TemporaryFiles {
            get { return _temporaryFiles; }
        }

        /// <summary>
        /// Gets the extra set of output files for the project.
        /// </summary>
        /// <value>
        /// The extra set of output files for the project.
        /// </value>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public Hashtable ExtraOutputFiles {
            get { return _extraOutputFiles; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        protected DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        protected GacCache GacCache {
            get { return _gacCache; }
        }
        
        protected ReferencesResolver ReferencesResolver {
            get { return _refResolver; }
        }

        #endregion Protected Instance Properties

        #region Public Instance Methods

        public bool Compile(string configuration) {
            ConfigurationBase configurationSettings = (ConfigurationBase) ProjectConfigurations[configuration];
            if (configurationSettings == null) {
                Log(Level.Info, "Configuration '{0}' does not exist. Skipping.", configuration);
                return true;
            }

            if (!BuildConfigurations.ContainsKey(configuration)) {
                Log(Level.Info, "Skipping '{0}' [{1}] ...", Name, configuration);
                return true;
            }

            Log(Level.Info, "Building '{0}' [{1}] ...", Name, configuration);

            // ensure output directory exists
            configurationSettings.OutputDir.Create();

            // build the project            
            return Build(configurationSettings);
        }

        public string GetOutputPath(string configuration) {
            ConfigurationBase config = (ConfigurationBase) ProjectConfigurations[configuration];
            if (config == null) {
                return null;
            }

            return config.OutputPath;
        }

        public abstract void Load(Solution sln, string fileName);

        public ConfigurationBase GetConfiguration(string configuration) {
            return (ConfigurationBase) ProjectConfigurations[configuration];
        }

        public StringCollection GetAssemblyReferences(string configuration) {
            ConfigurationBase config = GetConfiguration(configuration);
            if (config == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Configuration '{0}' does not exist for project '{1}'.",
                    configuration, Name), Location.UnknownLocation);
            }
            return GetAssemblyReferences(config);
        }

        public StringCollection GetAssemblyReferences(ConfigurationBase config) {
            Hashtable uniqueReferences = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach (Reference reference in References) {
                StringCollection references = reference.GetAssemblyReferences(config);
                foreach (string assemblyReference in references) {
                    if (!uniqueReferences.ContainsKey(assemblyReference)) {
                        uniqueReferences.Add(assemblyReference, null);
                    }
                }
            }

            StringCollection assemblyReferences = new StringCollection();
            foreach (string assemblyReference in uniqueReferences.Keys) {
                assemblyReferences.Add(assemblyReference);
            }
            return assemblyReferences;
        }

        /// <summary>
        /// Gets the complete set of output files for the project.
        /// configuration.
        /// </summary>
        /// <value>
        /// The complete set of output files for the project.
        /// </value>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public Hashtable GetOutputFiles(string configuration) {
            ConfigurationBase config = GetConfiguration(configuration);
            if (config == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Configuration '{0}' does not exist for project '{1}'.",
                    configuration, Name), Location.UnknownLocation);
            }

            Hashtable outputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach (Reference reference in References) {
                if (!reference.CopyLocal) {
                    continue;
                }

                Hashtable referenceOutputFiles = reference.GetOutputFiles(config);
                foreach (DictionaryEntry de in referenceOutputFiles) {
                    outputFiles[de.Key] = de.Value;
                }
            }

            // determine output file of project
            string projectOutputFile = config.OutputPath;

            // ensure output file exists
            if (!File.Exists(projectOutputFile)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Couldn't find output file '{0}' for project '{1}'.",
                    projectOutputFile, Name), Location.UnknownLocation);
            }

            // get list of files related to project output file (eg. debug symbols,
            // xml doc, ...), this will include the project output file itself
            Hashtable relatedFiles = Reference.GetRelatedFiles(projectOutputFile);

            // add each related file to set of primary output files
            foreach (DictionaryEntry de in relatedFiles) {
                outputFiles[(string) de.Key] = (string) de.Value;
            }

            // add extra project-level output files
            foreach (DictionaryEntry de in ExtraOutputFiles) {
                outputFiles[(string) de.Key] = (string) de.Value;
            }

            // add extra configuration-level output files
            foreach (DictionaryEntry de in config.ExtraOutputFiles) {
                outputFiles[(string) de.Key] = (string) de.Value;
            }

            // return output files for the project
            return outputFiles;
        }

        #endregion Public Instance Methods

        #region Protected Internal Instance Methods

        /// <summary>
        /// Expands the given macro.
        /// </summary>
        /// <param name="macro">The macro to expand.</param>
        /// <returns>
        /// The expanded macro or <see langword="null" /> if the macro is not
        /// supported.
        /// </returns>
        protected internal virtual string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "projectname": // E.g. WindowsApplication1
                    return Name;
                case "projectpath": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\WindowsApplications1.csproj
                    return ProjectPath;
                case "projectfilename": // E.g. WindowsApplication1.csproj
                    return Path.GetFileName(ProjectPath);
                case "projectext": // .csproj
                    return Path.GetExtension(ProjectPath);
                case "projectdir": // ProjectPath without ProjectFileName at the end
                    return Path.GetDirectoryName(ProjectPath) 
                        + Path.DirectorySeparatorChar;
                default:
                    return null;
            }
        }

        #endregion Protected Internal Instance Methods

        #region Protected Instance Methods

        protected abstract bool Build(ConfigurationBase configurationSettings);

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        protected void Log(Level messageLevel, string message) {
            if (SolutionTask != null) {
                SolutionTask.Log(messageLevel, message);
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
            if (SolutionTask != null) {
                SolutionTask.Log(messageLevel, message, args);
            }
        }

        #endregion Protected Instance Methods

        #region Protected Static Methods

        protected static XmlDocument LoadXmlDocument(string fileName) {
            return ProjectFactory.LoadProjectXml(fileName);
        }

        #endregion Protected Static Methods

        #region Private Static Methods

        #endregion Private Static Methods

        #region Private Instance Fields

        private SolutionTask _solutionTask;
        private TempFileCollection _temporaryFiles;
        private DirectoryInfo _outputDir;
        private Hashtable _projectConfigurations;
        private Hashtable _buildConfigurations;
        private GacCache _gacCache;
        private ReferencesResolver _refResolver;
        private Hashtable _extraOutputFiles;

        #endregion Private Instance Fields
    }
}
