//
// NAntContrib
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
        bool _compile = false;
        FileSet _resources = new FileSet();

        /// <summary>Input file to process.</summary>
        [TaskAttribute("input", Required=false)]
        public string Input { get { return _input; } set { _input = value;} }

        /// <summary>Name of the resource file to output.</summary>
        [TaskAttribute("output", Required=false)]
        public string Output { get { return _output; } set {_output = value;} }

        /// <summary>If true use the fileset to determine the list of files to convert (<c>/compile</c> flag).</summary>
        [TaskAttribute("compile")]
        [BooleanValidator()]
        public bool Compile { get { return _compile; } set {_compile = value;} }

        /// <summary>Takes a list of .resX or .txt files to convert to .resources files.</summary>
        [FileSet("resources")]
        public FileSet Resources { get { return _resources; } }

        public override string ProgramFileName { get { return Name; } }

        public override string ProgramArguments { get { return _arguments; } }
                
        protected virtual void WriteOptions(TextWriter writer) {
        }

        protected string GetOutputPath() {
            return Path.GetFullPath(Path.Combine(BaseDirectory, Output));
        }

        protected string GetInputPath() {
            return Path.GetFullPath(Path.Combine(BaseDirectory, Input));
        }

        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            FileInfo outputFileInfo = new FileInfo(GetOutputPath());
            if (!outputFileInfo.Exists) {
                return true;
            }

            FileInfo inputFileInfo = new FileInfo(GetInputPath());
            if (!inputFileInfo.Exists) {
                return true;
            }

            if (outputFileInfo.LastWriteTime < inputFileInfo.LastWriteTime) {
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                StringBuilder sb = new StringBuilder ();
                StringWriter writer = new StringWriter ( sb );

                try {
                    Log.WriteLine(LogPrefix + "Compiling {0} to {1}", GetInputPath(), GetOutputPath());

                    // specific compiler options
                    WriteOptions(writer);

                    // Microsoft common compiler options
                    if (Compile) {
                        bool addComma = false;
                        writer.Write(" /compile" );

                        foreach (string fileName in Resources.FileNames) {
                            if ( addComma ) {
                                writer.Write(",{0}",fileName );
                            } else {
                                writer.Write("{0}",fileName );
                                addComma = true;
                            }
                        }
                    } else {
                        writer.Write(" \"{0}\"", Input );
                    }

                    writer.Write(" \"{0}\"", Output );

                    // Make sure to close the response file otherwise contents
                    // will not be written to disc and EXecuteTask() will fail.
                    writer.Close();
                    _arguments = sb.ToString ();

                    // display response file contents
                    Log.WriteLineIf(Verbose, _arguments);

                    // call base class to do the work
                    base.ExecuteTask();
                } finally {
                    // make sure we delete response file even if an exception is thrown
                    writer.Close(); // make sure stream is closed or file cannot be deleted
                }
            }
        }
    }
}
