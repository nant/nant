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

namespace SourceForge.NAnt.Tasks {

    using System;
    using System.IO;
    using SourceForge.NAnt.Attributes;

    /// <summary>Executes a system command.</summary>
    /// <example>
    ///   <para>Ping nant.sourceforge.net.</para>
    ///   <code><![CDATA[<exec program="ping" commandline="nant.sourceforge.net"/>]]></code>
    /// </example>
    [TaskName("exec")]
    public class ExecTask : ExternalProgramBase {
        
        string _program = null;
        string _commandline = null;
        string _baseDirectory = null;
        string _workingDirectory = null;
        int _timeout = Int32.MaxValue;
        string _outputFile = null;
        bool _outputAppend = false;
      
        /// <summary>The program to execute without command arguments.</summary>
        [TaskAttribute("program", Required=true)]
        public string FileName  { set { _program = value; } }                
                
        /// <summary>The command line arguments for the program.</summary>
        [TaskAttribute("commandline")]public string Arguments { set { _commandline = value; } }

        /// <summary>The file to which the standard output will be redirected.</summary>
        /// <remarks>By default, the standard output is redirected to the console.</remarks>
        [TaskAttribute("output", Required=false)]
        public string Output { set { _outputFile = value; } }
		
        /// <summary>true if the output file is to be appended to.</summary>
        /// <remarks>False by default.</remarks>
        [TaskAttribute("append", Required=false)]
        [BooleanValidator()]
        public bool Append { set { _outputAppend = value; } }

        public override string ProgramFileName  { 
            get { 
                if( null == _baseDirectory || Path.IsPathRooted(_program) ) {
                    return _program;
                }
                else {
                    return Path.GetFullPath( Path.Combine( BaseDirectory, _program ));
                }
            }
        }    
        public override string ProgramArguments { get { return _commandline; } }
        
        /// <summary>The directory the program is in.</summary>
        /// <remarks><para>The basedir will be evaluated relative to the project's BaseDirectory if it is relative.</para></remarks>
        [TaskAttribute("basedir")]
        public override string BaseDirectory    { get { return Project.GetFullPath(_baseDirectory); } set { _baseDirectory = value; } }

        /// <summary>The directory in which the command will be executed.</summary>
        /// <remarks><para>The working will be evaluated relative to the project's BaseDirectory if it is relative.</para></remarks>
        [TaskAttribute("workingdir")]
        public virtual string WorkingDirectory    { get { return Project.GetFullPath(_workingDirectory); } set { _workingDirectory = value; } }
        
        public override string OutputFile { get { return _outputFile; } }
        
        public override bool OutputAppend { get { return _outputAppend; } }

        /// <summary>Stop the build if the command does not finish within the specified time.  Specified in milliseconds.  Default is no time out.</summary>
        [TaskAttribute("timeout")]
        [Int32Validator()]
        public override int TimeOut { get { return _timeout; } set { _timeout = value; }  }
        
        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + "{0} {1}", ProgramFileName, GetCommandLine());
            base.ExecuteTask();
        }

        protected override void PrepareProcess(ref System.Diagnostics.Process process) {
            base.PrepareProcess(ref process);
            if(_workingDirectory != null) {
                process.StartInfo.WorkingDirectory = WorkingDirectory;
            }
        }
    }
}
