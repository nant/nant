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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Wraps <c>al.exe</c>, the assembly linker for the .NET Framework.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   All specified sources will be embedded using the <c>/embed</c> flag.  
    ///   Other source types are not supported.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Create a library containing all icon files in the current directory.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <al output="MyIcons.dll" target="lib">
    ///     <sources>
    ///         <includes name="*.ico" />
    ///     </sources>
    /// </al>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("al")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class AssemblyLinkerTask : NAnt.Core.Tasks.ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private FileInfo _outputFile;
        private string _target;
        private string _culture;
        private FileInfo _templateFile;
        private FileInfo _keyfile;
        private FileSet _resources = new FileSet();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file for the assembly manifest.
        /// </summary>
        /// <value>
        /// The complete output path for the assembly manifest.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/out</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }
        
        /// <summary>
        /// The target type (one of <c>lib</c>, <c>exe</c>, or <c>winexe</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/t[arget]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("target", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string OutputTarget {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
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
            set { _culture = StringUtils.ConvertEmptyToNull(value); }
        }
         
        /// <summary>
        /// Specifies an assembly from which to get all options except the 
        /// culture field.
        /// </summary>
        /// <value>
        /// The complete path to the assembly template.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/template:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("template", Required=false)]
        public FileInfo TemplateFile {
            get { return _templateFile; }
            set { _templateFile = value; }
        }

        /// <summary>
        /// Specifies a file (filename) that contains a key pair or
        /// just a public key to sign an assembly.
        /// </summary>
        /// <value>
        /// The complete path to the key file.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/keyf[ile]:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("keyfile", Required=false)]
        public FileInfo KeyFile {
            get { return _keyfile; }
            set { _keyfile = value; }
        }

        /// <summary>
        /// The set of resources to embed.
        /// </summary>
        [BuildElement("sources")]
        public FileSet Resources {
            get { return _resources; }
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
                    return "@" + "\"" + _responseFileName + "\""; 
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Generates an assembly manifest.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Resources.BaseDirectory == null) {
                Resources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();
                StreamWriter writer = new StreamWriter(_responseFileName);

                try {
                    Log(Level.Info, LogPrefix + "Compiling {0} files to '{1}'.", Resources.FileNames.Count, OutputFile.FullName);

                    // write output target
                    writer.WriteLine("/target:\"{0}\"", OutputTarget);

                    // write output file
                    writer.WriteLine("/out:\"{0}\"", OutputFile.FullName);

                    // write culture associated with output assembly
                    if (Culture != null) {
                        writer.WriteLine("/culture:\"{0}\"", Culture);
                    }
                    
                    // suppresses display of the sign-on banner
                    writer.WriteLine("/nologo");
                    
                    // write path to template assembly
                    if (TemplateFile != null) {
                        writer.WriteLine("/template:\"{0}\"", TemplateFile.FullName);
                    }

                    if (KeyFile != null) {
                        writer.WriteLine("/keyfile:\"{0}\"", KeyFile.FullName);
                    }

                    // write embedded resources to response file
                    foreach (string resourceFile in Resources.FileNames) {
                        writer.WriteLine("/embed:\"{0}\"", resourceFile);
                    }

                    // make sure to close the response file otherwise contents
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
        /// <see langword="true" /> if the assembly manifest needs compiling; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        protected virtual bool NeedsCompiling() {
            if (!OutputFile.Exists) {
                return true;
            }

            // check if (embedded)resources were updated
            string fileName = FileSet.FindMoreRecentLastWriteTime(Resources.FileNames, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // check if template file was updated
            if (TemplateFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(TemplateFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                    return true;
                }
            }

            // check if key file was updated
            if (KeyFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(KeyFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                    return true;
                }
            }

            // check the arguments for /embed or /embedresource options
            StringCollection embeddedResourceFiles = new StringCollection();
            foreach (Argument argument in Arguments) {
                if (argument.IfDefined && !argument.UnlessDefined) {
                    string argumentValue = argument.Value;
                    // check whether argument specifies resource file to embed
                    if (argumentValue != null && (argumentValue.StartsWith("/embed:") || argumentValue.StartsWith("/embedresource:"))) {
                        // determine path to resource file
                        string path = argumentValue.Substring(argumentValue.IndexOf(':') + 1);
                        int indexOfComma = path.IndexOf(',');
                        if (indexOfComma != -1) {
                            path = path.Substring(0, indexOfComma);
                        }
                        // resolve path to full path (relative to project base dir)
                        path = Project.GetFullPath(path);
                        // add path to collection of resource files
                        embeddedResourceFiles.Add(path);
                    }
                }
            }

            // check if embedded resources passed as arguments were updated
            fileName = FileSet.FindMoreRecentLastWriteTime(embeddedResourceFiles, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, LogPrefix + "'{0}' has been updated, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        #endregion Protected Instance Methods
    }
}
