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

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
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
    public class AssemblyLinkerTask : ExternalProgramBase {
        #region Private Instance Fields

        string _arguments;
        string _output = null;
        string _target = null;
        string _culture = null;
        string _template = null;
        FileSet _sources = new FileSet();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The name of the output file for the assembly manifest.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/out</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("output", Required=true)]
        public string Output {
            get { return _output; }
            set {_output = value; }
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
            set {_target = value; }
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
            set {_culture = value; }
        }
         
        /// <summary>
        /// Specifies an assembly from which to get all options except the culture field.
        /// The given filename must have a strong name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Corresponds with the <c>/template:</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("template", Required=false)]
        public string Template {
            get { return _template; }
            set {_template = value; }
        }

        /// <summary>
        /// The set of source files to embed.
        /// </summary>
        [FileSet("sources")]
        public FileSet Sources {
            get { return _sources; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties
        
        /// <summary>
        /// Gets the complete output path.
        /// </summary>
        /// <value>The complete output path.</value>
        protected string OutputPath {
            get { return Path.GetFullPath(Path.Combine(BaseDirectory, Output)); }
        }

        #endregion Protected Instance Properties

        #region Override implementation of ExternalProgramBase
           
        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {
            get { 
                if (Project.CurrentFramework != null) {
                    string FrameworkDir = Project.CurrentFramework.FrameworkDirectory.FullName;
                    return Path.Combine(FrameworkDir, ExeName + ".exe");
                } else {
                    return ExeName;
                }
            }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return _arguments; }
        }

        /// <summary>
        /// Generates an assembly manifest.
        /// </summary>
        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                StringBuilder sb = new StringBuilder();
                StringWriter writer = new StringWriter(sb, CultureInfo.InvariantCulture);

                try {
                    if (Sources.BaseDirectory == null) {
                        Sources.BaseDirectory = BaseDirectory;
                    }

                    Log.WriteLine(LogPrefix + "Compiling {0} files to {1}", Sources.FileNames.Count, OutputPath);

                    // Microsoft common compiler options
                    writer.Write(" /t:{0}", OutputTarget);
                    writer.Write(" /out:\"{0}\"", OutputPath);

                    if (Culture != null) {
                        writer.Write(" /culture:{0}", Culture);
                    }

                    if (Template != null) {
                        writer.Write(" /template:\"{0}\"", Template);
                    }

                    foreach (string fileName in Sources.FileNames) {
                        writer.Write(" /embed:\"{0}\"", fileName);
                    }

                    // Make sure to close the response file otherwise contents
                    // Will not be written to disk and ExecuteTask() will fail.
                    writer.Close();
                    _arguments = sb.ToString();

                    // display response file contents
                    Log.WriteLineIf(Verbose, _arguments);

                    // call base class to do the work
                    base.ExecuteTask();
                } finally {
                    // make sure we delete response file even if an exception is thrown
                    writer.Close(); // make sure stream is closed or file cannot be deleted
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
            FileInfo outputFileInfo = new FileInfo(OutputPath);
            if (!outputFileInfo.Exists) {
                return true;
            }

            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        #endregion Protected Instance Methods
    }
}
