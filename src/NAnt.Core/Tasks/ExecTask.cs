// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Executes a system command.
    /// </summary>
    /// <example>
    ///   <para>Ping nant.sourceforge.net.</para>
    ///   <code><![CDATA[<exec program="ping" commandline="nant.sourceforge.net"/>]]></code>
    /// </example>
    [TaskName("exec")]
    public class ExecTask : ExternalProgramBase {
        #region Private Instance Fields

        string _program = null;
        string _commandline = null;
        string _baseDirectory = null;
        string _workingDirectory = null;
        string _outputFile = null;
        bool _outputAppend = false;
        OptionCollection _environment = new OptionCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The program to execute without command arguments.
        /// </summary>
        [TaskAttribute("program", Required=true)]
        public string FileName {
            get { return _program; }
            set { _program = value; }
        }

        /// <summary>
        /// The command-line arguments for the program.
        /// </summary>
        [TaskAttribute("commandline")]
        public string CommandLineArguments {
            get { return _commandline; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _commandline = value;
                } else {
                    _commandline = null;
                }
            }
        }

        /// <summary>
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElementCollection("environment")]
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
            get { return _workingDirectory; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _workingDirectory = Project.GetFullPath(value);
                } else {
                    _workingDirectory = null;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {
            get {
                if (_baseDirectory == null || Path.IsPathRooted(_program)) {
                    return _program;
                } else {
                    return Path.Combine(Path.GetFullPath(BaseDirectory), _program);
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
            get { return _baseDirectory; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _baseDirectory = Project.GetFullPath(value);
                } else {
                    _baseDirectory = null;
                }
            }
        }

        /// <summary>
        /// The file to which the standard output will be redirected.
        /// </summary>
        /// <remarks>By default, the standard output is redirected to the console.</remarks>
        [TaskAttribute("output", Required=false)]
        public override string OutputFile {
            get { return _outputFile; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _outputFile = Project.GetFullPath(value);
                } else {
                    _outputFile = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether output should be appended 
        /// to the output file. Default value is <c>false</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> if output should be appended to the <see cref="OutputFile" />; 
        /// otherwise, <c>false</c>.
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
            if (WorkingDirectory != null) {
                process.StartInfo.WorkingDirectory = WorkingDirectory;
            }

            foreach (Option option in Environment) {
                if (option.Value == null) {
                    process.StartInfo.EnvironmentVariables[option.OptionName] = "";
                } else {
                    process.StartInfo.EnvironmentVariables[option.OptionName] = option.Value;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase
    }
}
