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

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Provides the abstract base class for tasks that execute external applications.
    /// </summary>
    public abstract class ExternalProgramBase : Task {
        #region Private Instance Fields

        private Hashtable _htThreadStream = new Hashtable();
        private ProgramArgumentCollection _arguments = new ProgramArgumentCollection();

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
        public virtual int TimeOut {
            get { return Int32.MaxValue; }
            set {}
        }

        /// <summary>
        /// The command-line arguments for the external program.
        /// </summary>
        [BuildElementArray("arg")]
        public virtual ProgramArgumentCollection Arguments {
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
        /// Gets the command-line arguments, separated by spaces.
        /// </summary>
        public string CommandLine {
            get {
                // append any nested <arg> arguments to the command line
                StringBuilder arguments = new StringBuilder(ProgramArguments);

                foreach(ProgramArgument arg in Arguments) {
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
                            Log.WriteLine(
                                string.Format(CultureInfo.InvariantCulture, 
                                "{0} skipped arg element without value and file attribute.",
                                Location));
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
                process.StartInfo.Arguments = ProgramFileName + " " + CommandLine;
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
            StreamReader reader = (StreamReader) _htThreadStream[Thread.CurrentThread.Name];
            while (true) {
                string strLogContents = reader.ReadLine();
                if (strLogContents == null)
                    break;
                // Ensure only one thread writes to the log at any time
                lock (_htThreadStream) {
                    logger.Info(strLogContents);
                    //do not print LogPrefix, just pad that length.
                    Log.WriteLine(new string(char.Parse(" "), LogPrefix.Length) + strLogContents);

                    if (OutputFile != null && OutputFile.Length != 0) {
                        StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
                        writer.Write(strLogContents);
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
                    Log.WriteLine(new string(char.Parse(" "), LogPrefix.Length) + strLogContents);

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

    /// <summary>
    /// Represents a command-line argument.
    /// </summary>
    public class ProgramArgument : Element {
        #region Private Instance Fields

        private string _value = null;
        private string _file = null;
        private bool _ifDefined = true;
        private bool _unlessDefined = false;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramArgument" /> class.
        /// </summary>
        public ProgramArgument() {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramArgument" /> class
        /// with the specified value.
        /// </summary>
        public ProgramArgument(string value) {
            _value = value;
        }

        #endregion Public Instance Constructors

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

    /// <summary>
    /// Contains a strongly typed collection of <see cref="ProgramArgument"/> objects.
    /// </summary>
    [Serializable]
    public class ProgramArgumentCollection : CollectionBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramArgumentCollection"/> class.
        /// </summary>
        public ProgramArgumentCollection() {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramArgumentCollection"/> class
        /// with the specified <see cref="ProgramArgumentCollection"/> instance.
        /// </summary>
        public ProgramArgumentCollection(ProgramArgumentCollection value) {
            AddRange(value);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramArgumentCollection"/> class
        /// with the specified array of <see cref="ProgramArgument"/> instances.
        /// </summary>
        public ProgramArgumentCollection(ProgramArgument[] value) {
            AddRange(value);
        }

        #endregion Public Instance Constructors
        
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public ProgramArgument this[int index] {
            get {return ((ProgramArgument)(base.List[index]));}
            set {base.List[index] = value;}
        }

        /// <summary>
        /// Gets the <see cref="ProgramArgument"/> with the specified value.
        /// </summary>
        /// <param name="value">The value of the <see cref="ProgramArgument"/> to get.</param>
        [System.Runtime.CompilerServices.IndexerName("Item")]
        public ProgramArgument this[string value] {
            get {
                if (value != null) {
                    // Try to locate instance using Value
                    foreach (ProgramArgument ProgramArgument in base.List) {
                        if (value.Equals(ProgramArgument.Value)) {
                            return ProgramArgument;
                        }
                    }
                }
                return null;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods
        
        /// <summary>
        /// Adds a <see cref="ProgramArgument"/> to the end of the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProgramArgument"/> to be added to the end of the collection.</param> 
        /// <returns>The position into which the new element was inserted.</returns>
        public int Add(ProgramArgument item) {
            return base.List.Add(item);
        }

        /// <summary>
        /// Adds the elements of a <see cref="ProgramArgument"/> array to the end of the collection.
        /// </summary>
        /// <param name="items">The array of <see cref="ProgramArgument"/> elements to be added to the end of the collection.</param> 
        public void AddRange(ProgramArgument[] items) {
            for (int i = 0; (i < items.Length); i = (i + 1)) {
                Add(items[i]);
            }
        }

        /// <summary>
        /// Adds the elements of a <see cref="ProgramArgumentCollection"/> to the end of the collection.
        /// </summary>
        /// <param name="items">The <see cref="ProgramArgumentCollection"/> to be added to the end of the collection.</param> 
        public void AddRange(ProgramArgumentCollection items) {
            for (int i = 0; (i < items.Count); i = (i + 1)) {
                Add(items[i]);
            }
        }
        
        /// <summary>
        /// Determines whether a <see cref="ProgramArgument"/> is in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProgramArgument"/> to locate in the collection.</param> 
        /// <returns>
        /// <c>true</c> if <paramref name="item"/> is found in the collection;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(ProgramArgument item) {
            return base.List.Contains(item);
        }

        /// <summary>
        /// Determines whether a <see cref="ProgramArgument"/> with the specified
        /// value is in the collection.
        /// </summary>
        /// <param name="value">The argument value to locate in the collection.</param> 
        /// <returns>
        /// <c>true</c> if a <see cref="ProgramArgument" /> with value 
        /// <paramref name="value"/> is found in the collection; otherwise, 
        /// <c>false</c>.
        /// </returns>
        public bool Contains(string value) {
            return this[value] != null;
        }
        
        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified index of the target array.        
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing.</param> 
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(ProgramArgument[] array, int index) {
            base.List.CopyTo(array, index);
        }
        
        /// <summary>
        /// Retrieves the index of a specified <see cref="ProgramArgument"/> object in the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProgramArgument"/> object for which the index is returned.</param> 
        /// <returns>
        /// The index of the specified <see cref="ProgramArgument"/>. If the <see cref="ProgramArgument"/> is not currently a member of the collection, it returns -1.
        /// </returns>
        public int IndexOf(ProgramArgument item) {
            return base.List.IndexOf(item);
        }
        
        /// <summary>
        /// Inserts a <see cref="ProgramArgument"/> into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The <see cref="ProgramArgument"/> to insert.</param>
        public void Insert(int index, ProgramArgument item) {
            base.List.Insert(index, item);
        }
        
        /// <summary>
        /// Returns an enumerator that can iterate through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="ProgramArgumentEnumerator"/> for the entire collection.
        /// </returns>
        public new ProgramArgumentEnumerator GetEnumerator() {
            return new ProgramArgumentEnumerator(this);
        }
        
        /// <summary>
        /// Removes a member from the collection.
        /// </summary>
        /// <param name="item">The <see cref="ProgramArgument"/> to remove from the collection.</param>
        public void Remove(ProgramArgument item) {
            base.List.Remove(item);
        }
        
        #endregion Public Instance Methods
    }

    /// <summary>
    /// Enumerates the <see cref="ProgramArgument"/> elements of a <see cref="ProgramArgumentCollection"/>.
    /// </summary>
    public class ProgramArgumentEnumerator : IEnumerator {
        #region Internal Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramArgumentEnumerator"/> class
        /// with the specified <see cref="ProgramArgumentCollection"/>.
        /// </summary>
        /// <param name="arguments">The collection that should be enumerated.</param>
        internal ProgramArgumentEnumerator(ProgramArgumentCollection arguments) {
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
        public ProgramArgument Current {
            get { return (ProgramArgument) _baseEnumerator.Current; }
        }

        object IEnumerator.Current {
            get { return _baseEnumerator.Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the enumerator was successfully advanced to the next element; 
        /// <c>false</c> if the enumerator has passed the end of the collection.
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
}
