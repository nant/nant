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
        protected ProjectBase(SolutionTask solutionTask, TempFileCollection tempFiles, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) {
            _projectConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _buildConfigurations = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _solutionTask = solutionTask;
            _tempFiles = tempFiles;
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

        #endregion Public Instance Properties

        #region Protected Instance Properties

        protected SolutionTask SolutionTask {
            get { return _solutionTask; }
        }

        protected TempFileCollection TempFiles {
            get { return _tempFiles; }
        }

        protected DirectoryInfo OutputDir {
            get { return _outputDir; }
        }

        protected string LogPrefix {
            get { 
                if (SolutionTask != null) {
                    return SolutionTask.LogPrefix;
                }

                return string.Empty;
            }
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
                Log(Level.Info, LogPrefix + "Configuration '{0}' does not exist. Skipping.", configuration);
                return true;
            }

            if (!BuildConfigurations.ContainsKey(configuration)) {
                Log(Level.Info, LogPrefix + "Skipping '{0}' [{1}]...", Name, configuration);
                return true;
            }

            Log(Level.Info, LogPrefix + "Building '{0}' [{1}]...", Name, configuration);

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

        #endregion Public Instance Methods

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
        private TempFileCollection _tempFiles;
        private DirectoryInfo _outputDir;
        private Hashtable _projectConfigurations;
        private Hashtable _buildConfigurations;
        private GacCache _gacCache;
        private ReferencesResolver _refResolver;

        #endregion Private Instance Fields
    }
}
