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
// Aaron Anderson (aaron@skypoint.com | aaron.anderson@farmcreditbank.com)

using System.Collections.Specialized;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Exports a .NET assembly to a type library that can be used from unmanaged 
    /// code (wraps Microsoft's tlbexp.exe).
    /// </summary>
    /// <remarks>
    ///   <para><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></para>
    /// </remarks>
    /// <example>
    ///   <para>Export <c>DotNetAssembly.dll</c> to <c>LegacyCOM.dll</c>.</para>
    ///   <code><![CDATA[<tlbexp assembly="DotNetAssembly.dll" output="LegacyCOM.dll"/>]]></code>
    /// </example>
    [TaskName("tlbexp")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class TlbExpTask : ExternalProgramBase {
        #region Private Instance Fields

        string _assembly = null;
        string _output = null;
        string _names = null;
        string _programArguments = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Gets or set the assembly for which to export a type library.
        /// </summary>
        /// <value>The assembly for which to export a type library.</value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("assembly", Required=false)]
        public string Assembly {
            get { return (_assembly != null) ? Project.GetFullPath(_assembly) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _assembly = value;
                } else {
                    _assembly = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the type library file to generate.
        /// </summary>
        /// <value>
        /// The name of the type library file to generate.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
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
        /// Gets or sets the file used to determine capitalization of names in a 
        /// type library.
        /// </summary>
        /// <value>
        /// The file used to determine capitalization of names in a type library.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("names")]
        public string Names {
            get { return (_names != null) ? Project.GetFullPath(_names) : null; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    _names = value;
                } else {
                    _names = null;
                }
            }
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
        /// Exports the type library.
        /// </summary>
        protected override void ExecuteTask() {
            //Check to see if any of the underlying interop dlls or the typelibs have changed
            //Otherwise, it's not necessary to reimport.
            if (NeedsCompiling()) {
                //Using a stringbuilder vs. StreamWriter since this program will not accept response files.
                StringBuilder writer = new StringBuilder();

                try {
                    writer.Append("\"" + Assembly + "\"");

                    // Any option that specifies a file name must be wrapped in quotes
                    // to handle cases with spaces in the path.
                    writer.AppendFormat(" /out:\"{0}\"", Output);

                    // Microsoft common compiler options
                    writer.Append(" /nologo");

                    // Filename used to determine capitalization of names in typelib
                    if (Names != null) {
                        writer.AppendFormat(" /names:\"{0}\"", Names);
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
        /// Determines whether the type library needs to be exported again.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the type library needs to be exported; otherwise, 
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

            // if we made it here then we don't have to reimport the typelib.
            return false;
        }

        #endregion Protected Instance Methods
    }
}
