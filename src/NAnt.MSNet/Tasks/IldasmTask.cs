// IldasmTask.cs
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

using System.Globalization;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.MSNet.Tasks {
    /// <summary>
    /// Disassembles any portable executable (PE) file that contains
    /// intermediate language (IL) code.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   Files are only disassembled if the input file is newer than the output
    ///   file, or if the output file does not exist.  However, you can 
    ///   explicitly force files to be disassembled with the 
    ///   <see cref="ForceRebuild" /> attribute.
    ///   </para>
    ///   <para>
    ///   A <see cref="FileSet" /> can be used to select files to disassemble. 
    ///   To use a <see cref="FileSet" />, the <see cref="ToDirectory" /> 
    ///   attribute must be set. The file name of the output file will be equal 
    ///   to the file name of the input file, but with extension &quot;.il&quot;.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Disassembles <c>helloworld.exe</c> to <c>helloworld.il</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <ildasm input="helloworld.exe" output="helloworld.il" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Disassembles a set of PE files into the specified directory.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <ildasm todir=".">
    ///     <assemblies>
    ///         <include name="*.exe" />
    ///         <include name="*.dll" />
    ///     </assemblies>
    /// </ildasm>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("ildasm")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class IldasmTask : ExternalProgramBase {
        #region Private Instance Fields

        private const string _TargetExt = "il";
        private bool _all;
        private bool _bytes;
        private bool _forceRebuild;
        private bool _header;
        private bool _lineNumbers;
        private bool _noIL;
        private bool _publicOnly;
        private bool _quoteAllNames;
        private bool _rawExceptionHandling;
        private bool _source;
        private bool _tokens;
        private bool _unicode;
        private bool _utf8;
        private string _item;
        private string _visibility;
        private DirectoryInfo _toDir;
        private FileInfo _inputFile;
        private FileInfo _outputFile;
        private FileSet _assemblies;
        private string _options;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies whether or not the disassembler should combine the
        /// <c>/HEADER</c>, <c>/BYTE</c>, and <c>/TOKENS</c> options. The default 
        /// is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the disassembler should combine the
        /// <c>/HEADER</c>, <c>/BYTE</c>, and <c>/TOKENS</c> options;
        /// otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/ALL</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("all")]
        [BooleanValidator()]
        public bool All {
            get { return _all; }
            set { _all = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should generate the
        /// IL stream bytes (in hexadecimal notation) as instruction comments.
        /// The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the IL stream bytes should be generated
        /// as instruction comments; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/BYTE</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("bytes")]
        [BooleanValidator()]
        public bool Bytes {
            get { return _bytes; }
            set { _bytes = value; }
        }

        /// <summary>
        /// Instructs NAnt to rebuild the output file regardless of the file
        /// timestamps. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output file should be rebuilt
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
        /// Specifies whether or not the disassembler should include PE header
        /// information and runtime header information in the output. The default 
        /// is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if PE header information and runtime header
        /// information should be included in the output; otherwise,
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/HEADER</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("header")]
        [BooleanValidator()]
        public bool Header {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// Specifies the PE file to disassemble.
        /// </summary>
        /// <value>
        /// A <see cref="System.IO.FileInfo" /> that represents the PE file
        /// to disassemble.
        /// </value>
        [TaskAttribute("input", Required=false)]
        public FileInfo InputFile {
            get { return _inputFile; }
            set { _inputFile = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should include
        /// references to original source lines. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if references to original source lines
        /// should be included; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/LINENUM</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("linenumbers")]
        [BooleanValidator()]
        public bool LineNumbers {
            get { return _lineNumbers; }
            set { _lineNumbers = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should suppress ILASM
        /// code output. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if ILASM code output should be suppresses;
        /// otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/NOIL</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("noil")]
        [BooleanValidator()]
        public bool NoIL {
            get { return _noIL; }
            set { _noIL = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should disassemble
        /// public items only. This is a shortcut for <see cref="Visibility" />="pub".
        /// The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if only public items should be
        /// disassembled; otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/PUBONLY</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("publiconly")]
        [BooleanValidator()]
        public bool PublicOnly {
            get { return _publicOnly; }
            set { _publicOnly = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should enclose all names
        /// in single quotation marks. By default, only names that don't match
        /// the ILASM definition of a simple name are quoted. The default is 
        /// <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if all names should be enclosed in single
        /// quotation marks; otherwise, <see langword="false" />. The default
        /// is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/QUOTEALLNAMES</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("quoteallnames")]
        [BooleanValidator()]
        public bool QuoteAllNames {
            get { return _quoteAllNames; }
            set { _quoteAllNames = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should generate
        /// structured exception handling clauses in canonical (label) form.
        /// The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if structured exception handling clauses
        /// should be generated in canonical form; otherwise,
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/RAWEH</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("rawexceptionhandling")]
        [BooleanValidator()]
        public bool RawExceptionHandling {
            get { return _rawExceptionHandling; }
            set { _rawExceptionHandling = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should generate
        /// original source lines as comments. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if original source lines should be
        /// generated as comments; otherwise, <see langword="false" />.
        /// The default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/SOURCE</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("source")]
        [BooleanValidator()]
        public bool Source {
            get { return _source; }
            set { _source = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should generate metadata
        /// token values as comments. The default is <see langword="false" />.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if metadata token values should be
        /// generated as comments; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/TOKENS</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("tokens")]
        [BooleanValidator()]
        public bool Tokens {
            get { return _tokens; }
            set { _tokens = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should use the UNICODE
        /// encoding when generating the output. The default is ANSI.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output should be generated using
        /// the UNICODE encoding; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/UNICODE</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("unicode")]
        [BooleanValidator()]
        public bool Unicode {
            get { return _unicode; }
            set { _unicode = value; }
        }

        /// <summary>
        /// Specifies whether or not the disassembler should use the UTF-8
        /// encoding when generating the output. The default is ANSI.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the output should be generated using
        /// the UTF-8 encoding; otherwise, <see langword="false" />. The
        /// default is <see langword="false" />.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/UTF8</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("utf8")]
        [BooleanValidator()]
        public bool Utf8 {
            get { return _utf8; }
            set { _utf8 = value; }
        }

        /// <summary>
        /// Instructs the disassembler to disassemble the specified item only.
        /// </summary>
        /// <value>
        /// A <see cref="System.String" /> that specifies the item to
        /// disassemble.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/ITEM</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("item", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string Item {
            get { return _item; }
            set { _item = value; }
        }

        /// <summary>
        /// Instructs the disassembler to disassemble only the items with the
        /// specified visibility. Possible values are <c>PUB</c>, <c>PRI</c>,
        /// <c>FAM</c>, <c>ASM</c>, <c>FAA</c>, <c>FOA</c>, <c>PSC</c>,
        /// or any combination of them separated by <c>+</c>.
        /// </summary>
        /// <value>
        /// A <see cref="System.String" /> that contains the visibility
        /// suboptions.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/VISIBILITY</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("visibility", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string Visibility {
            get { return _visibility; }
            set { _visibility = value; }
        }

        /// <summary>
        /// Specifies the name of the output file created by the disassembler.
        /// </summary>
        /// <value>
        /// A <see cref="System.IO.FileInfo" /> that represents the name of
        /// the output file.
        /// </value>
        /// <remarks>
        /// <para>
        /// Corresponds to the <c>/OUT</c> flag.
        /// </para>
        /// </remarks>
        [TaskAttribute("output", Required=false)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Specifies the directory to which outputs will be stored.
        /// </summary>
        /// <value>
        /// A <see cref="System.IO.DirectoryInfo" /> that represents the
        /// directory to which outputs will be stored.
        /// </value>
        [TaskAttribute("todir", Required=false)]
        public DirectoryInfo ToDirectory {
            get { return _toDir; }
            set { _toDir = value; }
        }
       
        /// <summary>
        /// Specifies a list of PE files to disassemble. To use a <see cref="FileSet" />, 
        /// the <see cref="ToDirectory" /> attribute must be specified.
        /// </summary>
        /// <value>
        /// A <see cref="NAnt.Core.Types.FileSet" /> that represents the set
        /// of PE files to disassemble.
        /// </value>
        [BuildElement("assemblies")]
        public FileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// The command-line arguments for the external program.
        /// </summary>
        /// <remarks>
        /// Overridden to ensure the &lt;arg&gt; elements would not be exposed
        /// to build authors.
        /// </remarks>
        public override ArgumentCollection Arguments {
            get { return base.Arguments; }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// A <see cref="System.String" /> that contains the command-line
        /// arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return _options; }
        }

        /// <summary>
        /// Checks whether the task is initialized with valid attributes.
        /// </summary>
        protected override void Initialize() {
            if (ToDirectory == null && Assemblies != null && Assemblies.Includes.Count > 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3001")), Location);
            }

            if (InputFile != null && Assemblies != null && Assemblies.Includes.Count > 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3002")), Location);
            }

            if (OutputFile == null && ToDirectory == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3003")), Location);
            }

            if (OutputFile != null && ToDirectory != null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    ResourceUtils.GetString("NA3004")), Location);
            }
        }

        /// <summary>
        /// Disassembles the PE file(s).
        /// </summary>
        protected override void ExecuteTask() {
            // disassemble a single PE file
            if (InputFile != null) {
                DisassemblyFile(InputFile);
            } else {
                foreach (string inputFile in Assemblies.FileNames) {
                    DisassemblyFile(new FileInfo(inputFile));
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Private Instance Methods

        /// <summary>
        /// Disassembles the specified PE file.
        /// </summary>
        /// <param name="inputFile">The PE file to disassemble.</param>
        private void DisassemblyFile(FileInfo inputFile) {
            // verify if input file actually exists
            if (!inputFile.Exists) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA3005"), inputFile.FullName), Location);
            }

            // determine output file corresponding with PE file
            FileInfo outputFile = GetOutputFile(inputFile);

            // only actually perform disassembly if necessary
            if (NeedsDisassembling(inputFile, outputFile)) {
                Log(Level.Info, ResourceUtils.GetString("String_Disassembling"),
                    inputFile.FullName, outputFile.FullName);

                // ensure output directory exists
                if (!outputFile.Directory.Exists) {
                    outputFile.Directory.Create();
                }

                // set command-line arguments for the disassembler
                WriteOptions(inputFile, outputFile);

                // call base class to do the work
                base.ExecuteTask();
            }
        }

        /// <summary>
        /// Determines the full path and extension for the output file.
        /// </summary>
        /// <param name="inputFile">
        /// A <see cref="System.IO.FileInfo" /> that represents the PE file
        /// file for which the corresponding output file should be determined.
        /// </param>
        /// <returns>
        /// A <see cref="System.IO.FileInfo" /> that represents the full path
        /// for the output file.
        /// </returns>
        /// <exception cref="BuildException">The path of the output file could not be determined.</exception>
        private FileInfo GetOutputFile(FileInfo inputFile) {
            FileInfo outputFile;

            if (OutputFile != null) {
                outputFile = new FileInfo(OutputFile.FullName);
            } else if (ToDirectory != null) {
                outputFile = new FileInfo(Path.Combine(ToDirectory.FullName, inputFile.Name));
                outputFile = new FileInfo(Path.ChangeExtension(outputFile.FullName, _TargetExt));
            } else {
                // we should never actually get here (if the checks in 
                // Initialize have been executed
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA3006"), inputFile.FullName), Location);
            }

            return outputFile;
        }

        /// <summary>
        /// Writes the disassembler options.
        /// </summary>
        private void WriteOptions(FileInfo inputFile, FileInfo outputFile) {
            using (StringWriter writer = new StringWriter()) {
                // always direct the output to console
                WriteOption(writer, "TEXT");
                WriteOption(writer, "NOBAR");

                if (All) {
                    WriteOption(writer, "ALL");
                }

                if (Bytes) {
                    WriteOption(writer, "BYTES");
                }

                if (Header) {
                    WriteOption(writer, "HEADER");
                }

                if (LineNumbers) {
                    WriteOption(writer, "LINENUM");
                }

                if (NoIL) {
                    WriteOption(writer, "NOIL");
                }

                if (PublicOnly) {
                    WriteOption(writer, "PUBONLY");
                }

                if (QuoteAllNames) {
                    WriteOption(writer, "QUOTEALLNAMES");
                }

                if (RawExceptionHandling) {
                    WriteOption(writer, "RAWEH");
                }

                if (Source) {
                    WriteOption(writer, "SOURCE");
                }

                if (Tokens) {
                    WriteOption(writer, "TOKENS");
                }

                if (Unicode) {
                    WriteOption(writer, "UNICODE");
                }

                if (Utf8) {
                    WriteOption(writer, "UTF8");
                }

                if (Item != null) {
                    WriteOption(writer, "ITEM", Item);
                }

                if (Visibility != null) {
                    WriteOption(writer, "VISIBILITY", Visibility.ToUpper());
                }

                // specifiy path of output file
                WriteOption(writer, "OUT", outputFile.FullName);

                // specify path of PE file to disassembly
                writer.Write(" \"" + inputFile.FullName + "\" ");

                _options = writer.ToString();

                // close the StringWriter and the underlying stream
                writer.Close();
            }
        }

        /// <summary>
        /// Writes an option using the default output format.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="StringWriter" /> to which the disassembler options
        /// should be written.
        ///</param>
        /// <param name="name">
        /// A <see cref="System.String" /> that contains the name of the
        /// option which should be passed to the disassembler.
        /// </param>
        private void WriteOption(StringWriter writer, string name) {
            writer.Write("/{0} ", name);
        }

        /// <summary>
        /// Writes an option and its value using the default output format.
        /// </summary>
        /// <param name="writer">
        /// The <see cref="StringWriter" /> to which the disassembler options
        /// should be written.
        /// </param>
        /// <param name="name">
        /// A <see cref="System.String" /> that contains the name of the
        /// option which should be passed to the disassembler.
        /// </param>
        /// <param name="arg">
        /// A <see cref="System.String" /> that contains the value of the
        /// option which should be passed to the disassembler.
        /// </param>
        private void WriteOption(StringWriter writer, string name, string arg) {
            // always quote arguments
            writer.Write("\"/{0}={1}\" ", name, arg);
        }

        /// <summary>
        /// Determines whether or not disassembling is needed.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if disassembling is needed; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private bool NeedsDisassembling(FileInfo inputFile, FileInfo outputFile) {
            if (ForceRebuild) {
                Log(Level.Verbose, ResourceUtils.GetString("String_RebuildAttributeSetToTrue"));
                return true;
            }

            // check if output file already exists
            if (!outputFile.Exists) {
                Log(Level.Verbose, ResourceUtils.GetString("String_OutputFileDoesNotExist"),
                    outputFile.FullName);
                return true;
            }

             // check if the source assembly has been updated
             string fileName = FileSet.FindMoreRecentLastWriteTime(
                inputFile.FullName, outputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, ResourceUtils.GetString("String_FileHasBeenUpdated"),
                    fileName);
                return true;
            }

            // no need to disassembly the input file
            return false;
        }

        #endregion Private Instance Methods
    }
}
