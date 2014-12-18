// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Scott Ford (sford@RJKTECH.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;   
using System.Globalization;
using System.IO;
using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public abstract class ReferenceBase {
        #region Protected Instance Constructors

        protected ReferenceBase(ReferencesResolver referencesResolver, ProjectBase parent) {
            _referencesResolver = referencesResolver;
            _parent = parent;
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets a value indicating whether the output file(s) of this reference 
        /// should be copied locally.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output file(s) of this reference 
        /// should be copied locally; otherwise, <see langword="false" />.
        /// </value>
        public abstract bool CopyLocal {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether this reference represents a system 
        /// assembly.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if this reference represents a system 
        /// assembly; otherwise, <see langword="false" />.
        /// </value>
        protected abstract bool IsSystem {
            get;
        }

        public abstract string Name {
            get;
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the project in which the reference is defined.
        /// </summary>
        protected ProjectBase Parent {
            get { return _parent; }
        }

        protected SolutionTask SolutionTask {
            get { return Parent.SolutionTask; }
        }

        protected ReferencesResolver ReferencesResolver {
            get { return _referencesResolver; }
        }

        #endregion Protected Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Gets the output path of the reference, without taking the "copy local"
        /// setting into consideration.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The full output path of the reference.
        /// </returns>
        public abstract string GetPrimaryOutputFile(Configuration solutionConfiguration);

        /// <summary>
        /// Gets the complete set of output files of the reference for the 
        /// specified configuration.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <param name="outputFiles">The set of output files to be updated.</param>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public abstract void GetOutputFiles(Configuration solutionConfiguration, Hashtable outputFiles);

        /// <summary>
        /// Gets the complete set of assemblies that need to be referenced when
        /// a project references this component.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The complete set of assemblies that need to be referenced when a 
        /// project references this component.
        /// </returns>
        public abstract StringCollection GetAssemblyReferences(Configuration solutionConfiguration);

        /// <summary>
        /// Gets the timestamp of the reference.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// The timestamp of the reference.
        /// </returns>
        public abstract DateTime GetTimestamp(Configuration solutionConfiguration);

        /// <summary>
        /// Gets a value indicating whether the reference is managed for the
        /// specified configuration.
        /// </summary>
        /// <param name="solutionConfiguration">The solution configuration that is built.</param>
        /// <returns>
        /// <see langword="true" /> if the reference is managed for the
        /// specified configuration; otherwise, <see langword="false" />.
        /// </returns>
        public abstract bool IsManaged(Configuration solutionConfiguration);

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Returns the date and time the specified file was last written to.
        /// </summary>
        /// <param name="fileName">The file for which to obtain write date and time information.</param>
        /// <returns>
        /// A <see cref="DateTime" /> structure set to the date and time that 
        /// the specified file was last written to, or 
        /// <see cref="DateTime.MaxValue" /> if the specified file does not
        /// exist.
        /// </returns>
        protected DateTime GetFileTimestamp(string fileName) {
            if (!File.Exists(fileName)) {
                return DateTime.MaxValue;
            }

            return File.GetLastWriteTime(fileName);
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the underlying task.
        /// </remarks>
        protected void Log(Level messageLevel, string message) {
            SolutionTask.Log(messageLevel, message);
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
            SolutionTask.Log(messageLevel, message, args);
        }

        #endregion Protected Instance Methods

        #region Public Static Methods

        public static void GetRelatedFiles(string file, Hashtable relatedFiles) {
            // determine directory of specified file
            string directory = Path.GetDirectoryName(file);

            // check whether the directory of the specified file actually 
            // exists
            if (StringUtils.ConvertEmptyToNull(directory) == null || !Directory.Exists(directory)) {
                return;
            }

            // file itself should always be added
            relatedFiles[file] = Path.GetFileName(file);

            // pattern indicating what files to scan
            string relatedFilesPattern = Path.GetFileName(Path.ChangeExtension(file, ".*"));

            // iterate over each file matching the pattern
            foreach (string relatedFile in Directory.GetFiles(Path.GetDirectoryName(file), relatedFilesPattern)) {
                // ignore files that do not have same base filename as reference file
                // eg. when reference file is MS.Runtime.dll, we do not want files 
                //     named MS.Runtime.Interop.dll
                if (string.Compare(Path.GetFileNameWithoutExtension(relatedFile), Path.GetFileNameWithoutExtension(file), true, CultureInfo.InvariantCulture) != 0) {
                    continue;
                }

                // ignore any other the garbage files created
                string fileExtension = Path.GetExtension(relatedFile).ToLower(CultureInfo.InvariantCulture);
                if (fileExtension != ".dll" && fileExtension != ".xml" && fileExtension != ".pdb" && fileExtension != ".mdb") {
                    continue;
                }

                relatedFiles[relatedFile] = Path.GetFileName(relatedFile);
            }
        }

        #endregion Public Static Methods

        #region Private Instance Fields

        private ProjectBase _parent;
        private ReferencesResolver _referencesResolver;

        #endregion Private Instance Fields
    }
}
