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
// original author unknown
// Ian MacLean (ian_maclean@another.com)

using System;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

namespace NAnt.VisualCpp.Tasks {
    /// <summary>
    /// This tasks allows you to run MIDL.exe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This task only supports a small subset of the MIDL.EXE command line 
    /// switches, but you can use the options element to specify any other
    /// unsupported commands you want to specify.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <midl
    ///     env="win32"
    ///     Oi="cf"
    ///     tlb="${outputdir}\TempAtl.tlb"
    ///     header="${outputdir}\TempAtl.h"
    ///     iid="${outputdir}\TempAtl_i.c"
    ///     proxy="${outputdir}\TempAtl_p.c"
    ///     filename="TempAtl.idl"
    /// >
    ///     <defines>
    ///         <define name="_DEBUG"/>
    ///         <define name="WIN32" value="1"/>
    ///     </defines>
    ///     <options>
    ///         <option name="/mktyplib203"/>
    ///         <option name="/error" value="allocation"/>
    ///     </options>
    /// </midl>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("midl")]
    public class MidlTask : ExternalProgramBase {
        #region Private Instance Fields

        private string _responseFileName;
        private string _acf;
        private string _align;
        private bool _appConfig;
        private string _char;
        private string _client;
        private string _cstub;
        // TODO: /D!!!!!
        private string _dlldata;
        private string _env = "win32";
        // TODO: /error
        private string _Oi;
        private FileInfo _header;
        private FileInfo _iid;
        private FileInfo _proxy;
        private FileInfo _tlb;
        private FileInfo _filename;
        private OptionCollection _options = new OptionCollection();
        private OptionCollection _defines = new OptionCollection();

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string PROG_FILE_NAME = "midl.exe";

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// The /acf switch allows the user to supply an
        /// explicit ACF file name. The switch also
        /// allows the use of different interface names in
        /// the IDL and ACF files.
        /// </summary>
        [TaskAttribute("acf")]
        public string Acf {
            get { return _acf; }
            set { _acf = value; }
        }

        /// <summary>
        /// The /align switch is functionally the same as the
        /// MIDL /Zp option and is recognized by the MIDL compiler
        /// solely for backward compatibility with MkTypLib.
        /// </summary>
        /// <remarks>The alignment value can be 1, 2, 4, or 8.</remarks>
        [TaskAttribute("align")]
        public string Align {
            get { return _align; }
            set { _align = value; }
        }

        /// <summary>
        /// The /app_config switch selects application-configuration
        /// mode, which allows you to use some ACF keywords in the
        /// IDL file. With this MIDL compiler switch, you can omit
        /// the ACF and specify an interface in a single IDL file.
        /// </summary>
        [TaskAttribute("app_config"), BooleanValidator()]
        public bool AppConfig {
            get { return _appConfig; }
            set { _appConfig = value; }
        }

        /// <summary>
        /// The /char switch helps to ensure that the MIDL compiler
        /// and C compiler operate together correctly for all char
        /// and small types.
        /// </summary>
        /// <remarks>Can be one of signed | unsigned | ascii7 </remarks>
        [TaskAttribute("char")]
        public string Char {
            get { return _char; }
            set { _char = value; }
        }

        /// <summary>
        /// The /client switch directs the MIDL compiler to generate
        /// client-side C source files for an RPC interface
        /// </summary>
        /// <remarks>can be one of stub | none</remarks>
        [TaskAttribute("client")]
        public string Client {
            get { return _client; }
            set { _client = value; }
        }

        /// <summary>
        /// The /cstub switch specifies the name of the client
        /// stub file for an RPC interface.
        /// </summary>
        [TaskAttribute("cstub")]
        public string CStub {
            get { return _cstub; }
            set { _cstub = value; }
        }

        /// <summary>
        /// The /dlldata switch is used to specify the file
        /// name for the generated dlldata file for a proxy
        /// DLL. The default file name Dlldata.c is used if
        /// the /dlldata switch is not specified.
        /// </summary>
        [TaskAttribute("dlldata")]
        public string DllData {
            get { return _dlldata; }
            set { _dlldata = value; }
        }

        /// <summary>
        /// The /env switch selects the
        /// environment in which the application runs.
        /// </summary>
        /// <remarks>It can take the values win32 and win64</remarks>
        [TaskAttribute("env")]
        public string Env {
            get { return _env; }
            set { _env = value; }
        }

        /// <summary>
        /// The /Oi switch directs the MIDL compiler to
        /// use a fully-interpreted marshaling method.
        /// The /Oic and /Oicf switches provide additional
        /// performance enhancements.
        /// </summary>
        /// <remarks>
        /// If you specify the Oi attribute, you must set it to
        /// one of the values:
        /// - Oi=""
        /// - Oi="c"
        /// - Oi="f"
        /// - Oi="cf"
        /// </remarks>
        [TaskAttribute("Oi")]
        public string Oi {
            get { return _Oi; }
            set { _Oi = value; }
        }

        /// <summary>
        /// Specifies a file name for the type library generated by the MIDL 
        /// compiler.
        /// </summary>
        [TaskAttribute("tlb", Required=true)]
        public FileInfo Tlb {
            get { return _tlb; }
            set { _tlb = value; }
        }

        /// <summary>
        /// Specifies the name of the header file.
        /// </summary>
        [TaskAttribute("header")]
        public FileInfo Header {
            get { return _header; }
            set { _header = value; }
        }

