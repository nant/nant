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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Aaron Anderson (gerry_shaw@yahoo.com)
// Ian MacLean (ian@maclean.ms)

using System.Collections.Specialized;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Imports a type library to a .NET assembly (wraps Microsoft's tlbimp.exe).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This task lets you easily create interop assemblies.  By default, it will 
    /// not reimport if the underlying COM TypeLib or reference has not changed.
    /// </para>
    /// <para><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></para>
    /// </remarks>
    /// <example>
    ///   <para>Import <c>LegacyCOM.dll</c> to <c>DotNetAssembly.dll</c>.</para>
    ///   <code><![CDATA[<tlbimp typelib="LegacyCOM.dll" output="DotNetAssembly.dll"/>]]></code>
    /// </example>
    [TaskName("tlbimp")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class TlbImpTask : ExternalProgramBase {
        #region Private Instance Fields

        string _output = null;
        string _namespace = null;
        string _asmVersion = null; 
        bool _delaySign = false;
        bool _primary = false;
        string _publicKey = null;
        string _keyFile = null;
        string _keyContainer = null;
        FileSet _references = new FileSet();
        bool _strictref = false;
        bool _sysarray = false;
        bool _unsafe = false;
        string _typelib = null;
        string _programArguments = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies the <b>/out</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("output", Required=true)]
        public string Output {
            get { return (_output != null) ? Project.GetFullPath(_output) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _output = value;
                } else {
                    _output = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the namespace in which to produce the assembly.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value>The namespace in which to produce the assembly.</value>
        [TaskAttribute("namespace")]
        public string Namespace {
            get { return _namespace; }
            set {
                if (value != null && value.Trim().Length != 0) {
                    _namespace = value;
                } else {
                    _namespace = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the version number of the assembly to produce.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The version number should be in the format major.minor.build.revision.
        /// </para>
        /// <para>
        /// <a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a>
        /// </para>
        /// </remarks>
        /// <value></value>
        [TaskAttribute("asmversion")]
        public string AsmVersion {
            get { return _asmVersion; }
            set {
                if (value != null && value.Trim().Length != 0) {
                    _asmVersion = value;
                } else {
                    _asmVersion = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the resulting assembly should 
        /// be signed with a strong name using delayed signing.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("delaysign")]
        [BooleanValidator()]
        public bool DelaySign {
            get { return _delaySign; }
            set { _delaySign = value; }
        }

        /// <summary>
        /// Specifies the <b>/primary</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("primary")]
        [BooleanValidator()]
        public bool Primary {
            get { return _primary; }
            set { _primary = value; }
        }

        /// <summary>
        /// Specifies the <b>/publickey</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("publickey")]
        public string PublicKey {
            get { return (_publicKey != null) ? Project.GetFullPath(_publicKey) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _publicKey = value;
                } else {
                    _publicKey = null;
                }
            }
        }

        /// <summary>
        /// Specifies the <b>/keyfile</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("keyfile")]
        public string KeyFile {
            get { return (_keyFile != null) ? Project.GetFullPath(_keyFile) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _keyFile = value;
                } else {
                    _keyFile = null;
                }
            }
        }

        /// <summary>
        /// Specifies the <b>/keycontainer</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("keycontainer")]
        public string KeyContainer {
            get { return _keyContainer; }
            set {
                if (value != null && value.Trim().Length != 0) {
                    _keyContainer = value;
                } else {
                    _keyContainer = null;
                }
            }
        }

        /// <summary>
        /// Specifies the <b>/reference</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [FileSet("references")]
        public FileSet References {
            get { return _references; } 
            set { _references = value; }
        }

        /// <summary>
        /// Specifies the <b>/strictref</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("strictref")]
        [BooleanValidator()]
        public bool StrictRef {
            get { return _strictref;}
            set { _strictref = value; }
        }

        /// <summary>
        /// Specifies the <b>/sysarray</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("sysarray")]
        [BooleanValidator()]
        public bool SysArray {
            get { return _sysarray; }
            set { _sysarray = value; }
        }

        /// <summary>
        /// Specifies the source type library that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("typelib", Required=true)]
        public string TypeLib {
            get { return (_typelib != null) ? Project.GetFullPath(_typelib) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _typelib = value;
                } else {
                    _typelib = null;
                }
            }
        }

        /// <summary>
        /// Specifies the <b>/unsafe</b> option that gets passed to the type library importer.
        /// </summary>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        /// <value></value>
        [TaskAttribute("unsafe")]
        [BooleanValidator()]
        public bool Unsafe {
            get { return _unsafe; }
            set { _unsafe = value; }
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
            get { return _programArguments; }
        }

        /// <summary>
        /// Imports the type library to a .NET assembly.
        /// </summary>
        protected override void ExecuteTask() {
            //Check to see if any of the underlying interop dlls or the typelibs have changed
            //Otherwise, it's not necessary to reimport.
            if (NeedsCompiling()) {
                //Using a stringbuilder vs. StreamWriter since this program will not accept response files.
                StringBuilder writer = new StringBuilder();

                try {
                    if (References.BaseDirectory == null) {
                        References.BaseDirectory = BaseDirectory;
                    }

                    writer.Append("\"" + _typelib + "\"");

                    // Any option that specifies a file name must be wrapped in quotes
                    // to handle cases with spaces in the path.
                    writer.AppendFormat(" /out:\"{0}\"", Output);

                    // Microsoft common compiler options
                    writer.Append(" /nologo");

                    if (AsmVersion != null) {
                        writer.AppendFormat(" /asmversion:{0}", AsmVersion);
                    }

                    if (Namespace != null) {
                        writer.AppendFormat(" /namespace:{0}", Namespace);
                    }

                    if (Primary) {
                        writer.Append(" /primary");
                    }

                    if (Unsafe) {
                        writer.Append(" /unsafe");
                    }

                    if (DelaySign) {
                        writer.Append(" /delaysign");
                    }

                    if (PublicKey != null) {
                        writer.AppendFormat(" /publickey:{0}", _publicKey);
                    }

                    if (KeyFile != null) {
                        writer.AppendFormat(" /keyfile:\"{0}\"", _keyFile);
                    }

                    if (KeyContainer != null) {
                        writer.AppendFormat(" /keycontainer:{0}", _keyContainer);
                    }

                    if (StrictRef) {
                        writer.Append(" /strictref");
                    }

                    if (SysArray) {
                        writer.Append(" /sysarray");
                    }

                    if (!Verbose) {
                        writer.Append(" /silent");
                    }

                    foreach (string fileName in References.FileNames) {
                        writer.AppendFormat(" /reference:\"{0}\"", fileName);
                    }

                    // call base class to do the work
                    _programArguments = writer.ToString();
                    base.ExecuteTask();
                } finally {
                    writer = null;
                }
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the type library needs to be imported again.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the type library needs to be imported; otherwise, 
        /// <c>false</c>.
        /// </returns>
        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            FileInfo outputFileInfo = new FileInfo(Output);
            if (!outputFileInfo.Exists) {
                return true;
            }

            //HACK:(POSSIBLY)Is there any other way to pass in a single file to check to see if it needs to be updated?
            StringCollection fileset = new StringCollection();
            fileset.Add(outputFileInfo.FullName);
            string fileName = FileSet.FindMoreRecentLastWriteTime(fileset, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Info, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            fileName = FileSet.FindMoreRecentLastWriteTime(References.FileNames, outputFileInfo.LastWriteTime);
            if (fileName != null) {
                Log(Level.Info, LogPrefix + "{0} is out of date, recompiling.", fileName);
                return true;
            }

            // if we made it here then we don't have to reimport the typelib.
            return false;
        }

        #endregion Protected Instance Methods
    }
}
