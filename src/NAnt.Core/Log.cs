// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// John R. Hicks (angryjohn69@nc.rr.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

// Some of this class was based on code from the Mono class library.
// Copyright (C) 2002 John R. Hicks <angryjohn69@nc.rr.com>

// The events described in this file are based on the comments and
// structure of Ant.
// Copyright (C) Copyright (c) 2000,2002 The Apache Software Foundation.
// All rights reserved.


using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SourceForge.NAnt {

    public class BuildEventArgs : EventArgs
    {
        protected string _name = "";

        public BuildEventArgs(string name) {
            _name = name;
        }

        public string Name {
            get {
                return _name;
            }

            set {
                _name = value;
            }
        }
    }

    /// <summary>Delegate to handle Build events</summary>
    public delegate void BuildEventHandler(object sender, BuildEventArgs e);

    public interface IBuildEventConsumer
    {
        /// <summary>Signals that a build has started. This event is fired before any targets have started.</summary>
        void BuildStarted(object sender, BuildEventArgs e);

        /// <summary>Signals that the last target has finished. This event will still be fired if an error occurred during the build.</summary>
        void BuildFinished(object sender, BuildEventArgs e);

        /// <summary>Signals that a target has started.</summary>
        void TargetStarted(object sender, BuildEventArgs e);

        /// <summary>Signals that a target has finished. This event will still be fired if an error occurred during the build.</summary>
        void TargetFinished(object sender, BuildEventArgs e);

        /// <summary>Signals that a task has started.</summary>
        void TaskStarted(object sender, BuildEventArgs e);

        /// <summary>Signals that a task has finished. This event will still be fired if an error occurred during the build.</summary>
        void TaskFinished(object sender, BuildEventArgs e);
    }

    public abstract class LogListener {

        public abstract void Write(string message);
        public abstract void WriteLine(string message);

        public virtual void Flush() {
        }
    }

    /// <summary>The standard logger that will suffice for any command line based nant runner.</summary>
    public class ConsoleLogger : LogListener {
        public override void Write(string message) {
            Console.Write(message);
        }

        public override void WriteLine(string message) {
            Console.WriteLine(message);
        }
    }

    /// <summary>Used for test classes to check output.</summary>
    public class StringLogger : LogListener {
        private StringWriter _writer = new StringWriter();

        public override void Write(string message) {
            _writer.Write(message);
        }

        public override void WriteLine(string message) {
            _writer.WriteLine(message);
        }

        /// <summary>Returns the contents of log captured.</summary>
        public override string ToString() {
            return _writer.ToString();
        }
    }

    public sealed class LogListenerCollection : ArrayList {
    }

    /// <summary>Provides a set of methods and properties that log the execution of the build process.  This class cannot be inherited.</summary>
    public sealed class Log {
        private static bool _autoFlush;
        private static int _indentLevel;
        private static int _indentSize;
        private static bool _needIndent; // true if the output should be indented; otherwise, false
        private static LogListenerCollection _listeners;

        static Log() {
            _autoFlush = false;
            _indentLevel = 0;
            _indentSize = 4;
            _needIndent = true;
            _listeners = new LogListenerCollection();
            _listeners.Add(new ConsoleLogger());
        }

        /// <summary>Gets or sets whether Flush should be called on the Listeners after every write.</summary>
        public static bool AutoFlush {
            get { return _autoFlush; }
            set { _autoFlush = value; }
        }

        /// <summary>Gets or sets the indent level.  Default is zero.</summary>
        public static int IndentLevel {
            get { return _indentLevel; }
            set { _indentLevel = value; }
        }

        /// <summary>Gets or sets the number of spaces in an indent.  Default is four.</summary>
        public static int IndentSize {
            get { return _indentSize; }
            set { _indentSize = value; }
        }

        /// <summary>Gets the collection of listeners that is monitoring the log output.</summary>
        public static LogListenerCollection Listeners {
            get { return _listeners; }
        }

        /// <summary>Flushes the output buffer, and causes buffered data to be written to the Listeners.</summary>
        public static void Flush() {
            foreach (LogListener l in _listeners) {
                l.Flush();
            }
        }

        /// <summary>Increases the current IndentLevel by one.</summary>
        public static void Indent() {
            _indentLevel++;
        }

        /// <summary>Decreases the current IndentLevel by one.</summary>
        public static void Unindent() {
            if (_indentLevel > 0) {
                _indentLevel--;
            }
        }

        /// <summary>Indents the message if needed.</summary>
        private static string FormatMessage(string message) {
            // if we are starting a new line then first indent the string
            if (_needIndent) {
                if (IndentLevel > 0) {
                    StringBuilder sb = new StringBuilder(message);
                    sb.Insert(0, " ", IndentLevel * IndentSize);
                    message = sb.ToString();
                }
                _needIndent = false;
            }
            return message;
        }

        /// <summary>Writes the given message to the log.</summary>
        public static void Write(string message) {
            message = FormatMessage(message);
            foreach (LogListener l in _listeners) {
                l.Write(message);
            }

            if (AutoFlush) {
                Flush();
            }
        }

        /// <summary>Writes the given message to the log.</summary>
        public static void Write(string format, params object[] arg) {
            Write(String.Format(format, arg));
        }

        /// <summary>Writes the given message to the log if condition is true.</summary>
        public static void WriteIf(bool condition, string message) {
            if (condition) {
                Write(message);
            }
        }

        /// <summary>Writes the given message to the log if condition is true.</summary>
        public static void WriteIf(bool condition, string format, params object[] arg) {
            if (condition) {
                Write(String.Format(format, arg));
            }
        }

        /// <summary>Writes the given message to the log.</summary>
        public static void WriteLine(string message) {
            Write(message + Environment.NewLine);
            _needIndent = true;
        }

        /// <summary>Writes the given message to the log.</summary>
        public static void WriteLine() {
            WriteLine(String.Empty);
        }

        /// <summary>Writes the given message to the log.</summary>
        public static void WriteLine(string format, params object[] arg) {
            WriteLine(String.Format(format, arg));
        }

        /// <summary>Writes the given message to the log if condition is true.</summary>
        public static void WriteLineIf(bool condition, string message) {
            if (condition) {
                WriteLine(message);
            }
        }

        /// <summary>Writes the given message to the log if condition is true.</summary>
        public static void WriteLineIf(bool condition, string format, params object[] arg) {
            if (condition) {
                WriteLine(String.Format(format, arg));
            }
        }
    }
}
