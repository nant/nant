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
// John R. Hicks (angryjohn69@nc.rr.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net)
//
// Some of this class was based on code from the Mono class library.
// Copyright (C) 2002 John R. Hicks <angryjohn69@nc.rr.com>
//
// The events described in this file are based on the comments and
// structure of Ant.
// Copyright (C) Copyright (c) 2000,2002 The Apache Software Foundation.
// All rights reserved.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Web.Mail;

using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Defines the set of levels recognised by the NAnt logging system.
    /// </summary>
    [TypeConverter(typeof(LevelConverter))]
    public enum Level : int {
        /// <summary>
        /// Designates fine-grained informational events that are most useful 
        /// to debug a build process.
        /// </summary>
        Debug = 1000,

        /// <summary>
        /// Designates events that offer a more detailed view of the build 
        /// process.
        /// </summary>
        Verbose = 2000,

        /// <summary>
        /// Designates informational events that are useful for getting a 
        /// high-level view of the build process.
        /// </summary>
        Info = 3000,

        /// <summary>
        /// Designates potentionally harmful events.
        /// </summary>
        Warning = 4000,

        /// <summary>
        /// Designates error events.
        /// </summary>
        Error = 5000,

        /// <summary>
        /// Can be used to suppress all messages.
        /// </summary>
        /// <remarks>
        /// No events should be logged with this <see cref="Level" />.
        /// </remarks>
        None = 9999
    }

    /// <summary>
    /// Specialized <see cref="EnumConverter" /> for <see cref="Level" />
    /// that ignores case when converting from string.
    /// </summary>
    internal class LevelConverter : EnumConverter {
        /// <summary>
        /// Initializes a new instance of the <see cref="LevelConverter" />
        /// class.
        /// </summary>
        public LevelConverter() : base(typeof(Level)) {
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the 
        /// specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="culture">A <see cref="CultureInfo"/> object. If a <see langword="null"/> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="Object"/> to convert.</param>
        /// <returns>
        /// An <see cref="Object"/> that represents the converted value.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            string stringValue = value as string;
            if (stringValue != null)
                return Enum.Parse(EnumType, stringValue, true);

            // default to EnumConverter behavior
            return base.ConvertFrom(context, culture, value);
        }
    }

    /// <summary>
    /// Class representing an event occurring during a build.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An event is built by specifying either a project, a task or a target.
    /// </para>
    /// <para>
    /// A <see cref="Project" /> level event will only have a <see cref="Project" /> 
    /// reference.
    /// </para>
    /// <para>
    /// A <see cref="Target" /> level event will have <see cref="Project" /> and 
    /// <see cref="Target" /> references.
    /// </para>
    /// <para>
    /// A <see cref="Task" /> level event will have <see cref="Project" />, 
    /// <see cref="Target" /> and <see cref="Task" /> references.
    /// </para>
    /// </remarks>
    public class BuildEventArgs : EventArgs {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildEventArgs" /> 
        /// class.
        /// </summary>
        public BuildEventArgs() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildEventArgs" />
        /// class for a <see cref="Project" /> level event.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> that emitted the event.</param>
        public BuildEventArgs(Project project) {
            _project = project;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildEventArgs" />
        /// class for a <see cref="Target" /> level event.
        /// </summary>
        /// <param name="target">The <see cref="Target" /> that emitted the event.</param>
        public BuildEventArgs(Target target) {
            _project = target.Project;
            _target = target;
        }
    
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildEventArgs" />
        /// class for a <see cref="Task" /> level event.
        /// </summary>
        /// <param name="task">The <see cref="Task" /> that emitted the event.</param>
        public BuildEventArgs(Task task) {
            _project = task.Project;
            _target = task.Parent as Target;
            _task = task;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the message associated with this event.
        /// </summary>
        /// <value>
        /// The message associated with this event.
        /// </value>
        public string Message {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// Gets or sets the priority level associated with this event.
        /// </summary>
        /// <value>
        /// The priority level associated with this event.
        /// </value>
        public Level MessageLevel {
            get { return _messageLevel; }
            set { _messageLevel = value; }
        }
    
        /// <summary>
        /// Gets or sets the <see cref="Exception" /> associated with this event.
        /// </summary>
        /// <value>
        /// The <see cref="Exception" /> associated with this event.
        /// </value>
        public Exception Exception {
            get { return _exception; }
            set { _exception = value; }
        }

        /// <summary>
        /// Gets the <see cref="Project" /> that fired this event.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> that fired this event.
        /// </value>
        public Project Project {
            get { return _project; }
        }

        /// <summary>
        /// Gets the <see cref="Target" /> that fired this event.
        /// </summary>
        /// <value>
        /// The <see cref="Target" /> that fired this event, or a null reference 
        /// if this is a <see cref="Project" /> level event.
        /// </value>
        public Target Target {
            get { return _target; }
        }

        /// <summary>
        /// Gets the <see cref="Task" /> that fired this event.
        /// </summary>
        /// <value>
        /// The <see cref="Task" /> that fired this event, or <see langword="null" />
        /// if this is a <see cref="Project" /> or <see cref="Target" /> level 
        /// event.
        /// </value>
        public Task Task {
            get { return _task; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private readonly Project _project;
        private readonly Target _target;
        private readonly Task _task;
        private string _message;
        private Level _messageLevel = Level.Verbose;
        private Exception _exception;

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Represents the method that handles the build events.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
    public delegate void BuildEventHandler(object sender, BuildEventArgs e);

    /// <summary>
    /// Instances of classes that implement this interface can register to be 
    /// notified when things happen during a build.
    /// </summary>
    public interface IBuildListener {
        /// <summary>
        /// Signals that a build has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event is fired before any targets have started.
        /// </remarks>
        void BuildStarted(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that the last target has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        void BuildFinished(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that a target has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        void TargetStarted(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that a target has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        void TargetFinished(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that a task has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        void TaskStarted(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that a task has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        void TaskFinished(object sender, BuildEventArgs e);

        /// <summary>
        /// Signals that a message has been logged.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        void MessageLogged(object sender, BuildEventArgs e);
    }

    /// <summary>
    /// Interface used by NAnt to log the build output. 
    /// </summary>
    /// <remarks>
    /// Depending on the supplied command-line arguments, NAnt will set the
    /// <see cref="OutputWriter" /> to <see cref="Console.Out" /> or a
    /// <see cref="StreamWriter" />  with a file as backend store.
    /// </remarks>
    public interface IBuildLogger : IBuildListener {
        /// <summary>
        /// Gets or sets the highest level of message this logger should respond 
        /// to.
        /// </summary>
        /// <value>The highest level of message this logger should respond to.</value>
        /// <remarks>
        /// Only messages with a message level higher than or equal to the given 
        /// level should actually be written to the log.
        /// </remarks>
        Level Threshold {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to produce emacs (and other
        /// editor) friendly output.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output is to be unadorned so that emacs 
        /// and other editors can parse files names, etc.
        /// </value>
        bool EmacsMode {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> to which the logger is 
        /// to send its output.
        /// </summary>
        TextWriter OutputWriter {
            get;
            set;
        }

        /// <summary>
        /// Flushes buffered build events or messages to the underlying storage.
        /// </summary>
        void Flush();
    }

    /// <summary>
    /// Implementation of the default logger.
    /// </summary>
    [Serializable()]
    public class DefaultLogger : IBuildLogger {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultLogger" /> 
        /// class.
        /// </summary>
        public DefaultLogger() {
        }

        #endregion Public Instance Constructors

        #region Implementation of IBuildLogger

        /// <summary>
        /// Gets or sets the highest level of message this logger should respond 
        /// to.
        /// </summary>
        /// <value>
        /// The highest level of message this logger should respond to.
        /// </value>
        /// <remarks>
        /// Only messages with a message level higher than or equal to the given 
        /// level should be written to the log.
        /// </remarks>
        public virtual Level Threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to produce emacs (and other
        /// editor) friendly output.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output is to be unadorned so that emacs 
        /// and other editors can parse files names, etc. The default is
        /// <see langword="false" />.
        /// </value>
        public virtual bool EmacsMode {
            get { return _emacsMode; }
            set { _emacsMode = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> to which the logger is 
        /// to send its output.
        /// </summary>
        /// <value>
        /// The <see cref="TextWriter" /> to which the logger sends its output.
        /// </value>
        public virtual TextWriter OutputWriter {
            get { return _outputWriter; }
            set { _outputWriter = value; }
        }

        /// <summary>
        /// Flushes buffered build events or messages to the underlying storage.
        /// </summary>
        public virtual void Flush() {
            if (OutputWriter != null) {
                OutputWriter.Flush();
            }
        }

        #endregion Implementation of IBuildLogger

        #region Implementation of IBuildListener

        /// <summary>
        /// Signals that a build has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event is fired before any targets have started.
        /// </remarks>
        public virtual void BuildStarted(object sender, BuildEventArgs e) {
            _buildReports.Push(new BuildReport(DateTime.Now));
        }

        /// <summary>
        /// Signals that the last target has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        public virtual void BuildFinished(object sender, BuildEventArgs e) {
            Exception error = e.Exception;
            int indentationLevel = 0;

            if (e.Project != null) {
                indentationLevel = e.Project.IndentationLevel * e.Project.IndentationSize;
            }

            BuildReport report = (BuildReport) _buildReports.Pop();

            if (error == null) {
                OutputMessage(Level.Info, string.Empty, indentationLevel);
                if (report.Errors == 0 && report.Warnings == 0) {
                    OutputMessage(Level.Info, "BUILD SUCCEEDED", indentationLevel);
                } else {
                    OutputMessage(Level.Info, string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("String_BuildSucceeded"), 
                        report.Errors, report.Warnings), indentationLevel);
                }
                OutputMessage(Level.Info, string.Empty, indentationLevel);
            } else {
                OutputMessage(Level.Error, string.Empty, indentationLevel);
                if (report.Errors == 0 && report.Warnings == 0) {
                    OutputMessage(Level.Error, "BUILD FAILED", indentationLevel);
                } else {
                    OutputMessage(Level.Info, string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("String_BuildFailed"), 
                        report.Errors, report.Warnings), indentationLevel);
                }
                OutputMessage(Level.Error, string.Empty, indentationLevel);

                if (error is BuildException) {
                    if (Threshold <= Level.Verbose) {
                        OutputMessage(Level.Error, error.ToString(), indentationLevel);
                    } else {
                        if (error.Message != null) {
                            OutputMessage(Level.Error, error.Message, indentationLevel);
                        }

                        // output nested exceptions
                        Exception nestedException = error.InnerException;
                        int exceptionIndentationLevel = indentationLevel;
                        int indentShift = 4; //e.Project.IndentationSize;
                        while (nestedException != null && !String.IsNullOrEmpty(nestedException.Message)) {
                            exceptionIndentationLevel += indentShift;
                            OutputMessage(Level.Error, nestedException.Message, exceptionIndentationLevel);
                            nestedException = nestedException.InnerException;
                        }
                    }
                } else {
                    OutputMessage(Level.Error, "INTERNAL ERROR", indentationLevel);
                    OutputMessage(Level.Error, string.Empty, indentationLevel);
                    OutputMessage(Level.Error, error.ToString(), indentationLevel);
                    OutputMessage(Level.Error, string.Empty, indentationLevel);
                    OutputMessage(Level.Error, "Please send bug report to nant-developers@lists.sourceforge.net.", indentationLevel);
                }

                OutputMessage(Level.Error, string.Empty, indentationLevel);
            }

            // output total build time
            TimeSpan buildTime = DateTime.Now - report.StartTime;
            OutputMessage(Level.Info, string.Format(CultureInfo.InvariantCulture, 
                ResourceUtils.GetString("String_TotalTime") + Environment.NewLine, 
                Math.Round(buildTime.TotalSeconds, 1)), indentationLevel);

            // make sure all messages are written to the underlying storage
            Flush();
        }

        /// <summary>
        /// Signals that a target has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public virtual void TargetStarted(object sender, BuildEventArgs e) {
            int indentationLevel = 0;

            if (e.Project != null) {
                indentationLevel = e.Project.IndentationLevel * e.Project.IndentationSize;
            }

            if (e.Target != null) {
                OutputMessage(Level.Info, string.Empty, indentationLevel);
                OutputMessage(
                    Level.Info, 
                    string.Format(CultureInfo.InvariantCulture, "{0}:", e.Target.Name), 
                    indentationLevel);
                OutputMessage(Level.Info, string.Empty, indentationLevel);
            }
        }

        /// <summary>
        /// Signals that a task has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        public virtual void TargetFinished(object sender, BuildEventArgs e) {
        }

        /// <summary>
        /// Signals that a task has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public virtual void TaskStarted(object sender, BuildEventArgs e) {
        }

        /// <summary>
        /// Signals that a task has finished.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event will still be fired if an error occurred during the build.
        /// </remarks>
        public virtual void TaskFinished(object sender, BuildEventArgs e) {
        }

        /// <summary>
        /// Signals that a message has been logged.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// Only messages with a priority higher or equal to the threshold of 
        /// the logger will actually be output in the build log.
        /// </remarks>
        public virtual void MessageLogged(object sender, BuildEventArgs e) {
            if (_buildReports.Count > 0) {
                if (e.MessageLevel == Level.Error) {
                    BuildReport report = (BuildReport) _buildReports.Peek();
                    report.Errors++;
                } else if (e.MessageLevel == Level.Warning) {
                    BuildReport report = (BuildReport) _buildReports.Peek();
                    report.Warnings++;
                }
            }

            // output the message
            OutputMessage(e);
        }

        #endregion Implementation of IBuildListener

        #region Protected Instance Methods

        /// <summary>
        /// Empty implementation which allows derived classes to receive the
        /// output that is generated in this logger.
        /// </summary>
        /// <param name="message">The message being logged.</param>
        protected virtual void Log(string message) {
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Outputs an indented message to the build log if its priority is 
        /// greather than or equal to the <see cref="Threshold" /> of the 
        /// logger.
        /// </summary>
        /// <param name="messageLevel">The priority of the message to output.</param>
        /// <param name="message">The message to output.</param>
        /// <param name="indentationLength">The number of characters that the message should be indented.</param>
        private void OutputMessage(Level messageLevel, string message, int indentationLength) {
            OutputMessage(CreateBuildEvent(messageLevel, message), indentationLength);
        }

        /// <summary>
        /// Outputs an indented message to the build log if its priority is 
        /// greather than or equal to the <see cref="Threshold" /> of the 
        /// logger.
        /// </summary>
        /// <param name="e">The event to output.</param>
        private void OutputMessage(BuildEventArgs e) {
            int indentationLength = 0;
            
            if (e.Project != null) {
                indentationLength = e.Project.IndentationLevel * e.Project.IndentationSize;
            }

            OutputMessage(e, indentationLength);
        }

        /// <summary>
        /// Outputs an indented message to the build log if its priority is 
        /// greather than or equal to the <see cref="Threshold" /> of the 
        /// logger.
        /// </summary>
        /// <param name="e">The event to output.</param>
        /// <param name="indentationLength">The number of characters that the message should be indented.</param>
        private void OutputMessage(BuildEventArgs e, int indentationLength) {
            if (e.MessageLevel >= Threshold) {
                string txt = e.Message;

                // beautify the message a bit
                txt = txt.Replace("\t", " "); // replace tabs with spaces
                txt = txt.Replace("\r", ""); // get rid of carriage returns

                // split the message by lines - the separator is "\n" since we've eliminated
                // \r characters
                string[] lines = txt.Split('\n');
                string label = String.Empty;

                if (e.Task != null && !EmacsMode) {
                    label = string.Format("[{0}{1}] ", (e.Target == null ? string.Empty : e.Target.Name + " "), e.Task.Name);
                    label = label.PadLeft(e.Project.IndentationSize);
                }

                if (indentationLength > 0) {
                    label = new String(' ', indentationLength) + label;
                }

                foreach (string line in lines) {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(label);
                    sb.Append(line);

                    string indentedMessage = sb.ToString();

                    // output the message to the console
                    Console.Out.WriteLine(indentedMessage);

                    // if an OutputWriter was set, write the message to it
                    if (OutputWriter != null) {
                        OutputWriter.WriteLine(indentedMessage);
                    }

                    Log(indentedMessage);
                }
            }
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        private static BuildEventArgs CreateBuildEvent(Level messageLevel, string message) {
            BuildEventArgs buildEvent = new BuildEventArgs();
            buildEvent.MessageLevel = messageLevel;
            buildEvent.Message = message;
            return buildEvent;
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private Level _threshold = Level.Info;
        private TextWriter _outputWriter;
        private bool _emacsMode;

        /// <summary>
        /// Holds a stack of reports for all running builds.
        /// </summary>
        private readonly Stack _buildReports = new Stack();

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Used to store information about a build, to allow better reporting to 
    /// the user.
    /// </summary>
    [Serializable()]
    public class BuildReport {
        /// <summary>
        /// Errors encountered so far.
        /// </summary>
        public int Errors;

        /// <summary>
        /// Warnings encountered so far.
        /// </summary>
        public int Warnings;

        /// <summary>
        /// The start time of the build process.
        /// </summary>
        public readonly DateTime StartTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildReport"/> class.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        public BuildReport(DateTime startTime) {
            StartTime = startTime;
            Errors = 0;
            Warnings = 0;
        }
    }

    /// <summary>
    /// Buffers log messages from DefaultLogger, and sends an e-mail with the
    /// results.
    /// </summary>
    /// <remarks>
    /// The following properties are used to send the mail :
    /// <list type="table">
    ///     <listheader>
    ///         <term>Property</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term>MailLogger.mailhost</term>
    ///         <description>Mail server to use. [default: localhost]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.from</term>
    ///         <description>The address of the e-mail sender.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.failure.notify</term>
    ///         <description>Send build failure e-mails ? [default: true]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.success.notify</term>
    ///         <description>Send build success e-mails ? [default: true]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.failure.to</term>
    ///         <description>The address to send build failure messages to.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.success.to</term>
    ///         <description>The address to send build success messages to.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.failure.subject</term>
    ///         <description>The subject of build failure messages. [default: "Build Failure"]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.success.subject</term>
    ///         <description>The subject of build success messages. [default: "Build Success"]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.success.attachments</term>
    ///         <description>The ID of a fileset holdng set of files to attach when the build is successful.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.failure.attachments</term>
    ///         <description>The ID of a fileset holdng set of files to attach when the build fails.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.body.encoding</term>
    ///         <description>The encoding type of the body of the e-mail message. [default: system's ANSI code page]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.smtp.username</term>
    ///         <description>The name of the user to login to the SMTP server.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.smtp.password</term>
    ///         <description>The password of the specified user.</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.smtp.enablessl</term>
    ///         <description>Specifies whether to use SSL to encrypt the connection. [default: false]</description>
    ///     </item>
    ///     <item>
    ///         <term>MailLogger.smtp.port</term>
    ///         <description>The SMTP server port to connect to. [default: 25]</description>
    ///     </item>
    /// </list>
    /// </remarks>
    [Serializable()]
    public class MailLogger : DefaultLogger {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MailLogger" /> 
        /// class.
        /// </summary>
        public MailLogger() : base() {
        }

        #endregion Public Instance Constructors

        #region Override implementation of DefaultLogger

        /// <summary>
        /// Signals that a build has started.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        /// <remarks>
        /// This event is fired before any targets have started.
        /// </remarks>
        public override void BuildStarted(object sender, BuildEventArgs e) {
            base.BuildStarted (sender, e);

            // add an item to the project stack
            _projectStack.Push(null);
        }
            

        /// <summary>
        /// Signals that the last target has finished, and send an e-mail with 
        /// the build results.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> object that contains the event data.</param>
        public override void BuildFinished(object sender, BuildEventArgs e) {
#if (NET_1_1)
            const string cdoNamespaceURI = "http://schemas.microsoft.com/cdo/configuration/";
#endif

            base.BuildFinished(sender, e);

            // remove an item from the project stack
            _projectStack.Pop();

            // check if there are still nested projects executing
            if (_projectStack.Count != 0) {
                // do not yet send the mail, as it should only be sent when the
                // main project is finished
                return;
            }

            Encoding bodyEncoding = null;
            Project project = e.Project;
            PropertyDictionary properties = project.Properties;

            bool success = (e.Exception == null);
            string prefix = success ? "success" : "failure";

            try {
                string propertyValue = GetPropertyValue(properties, prefix + ".notify", "true", false);

                bool notify = true;

                try {
                    notify = Convert.ToBoolean(propertyValue, CultureInfo.InvariantCulture);
                } catch {
                    notify = true;
                }

                propertyValue = GetPropertyValue(properties, "body.encoding", null, false);

                try {
                    if (propertyValue != null) {
                        bodyEncoding = Encoding.GetEncoding(propertyValue);
                    }
                } catch {
                    // ignore invalid encoding
                }

                if (!notify) {
                    return;
                }

                // create message to send
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = GetPropertyValue(properties, "from", null, true);
                mailMessage.To = GetPropertyValue(properties, prefix + ".to", null, true);
                mailMessage.Subject = GetPropertyValue(properties, prefix + ".subject",
                    (success) ? "Build Success" : "Build Failure", false);
                mailMessage.Body = _buffer.ToString();

                string smtpUsername = GetPropertyValue(properties, "smtp.username", null, false);
                string smtpPort = GetPropertyValue(properties, "smtp.port", null, false);
                string smtpEnableSSL = GetPropertyValue(properties, "smtp.enablessl", null, false);

#if (NET_1_1)
                // when either a user name or a specific port is specified, or
                // SSL is enabled, then send the message must be sent using the 
                // network (instead of using the local SMTP pickup directory)
                if (smtpUsername != null || smtpPort != null || IsSSLEnabled(properties)) {
                    mailMessage.Fields[cdoNamespaceURI + "sendusing"] = 2; // cdoSendUsingPort
                }
#endif

                if (smtpUsername != null) {
#if (NET_1_1)
                    mailMessage.Fields[cdoNamespaceURI + "smtpauthenticate"] = 1;
                    mailMessage.Fields[cdoNamespaceURI + "sendusername"] = smtpUsername;
#else
                    Console.Error.WriteLine("[MailLogger] MailLogger.smtp.username"
                        + " is not supported if NAnt is built targeting .NET"
                        + " Framework 1.0.");
#endif
                }

                string smtpPassword = GetPropertyValue(properties, "smtp.password", null, false);
                if (smtpPassword != null) {
#if (NET_1_1)
                    mailMessage.Fields[cdoNamespaceURI + "sendpassword"] = smtpPassword;
#else
                    Console.Error.WriteLine("[MailLogger] MailLogger.smtp.password"
                        + " is not supported if NAnt is built targeting .NET"
                        + " Framework 1.0.");
#endif
                }

                if (smtpPort != null) {
#if (NET_1_1)
                    mailMessage.Fields[cdoNamespaceURI + "smtpserverport"] = smtpPort;
#else
                    Console.Error.WriteLine("[MailLogger] MailLogger.smtp.port"
                        + " is not supported if NAnt is built targeting .NET"
                        + " Framework 1.0.");
#endif
                }

                if (smtpEnableSSL != null) {
#if (NET_1_1)
                    mailMessage.Fields[cdoNamespaceURI + "smtpusessl"] = smtpEnableSSL;
#else
                    Console.Error.WriteLine("[MailLogger] MailLogger.smtp.enablessl"
                        + " is not supported if NAnt is built targeting .NET"
                        + " Framework 1.0.");
#endif
                }
                
                // attach files in fileset to message
                AttachFiles(mailMessage, project, GetPropertyValue(properties, 
                    prefix + ".attachments", null, false));

                // set encoding of body
                if (bodyEncoding != null) {
                    mailMessage.BodyEncoding = bodyEncoding;
                }

                // send the message
                SmtpMail.SmtpServer = GetPropertyValue(properties, "mailhost", "localhost", false);
                SmtpMail.Send(mailMessage);
            } catch (Exception ex) {
                Console.Error.WriteLine("[MailLogger] E-mail could not be sent!");
                Console.Error.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Receives and buffers log messages.
        /// </summary>
        /// <param name="message">The message being logged.</param>
        protected override void Log(string message) {
            _buffer.Append(message).Append(Environment.NewLine);
        }

        #endregion Override implementation of DefaultLogger

        #region Private Instance Methods

        /// <summary>
        /// Gets the value of the specified property.
        /// </summary>
        /// <param name="properties">Properties to obtain value from.</param>
        /// <param name="name">Suffix of property name.  "MailLogger" will be prepended internally.</param>
        /// <param name="defaultValue">Value returned if property is not present in <paramref name="properties" />.</param>
        /// <param name="required">Value indicating whether the property should exist, or have a default value set.</param>
        /// <returns>
        /// The value of the specified property; or the default value if the 
        /// property is not present in <paramref name="properties" />.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="required" /> is <see langword="true" />, and the specified property is not present and no default value has been given.</exception>
        private string GetPropertyValue(PropertyDictionary properties, string name, string defaultValue, bool required) {
            string propertyName = "MailLogger." + name;
            string value = (string) properties[propertyName];

            if (value == null) {
                value = defaultValue;
            }

            if (required && value == null) {
                throw new ArgumentNullException(string.Format(CultureInfo.InvariantCulture, "Missing required parameter {0}.", propertyName));
            }

            return value;
        }

        private bool IsSSLEnabled (PropertyDictionary properties) {
            string enableSSL = GetPropertyValue(properties, "smtp.enablessl", null, false);
            if (enableSSL != null) {
                try {
                    return bool.Parse (enableSSL);
                } catch (FormatException) {
                    throw new ArgumentException (string.Format (CultureInfo.InvariantCulture,
                        "Invalid value '{0}' for MailLogger.smtp.enablessl property.",
                        enableSSL));
                }
            }
            return false;
        }

        private void AttachFiles(MailMessage mail, Project project, string filesetID) {
            if (String.IsNullOrEmpty(filesetID)) {
                return;
            }

            // lookup fileset
            FileSet fileset = project.DataTypeReferences[filesetID] as FileSet;
            if (fileset == null) {
                Console.Error.WriteLine("[MailLogger] Fileset \"{0}\" is not"
                    + " defined. No files have been attached.", filesetID);
                return;
            }

            foreach (string fileName in fileset.FileNames) {
                if (!File.Exists(fileName)) {
                    Console.Error.WriteLine("[MailLogger] Attachment \"{0}\""
                        + " does not exist. Skipping.", filesetID);
                    continue;
                }

                // create attachment
                MailAttachment attachment = new MailAttachment(fileName, 
                    MailEncoding.UUEncode);
                // add attachment to mail
                mail.Attachments.Add(attachment);
            }
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        /// <summary>
        /// Buffer in which the message is constructed prior to sending.
        /// </summary>
        private StringBuilder _buffer = new StringBuilder();

        /// <summary>
        /// Holds the stack of currently executing projects.
        /// </summary>
        private Stack _projectStack = new Stack();

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Contains a strongly typed collection of <see cref="IBuildListener"/> 
    /// objects.
    /// </summary>
    [Serializable()]
    public class BuildListenerCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildListenerCollection"/> 
        /// class.
        /// </summary>
        public BuildListenerCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildListenerCollection"/> 
        /// class with the specified <see cref="BuildListenerCollection"/> instance.
        /// </summary>
        public BuildListenerCollection(BuildListenerCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildListenerCollection"/> 
        /// class with the specified array of <see cref="IBuildListener"/> instances.
        /// </summary>
        public BuildListenerCollection(IBuildListener[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public IBuildListener this[int index] {
            get { return ((IBuildListener)(base.List[index])); }
            set { base.List[index] = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="IBuildListener"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="IBuildListener"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(IBuildListener item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="IBuildListener"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="IBuildListener"/> elements to be added to the end of the collection.</param> 
        public void AddRange(IBuildListener[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="BuildListenerCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="BuildListenerCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(BuildListenerCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="IBuildListener"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="IBuildListener"/> to locate in the collection.</param> 
        /// <returns>
        /// <see langword="true" /> if <paramref name="item"/> is found in the 
        /// collection; otherwise, <see langword="false" />.
        /// </returns>
        public bool Contains(IBuildListener item) {
            return base.List.Contains(item);
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(IBuildListener[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="IBuildListener"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="IBuildListener"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="IBuildListener"/>. If the <see cref="IBuildListener"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(IBuildListener item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="IBuildListener"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="IBuildListener"/> to insert.</param>
        public void Insert(int index, IBuildListener item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="BuildListenerEnumerator"/> for the entire collection.
        /// </returns>
        public new BuildListenerEnumerator GetEnumerator() {
            return new BuildListenerEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="IBuildListener"/> to remove from the collection.</param>
        public void Remove(IBuildListener item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="IBuildListener"/> elements of a <see cref="BuildListenerCollection"/>.
    /// </summary>
    public class BuildListenerEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildListenerEnumerator"/> class
        /// with the specified <see cref="BuildListenerCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal BuildListenerEnumerator(BuildListenerCollection arguments) {
            IEnumerable temp = (IEnumerable) (arguments);
            _baseEnumerator = temp.GetEnumerator();
        }

        #endregion Internal Instance Constructors

        #region Implementation of IEnumerator
            
        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <returns>
        /// The current element in the collection.
        /// </returns>
        public IBuildListener Current {
            get { return (IBuildListener) _baseEnumerator.Current; }
        }

        object IEnumerator.Current {
            get { return _baseEnumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced 
        /// to the next element; <see langword="false" /> if the enumerator has 
        /// passed the end of the collection.
        /// </returns>
        public bool MoveNext() {
            return _baseEnumerator.MoveNext();
        }

        bool IEnumerator.MoveNext() {
            return _baseEnumerator.MoveNext();
        }
            
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the 
        /// first element in the collection.
        /// </summary>
        public void Reset() {
            _baseEnumerator.Reset();
        }
            
        void IEnumerator.Reset() {
            _baseEnumerator.Reset();
        }

        #endregion Implementation of IEnumerator

        #region Private Instance Fields
    
        private IEnumerator _baseEnumerator;

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Implements a <see cref="TextWriter" /> for writing information to 
    /// the NAnt logging infrastructure.
    /// </summary>
    public class LogWriter : TextWriter {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter" /> class 
        /// for the specified <see cref="Task" /> with the specified output 
        /// level and format provider.
        /// </summary>
        /// <param name="task">Determines the indentation level.</param>
        /// <param name="outputLevel">The <see cref="Level" /> with which messages will be output to the build log.</param>
        /// <param name="formatProvider">An <see cref="IFormatProvider" /> object that controls formatting.</param>
        public LogWriter(Task task, Level outputLevel, IFormatProvider formatProvider) : base(formatProvider) {
            _task = task;
            _outputLevel = outputLevel;
        }

        #endregion Public Instance Constructors

        #region Override implementation of TextWriter

        /// <summary>
        /// Gets the <see cref="Encoding" /> in which the output is written.
        /// </summary>
        /// <value>
        /// The <see cref="LogWriter" /> always writes output in UTF8 
        /// encoding.
        /// </value>
        public override Encoding Encoding {
            get { return Encoding.UTF8; }
        }

        /// <summary>
        /// Writes a character array to the buffer.
        /// </summary>
        /// <param name="chars">The character array to write to the text stream.</param>
        public override void Write(char[] chars) {
            Write(new string(chars, 0, chars.Length -1));
        }

        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value) {
            _message.Append(value);
        }

        /// <summary>
        /// Writes an empty string to the logging infrastructure.
        /// </summary>
        public override void WriteLine() {
            WriteLine(string.Empty);
        }


        /// <summary>
        /// Writes a string to the logging infrastructure.
        /// </summary>
        /// <param name="value">The string to write. If <paramref name="value" /> is a null reference, only the line termination characters are written.</param>
        public override void WriteLine(string value) {
            _message.Append(value);
            
            if (_task.IsLogEnabledFor(OutputLevel))
                _task.Log(OutputLevel, _message.ToString());
            
            _message.Length = 0;
        }

        /// <summary>
        /// Writes out a formatted string using the same semantics as 
        /// <see cref="M:string.Format(string, object[])" />.
        /// </summary>
        /// <param name="line">The formatting string.</param>
        /// <param name="args">The object array to write into format string.</param>
        public override void WriteLine(string line, params object[] args) {
            _message.AppendFormat(CultureInfo.InvariantCulture, line, args);
            
            if(_task.IsLogEnabledFor(OutputLevel))
                _task.Log(OutputLevel, _message.ToString());
            
            _message.Length = 0;
        }

        /// <summary>
        /// Causes any buffered data to be written to the logging infrastructure.
        /// </summary>
        public override void Flush() {
            if (_message.Length != 0) {
                if(_task.IsLogEnabledFor(OutputLevel))
                    _task.Log(OutputLevel, _message.ToString());
                
                _message.Length = 0;
            }
        }

        /// <summary>
        /// Closes the current writer and releases any system resources 
        /// associated with the writer.
        /// </summary>
        public override void Close() {
            Flush();
            base.Close();
        }

        #endregion Override implementation of TextWriter

        #region Override implementation of MarshalByRefObject

        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for 
        /// this instance.
        /// </summary>
        /// <returns>
        /// An object of type <see cref="ILease" /> used to control the lifetime 
        /// policy for this instance. This is the current lifetime service object 
        /// for this instance if one exists; otherwise, a new lifetime service 
        /// object initialized with a lease that will never time out.
        /// </returns>
        public override Object InitializeLifetimeService() {
            ILease lease = (ILease) base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial) {
                lease.InitialLeaseTime = TimeSpan.Zero;
            }
            return lease;
        }

        #endregion Override implementation of MarshalByRefObject

        #region Protected Instance Properties

        /// <summary>
        /// Gets the <see cref="Level" /> with which messages will be output to
        /// the build log.
        /// </summary>
        protected Level OutputLevel {
            get { return _outputLevel; }
        }

        #endregion Protected Instance Properties

        #region Private Instance Fields

        private readonly Task _task;
        private readonly Level _outputLevel;
        private StringBuilder _message = new StringBuilder();

        #endregion Private Instance Fields
    }
}
