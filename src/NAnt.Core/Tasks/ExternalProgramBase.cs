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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Provides the abstract base class for tasks that execute external applications.
    /// </summary>
    public abstract class ExternalProgramBase : Task {
        #region Private Instance Fields

        private Hashtable _htThreadStream = new Hashtable();
        private StringCollection _args = new StringCollection();       

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// Gets the application to start.
        /// </summary>
        public abstract string ProgramFileName { get; }

        /// <summary>
        /// Gets the command line arguments for the application.
        /// </summary>
        public abstract string ProgramArguments { get; }

        /// <summary>
        /// The file to which the standard output will be redirected.
        /// </summary>
        public virtual string OutputFile { 
            get { return null; } 
        }
        
        /// <summary>
        /// true if the output file is to be appended to.
        /// </summary>
        public virtual bool OutputAppend { 
            get { return false; } 
        }
      
        /// <summary>
        /// Gets the working directory for the application.
        /// </summary>
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
        /// Defaults to task name but can be overridden by derived classes.
        /// </summary>
        public virtual string ExeName {
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
        /// <summary>
        /// The maximum amount of time the application is allowed to execute, 
        /// expressed in milliseconds.  Defaults to no time-out.
        /// </summary>
        public virtual int TimeOut { 
            get { return Int32.MaxValue; } 
            set{} 
        }

        [BuildElementArray("arg")]
        public ArgElement[] ArgElements {
            set {
                ArgElement[] args = value as ArgElement[];
                foreach(ArgElement arg in args) {
                    if (arg.IfDefined && !arg.UnlessDefined) {
                        if (arg.Value != null || arg.File != null) {
                            _args.Add(arg.File == null ? arg.Value : arg.File);
                        } else {
                            Log.WriteLine(
                                string.Format(CultureInfo.InvariantCulture, 
                                "{0} skipped arg element without value and file attribute.",
                                Location));
                        }
                    }
                }
            }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// Get the command line arguments for the application.
        /// </summary>
        protected StringCollection Args {
            get { return _args; }
        }

        #endregion Protected Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            try {
                // Start the external process
                Process process = StartProcess();
                Thread outputThread = new Thread( new ThreadStart( StreamReaderThread_Output ) );
                outputThread.Name = "Output";
                Thread errorThread = new Thread( new ThreadStart( StreamReaderThread_Error ) );
                errorThread.Name = "Error";
                _htThreadStream[ outputThread.Name ] = process.StandardOutput;
                _htThreadStream[ errorThread.Name ] = process.StandardError;

                outputThread.Start();
                errorThread.Start();

                // Wait for the process to terminate
                process.WaitForExit(TimeOut);
                // Wait for the threads to terminate
                outputThread.Join();
                errorThread.Join();
                _htThreadStream.Clear();

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
                    Log.WriteLine(e.Message);
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
        /// Gets the command line arguments, separated by spaces.
        /// </summary>
        public string GetCommandLine() {
            // append any nested <arg> arguments to command line
            StringBuilder arguments = new StringBuilder(ProgramArguments);
            foreach (string arg in Args) {
                arguments.Append(' ');
                //if the arg contains a space, but isn't quoted, quote it.
                if(arg.IndexOf(" ") > 0 && !(arg.StartsWith("\"") && arg.EndsWith("\""))) {
                    arguments.Append("\"");
                    arguments.Append(arg);
                    arguments.Append("\"");
                }
                else {
                    arguments.Append(arg);
                }
            }
            return arguments.ToString();
        }

        #endregion Public Instance Methods

        #region Public Instance Methods

        /// <summary>
        /// Sets the StartInfo Options and returns a new Process that can be run.
        /// </summary>
        /// <returns>new Process with information about programs to run, etc.</returns>
        protected virtual void PrepareProcess(ref Process process){
            // create process (redirect standard output to temp buffer)
            if (Project.CurrentFramework != null && UsesRuntimeEngine && Project.CurrentFramework.RuntimeEngine != null) {
                process.StartInfo.FileName = Project.CurrentFramework.RuntimeEngine.FullName;
                process.StartInfo.Arguments = ProgramFileName + " " + GetCommandLine();
            } else {
                process.StartInfo.FileName = ProgramFileName;
                process.StartInfo.Arguments = GetCommandLine();
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
            PrepareProcess(ref p);
            try {
                string msg = string.Format(
                    CultureInfo.InvariantCulture, 
                    LogPrefix + "Starting '{1} ({2})' in '{0}'", 
                    p.StartInfo.WorkingDirectory, 
                    p.StartInfo.FileName, 
                    p.StartInfo.Arguments);

                logger.Info(msg);
                Log.WriteLineIf(Verbose, msg);

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
            StreamReader reader = ( StreamReader )_htThreadStream[ Thread.CurrentThread.Name ];
            while ( true ) {                        
                string strLogContents = reader.ReadLine();
                if ( strLogContents == null )
                    break;
                // Ensure only one thread writes to the log at any time
                lock ( _htThreadStream ) {
                    
                    logger.Info(strLogContents);
                    //do not print LogPrefix, just pad that length...
                    Log.WriteLine(new string(char.Parse(" "), LogPrefix.Length) + strLogContents);

                    if (OutputFile != null && OutputFile != "") {
                        StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
                        writer.Write(strLogContents);
                        writer.Close();
                    }
                }
            }
        }
        /// <summary>        /// Reads from the stream until the external program is ended.        /// </summary>
        private void StreamReaderThread_Error() {
            StreamReader reader = ( StreamReader )_htThreadStream[ Thread.CurrentThread.Name ];
            while ( true ) {                        
                string strLogContents = reader.ReadLine();
                if ( strLogContents == null )
                    break;
                // Ensure only one thread writes to the log at any time
                lock ( _htThreadStream ) {
                    
                    logger.Error(strLogContents);
                    //do not print LogPrefix, just pad that length...
                    Log.WriteLine(new string(char.Parse(" "), LogPrefix.Length) + strLogContents);

                    if (OutputFile != null && OutputFile != "") {
                        StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
                        writer.Write(strLogContents);
                        writer.Close();
                    }
                }
            }
        }

        #endregion Private Instance Methods
    }
    
    public class ArgElement : Element {
        #region Private Instance Fields

        private string _value = null;
        private string _file = null;
        private bool _ifDefined = true;
        private bool _unlessDefined = false;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Value of this argument.
        /// </summary>
        [TaskAttribute("value")]
        public string Value {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// File of this argument.
        /// </summary>
        [TaskAttribute("file")]
        public string File {
            get { return _file; }
            set { _file = Project.GetFullPath(value); }
        }

        /// <summary>
        /// Indicates if the argument should be passed to the external program. 
        /// If true then the argument will be passed; otherwise skipped. 
        /// Default is "true".
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the argument should not be passed to the external program. 
        /// If false then the argument will be passed; otherwise skipped. 
        /// Default is "false".
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties
    }
}