        /// <summary>
        /// Specifies the name of the interface identifier file for a COM 
        /// interface, overriding the default name obtained by adding _i.c 
        /// to the IDL file name.
        /// </summary>
        [TaskAttribute("iid")]
        public FileInfo Iid {
            get { return _iid; }
            set { _iid = value; }
        }

        /// <summary>
        /// Specifies the name of the interface proxy file for a COM interface.
        /// </summary>
        [TaskAttribute("proxy")]
        public FileInfo Proxy {
            get { return _proxy; }
            set { _proxy = value; }
        }

        /// <summary>
        /// Name of .IDL file to process.
        /// </summary>
        [TaskAttribute("filename", Required=true)]
        public FileInfo Filename {
            get { return _filename; }
            set { _filename = value; }
        }

        /// <summary>
        /// Additional options to pass to midl.exe.
        /// </summary>
        [BuildElementCollection("options", "option")]
        public OptionCollection Options {
            get { return _options; }
        }

        /// <summary>
        /// Macro definitions to pass to mdil.exe.
        /// Each entry will generate a /D
        /// </summary>
        [BuildElementCollection("defines", "define")]
        public OptionCollection Defines {
            get { return _defines; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Filename of program to execute
        /// </summary>
        public override string ProgramFileName {
            get { return PROG_FILE_NAME; }
        }

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return "@" + "\"" + _responseFileName + "\""; }
        }

        /// <summary>
        /// This is where the work is done.
        /// </summary>
        protected override void ExecuteTask() {
            if (NeedsCompiling()) {
                // create temp response file to hold compiler options
                _responseFileName = Path.GetTempFileName();

                try {
                    using (StreamWriter writer = new StreamWriter(_responseFileName)) {
                        WriteResponseFile(writer);
                    }

                    if (Verbose) {
                        // display response file contents
                        Log(Level.Info, "Contents of " + _responseFileName);
                        StreamReader reader = File.OpenText(_responseFileName);
                        Log(Level.Info, reader.ReadToEnd());
                        reader.Close();
                    }

                    base.ExecuteTask();
                } finally {
                    // make sure we delete the response file
                    File.Delete(_responseFileName);
                    _responseFileName = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Private Instance Methods

        /// <summary>
        /// Check output files to see if we need rebuilding.
        /// </summary>
        /// <see langword="true" /> if a rebuild is needed; otherwise, 
        /// <see langword="false" />.
        private bool NeedsCompiling() {
            //
            // we should check out against all four
            // output file
            //
            if (NeedsCompiling(Tlb)) {
                return true;
            } else if (Header != null && NeedsCompiling(Header)) {
                return true;
            } else if (Iid != null && NeedsCompiling(Iid)) {
                return true;
            }
            /*
                     if ( NeedsCompiling(_proxy) )
                        return true;
            */
            return false;
        }

        /// <summary>
        /// Check output files to see if we need rebuilding.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if a rebuild is needed; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        private bool NeedsCompiling(FileInfo outputFile) {
            if (!outputFile.Exists) {
                Log(Level.Verbose, "Output file '{0}' does not exist, recompiling.", 
                    outputFile.FullName);
                return true;
            }
            string fileName = FileSet.FindMoreRecentLastWriteTime(Filename.FullName, 
                outputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' is out of date, recompiling.", fileName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Writes the response file for <c>midl.exe</c>.
        /// </summary>
        private void WriteResponseFile(TextWriter writer) {
            // suppresses display of the sign-on banner                    
            writer.WriteLine("/nologo");

            writer.WriteLine("/env " + _env);

            if (_acf != null)
                writer.WriteLine("/acf {0}", _acf);
            if (_align != null)
                writer.WriteLine("/align {0}", _align);
            if (_appConfig)
                writer.WriteLine("/app_config");
            if (_char != null)
                writer.WriteLine("/char {0}", _char);
            if (_client != null)
                writer.WriteLine("/client {0}", _client);
            if (_cstub != null)
                writer.WriteLine("/cstub {0}", _cstub);
            if (_dlldata != null)
                writer.WriteLine("/dlldata \"{0}\"", DllData );

            if (_Oi != null)
                writer.WriteLine("/Oi" + _Oi);
            if (Tlb != null)
                writer.WriteLine("/tlb \"{0}\"", Tlb.FullName);
            if (_header != null)
                writer.WriteLine("/header \"{0}\"", Header.FullName);
            if (Iid != null)
                writer.WriteLine("/iid \"{0}\"", Iid.FullName);
            if (Proxy != null)
                writer.WriteLine("/proxy \"{0}\"", Proxy.FullName);

            foreach (Option define in _defines) {
                if (IfDefined && !UnlessDefined) {
                    if (define.Value == null) {
                        writer.WriteLine("/D " + define.OptionName);
                    } else {
                        writer.WriteLine("/D " + define.OptionName + "=" + define.Value);
                    }
                }
            }

            foreach (Option option in _options) {
                if (IfDefined && !UnlessDefined) {
                    if (option.Value == null) {
                        writer.WriteLine(option.OptionName);
                    } else {
                        writer.WriteLine(option.OptionName + " " + option.Value);
                    }
                }
            }

            writer.WriteLine("\"{0}\"", Filename.FullName);
        }

        #endregion Private Instance Methods
    }
}
