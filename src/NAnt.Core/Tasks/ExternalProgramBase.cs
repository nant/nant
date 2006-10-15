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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Provides the abstract base class for tasks that execute external applications.
    /// </summary>
    [Serializable()]
    public abstract class ExternalProgramBase : Task {
        #region Private Instance Fields
        
        private StreamReader _stdError;
        private StreamReader _stdOut;
        private ArgumentCollection _arguments = new ArgumentCollection();
        private bool _managed;
        private string _exeName;
        private int _timeout = Int32.MaxValue;
        private TextWriter _outputWriter;
        private TextWriter _errorWriter;
        private int _exitCode = UnknownExitCode;

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
        /// Will be used to ensure thread-safe operations.
        /// </summary>
        private static object _lockObject = new object();

        #endregion Private Static Fields

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
        [FrameworkConfigurable("useruntimeengine")]
        [Obsolete("Use the managed attribute and Managed property instead.", false)]
        public virtual bool UseRuntimeEngine {
            get { return Managed; }
            set { Managed = value; }
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
        [FrameworkConfigurable("managed")]
        public virtual bool Managed {
            get { return _managed; }
            set { _managed = value; }
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
                outputThread = new Thread(new ThreadStart(StreamReaderThread_Output));
                errorThread = new Thread(new ThreadStart(StreamReaderThread_Error));

                _stdOut = process.StandardOutput;
                _stdError = process.StandardError;

                outputThread.Start();
                errorThread.Start();

                // Wait for the process to terminate
                process.WaitForExit(TimeOut);

                // Wait for the threads to terminate
                outputThread.Join(2000);
                errorThread.Join(2000); 

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

                foreach (Argument arg in Arguments) {
                    if (arg.IfDefined && !arg.UnlessDefined) {
                        arguments.Append(' ');
                        arguments.Append(arg.ToString());
                    }
                }
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
            // create process (redirect standard output to temp buffer)
            if (Project.TargetFramework != null && Managed && Project.TargetFramework.RuntimeEngine != null) {
                process.StartInfo.FileName = Project.TargetFramework.RuntimeEngine.FullName;
                process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}", ProgramFileName, CommandLine);
            } else {
                process.StartInfo.FileName = ProgramFileName;
                process.StartInfo.Arguments = CommandLine;
            }
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //required to allow redirects
            process.StartInfo.UseShellExecute = false;
            // do not start process in new window
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = BaseDirectory.FullName;

            // set framework-specific environment variables if executing the 
            // external process using the runtime engine of the currently
            // active framework
            if (Project.TargetFramework != null && Managed) {
                foreach (EnvironmentVariable environmentVariable in Project.TargetFramework.EnvironmentVariables) {
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
                    OutputWriter.WriteLine(logContents);
                    if (Output != null) {
                        StreamWriter writer = new StreamWriter(Output.FullName, doAppend);
                        writer.WriteLine(logContents);
                        doAppend = true;
                        writer.Close();
                    }
                }
            }
            OutputWriter.Flush();
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
            ErrorWriter.Flush();
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
            } else {
                // rely on it being on the path.
                fullPath = ExeName;
            }
            return fullPath;
        }

        #endregion Private Instance Methods
    }
}
