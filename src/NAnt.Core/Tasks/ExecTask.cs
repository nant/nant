// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Executes a system command.
    /// </summary>
    /// <example>
    ///   <para>Ping nant.sourceforge.net.</para>
    ///   <code>
    ///     <![CDATA[
    /// <exec program="ping" commandline="nant.sourceforge.net" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("exec")]
    public class ExecTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _program = null;
        private string _commandline = null;
        private string _baseDirectory = null;
        private string _workingDirectory = null;
        private string _outputFile = null;
        private bool _outputAppend = false;
        private OptionCollection _environment = new OptionCollection();
        private bool _useRuntimeEngine = false;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The program to execute without command arguments.
        /// </summary>
        [TaskAttribute("program", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string FileName {
            get { return _program; }
            set { _program = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The command-line arguments for the program.
        /// </summary>
        [TaskAttribute("commandline")]
        public string CommandLineArguments {
            get { return _commandline; }
            set { _commandline = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElementCollection("environment", "option")]
        public OptionCollection Environment {
            get { return _environment; }
        }

        /// <summary>
        /// The directory in which the command will be executed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The working directory will be evaluated relative to the project's
        /// baseDirectory if it is relative.
        /// </para>
        /// </remarks>
        [TaskAttribute("workingdir")]
        public string WorkingDirectory {
            get { return Project.GetFullPath(_workingDirectory); }
            set { _workingDirectory = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Specifies whether the external program should be executed using a 
        /// runtime engine, if configured. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the external program should be executed 
        /// using a runtime engine; otherwise, <see langword="false" />.
        /// </value>
        [TaskAttribute("useruntimeengine")]
        [FrameworkConfigurable("useruntimeengine")]
        public override bool UseRuntimeEngine {
            get { return _useRuntimeEngine; }
            set { _useRuntimeEngine = value; }
        }

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {
            get {
                if (_baseDirectory == null || Path.IsPathRooted(FileName)) {
                    return FileName;
                } else {
                    return Path.Combine(Path.GetFullPath(BaseDirectory), FileName);
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
            get { return _commandline; }
        }
        
        /// <summary>
        /// The directory the program is in.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The basedir will be evaluated relative to the project's baseDirectory 
        /// if it is relative.
        /// </para>
        /// </remarks>
        [TaskAttribute("basedir")]
        public override string BaseDirectory {
            get { return Project.GetFullPath(_baseDirectory); }
            set { _baseDirectory = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The file to which the standard output will be redirected.
        /// </summary>
        /// <remarks>
        /// By default, the standard output is redirected to the console.
        /// </remarks>
        [TaskAttribute("output", Required=false)]
        public override string OutputFile {
            get { return (_outputFile != null) ? Project.GetFullPath(_outputFile) : null; }
            set { _outputFile = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether output should be appended 
        /// to the output file. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output should be appended to the <see cref="OutputFile" />; 
        /// otherwise, <see langword="false" />.
        /// </value>
        [TaskAttribute("append", Required=false)]
        public override bool OutputAppend {
            get { return _outputAppend; }
            set { _outputAppend = value; }
        }

        /// <summary>
        /// Executes the external program.
        /// </summary>
        protected override void ExecuteTask() {
            Log(Level.Info, LogPrefix + "{0} {1}", ProgramFileName, CommandLine);
            base.ExecuteTask();
        }

        protected override void PrepareProcess(System.Diagnostics.Process process) {
            base.PrepareProcess(process);
            // only set WorkingDirectory if it was explicitly set, otherwise
            // leave default (which is BaseDirectory)
            if (_workingDirectory != null) {
                process.StartInfo.WorkingDirectory = WorkingDirectory;
            }

            foreach (Option option in Environment) {
                if (option.IfDefined && !option.UnlessDefined) {
                    if (option.Value == null) {
                        process.StartInfo.EnvironmentVariables[option.OptionName] = "";
                    } else {
                        process.StartInfo.EnvironmentVariables[option.OptionName] = option.Value;
                    }
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase
    }
}
