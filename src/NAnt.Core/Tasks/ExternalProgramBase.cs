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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using NAnt.Core.Attributes;
using NAnt.Core.Configuration;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Provides the abstract base class for tasks that execute external applications.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   When a <see cref="ProgramLocationAttribute" /> is applied to the
    ///   deriving class and <see cref="ExeName" /> does not return an
    ///   absolute path, then the program to execute will first be searched for
    ///   in the location specified by <see cref="ProgramLocationAttribute.LocationType" />.
    ///   </para>
    ///   <para>
    ///   If the program does not exist in that location, then the list of tool
    ///   paths of the current target framework will be scanned in the order in
    ///   which they are defined in the NAnt configuration file.
    ///   </para>
    /// </remarks>
    [Serializable()]
    public abstract class ExternalProgramBase : Task {
        #region Private Instance Fields
        
        private StreamReader _stdError;
        private StreamReader _stdOut;
        private ArgumentCollection _arguments = new ArgumentCollection();
        private ManagedExecution _managed = ManagedExecution.Default;
        private string _exeName;
        private int _timeout = Int32.MaxValue;
        private TextWriter _outputWriter;
        private TextWriter _errorWriter;
        private int _exitCode = UnknownExitCode;
        private bool _spawn;
        private int _processId = 0;
        private bool _useRuntimeEngine;

        #endregion Private Instance Fields

        #region Public Static Fields

        /// <summary>
        /// Defines the exit code that will be returned by <see cref="ExitCode" />
        /// if the process could not be started, or did not exit (in time).
        /// </summary>
        public const int UnknownExitCode = -1000;

        #endregion Public Static Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Used to specify the time out value (in milliseconds) to use for the
        /// executetask method output threads
        /// </summary>
        private static readonly int outputTimeout;

        /// <summary>
        /// The app.config app settings key to get the output timeout value from.
        /// </summary>
        private const string outputTimeoutKey = "nant.externalprogram.output.timeout";

        /// <summary>
        /// Will be used to ensure thread-safe operations.
        /// </summary>
        private static object _lockObject = new object();

        #endregion Private Static Fields

        #region Static Constructors

        /// <summary>
        /// Static constructor that retrieves the specified timeout value for program
        /// output.
        /// </summary>
        static ExternalProgramBase()
        {
            // Assigns the appropriate timeout value (in milliseconds) to use for
            // the output threads.
            const int defaultTimeout = 2000;

            string appSettingStage = 
                ConfigurationManager.AppSettings.Get(outputTimeoutKey);

            if (!String.IsNullOrEmpty(appSettingStage))
            {
                int stageTimeout;
                if (Int32.TryParse(appSettingStage, out stageTimeout))
                {
                    // Make sure that the stage Timeout value is valid
                    if (stageTimeout >= 0 || stageTimeout == Timeout.Infinite)
                    {
                        outputTimeout = stageTimeout;
                        return;
                    }
                }
            }

            // If the default timeout isn't specified or valid, use the specified
            // default value.
            outputTimeout = defaultTimeout;
        }

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// The name of the executable that should be used to launch the 
        /// external program.
        /// </summary>
        /// <value>
        /// The name of the executable that should be used to launch the external
        /// program, or <see langword="null" /> if no name is specified.
        /// </value>
        /// <remarks>
        /// If available, the configured value in the NAnt configuration
        /// file will be used if no name is specified.
        /// </remarks>
        [FrameworkConfigurable("exename")]
        public virtual string ExeName {
            get { return (_exeName != null) ? _exeName : Name; }
            set { _exeName = value; }
        }

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>
        /// The filename of the external program.
        /// </value>
        /// <remarks>
        /// Override in derived classes to explicitly set the location of the 
        /// external tool.
        /// </remarks>
        public virtual string ProgramFileName { 
            get { return DetermineFilePath(); }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public abstract string ProgramArguments { get; }

        /// <summary>
        /// Gets the file to which the standard output should be redirected.
        /// </summary>
        /// <value>
        /// The file to which the standard output should be redirected, or 
        /// <see langword="null" /> if the standard output should not be
        /// redirected.
        /// </value>
        /// <remarks>
        /// The default implementation will never allow the standard output
        /// to be redirected to a file.  Deriving classes should override this 
        /// property to change this behaviour.
        /// </remarks>
        public virtual FileInfo Output {
            get { return null; } 
            set {} //so that it can be overriden.
        }
        
        /// <summary>
        /// Gets a value indicating whether output will be appended to the 
        /// <see cref="Output" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if output should be appended to the <see cref="Output" />; 
        /// otherwise, <see langword="false" />.
        /// </value>
        public virtual bool OutputAppend {
            get { return false; } 
            set {} //so that it can be overriden.
        }
      
        /// <summary>
        /// Gets the working directory for the application.
        /// </summary>
        /// <value>
        /// The working directory for the application.
        /// </value>
        public virtual DirectoryInfo BaseDirectory {
            get { return new DirectoryInfo(Project.BaseDirectory); }
            set {} // so that it can be overriden.
        }

        /// <summary>
        /// The maximum amount of time the application is allowed to execute, 
        /// expressed in milliseconds.  Defaults to no time-out.
        /// </summary>
        [TaskAttribute("timeout")]
        [Int32Validator()]
        public int TimeOut {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// The command-line arguments for the external program.
        /// </summary>
        [BuildElementArray("arg")]
        public virtual ArgumentCollection Arguments {
            get { return _arguments; }
        }

        /// <summary>
        /// Specifies whether the external program is a managed application
        /// which should be executed using a runtime engine, if configured. 
        /// The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the external program should be executed 
        /// using a runtime engine; otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        ///   <para>
        ///   The value of <see cref="UseRuntimeEngine" /> is only used from
        ///   <see cref="Managed" />, and then only if its value is set to
        ///   <see cref="ManagedExecution.Default" />. In which case
        ///   <see cref="Managed" /> returns <see cref="ManagedExecution.Auto" />
        ///   if <see cref="UseRuntimeEngine" /> is <see langword="true" />.
        ///   </para>
        ///   <para>
        ///   In all other cases, the value of <see cref="UseRuntimeEngine" />
        ///   is ignored.
        ///   </para>
        /// </remarks>
        [FrameworkConfigurable("useruntimeengine")]
        [Obsolete("Use the managed attribute and Managed property instead.", false)]
        public virtual bool UseRuntimeEngine {
            get { return _useRuntimeEngine; }
            set { _useRuntimeEngine = value; }
        }

        /// <summary>
        /// Specifies whether the external program should be treated as a managed
        /// application, possibly forcing it to be executed under the currently
        /// targeted version of the CLR.
        /// </summary>
        /// <value>
        /// A <see cref="ManagedExecution" /> indicating how the program should
        /// be treated.
        /// </value>
        /// <remarks>
        ///   <para>
        ///   If <see cref="Managed" /> is set to <see cref="ManagedExecution.Default" />,
        ///   which is the default value, and <see cref="UseRuntimeEngine" /> is
        ///   <see langword="true" /> then <see cref="ManagedExecution.Auto" />
        ///   is returned.
        ///   </para>
        ///   <para>
        ///   When the changing <see cref="Managed" /> to <see cref="ManagedExecution.Default" />,
        ///   then <see cref="UseRuntimeEngine" /> is set to <see langword="false" />;
        ///   otherwise, it is changed to <see langword="true" />.
        ///   </para>
        /// </remarks>
        [FrameworkConfigurable("managed")]
        public virtual ManagedExecution Managed {
            get {
                // deal with cases where UseRuntimeEngine is overridden to
                // return true by default
                if (UseRuntimeEngine && _managed == ManagedExecution.Default) {
                    return ManagedExecution.Auto;
                }

                return _managed;
            }
            set {
                _managed = value;
                UseRuntimeEngine = (value != ManagedExecution.Default);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> to which standard output
        /// messages of the external program will be written.
        /// </summary>
        /// <value>
        /// The <see cref="TextWriter" /> to which standard output messages of 
        /// the external program will be written.
        /// </value>
        /// <remarks>
        /// By default, standard output messages wil be written to the build log
        /// with level <see cref="Level.Info" />.
        /// </remarks>
        public virtual TextWriter OutputWriter {
            get { 
                if (_outputWriter == null) {
                    _outputWriter = new LogWriter(this, Level.Info, 
                        CultureInfo.InvariantCulture);
                }
                return _outputWriter;
            }
            set { _outputWriter = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="TextWriter" /> to which error output
        /// of the external program will be written.
        /// </summary>
        /// <value>
        /// The <see cref="TextWriter" /> to which error output of the external 
        /// program will be written.
        /// </value>
        /// <remarks>
        /// By default, error output wil be written to the build log with level 
        /// <see cref="Level.Warning" />.
        /// </remarks>
        public virtual TextWriter ErrorWriter {
            get { 
                if (_errorWriter == null) {
                    _errorWriter = new LogWriter(this, Level.Warning, 
                        CultureInfo.InvariantCulture);
                }
                return _errorWriter;
            }
            set { _errorWriter = value; }
        }

        /// <summary>
        /// Gets the value that the process specified when it terminated.
        /// </summary>
        /// <value>
        /// The code that the associated process specified when it terminated, 
        /// or <c>-1000</c> if the process could not be started or did not 
        /// exit (in time).
        /// </value>
        public int ExitCode {
            get { return _exitCode; }
        }

        /// <summary>
        /// Gets the unique identifier for the spawned application.
        /// </summary>
        protected int ProcessId {
            get {
                if (!Spawn) {
                    throw new InvalidOperationException ("The unique identifier" +
                        " only applies to spawned applications.");
                }
                if (_processId == 0) {
                    throw new InvalidOperationException ("The application was not started.");
                }
                return _processId;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the application should be
        /// spawned. If you spawn an application, its output will not be logged
        /// by NAnt. The default is <see langword="false" />.
        /// </summary>
        public virtual bool Spawn {
            get { return _spawn; }
            set { _spawn = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Starts the external process and captures its output.
        /// </summary>
        /// <exception cref="BuildException">
        ///   <para>The external process did not finish within the configured timeout.</para>
        ///   <para>-or-</para>
        ///   <para>The exit code of the external process indicates a failure.</para>
        /// </exception>
        protected override void ExecuteTask() {
            Thread outputThread = null;
            Thread errorThread = null;

            try {
                // Start the external process
                Process process = StartProcess();

                if (Spawn) {
                    _processId = process.Id;
                    return;
                }

                outputThread = new Thread(new ThreadStart(StreamReaderThread_Output));
                errorThread = new Thread(new ThreadStart(StreamReaderThread_Error));

                _stdOut = process.StandardOutput;
                _stdError = process.StandardError;

                outputThread.Start();
                errorThread.Start();

                // Wait for the process to terminate
                process.WaitForExit(TimeOut);

                // Wait for the threads to terminate
                outputThread.Join(outputTimeout);
                errorThread.Join(outputTimeout);

                if (!process.HasExited) {
                    try {
                        process.Kill();
                    } catch {
                        // ignore possible exceptions that are thrown when the
                        // process is terminated
                    }

                    throw new BuildException(
                        String.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1118"), 
                        ProgramFileName, 
                        TimeOut), 
                        Location);
                }

                _exitCode = process.ExitCode;

                if (process.ExitCode != 0) {
                    throw new BuildException(
                        String.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1119"), 
                        ProgramFileName, 
                        process.ExitCode), 
                        Location);
                }
            } catch (BuildException e) {
                if (FailOnError) {
                    throw;
                } else {
                    logger.Error("Execution Error", e);
                    Log(Level.Error, e.Message);
                }
            } catch (Exception e) {
                logger.Error("Execution Error", e);

                throw new BuildException(
                    string.Format(CultureInfo.InvariantCulture, "{0}: {1} had errors. Please see log4net log.", GetType().ToString(), ProgramFileName), 
                    Location, 
                    e);
            } finally {
                // ensure outputThread is always aborted
                if (outputThread != null && outputThread.IsAlive) {
                    outputThread.Abort();
                }
                // ensure errorThread is always aborted
                if (errorThread != null && errorThread.IsAlive) {
                    errorThread.Abort();
                }
            }
        }

        #endregion Override implementation of Task

        #region Public Instance Methods

        /// <summary>
        /// Gets the command-line arguments, separated by spaces.
        /// </summary>
        public string CommandLine {
            get {
                // append any nested <arg> arguments to the command line
                StringBuilder arguments = new StringBuilder(ProgramArguments);
                Arguments.ToString(arguments);
                return arguments.ToString();
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Updates the <see cref="ProcessStartInfo" /> of the specified 
        /// <see cref="Process"/>.
        /// </summary>
        /// <param name="process">The <see cref="Process" /> of which the <see cref="ProcessStartInfo" /> should be updated.</param>
        protected virtual void PrepareProcess(Process process){
            ManagedExecutionMode executionMode = ManagedExecutionMode;

            // create process (redirect standard output to temp buffer)
            if (executionMode != null && executionMode.Engine != null) {
                process.StartInfo.FileName = executionMode.Engine.Program.FullName;
                StringBuilder arguments = new StringBuilder();
                executionMode.Engine.Arguments.ToString (arguments);
                if (arguments.Length >= 0) {
                    arguments.Append (' ');
                }
                arguments.AppendFormat("\"{0}\" {1}", ProgramFileName, CommandLine);
                process.StartInfo.Arguments = arguments.ToString();
            } else {
                process.StartInfo.FileName = ProgramFileName;
                process.StartInfo.Arguments = CommandLine;
            }
            if (!Spawn) {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }
            // required to allow redirects and allow environment variables to
            // be set
            process.StartInfo.UseShellExecute = false;
            // do not start process in new window unless we're spawning (if not,
            // the console output of spawned application is not displayed on MS)
            process.StartInfo.CreateNoWindow = !Spawn;
            process.StartInfo.WorkingDirectory = BaseDirectory.FullName;

            // set framework-specific environment variables if executing the 
            // external process using the runtime engine of the currently
            // active framework
            if (executionMode != null) {
                foreach (EnvironmentVariable environmentVariable in executionMode.Environment.EnvironmentVariables) {
                    if (environmentVariable.IfDefined && !environmentVariable.UnlessDefined) {
                        if (environmentVariable.Value == null) {
                            process.StartInfo.EnvironmentVariables[environmentVariable.VariableName] = "";
                        } else {
                            process.StartInfo.EnvironmentVariables[environmentVariable.VariableName] = environmentVariable.Value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Starts the process and handles errors.
        /// </summary>
        /// <returns>The <see cref="Process" /> that was started.</returns>
        protected virtual Process StartProcess() {
            Process p = new Process();
            PrepareProcess(p);
            try {
                string msg = string.Format(
                    CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("String_Starting_Program"), 
                    p.StartInfo.WorkingDirectory, 
                    p.StartInfo.FileName, 
                    p.StartInfo.Arguments);

                logger.Info(msg);
                Log(Level.Verbose, msg);

                p.Start();
                return p;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA1121"), p.StartInfo.FileName), Location, ex);
            }
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Reads from the stream until the external program is ended.
        /// </summary>
        private void StreamReaderThread_Output() {
            StreamReader reader = _stdOut;
            bool doAppend = OutputAppend;

            while (true) {
                string logContents = reader.ReadLine();
                if (logContents == null) {
                    break;
                }

                // ensure only one thread writes to the log at any time
                lock (_lockObject) {
                    if (Output != null) {
                        StreamWriter writer = new StreamWriter(Output.FullName, doAppend);
                        writer.WriteLine(logContents);
                        doAppend = true;
                        writer.Close();
                    } else {
                        OutputWriter.WriteLine(logContents);
                    }
                }
            }

            lock (_lockObject) {
                OutputWriter.Flush();
            }
        }

        /// <summary>
        /// Reads from the stream until the external program is ended.
        /// </summary>
        private void StreamReaderThread_Error() {
            StreamReader reader = _stdError;
            bool doAppend = OutputAppend;

            while (true) {
                string logContents = reader.ReadLine();
                if (logContents == null) {
                    break;
                }

                // ensure only one thread writes to the log at any time
                lock (_lockObject) {
                    ErrorWriter.WriteLine(logContents);
                    if (Output != null) {
                        StreamWriter writer = new StreamWriter(Output.FullName, doAppend);
                        writer.WriteLine(logContents);
                        doAppend = true;
                        writer.Close();
                    }
                }
            }

            lock (_lockObject) {
                ErrorWriter.Flush();
            }
        }

        /// <summary>
        /// Determines the path of the external program that should be executed.
        /// </summary>
        /// <returns>
        /// A fully qualifies pathname including the program name.
        /// </returns>
        /// <exception cref="BuildException">The task is not available or not configured for the current framework.</exception>
        private string DetermineFilePath() {
            string fullPath = "";
            
            // if the Exename is already specified as a full path then just use that.
            if (ExeName != null && Path.IsPathRooted(ExeName)) {
                return ExeName;
            }

            // get the ProgramLocation attribute
            ProgramLocationAttribute programLocationAttribute = (ProgramLocationAttribute) Attribute.GetCustomAttribute(this.GetType(), 
                typeof(ProgramLocationAttribute));

            if (programLocationAttribute != null) {
                // ensure we have a valid framework set.
                if ((programLocationAttribute.LocationType == LocationType.FrameworkDir || 
                    programLocationAttribute.LocationType == LocationType.FrameworkSdkDir) &&
                    (Project.TargetFramework == null)) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1120") + Environment.NewLine, Name));
                }

                switch (programLocationAttribute.LocationType) {
                    case LocationType.FrameworkDir:
                        if (Project.TargetFramework.FrameworkDirectory != null) {
                            string frameworkDir = Project.TargetFramework.FrameworkDirectory.FullName;
                            fullPath = Path.Combine(frameworkDir, ExeName + ".exe");
                        } else {
                            throw new BuildException(
                                string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1124"), 
                                Project.TargetFramework.Name));
                        }
                        break;
                    case LocationType.FrameworkSdkDir:
                        if (Project.TargetFramework.SdkDirectory != null) {
                            string sdkDirectory = Project.TargetFramework.SdkDirectory.FullName;
                            fullPath = Path.Combine(sdkDirectory, ExeName + ".exe");
                        } else {
                            throw new BuildException(
                                string.Format(CultureInfo.InvariantCulture, 
                                ResourceUtils.GetString("NA1122"), 
                                Project.TargetFramework.Name));
                        }
                        break;
                }

                if (!File.Exists (fullPath)) {
                    string toolPath = Project.TargetFramework.GetToolPath (
                        ExeName + ".exe");
                    if (toolPath != null) {
                        fullPath = toolPath;
                    }
                }
            } else {
                // rely on it being on the path.
                fullPath = ExeName;
            }
            return fullPath;
        }

        private ManagedExecutionMode ManagedExecutionMode {
            get {
                if (Project.TargetFramework == null || Managed == ManagedExecution.Default) {
                    return null;
                }

                Runtime runtime = Project.TargetFramework.Runtime;
                if (runtime != null) {
                    return runtime.Modes.GetExecutionMode (Managed);
                }
                return null;
            }
        }

        #endregion Private Instance Methods
    }
}
