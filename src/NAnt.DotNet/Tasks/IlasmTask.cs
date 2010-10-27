// IlasmTask.cs
//
// Giuseppe Greco <giuseppe.greco@agamura.com>
// Copyright (C) 2004 Agamura, Inc.
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
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Compiles ILASM programs.
    /// </summary>
    /// <example>
    ///   <para>
    ///   Compiles <c>helloworld.il</c> to <c>helloworld.exe</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <ilasm target="exe" output="helloworld.exe" debug="true">
    ///     <sources>
    ///         <include name="helloworld.il" />
    ///     </sources>
    /// </ilasm>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("ilasm")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class IlasmTask : ExternalProgramBase {
        #region Private Instance Fields

        private bool _clock;
        private bool _debug;
        private bool _error;
        private bool _forceRebuild;
        private bool _listing;
        private int _alignment;
        private int _base;
        private int _flags;
        private int _subsystem;
        private string _target;
        private string _keySource;
        private FileInfo _keyFile;
        private FileInfo _outputFile;
        private FileInfo _resourceFile;
        private FileSet _sources;
        private string _options;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies whether or not the compiler should measure and report
        /// the compilation times.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the compilation times should be
        /// measured and reported; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/CLOCK</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("clock")]
        [BooleanValidator()]
        public bool Clock {
            get { return _clock; }
            set { _clock = value; }
        }

        /// <summary>
        /// Specifies whether or not the compiler should generate debug
        /// information.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if debug information should be generated;
        /// otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/DEBUG</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("debug")]
        [BooleanValidator()]
        public bool Debug {
            get { return _debug; }
            set { _debug = value; }
        }

        /// <summary>
        /// Specifies whether or not the compiler should attempt to create a
        /// PE file even if compilation errors have been reported.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a PE file has to be created even if
        /// compilation errors have been reported; otherwise,
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/ERROR</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("error")]
        [BooleanValidator()]
        public bool Error {
            get { return _error; }
            set { _error = value; }
        }

        /// <summary>
        /// Instructs NAnt to recompile the output file regardless of the file
        /// timestamps.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output file should be recompiled
        /// regardless of its timestamps; otherwise <see langword="false" />.
        /// The default is <see langword="false" />.
        /// </value>
        [TaskAttribute("rebuild")]
        [BooleanValidator()]
        public bool ForceRebuild {
            get { return _forceRebuild; }
            set { _forceRebuild = value; }
        }

        /// <summary>
        /// Specifies whether or not the compiler should type a formatted
        /// listing of the compilation result.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a formatted listing of the compilation
        /// result should be typed; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/LISTING</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("listing")]
        [BooleanValidator()]
        public bool Listing {
            get { return _listing; }
            set { _listing = value; }
        }

        /// <summary>
        /// Instructs the compiler to set the <i>FileAlignment</i> value in
        /// the PE header.
        /// </summary>
        /// <value>
        /// An <see cref="T:System.Int32" /> that represents the <i>FileAlignment</i>
        /// value to set in the PE header. The value must be a power of 2, in
        /// range from 512 to 65536.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/ALIGNMENT</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("alignment")]
        [Int32Validator()]
        public int Alignment {
            get { return _alignment; }
            set { _alignment = value; }
        }

        /// <summary>
        /// Instructs the compiler to set the <i>ImageBase</i> value in
        /// the PE header.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.Int32" /> that represents the <i>ImageBase</i>
        /// value to set in the PE header.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/BASE</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("base")]
        [Int32Validator()]
        public int Base {
            get { return _base; }
            set { _base = value; }
        }

        /// <summary>
        /// Instructs the compiler to set the <i>Flags</i> value in the CLR
        /// header.
        /// </summary>
        /// <value>
        /// An <see cref="T:System.Int32" /> that represents the <i>Flags</i>
        /// value to set in the CLR header. The most frequently value are 1
        /// (pre-IL code) and 2 (mixed code). The third bit indicating that
        /// the PE file is strong signed, is ignored.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/FLAGS</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("flags")]
        [Int32Validator()]
        public int Flags {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// Instructs the compiler to set the <i>Subsystem</i> value in the PE
        /// header.
        /// </summary>
        /// <value>
        /// An <see cref="T:System.Int32" /> that represents the <i>Subsystem</i>
        /// value to set in the PE header. The most frequently value are 3
        /// (console application) and 2 (GUI application).
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/SUBSYSTEM</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("subsystem")]
        [Int32Validator()]
        public int Subsystem {
            get { return _subsystem; }
            set { _subsystem = value; }
        }

        /// <summary>
        /// Specifies which output type should be generated.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.String" /> that contains the target type.
        /// Possible values are <c>dll</c> and <c>exe</c>.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/OUTPUT</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("target", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Target {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Instructs the compiler to generate a strong signature of the PE
        /// file.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.String" /> that contains the private
        /// encryption key.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/KEY=<![CDATA[@<]]>keysource<![CDATA[>]]></c>
        /// flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("keysource")]
        public string KeySource {
            get { return _keySource; }
            set { _keySource = value; }
        }

        /// <summary>
        /// Instructs the compiler to generate a strong signature of the PE
        /// file.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.IO.FileInfo" /> that represents the file
        /// containing the private encryption key.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/KEY=<![CDATA[<]]>keyfile<![CDATA[>]]></c>
        /// flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("keyfile")]
        public FileInfo KeyFile {
            get { return _keyFile; }
            set { _keyFile = value; }
        }

        /// <summary>
        /// Specifies the name of the output file created by the compiler.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.IO.FileInfo" /> that represents the name of
        /// the output file.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/OUTPUT</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Instructs the compiler to link the specified unmanaged resource
        /// file into the resulting PE file.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.IO.FileInfo" /> that represents the unmanaged
        /// resource file to link.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/RESOURCE</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("resourcefile")]
        public FileInfo ResourceFile {
            get { return _resourceFile; }
            set { _resourceFile = value; }
        }

        /// <summary>
        /// Specifies the set of source files to compile.
        /// </summary>
        /// <value>
        /// A <see cref="T:NAnt.Core.Types.FileSet" /> that represents the set
        /// of source files to compile.
        /// </value>
        [BuildElement("sources", Required=true)]
        public FileSet Sources {
            get { return _sources; }
            set { _sources = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// A <see cref="T:System.String" /> that contains the command-line
        /// arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return _options; }
        }

        /// <summary>
        /// Compiles the sources.
        /// </summary>
        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                //
                // ensure base directory is set, even if fileset has not been
                // initialized in build file
                //
                if (Sources.BaseDirectory == null) {
                    Sources.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
                }

                Log(Level.Info, ResourceUtils.GetString("String_CompilingFiles"),
                    Sources.FileNames.Count, OutputFile.FullName);

                //
                // set command-line arguments for the ILASM compiler
                //
                WriteOptions();

                //
                // call base class to do the work
                //
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Private Instance Methods

        /// <summary>
        /// Writes the compiler options.
        /// </summary>
        private void WriteOptions() {
            StringWriter writer = new StringWriter();

            try {
                //
                // always suppress logo and copyright statements
                //
                WriteOption(writer, "NOLOGO");

                //
                // suppress reporting compilation progress
                // unless Verbose is true
                //
                if (!Verbose) {
                    WriteOption(writer, "QUIET");
                }

                if (Clock) {
                    WriteOption(writer, "CLOCK");
                }

                if (Debug) {
                    WriteOption(writer, "DEBUG");
                }

                if (Error) {
                    WriteOption(writer, "ERROR");
                }

                if (Listing) {
                    WriteOption(writer, "LISTING");
                }

                if (Alignment > 0) {
                    WriteOption(writer, "ALIGNMENT", Alignment.ToString());
                }

                if (Base > 0) {
                    WriteOption(writer, "BASE", Base.ToString());
                }

                if (Flags > 0) {
                    WriteOption(writer, "FLAGS", Flags.ToString());
                }

                if (Subsystem > 0) {
                    WriteOption(writer, "SUBSYSTEM", Subsystem.ToString());
                }

                if (Target != null) {
                    WriteOption(writer, Target.ToUpper());
                }

                if (KeySource != null) {
                    WriteOption(writer, "KEY", "@" + KeySource);
                }

                if (KeyFile != null) {
                    WriteOption(writer, "KEY", KeyFile.FullName);
                }

                if (OutputFile != null) {
                    WriteOption(writer, "OUTPUT", OutputFile.FullName);
                }

                if (ResourceFile != null) {
                    WriteOption(writer, "RESOURCE", ResourceFile.FullName);
                }

                foreach (string fileName in Sources.FileNames) {
                    writer.Write(" \"" + fileName + "\" ");
                }

                _options = writer.ToString();
            } finally {
                //
                // close the StringWriter and the underlying stream
                //
                writer.Close();
            }
        }

        /// <summary>
        /// Writes an option using the default output format.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="StringWriter" /> to which the compiler options should
        /// be written.
        ///</param>
        /// <param name="name">
        /// A <see cref="T:System.String" /> that contains the name of the
        /// option which should be passed to the compiler.
        /// </param>
        private void WriteOption(StringWriter writer, string name) {
            writer.Write("/{0} ", name);
        }

        /// <summary>
        /// Writes an option and its value using the default output format.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="StringWriter" /> to which the compiler options should
        /// be written.
        /// </param>
        /// <param name="name">
        /// A <see cref="T:System.String" /> that contains the name of the
        /// option which should be passed to the compiler.
        /// </param>
        /// <param name="arg">
        /// A <see cref="T:System.String" /> that contains the value of the
        /// option which should be passed to the compiler.
        /// </param>
        private void WriteOption(StringWriter writer, string name, string arg) {
            //
            // always quote arguments
            //
            writer.Write("\"/{0}={1}\" ", name, arg);
        }

        /// <summary>
        /// Determines whether or not compilation is needed.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if compilation is needed; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private bool NeedsCompiling() {
            if (ForceRebuild) {
                Log(Level.Verbose, ResourceUtils.GetString("String_RebuildAttributeSetToTrue"));
                return true;
            }

            // check if output file exists
            if (!OutputFile.Exists) {
                Log(Level.Verbose, ResourceUtils.GetString("String_OutputFileDoesNotExist"),
                    OutputFile.FullName);
                return true;
            }

            // check if sources have been updated
            string fileName = FileSet.FindMoreRecentLastWriteTime(
                Sources.FileNames, OutputFile.LastWriteTime);

            if (fileName != null) {
                Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                    fileName);
                return true;
            }

            // check if unmanaged resources have been updated
            if (ResourceFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(
                    ResourceFile.FullName, OutputFile.LastWriteTime);

                if (fileName != null) {
                Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // check if strong name signature has been updated
            if (KeyFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(
                    KeyFile.FullName, OutputFile.LastWriteTime);

                if (fileName != null) {
                    Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                        fileName);
                    return true;
                }
            }

            // compilation not needed
            return false;
        }

        #endregion Private Instance Methods
    }
}
