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
// Ian MacLean (ian@maclean.ms)

using System;
using System.Threading;

using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// A task for sleeping a specified period of time, useful when a build or deployment process
    /// requires an interval between tasks.
    /// </summary>
    /// <example>
    ///   <para>Sleep 1 hour, 2 minutes, 3 seconds and 4 milliseconds.</para>
    ///   <code>
    ///     <![CDATA[
    /// <sleep hours="1" minutes="2" seconds="3" milliseconds="4" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Sleep 123 milliseconds.</para>
    ///   <code>
    ///     <![CDATA[
    /// <sleep milliseconds="123" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("sleep")]
    public class SleepTask : Task {
        #region Private Instance Fields
       
        private int _hours = 0;
        private int _minutes = 0;
        private int _seconds = 0;
        private int _milliseconds = 0;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Hours to add to the sleep time.
        /// </summary>
        [TaskAttribute("hours")]
        [Int32Validator(0, Int32.MaxValue)]
        public int Hours {
            get { return _hours; }
            set { _hours = value; }
        }
        
        /// <summary>
        /// Minutes to add to the sleep time.
        /// </summary>
        [TaskAttribute("minutes")]
        [Int32Validator(0, Int32.MaxValue)]
        public int Minutes {
            get { return _minutes; }
            set {_minutes = value; }
        }
        
        /// <summary>
        /// Seconds to add to the sleep time.
        /// </summary>
        [TaskAttribute("seconds")]
        [Int32Validator(0, Int32.MaxValue)]
        public int Seconds {
            get { return _seconds; }
            set { _seconds = value; }
        }
        
        /// <summary>
        /// Milliseconds to add to the sleep time.
        /// </summary>
        [TaskAttribute("milliseconds")]
        [Int32Validator(0, Int32.MaxValue)]
        public int Milliseconds {
            get { return _milliseconds; }
            set { _milliseconds = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        ///  Verify parameters.
        /// </summary>
        protected override void Initialize() {
            if (GetSleepTime() < 0) {
                throw new BuildException("Negative sleep periods are not supported.", Location);
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            int sleepTime = GetSleepTime();
            Log(Level.Info, "Sleeping for {0} milliseconds.", sleepTime);
            DoSleep(sleepTime);
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Return time to sleep.
        /// </summary>
        private int GetSleepTime() {
            return ((((int) Hours * 60) + Minutes) * 60 + Seconds) * 1000 + Milliseconds;
        }

        /// <summary>
        /// Sleeps for the specified number of milliseconds.
        /// </summary>
        /// <param name="millis">Number of milliseconds to sleep.</param>
        private void DoSleep(int millis ) {
            Thread.Sleep(millis);
        }

        #endregion Private Instance Methods
    }
}
