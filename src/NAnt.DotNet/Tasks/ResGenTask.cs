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

using SourceForge.NAnt.Attributes;
using System;
using System.IO;

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
    public class ResGenTask : MsftFXSDKExternalProgramBase {

        string _arguments;
        string _input = null; 
        string _output = null;        
        FileSet _resources = new FileSet();
        string _targetExt = "resources";
        string _toDir = null;

        /// <summary>Input file to process.</summary>
        [TaskAttribute("input", Required=false)]
        public string Input { get { return _input; } set { _input = value;} }

        /// <summary>Name of the resource file to output.</summary>
        [TaskAttribute("output", Required=false)]
        public string Output { get { return (_output == null) ? String.Empty : _output; } set {_output = value;} }

        /// <summary>The target type ( usually resources).</summary>
        [TaskAttribute("target", Required=false)]
        public string TargetExt { get { return _targetExt; } set {_targetExt = value;} }

        /// <summary>The directory to which outputs will be stored.</summary>
        [TaskAttribute("todir", Required=false)]
        public string ToDirectory { get { return (_toDir == null) ? BaseDirectory : _toDir; } set {_toDir = value;} }
       
        /// <summary>Takes a list of .resX or .txt files to convert to .resources files.</summary>
        [FileSet("resources")]
        public FileSet Resources { get { return _resources; } set { _resources = value; } }
                           

        public override string ProgramArguments { get { return _arguments; } }
                
        protected virtual void WriteOptions(TextWriter writer) {
        }
        
        public override string ExeName {           
            get {return Project.CurrentFramework.ResGenToolName; }                          
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
        
        // Process a single file or file group
        protected override void ExecuteTask() {
            _arguments = "";
            if (Resources.FileNames.Count > 0) {
                if (Output != String.Empty ){
                    throw new BuildException("output attribute is incompatible with fileset use.", Location);
                }
                foreach ( string filename in Resources.FileNames ) {
                    string outputFile = getOutputFile(filename);

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
                string outputFile = getOutputFile(inputFile);

                if (NeedsCompiling (inputFile, outputFile)) {
                    AppendArgument (String.Format ("\"{0}\" \"{1}\"", inputFile, outputFile));
                }
            }

            if ( _arguments.Length > 0) {
                // call base class to do the work
                base.ExecuteTask();
            }
        }
        
        // Determine the full path and extension for the output file
        private string getOutputFile(string filename) {
            FileInfo fileInfo = new FileInfo(filename);
            string outputFile = "";
            
            // If output is empty just change the extension 
            if (Output == String.Empty) {
                outputFile = Path.Combine (ToDirectory, fileInfo.Name);
                outputFile = Path.ChangeExtension( outputFile, TargetExt );
            } 
            else {
                outputFile = Path.Combine (ToDirectory, Output);
            }
            return outputFile;
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
        // 
        protected override bool UsesRuntimeEngine{ 
            get {                 
                // uncomment this when monoresgen no longer crashes when run with the mono runtime.
                //if ( Project.CurrentFramework.Name.IndexOf( "mono", 0 ) != -1 ) { // remove hardcoded ness
                //    return true;
                //}                
                return false;                
            }
        }
    }
}
