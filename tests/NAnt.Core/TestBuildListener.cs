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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;

using NAnt.Core;

namespace Tests.NAnt.Core {
    public class TestBuildListener : IBuildListener {
        #region Public Instance Constructors
        
        public TestBuildListener() {
            _executedTargets = new Hashtable();
            _executedTasks = new Hashtable();
            _targetStartTimes = new Hashtable();
            _targetFinishTimes = new Hashtable();
            _loggedMessages = new ArrayList();
        }

        #endregion Public Instance Constructors

        #region Implementation of IBuildListener

        public void BuildStarted(object sender, BuildEventArgs e) {
            _buildStartedFired = true;
        }

        public void BuildFinished(object sender, BuildEventArgs e) {
            _buildFinishedFired = true;
        }

        public void TargetStarted(object sender, BuildEventArgs e) {
            _targetStartedFired = true;

            if (e.Target != null) {
                if (_executedTargets.ContainsKey(e.Target.Name)) {
                    _executedTargets[e.Target.Name] = ((int) _executedTargets[e.Target.Name]) + 1;
                } else {
                    _executedTargets.Add(e.Target.Name, 1);
                }
                _targetStartTimes[e.Target.Name] = DateTime.UtcNow;
            }
        }

        public void TargetFinished(object sender, BuildEventArgs e) {
            _targetFinishedFired = true;
            _targetFinishTimes[e.Target.Name] = DateTime.UtcNow;
        }

        public void MessageLogged(object sender, BuildEventArgs e) {
            _loggedMessages.Add(e);
        }

        public void TaskStarted(object sender, BuildEventArgs e) {
            _taskStartedFired = true;

            if (e.Task != null) {
                if (_executedTasks.ContainsKey(e.Task.Name)) {
                    _executedTasks[e.Task.Name] = ((int) _executedTasks[e.Task.Name]) + 1;
                } else {
                    _executedTasks.Add(e.Task.Name, 1);
                }
            }
        }

        public void TaskFinished(object sender, BuildEventArgs e) {
            _taskFinishedFired = true;
        }

        #endregion Implementation of IBuildListener

        #region Public Instance Methods

        public int GetTargetExecutionCount(string target) {
            if (_executedTargets.ContainsKey(target)) {
                return (int) _executedTargets[target];
            } else {
                return 0;
            }
        }

        public DateTime GetTargetStartTime(string target) {
            if (_targetStartTimes.ContainsKey(target)) {
                return (DateTime) _targetStartTimes[target];
            } else {
                return DateTime.MinValue;
            }
        }

        public DateTime GetTargetFinishTime(string target) {
            if (_targetFinishTimes.ContainsKey(target)) {
                return (DateTime) _targetFinishTimes[target];
            } else {
                return DateTime.MinValue;
            }
        }

        public int GetTaskExecutionCount(string task) {
            if (_executedTasks.ContainsKey(task)) {
                return (int) _executedTasks[task];
            } else {
                return 0;
            }
        }

        public bool HasMessageBeenLogged(string message, bool exact) {
            foreach (BuildEventArgs buildEvent in _loggedMessages) {
                if (exact) {
                    if (buildEvent.Message == message) {
                        return true;
                    }
                } else if (buildEvent.Message.IndexOf(message) >= 0) {
                    return true;
                }
            }

            return false;
        }

        public bool HasMessageBeenLogged(Level level, string message, bool exact) {
            foreach (BuildEventArgs buildEvent in _loggedMessages) {
                if (buildEvent.MessageLevel == level) {
                    if (exact) {
                        if (buildEvent.Message == message) {
                            return true;
                        }
                    } else if (buildEvent.Message.IndexOf(message) >= 0) {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool HasBuildStartedFired {
            get { return _buildStartedFired; }
        }

        public bool HasBuildFinishedFired {
            get { return _buildFinishedFired; }
        }

        public bool HasTargetStartedFired {
            get { return _targetStartedFired; }
        }

        public bool HasTargetFinishedFired {
            get { return _targetFinishedFired; }
        }

        public bool HasTaskStartedFired {
            get { return _taskStartedFired; }
        }

        public bool HasTaskFinishedFired {
            get { return _taskFinishedFired; }
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private Hashtable _executedTargets;
        private Hashtable _executedTasks;
        private Hashtable _targetStartTimes;
        private Hashtable _targetFinishTimes;
        private ArrayList _loggedMessages;
        private bool _buildStartedFired;
        private bool _buildFinishedFired;
        private bool _targetStartedFired;
        private bool _targetFinishedFired;
        private bool _taskStartedFired;
        private bool _taskFinishedFired;

        #endregion Private Instance Fields
    }
}
