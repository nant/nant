// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
//
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

using System;
using System.IO;
using System.Text;
using SourceForge.NAnt;
using SourceForge.NAnt.Tasks;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {


    /// <summary>Converts files from one resource format to another (wraps Microsoft's resgen.exe).</summary>
    /// <example>
    ///   <para>Convert a resource file from the .resx to the .resources format</para>
    ///   <code>
    /// <![CDATA[
    /// <resgen input="translations.resx" output="translations.resources" />
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("resgen")]
    public class ResGenTask : ExternalProgramBase {

        string _arguments;
        string _input = null; 
        string _output = null;        
        FileSet _resources = new FileSet();
        string _targetExt = "resources";
        string _toDir = "";

        /// <summary>Input file to process.</summary>
        [TaskAttribute("input", Required=false)]
        public string Input { get { return _input; } set { _input = value;} }

        /// <summary>Name of the resource file to output.</summary>
        [TaskAttribute("output", Required=false)]
        public string Output { get { return _output; } set {_output = value;} }

        /// <summary>The target type ( usually resources).</summary>
        [TaskAttribute("target", Required=false)]
        public string TargetExt { get { return _targetExt; } set {_targetExt = value;} }

        /// <summary>The directory to which outputs will be stored.</summary>
        [TaskAttribute("todir", Required=false)]
        public string ToDirectory { get { return _toDir; } set {_toDir = value;} }
       
        /// <summary>Takes a list of .resX or .txt files to convert to .resources files.</summary>
        [FileSet("resources")]
		public FileSet Resources { get { return _resources; } set { _resources = value; } }
                           
        public override string ProgramFileName { get { return Name; } }

        public override string ProgramArguments { get { return _arguments; } }
                
        protected virtual void WriteOptions(TextWriter writer) {
        }

        protected virtual bool NeedsCompiling(string input, string output) {
              // return true as soon as we know we need to compile
  
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
  
              // if we made it here then we don't have to recompile
              return false;
        }

        protected void AppendArgument(string s) {
            _arguments += s;
        }
        
        protected override void ExecuteTask() {
            _arguments = "";
            if (Resources.FileNames.Count > 0) {
                foreach ( string filename in Resources.FileNames ) {
                    string outputFile = Path.ChangeExtension( filename, TargetExt );
                    if (NeedsCompiling (filename, outputFile)) {
                        if (_arguments.Length == 0) {
                            AppendArgument ("/compile");
                        }
                        AppendArgument (String.Format(" \"{0},{1}\"", filename, outputFile));
                    }
                }
                            
            } else {
                // Single file situation
                if (Input == null)
                    throw new BuildException("Resource generator needs either an input attribute, or a non-empty fileset.", Location);
                    
                string inputFile = Path.GetFullPath(Path.Combine (BaseDirectory, Input));
                string outputFile;
                               
                if (Output != null) {
                    if (ToDirectory == null)
                        ToDirectory = BaseDirectory;
                        
                    outputFile = Path.GetFullPath(
                        Path.Combine (ToDirectory, Output));
                } else
                    outputFile = Path.ChangeExtension (inputFile, TargetExt );

                if (NeedsCompiling (inputFile, outputFile)) {
                    AppendArgument (String.Format ("\"{0}\" \"{1}\"", inputFile, outputFile));
                }
            }

            if ( _arguments.Length > 0) {
                // call base class to do the work
                base.ExecuteTask();
            }
        }
        
        /// <summary>
        /// Clean up generated files
        /// </summary>
        public void RemoveOutputs () {
            foreach ( string filename in Resources.FileNames ) {
                string outputFile = Path.ChangeExtension( filename, TargetExt );
                if ( filename != outputFile) {
                    File.Delete (outputFile);
                }
                if (Input != null) {
                    outputFile = Path.ChangeExtension( Input, TargetExt );
                    if ( Input != outputFile) {
                        File.Delete (outputFile);
                    }
                }
            }                     
        }
    }
}
