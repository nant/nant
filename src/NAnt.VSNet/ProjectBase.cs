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
using System.CodeDom.Compiler;
using System.IO;
using System.Xml;

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
        protected ProjectBase(SolutionTask solutionTask, TempFileCollection tempFiles, DirectoryInfo outputDir) {
            _solutionTask = solutionTask;
            _tempFiles = tempFiles;
            _outputDir = outputDir;
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

        public abstract string[] Configurations {
            get;
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

        #endregion Protected Instance Properties

        #region Public Instance Methods

        public abstract string GetOutputPath(string configuration);

        public abstract bool Compile(string configuration, ArrayList alCSCArguments, string strLogFile, bool bVerbose, bool bShowCommands);

        public abstract void Load(Solution sln, string fileName);

        public abstract ConfigurationBase GetConfiguration(string configuration);

        #endregion Public Instance Methods

        #region Protected Static Methods

        protected static XmlDocument LoadXmlDocument(string fileName) {
            XmlDocument doc = new XmlDocument();
            if (!ProjectFactory.IsUrl(fileName)) {
                doc.Load(fileName);
            } else {
                Uri uri = new Uri(fileName);
                if (uri.Scheme == Uri.UriSchemeFile) {
                    doc.Load(uri.LocalPath);
                } else {
                    doc.LoadXml(WebDavClient.GetFileContentsStatic(fileName));
                }
            }

            return doc;
        }

        #endregion Protected Static Methods

        #region Private Static Methods

        #endregion Private Static Methods

        #region Private Instance Fields

        private SolutionTask _solutionTask;
        private TempFileCollection _tempFiles;
        private DirectoryInfo _outputDir;

        #endregion Private Instance Fields
    }
}
