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

// Joe Jones (joejo@microsoft.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Text;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    ///     Wraps al, the assembly linker for the .NET Framework.
    /// </summary>
    /// <remarks>
    ///   <para>All specified sources will be embedded using the <c>/embed</c> flag.  Other source types are not supported.</para>
    /// </remarks>
    /// <example>
    ///   <para>Create a library containing all icon files in the current directory.</para>
    ///   <code>
    /// <![CDATA[
    /// <al output="MyIcons.dll" target="lib">
    ///     <sources>
    ///         <includes name="*.ico"/>
    ///     </sources>
    /// </al>
    /// ]]>
    ///   </code>
    /// </example>
    [TaskName("al")]
    public class AlTask : ExternalProgramBase {

        string _arguments;        
        string _output = null;        
        string _target = null;       
        string _culture = null;        
        string _template = null;       
        FileSet _sources = new FileSet();

        /// <summary>The name of the output file for the assembly manifest.
        ///     This attribute corresponds to the <c>/out</c> flag.</summary>
        [TaskAttribute("output", Required=true)]
        public string Output { 
            get { return _output; } set {_output = value; } }
        
        /// <summary>The target type (one of "lib", "exe", or "winexe").
        ///     This attribute corresponds to the <c>/t[arget]:</c> flag.</summary>
        [TaskAttribute("target", Required=true)]
        public string OutputTarget { get { return _target; } set {_target = value; } }

        /// <summary>The culture string associated with the output assembly.
        ///     The string must be in RFC 1766 format, such as "en-US".
        ///     This attribute corresponds to the <c>/c[ulture]:</c> flag.</summary>
        [TaskAttribute("culture", Required=false)]
        public string Culture { get { return _culture; } set {_culture = value; } }
         
        /// <summary>Specifies an assembly from which to get all options except the culture field.
        ///     The given filename must have a strong name.
        ///     This attribute corresponds to the <c>/template:</c> flag.</summary>
        [TaskAttribute("template", Required=false)]
        public string Template { get { return _template; } set {_template = value; } }

        /// <summary>The set of source files to embed.</summary>
        [FileSet("sources")]
        public FileSet Sources { get { return _sources; } }

        public override string ProgramFileName { get { return Name; } /*set { Name = value; }*/ }
        public override string ProgramArguments { get { return _arguments; }  /*set { _arguments = value; }*/ }

        protected virtual void WriteOptions(TextWriter writer) {}

        protected string GetOutputPath() {
            return Path.GetFullPath(Path.Combine(BaseDirectory, Output));
        }

        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            FileInfo outputFileInfo = new FileInfo(GetOutputPath());
            if (!outputFileInfo.Exists) {
                return true;
            }

            string fileName = FileSet.FindMoreRecentLastWriteTime(Sources.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log.WriteLineIf(Verbose, LogPrefix + "{0} is out of date.", fileName);
                return true;
            }

            // if we made it here then we don't have to recompile
            return false;
        }

        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                StringBuilder sb = new StringBuilder ();
                StringWriter writer = new StringWriter(sb);

                try {
                    if (Sources.BaseDirectory == null) {
                        Sources.BaseDirectory = BaseDirectory;
                    }

                    Log.WriteLine(LogPrefix + "Compiling {0} files to {1}", Sources.FileNames.Count, GetOutputPath());

                    // specific compiler options
                    WriteOptions(writer);

                    // Microsoft common compiler options
                    writer.Write(" /t:{0}", OutputTarget);
                    writer.Write(" /out:\"{0}\"", GetOutputPath());

                    if ( null != Culture ) {
                        writer.Write(" /culture:{0}", Culture);
                    }

                    if ( null != Template ) {
                        writer.Write(" /template:\"{0}\"", Template);
                    }

                    foreach (string fileName in Sources.FileNames) {
                        writer.Write (" /embed:\"{0}\"", fileName);
                    }
                    // Make sure to close the response file otherwise contents
                    // Will not be written to disk and ExecuteTask() will fail.
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
