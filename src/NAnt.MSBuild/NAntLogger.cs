// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System;
using NAnt.Core;
using System.CodeDom.Compiler;
using System.Reflection;

namespace NAnt.MSBuild {

    /// <summary>
    /// Enum indicating the level of verbosity for the NAnt logger.
    /// </summary>
    public enum NAntLoggerVerbosity {
        /// <summary>Indicates no output</summary>
        Quiet,
        
        /// <summary>Indicates little output</summary>
        Minimal,
        
        /// <summary>Indicates normal output</summary>
        Normal,
        
        /// <summary>Indicates detailed output</summary>
        Detailed,
        
        /// <summary>Indicates all output</summary>
        Diagnostic
    };
    
    //internal interface ILogger { }
    //internal interface IEventSource { }
    //internal class BuildErrorEventArgs { }
    //internal class BuildWarningEventArgs { }
    //internal class BuildMessageEventArgs { }
    //internal class TaskStartedEventArgs  { }
    //internal class TaskFinishedEventArgs { }

    /// <summary>
    /// Logger classed used for MSBuild tasks in NAnt.
    /// </summary>
    public class NAntLogger {
    
        internal static NAntLogger Create(NAnt.Core.FrameworkInfo framework, Task task, NAntLoggerVerbosity verbosity, NAnt.MSBuild.BuildEngine.Engine engine) {
            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("C#");
            CompilerParameters par = new CompilerParameters();
            AssemblyName msbuildFrameworkName = engine.Assembly.GetName();
            msbuildFrameworkName.Name = "Microsoft.Build.Framework";
            Assembly msbuildFramework = Assembly.Load(msbuildFrameworkName);

            par.ReferencedAssemblies.Add(msbuildFramework.Location);
            par.ReferencedAssemblies.Add(typeof(NAnt.Core.Task).Assembly.Location);
            par.ReferencedAssemblies.Add(typeof(NAntLogger).Assembly.Location);
            par.GenerateInMemory = true;
            CompilerResults res = codeDomProvider.CompileAssemblyFromSource(par, _impl);
            if (res.Errors.HasErrors) {
                return null;
            }

            Type t = res.CompiledAssembly.GetType("NAntLoggerImpl");
            return (NAntLogger)Activator.CreateInstance(t, task, verbosity);
        }

        private const string _impl = @"
    using System;
    using Microsoft.Build.Framework;
    using NAnt.Core;
    using NAnt.MSBuild;
    internal class NAntLoggerImpl : NAntLogger, ILogger {
        private readonly Task _task;
        private LoggerVerbosity _verbosity;

        public NAntLoggerImpl(Task task, NAntLoggerVerbosity verbosity) {
            _task = task;
            _verbosity = (LoggerVerbosity)verbosity;
        }

        /// <summary>
        /// Initialize is guaranteed to be called by MSBuild at the start of the build
        /// before any events are raised.
        /// </summary>
        public void Initialize(IEventSource eventSource) {
            eventSource.TaskStarted += new TaskStartedEventHandler(eventSource_TaskStarted);
            eventSource.TaskFinished += new TaskFinishedEventHandler(eventSource_TaskFinished);
            eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
            eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
        }

        private string GetLocation(string file,int lineNumber, int columnNumber) {
            if (lineNumber != 0 || columnNumber != 0)
                return String.Format(""{2}({0},{1}): "", lineNumber, columnNumber,file);
            if (file.Length != 0)
                return file + "": "";
            return string.Empty;
        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e) {
            string line = String.Format(""{1}Error {0}: {2}"", e.Code, GetLocation(e.File,e.LineNumber, e.ColumnNumber), e.Message);
            _task.Log(Level.Error, line);
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e) {
            string line = String.Format(""{1}Warning {0}: {2}"", e.Code, GetLocation(e.File,e.LineNumber, e.ColumnNumber), e.Message);
            _task.Log(Level.Warning, line);
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e) {
            Level lev;
            switch (e.Importance) {
                case MessageImportance.High:
                    lev = Level.Info;
                    break;
                case MessageImportance.Low:
                    lev = Level.Debug;
                    break;
                default: //MessageImportance.Normal
                    lev = Level.Verbose;
                    break;
            }
            switch (Verbosity) {
                case LoggerVerbosity.Quiet:
                    lev -= 2000;
                    break;
                case LoggerVerbosity.Minimal:
                    lev -= 1000;
                    break;
                case LoggerVerbosity.Detailed:
                    lev += 1000;
                    break;
                case LoggerVerbosity.Diagnostic:
                    lev += 2000;
                    break;
            }
            _task.Log(lev, e.Message);
        }

        void eventSource_TaskStarted(object sender, TaskStartedEventArgs e) {
            Task task = new DummyTask(_task.Project, e.TaskName);
            _task.Project.OnTaskStarted(sender, new NAnt.Core.BuildEventArgs(task));
        }

        void eventSource_TaskFinished(object sender, TaskFinishedEventArgs e) {
            Task task = new DummyTask(_task.Project, e.TaskName);
            _task.Project.OnTaskFinished(sender, new NAnt.Core.BuildEventArgs(task));
        }

        public string Parameters {
            get { return string.Empty; }
            set {  }
        }

        public void Shutdown() {
        }

        public LoggerVerbosity Verbosity {
            get { return _verbosity; }
            set { _verbosity = value; }
        }
    }
    ";

        /// <summary>Sample task used for testing.</summary>
        protected class DummyTask : Task {
            string _name;
            
            /// <summary>
            /// Sample task constructor.
            /// </summary>
            /// <param name="p">Project to assign task to.</param>
            /// <param name="name">Sample name property.</param>
            public DummyTask(Project p, string name) {
                _name = name;
                Project = p;
            }

            /// <summary>Gets sample name for task.</summary>
            public override string Name {
                get { return _name; }
            }

            /// <summary>Test method.</summary>
            protected override void ExecuteTask() {
            }
        }
    }
}
