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
using System.IO;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
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
        int _timeout = Int32.MaxValue;
        string _outputFile = null;
        bool _outputAppend = false;
        OptionElementCollection _environment = new OptionElementCollection();

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
            set { _commandline = value; }
        }

        /// <summary>
        /// Environment variables to pass to the program.
        /// </summary>
        [BuildElementCollection("environment")]
        public OptionElementCollection Environment {
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
            set { _workingDirectory = value; }
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
            get { return Project.GetFullPath(_baseDirectory); }
            set { _baseDirectory = value; }
        }

        /// <summary>
        /// The file to which the standard output will be redirected.
        /// </summary>
        /// <remarks>By default, the standard output is redirected to the console.</remarks>
        [TaskAttribute("output", Required=false)]
        public override string OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// true if the output file is to be appended to. Default value is <c>false</c>.
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
        /// Stop the build if the command does not finish within the specified time. 
        /// Specified in milliseconds. Default is no time out.
        /// </summary>
        [TaskAttribute("timeout")]
        [Int32Validator()]
        public override int TimeOut {
            get { return _timeout; }
            set { _timeout = value; }
        }

        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + "{0} {1}", ProgramFileName, CommandLine);
            base.ExecuteTask();
        }

        protected override void PrepareProcess(System.Diagnostics.Process process) {
            base.PrepareProcess(process);
            if (_workingDirectory != null) {
                process.StartInfo.WorkingDirectory = WorkingDirectory;
            }

            foreach (OptionElement option in Environment) {
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
