// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//
// Joe Jones (joejo@microsoft.com)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Klemen Zagar (klemen@zagar.ws)
// Ian MacLean (ian_maclean@another.com)

using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Converts files from one resource format to another.
    /// <note>
    ///     <para>
    ///         If no todir is specified, the resource file will be created next to the input file.
    ///     </para>
    /// </note>
    /// </summary>
    /// <example>
    ///   <para>Convert a resource file from the .resx to the .resources format</para>
    ///   <code>
    /// <![CDATA[
    /// <resgen input="translations.resx" output="translations.resources" />
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("resgen")]
    public class ResGenTask : SdkExternalProgramBase {
        #region Private Instance Fields

        string _arguments = null;
        string _input = null; 
        string _output = null;        
        FileSet _resources = new FileSet();
        string _targetExt = "resources";
        string _toDir = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>Input file to process.</summary>
        [TaskAttribute("input", Required=false)]
        public string Input { get { return _input; } set { _input = value;} }

        /// <summary>Name of the resource file to output.</summary>
        [TaskAttribute("output", Required=false)]
        public string Output { get { return (_output == null) ? string.Empty : _output; } set {_output = value;} }

        /// <summary>The target type (usually resources).</summary>
        [TaskAttribute("target", Required=false)]
        public string TargetExt { get { return _targetExt; } set {_targetExt = value;} }

        /// <summary>The directory to which outputs will be stored.</summary>
        [TaskAttribute("todir", Required=false)]
        public string ToDirectory { get { return (_toDir == null) ? string.Empty : _toDir; } set {_toDir = value;} }
       
        /// <summary>Takes a list of .resX or .txt files to convert to .resources files.</summary>
        [FileSet("resources")]
        public FileSet Resources { get { return _resources; } set { _resources = value; } }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the command line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command line arguments for the external program.
        /// </value>
        public override string ProgramArguments { 
            get { return _arguments; } 
        }

        /// <summary>
        /// Gets the name of the executable that should be used to launch the
        /// external program.
        /// </summary>
        /// <value>
        /// The name of the executable that should be used to launch the
        /// external program.
        /// </value>
        /// <remarks>
        /// If a current framework is defined, the name of the executable will
        /// be retrieved from the configuration of the framework; otherwise the
        /// <see cref="Task.Name" /> will be used.
        /// </remarks>
        protected override string ExeName {           
            get {
                if (Project.CurrentFramework != null) {
                    return Project.CurrentFramework.ResGenToolName;
                } else {
                    return Name;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the external program should be executed
        /// using a runtime engine, if configured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the program should be executed using a runtime engine;
        /// otherwise, <c>false</c>.
        /// </value>
        protected override bool UsesRuntimeEngine { 
            get {                 
                // TO-DO : uncomment this when monoresgen no longer crashes when run with the mono runtime.
                //if (Project.CurrentFramework != null) {
                //      if (Project.CurrentFramework.Name.IndexOf("mono", 0) != -1 ) { // remove hardcoded ness
                //          return true;
                //      }
                //}                
                return false;                
            }
        }

        /// <summary>
        /// Converts a single file or group of files.
        /// </summary>
        protected override void ExecuteTask() {
            _arguments = "";
            if (Resources.FileNames.Count > 0) {
                if (Output != null && Output.Length != 0) {
                    throw new BuildException("Output attribute is incompatible with fileset use.", Location);
                }
                foreach (string filename in Resources.FileNames) {
                    string outputFile = getOutputFile(filename);

                    if (NeedsCompiling(filename, outputFile)) {
                        if (_arguments.Length == 0) {
                            AppendArgument ("/compile");
                        }
                        AppendArgument(string.Format(CultureInfo.InvariantCulture, " \"{0},{1}\"", filename, outputFile));
                    }
                }
            } else {
                // Single file situation
                if (Input == null) {
                    throw new BuildException("Resource generator needs either an input attribute, or a non-empty fileset.", Location);
                }

                string inputFile = Path.GetFullPath(Path.Combine (BaseDirectory, Input));
                string outputFile = getOutputFile(inputFile);

                if (NeedsCompiling(inputFile, outputFile)) {
                    AppendArgument(string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"", inputFile, outputFile));
                }
            }

            if ( _arguments.Length > 0) {
                // call base class to do the work
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Public Instance Methods

        /// <summary>
        /// Cleans up generated files.
        /// </summary>
        public void RemoveOutputs () {
            foreach (string filename in Resources.FileNames) {
                string outputFile = Path.ChangeExtension(filename, TargetExt);
                if (filename != outputFile) {
                    File.Delete (outputFile);
                }
                if (Input != null) {
                    outputFile = Path.ChangeExtension(Input, TargetExt);
                    if (Input != outputFile) {
                        File.Delete (outputFile);
                    }
                }
            }                     
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the specified input file needs to be compiled.
        /// </summary>
        /// <param name="input">The input file.</param>
        /// <param name="output">The output file.</param>
        /// <returns>
        /// <c>true</c> if the input file need to be compiled; otherwise 
        /// <c>false</c>.
        /// </returns>
        protected virtual bool NeedsCompiling(string input, string output) {
              FileInfo outputFileInfo = new FileInfo(output);
              if (!outputFileInfo.Exists) {
                  return true;
              }
  
              FileInfo inputFileInfo = new FileInfo(input);
              if (!inputFileInfo.Exists) {
                  return true;
              }
  
              if (outputFileInfo.LastWriteTime < inputFileInfo.LastWriteTime) {
                  return true;
              }
  
              return false;
        }

        /// <summary>
        /// Adds a command line argument to the command line for the external
        /// program that is used to convert the resource files.
        /// </summary>
        /// <param name="s">The argument that should be added to the command line.</param>
        protected void AppendArgument(string s) {
            _arguments += s;
        }
        
        #endregion Protected Instance Methods

        #region Private Instance Methods
       
        /// <summary>
        /// Determines the full path and extension for the output file.
        /// </summary>
        /// <param name="filename">The output file for which the full path and extension should be determined.</param>
        /// <returns>The full path (with extensions) for the specified file.</returns>
        private string getOutputFile(string filename) {
            FileInfo fileInfo = new FileInfo(filename);
            string outputFile = "";
            
            // If output is empty just change the extension 
            if (Output == null ||  Output.Length == 0) {
                if (ToDirectory == null || ToDirectory.Length == 0) {
                    outputFile = filename;
                } else {
                    outputFile = Path.Combine(ToDirectory, fileInfo.Name);
                }
                outputFile = Path.ChangeExtension( outputFile, TargetExt );
            } else {
                outputFile = Path.Combine (ToDirectory, Output);
            }
            return outputFile;
        }
        
        #endregion Private Instance Methods
    }
}
