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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Provides the abstract base class for tasks that execute external applications.</summary>
    public abstract class ExternalProgramBase : Task {

        /// <summary>Gets the application to start.</summary>
        public abstract string ProgramFileName { get; }                

        /// <summary>Gets the command line arguments for the application.</summary>
        public abstract string ProgramArguments { get; }

        /// <summary>The file to which the standard output will be redirected.</summary>
        public virtual string OutputFile { get { return null; } }
        
        /// <summary>true if the output file is to be appended to.</summary>
        public virtual bool OutputAppend { get { return false; } }
      
        /// <summary>Gets the working directory for the application.</summary>
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

        /// <summary>The maximum amount of time the application is allowed to execute, expressed in milliseconds.  Defaults to no time-out.</summary>
        public virtual int TimeOut { get { return Int32.MaxValue;  } set{} }

        StringCollection _args = new StringCollection();

        /// <summary>Get the command line arguments for the application.</summary>
        protected StringCollection Args {
            get { return _args; }
        }

        protected override void InitializeTask(XmlNode taskNode) {
            // initialize the _args collection
            foreach (XmlNode optionNode in taskNode) {
                if(optionNode.Name.Equals("arg")) {

                    // TODO: decide if we should enforce arg elements not being able
                    // to accept a file and value attribute on the same element.
                    // Ideally this would be done via schema and since it doesn't
                    // really hurt for now I'll leave it in.

                
                    XmlNode valueNode = optionNode.Attributes["value"];
                    if (valueNode != null) {
                        _args.Add(Project.ExpandProperties(valueNode.Value));
                    }

                    XmlNode fileNode  = optionNode.Attributes["file"];
                    if (fileNode != null) {
                        _args.Add(Project.GetFullPath(Project.ExpandProperties(fileNode.Value)));
                    }
                }
            }
        }

        /// <summary>Get the command line arguments, separated by spaces.</summary>
        public string GetCommandLine() {
            // append any nested <arg> arguments to command line
            StringBuilder arguments = new StringBuilder(ProgramArguments);
            foreach (string arg in _args) {
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

        
        /// <summary>
        /// Sets the StartInfo Options and returns a new Process that can be run.
        /// </summary>
        /// <returns>new Process with information about programs to run, etc.</returns>
        protected virtual void PrepareProcess(ref Process process){
            // create process (redirect standard output to temp buffer)
            process.StartInfo.FileName = ProgramFileName;
            process.StartInfo.Arguments = GetCommandLine();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = false;
            //required to allow redirects
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = BaseDirectory;
        }

        //Starts the process and handles errors.
        protected virtual Process StartProcess() {
            Process p = new Process();
            PrepareProcess(ref p);
            try {
                Log.WriteLineIf(Verbose, LogPrefix + "{0}>{1} {2}", p.StartInfo.WorkingDirectory, p.StartInfo.FileName, p.StartInfo.Arguments);
                p.Start();
            } catch (Exception e) {
                throw new BuildException(this.GetType().ToString() + ": Error running external program (" + ProgramFileName + "), see build log for details.", Location, e);
            }
            return p;
        }

        protected override void ExecuteTask() {
            Process process = StartProcess();
            try {
                // display standard output
                StreamReader stdOut = process.StandardOutput;
                string output = stdOut.ReadToEnd();

                /*
                // display standard error -- needs to implemented in separate stream
                StreamReader stdErr = process.StandardError;
                string errors = stdErr.ReadToEnd();
                if (errors.Length > 0) {
                    int indentLevel = Log.IndentLevel;
                    Log.IndentLevel = 0;
                    Log.WriteLine(errors);
                    Log.IndentLevel = indentLevel;
                }

                */

                // wait for program to exit
                process.WaitForExit(TimeOut);

                if (output.Length > 0) {
                    if (OutputFile == null) {
                        int indentLevel = Log.IndentLevel;
                        Log.IndentLevel = 0;
                        
                        if (process.ExitCode == 0)
                        {
                        	Log.WriteLine(output);
                        }
                        else
                        {
                        	Log.WriteMessage(output, "compilerError");
                        }
                        Log.IndentLevel = indentLevel;
                    } else if (OutputFile != "") {
                        StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
                        writer.Write(output);
                        writer.Close();
                    }
                }
            } catch (Exception e) {
                throw new BuildException(this.GetType().ToString() + ": Error during external program execution (" + ProgramFileName + "), see build log for details.", Location, e);
            }

            // Keep the FailOnError check to prevent programs that return non-zero even if they are not returning errors.
            if (FailOnError && process!= null && process.ExitCode != 0) {
                throw new BuildException("External program returned errors, see build log for details.", Location);
            }
        }
    }
}
