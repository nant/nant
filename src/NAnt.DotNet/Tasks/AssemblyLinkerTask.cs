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
// Joe Jones (joejo@microsoft.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Wraps Al.exe, the assembly linker for the .NET Framework.
    /// </summary>
    /// <remarks>
    ///   <para>All specified sources will be embedded using the <c>/embed</c> flag.  Other source types are not supported.</para>
    /// </remarks>
    /// <example>
    ///   <para>Create a library containing all icon files in the current directory.</para>
    ///   <code>
    /// <![CDATA[
    /// <al output="MyIcons.dll" target="lib">
    ///     <sources>
    ///         <includes name="*.ico"/>
    ///     </sources>
    /// </al>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("al")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class AssemblyLinkerTask : NAnt.Core.Tasks.ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName = null;
        private string _output = null;
        private string _target = null;
        private string _culture = null;
        private string _template = null;
        private string _keyfile = null;
        private FileSet _sources = new FileSet();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file for the assembly manifest.
        /// </summary>
        /// <value>The complete output path for the assembly manifest.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/out</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("output", Required=true)]
        public string Output {
            get { return _output; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _output = Project.GetFullPath(value);
                } else {
                    _output = null;
                }
            }
        }
        
        /// <summary>
        /// The target type (one of "lib", "exe", or "winexe").
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/t[arget]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("target", Required=true)]
        public string OutputTarget {
            get { return _target; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _target = value;
                } else {
                    _target = null;
                }
            }
        }

        /// <summary>
        /// The culture string associated with the output assembly.
        /// The string must be in RFC 1766 format, such as "en-US".
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/c[ulture]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("culture", Required=false)]
        public string Culture {
            get { return _culture; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _culture = value;
                } else {
                    _culture = null;
                }
            }
        }
         
        /// <summary>
        /// Specifies an assembly from which to get all options except the 
        /// culture field.
        /// </summary>
        /// <value>The complete path to the assembly template.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/template:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("template", Required=false)]
        public string Template {
            get { return _template; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _template = Project.GetFullPath(value);
                } else {
                    _template = null;
                }
            }
        }

        /// <summary>
        /// Specifies a file (filename) that contains a key pair or
        /// just a public key to sign an assembly.
        /// </summary>
        /// <value>The complete path to the key file.</value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/keyf[ile]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("keyfile", Required=false)]
        public string KeyFile {
            get { return _keyfile; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _keyfile = Project.GetFullPath(value);
                } else {
                    _keyfile = null;
                }
            }
        }

        /// <summary>
        /// The set of source files to embed.
        /// </summary>
        [FileSet("sources")]
        public FileSet Sources {
            get { return _sources; }
            set {_sources = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program or 
        /// <see langword="null" /> if the task is not being executed.
        /// </value>
        public override string ProgramArguments {
            get { 
                if (_responseFileName != null) {
                    return "@" + _responseFileName; 
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Generates an assembly manifest.
        /// </summary>
        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();
                StreamWriter writer = new StreamWriter(_responseFileName);

                try {
                    if (Sources.BaseDirectory == null) {
                        Sources.BaseDirectory = BaseDirectory;
                    }

                    Log(Level.Info, LogPrefix + "Compiling {0} files to {1}.", Sources.FileNames.Count, Output);

                    // write output target
                    writer.Write(" /target:{0}", OutputTarget);

                    // write output file
                    writer.Write(" /out:\"{0}\"", Output);

                    if (Culture != null) {
                        writer.Write(" /culture:{0}", Culture);
                    }

                    if (Template != null) {
                        writer.Write(" /template:\"{0}\"", Template);
                    }

                    if (KeyFile != null) {
                        writer.Write(" /keyfile:\"{0}\"", KeyFile);
                    }

                    foreach (string fileName in Sources.FileNames) {
                        writer.Write(" /embed:\"{0}\"", fileName);
                    }

                    // Make sure to close the response file otherwise contents
                    // Will not be written to disk and ExecuteTask() will fail.
                    writer.Close();

                    if (Verbose) {
                        // display response file contents
                        Log(Level.Verbose, LogPrefix + "Contents of {0}.", _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Verbose, reader.ReadToEnd());
                        reader.Close();
                    }

                    // call base class to do the work
                    base.ExecuteTask();
                } finally {
                    // make sure stream is closed or response file cannot be deleted
                    writer.Close(); 
                    // make sure we delete response file even if an exception is thrown
                    File.Delete(_responseFileName);
                    // initialize name of response file
                    _responseFileName = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the assembly manifest needs compiling or is 
        /// uptodate.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the assembly manifest needs compiling; otherwise,
        /// <c>false</c>.
        /// </returns>
        protected virtual bool NeedsCompiling() {
            FileInfo outputFileInfo = new FileInfo(Output);
            if (!outputFileInfo.Exists) {
                return true;
            }

            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "{0} is out of date.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        #endregion Protected Instance Methods
    }
}
