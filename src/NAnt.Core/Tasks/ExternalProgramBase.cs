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

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Provides the abstract base class for tasks that execute external applications.
    /// </summary>
    public abstract class ExternalProgramBase : Task {
        #region Private Instance Fields

        private Hashtable _htThreadStream = new Hashtable();
        private ArgumentCollection _arguments = new ArgumentCollection();
        private int _timeout = Int32.MaxValue;

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public abstract string ProgramFileName { get; }

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
        /// The file to which the standard output should be redirected.
        /// </value>
        public virtual string OutputFile {
            get { return null; } 
            set{} //so that it can be overriden.
        }
        
        /// <summary>
        /// Gets a value indicating whether output will be appended to the 
        /// <see cref="OutputFile" />.
        /// </summary>
        /// <value>
        /// <c>true</c> if output should be appended to the <see cref="OutputFile" />; 
        /// otherwise, <c>false</c>.
        /// </value>
        public virtual bool OutputAppend {
            get { return false; } 
            set{} //so that it can be overriden.
        }
      
        /// <summary>
        /// Gets the working directory for the application.
        /// </summary>
        /// <value>
        /// The working directory for the application.
        /// </value>
        public virtual string BaseDirectory {
            get {
                if (Project != null) {
                    return Project.BaseDirectory;
                } else {
                    return null;
                }
            }
            set{} //so that it can be overriden.
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

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Gets the name of executable that should be used to launch the
        /// external program.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation will return the name of the task as 
        /// <see cref="ExeName" />.
        /// </para>
        /// <para>
        /// Derived classes should override this property to change this behaviour.
        /// </para>
        /// </remarks>
        protected virtual string ExeName {
            get { return Name; }
        }

        /// <summary>
        /// Gets a value indicating whether the external program should be executed
        /// using a runtime engine, if configured.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation will always execute external programs without
        /// using a runtime engine.
        /// </para>
        /// <para>
        /// Derived classes should override this property to change this behaviour.
        /// </para>
        /// </remarks>
        protected virtual bool UsesRuntimeEngine {
            get { return false; }
        }

        #endregion Protected Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            try {
                // Start the external process
                Process process = StartProcess();
                Thread outputThread = new Thread(new ThreadStart(StreamReaderThread_Output));
                outputThread.Name = "Output";
                Thread errorThread = new Thread(new ThreadStart(StreamReaderThread_Error));
                errorThread.Name = "Error";
                _htThreadStream[outputThread.Name] = process.StandardOutput;
                _htThreadStream[errorThread.Name] = process.StandardError;

                outputThread.Start();
                errorThread.Start();

                // Wait for the process to terminate
                process.WaitForExit(TimeOut);

                // Wait for the threads to terminate
                outputThread.Join();
                errorThread.Join();
                _htThreadStream.Clear();

                if (!process.HasExited) {
                    throw new BuildException(
                        String.Format(CultureInfo.InvariantCulture, 
                        "External Program {0} did not finish within {1} milliseconds.", 
                        ProgramFileName, 
                        TimeOut), 
                        Location);
                }

                if (process.ExitCode != 0){
                    throw new BuildException(
                        String.Format(CultureInfo.InvariantCulture, 
                        "External Program Failed: {0} (return code was {1})", 
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
                    String.Format(CultureInfo.InvariantCulture, "{0}: {1} had errors. Please see log4net log.", GetType().ToString(), ProgramFileName), 
                    Location, 
                    e);
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

                foreach(Argument arg in Arguments) {
                    if (arg.IfDefined && !arg.UnlessDefined) {
                        if (arg.Value != null || arg.File != null) {
                            string argValue = arg.File == null ? arg.Value : arg.File;
                            arguments.Append(' ');
                            //if the arg contains a space, but isn't quoted, quote it.
                            if(argValue.IndexOf(" ") > 0 && !(argValue.StartsWith("\"") && argValue.EndsWith("\""))) {
                                arguments.Append("\"");
                                arguments.Append(argValue);
                                arguments.Append("\"");
                            } else {
                                arguments.Append(argValue);
                            }
                        } else {
                            Log(Level.Warning, "{0} skipped arg element without value and file attribute.", Location);
                        }
                    }
                }
                return arguments.ToString();
            }
        }

        #endregion Public Instance Methods

        #region Public Instance Methods

        /// <summary>
        /// Sets the StartInfo Options and returns a new Process that can be run.
        /// </summary>
        /// <returns>new Process with information about programs to run, etc.</returns>
        protected virtual void PrepareProcess(Process process){
            // create process (redirect standard output to temp buffer)
            if (Project.CurrentFramework != null && UsesRuntimeEngine && Project.CurrentFramework.RuntimeEngine != null) {
                process.StartInfo.FileName = Project.CurrentFramework.RuntimeEngine.FullName;
                process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}", ProgramFileName, CommandLine);
            } else {
                process.StartInfo.FileName = ProgramFileName;
                process.StartInfo.Arguments = CommandLine;
            }
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //required to allow redirects
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = BaseDirectory;
        }

        //Starts the process and handles errors.
        protected virtual Process StartProcess() {
            Process p = new Process();
            PrepareProcess(p);
            try {
                string msg = string.Format(
                    CultureInfo.InvariantCulture, 
                    LogPrefix + "Starting '{1} ({2})' in '{0}'", 
                    p.StartInfo.WorkingDirectory, 
                    p.StartInfo.FileName, 
                    p.StartInfo.Arguments);

                logger.Info(msg);
                Log(Level.Verbose, msg);

                p.Start();
            } catch (Exception e) {
                string msg = String.Format(CultureInfo.InvariantCulture, "<{0} task>{1} failed to start.", Name, p.StartInfo.FileName);
                logger.Error(msg, e);
                throw new BuildException(msg, Location, e);
            }
            return p;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>        /// Reads from the stream until the external program is ended.        /// </summary>
        private void StreamReaderThread_Output() {
            StreamReader reader = (StreamReader) _htThreadStream[Thread.CurrentThread.Name];
            bool doAppend = OutputAppend;
            while (true) {
                string strLogContents = reader.ReadLine();
                if (strLogContents == null)
                    break;
                // Ensure only one thread writes to the log at any time
                lock (_htThreadStream) {
                    logger.Info(strLogContents);
                    //do not print LogPrefix, just pad that length.
                    Log(Level.Info, new string(char.Parse(" "), LogPrefix.Length) + strLogContents);

                    if (OutputFile != null && OutputFile.Length != 0) {
                        StreamWriter writer = new StreamWriter(OutputFile, doAppend);
                        writer.WriteLine(strLogContents);
                        doAppend = true;
                        writer.Close();
                    }
                }
            }
        }
        /// <summary>        /// Reads from the stream until the external program is ended.        /// </summary>
        private void StreamReaderThread_Error() {
            StreamReader reader = (StreamReader) _htThreadStream[Thread.CurrentThread.Name];
            while (true) {
                string strLogContents = reader.ReadLine();
                if (strLogContents == null)
                    break;
                // Ensure only one thread writes to the log at any time
                lock (_htThreadStream) {
                    logger.Error(strLogContents);
                    //do not print LogPrefix, just pad that length.
                    Log(Level.Info, new string(char.Parse(" "), LogPrefix.Length) + strLogContents);

                    if (OutputFile != null && OutputFile.Length != 0) {
                        StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
                        writer.Write(strLogContents);
                        writer.Close();
                    }
                }
            }
        }

        #endregion Private Instance Methods
    }
}
