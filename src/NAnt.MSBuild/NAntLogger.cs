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

using Microsoft.Build.Framework;

using NAnt.Core;
using NAnt.Core.Util;

namespace NAnt.MSBuild {
    internal class NAntLogger : ILogger {
        private readonly Task _task;
        private LoggerVerbosity _verbosity;

        public NAntLogger(Task task, LoggerVerbosity verbosity) {
            _task = task;
            _verbosity = verbosity;
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
                return String.Format("{2}({0},{1}): ", lineNumber, columnNumber,file);
            if (file != "")
                return file + ": ";
            return "";
        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e) {
            string line = String.Format("{1}Error {0}: {2}", e.Code, GetLocation(e.File,e.LineNumber, e.ColumnNumber), e.Message);
            _task.Log(Level.Error, line);
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e) {
            string line = String.Format("{1}Warning {0}: {2}", e.Code, GetLocation(e.File,e.LineNumber, e.ColumnNumber), e.Message);
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
            switch (_verbosity) {
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
            get {
                return "";
            }
            set { 
            }
        }

        public void  Shutdown() {
        }

        public LoggerVerbosity Verbosity {
            get { return _verbosity; }
            set { _verbosity = value; }
        }

        private class DummyTask : Task {
            string _name;

            public DummyTask(Project p, string name) {
                _name = name;
                Project = p;
            }

            public override string Name {
                get { return _name;}
            }

            protected override void ExecuteTask() {
            }
        }
    }
}
