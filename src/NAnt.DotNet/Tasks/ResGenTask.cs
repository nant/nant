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
using NAnt.Core.Tasks;
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
    ///   <para>Convert a resource file from the <c>.resx</c> to the <c>.resources</c> format.</para>
    ///   <code>
    ///     <![CDATA[
    /// <resgen input="translations.resx" output="translations.resources" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Convert a fileset of <c>.resx</c> files to the <c>.resources</c> format.</para>
    ///   <code>
    ///     <![CDATA[
    /// <resgen todir="." >
    ///     <resources>
    ///         <includes name="*.resx" />
    ///     </resources>
    /// </resgen>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("resgen")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class ResGenTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _arguments = null;
        private string _input = null; 
        private string _output = null;
        private FileSet _resources = new FileSet();
        private string _targetExt = "resources";
        private string _toDir = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Input file to process.
        /// </summary>
        /// <value>
        /// The full path to the input file.
        /// </value>
        [TaskAttribute("input", Required=false)]
        public string Input {
            get { return (_input != null) ? Project.GetFullPath(_input) : null; }
            set { _input = SetStringValue(value); }
        }

        /// <summary>
        /// Name of the resource file to output.
        /// </summary>
        [TaskAttribute("output", Required=false)]
        public string Output {
            get { return _output; }
            set { _output = SetStringValue(value); }
        }

        /// <summary>
        /// The target type (usually <c>resources</c>).
        /// </summary>
        [TaskAttribute("target", Required=false)]
        public string TargetExt {
            get { return _targetExt; }
            set { _targetExt = SetStringValue(value); }
        }

        /// <summary>
        /// The directory to which outputs will be stored.
        /// </summary>
        [TaskAttribute("todir", Required=false)]
        public string ToDirectory {
            get { return Project.GetFullPath(_toDir); }
            set { _toDir = SetStringValue(value); }
        }
       
        /// <summary>
        /// Takes a list of <c>.resx</c> or <c>.txt</c> files to convert to <c>.resources</c> files.
        /// </summary>
        [FileSet("resources")]
        public FileSet Resources {
            get { return _resources; }
            set { _resources = value; }
        }

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
        /// Converts a single file or group of files.
        /// </summary>
        protected override void ExecuteTask() {
            _arguments = "";
            if (Resources.FileNames.Count > 0) {
                if (Output != null) {
                    throw new BuildException("Output attribute is incompatible with fileset use.", Location);
                }
                foreach (string filename in Resources.FileNames) {
                    string outputFile = GetOutputFile(filename);

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
                string outputFile = GetOutputFile(inputFile);

                if (NeedsCompiling(inputFile, outputFile)) {
                    AppendArgument(string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"", inputFile, outputFile));
                }
            }

            if (_arguments.Length > 0) {
                // call base class to do the work
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Public Instance Methods

        /// <summary>
        /// Cleans up generated files.
        /// </summary>
        public void RemoveOutputs() {
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
        private string GetOutputFile(string filename) {
            FileInfo fileInfo = new FileInfo(filename);
            string outputFile = "";
            
            // If output is empty just change the extension 
            if (Output == null) {
                if (ToDirectory == null) {
                    outputFile = filename;
                } else {
                    outputFile = Path.Combine(ToDirectory, fileInfo.Name);
                }
                outputFile = Path.ChangeExtension(outputFile, TargetExt);
            } else {
                if ( ToDirectory == null ) {
                    outputFile = Path.Combine( Project.BaseDirectory, Output);
                }
                else {
                    outputFile = Path.Combine(ToDirectory, Output);
                }
            }
            return outputFile;
        }
        
        #endregion Private Instance Methods
    }
}
