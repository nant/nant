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

using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Exports a .NET assembly to a type library that can be used from unmanaged 
    /// code (wraps Microsoft's <c>tlbexp.exe</c>).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   <see href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</see>
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Export <c>DotNetAssembly.dll</c> to <c>LegacyCOM.dll</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <tlbexp assembly="DotNetAssembly.dll" output="LegacyCOM.dll" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("tlbexp")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class TlbExpTask : ExternalProgramBase {
        #region Private Instance Fields

        private FileInfo _assemblyFile;
        private FileInfo _outputFile;
        private FileInfo _namesFile;
        private StringBuilder _argumentBuilder;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies the assembly for which to export a type library.
        /// </summary>
        /// <value>
        /// The assembly for which to export a type library.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("assembly", Required=true)]
        public FileInfo AssemblyFile {
            get { return _assemblyFile; }
            set { _assemblyFile = value; }
        }

        /// <summary>
        /// Specifies the name of the type library file to generate.
        /// </summary>
        /// <value>
        /// The name of the type library file to generate.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("output", Required=true)]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Specifies the file used to determine capitalization of names in a 
        /// type library.
        /// </summary>
        /// <value>
        /// The file used to determine capitalization of names in a type library.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryexportertlbexpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("names")]
        public FileInfo NamesFile {
            get { return _namesFile; }
            set { _namesFile = value; }
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
            get { 
                if (_argumentBuilder != null) {
                    return _argumentBuilder.ToString();
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Exports the type library.
        /// </summary>
        protected override void ExecuteTask() {
            //Check to see if any of the underlying interop dlls or the typelibs have changed
            //Otherwise, it's not necessary to reimport.
            if (NeedsCompiling()) {
                //Using a stringbuilder vs. StreamWriter since this program will not accept response files.
                _argumentBuilder = new StringBuilder();

                _argumentBuilder.Append("\"" + AssemblyFile.FullName + "\"");

                // Any option that specifies a file name must be wrapped in quotes
                // to handle cases with spaces in the path.
                _argumentBuilder.AppendFormat(" /out:\"{0}\"", OutputFile.FullName);

                // suppresses the Microsoft startup banner display
                _argumentBuilder.Append(" /nologo");

                if (Verbose) {
                    // displays extra information
                    _argumentBuilder.Append(" /verbose");
                } else {
                    // suppresses all output except for errors
                    _argumentBuilder.Append(" /silent");
                }

                // filename used to determine capitalization of names in typelib
                if (NamesFile != null) {
                    _argumentBuilder.AppendFormat(" /names:\"{0}\"", NamesFile.FullName);
                }

                // call base class to do the work
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the assembly needs to be exported to a type 
        /// library again.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the assembly needs to be exported to a 
        /// type library; otherwise, <see langword="false" />.
        /// </returns>
        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            if (!OutputFile.Exists) {
                Log(Level.Verbose, "Output file '{0}' does not exist, recompiling.", 
                    OutputFile.FullName);
                return true;
            }

            // check if the assembly was changed since the typelib was generated
            string fileName = FileSet.FindMoreRecentLastWriteTime(AssemblyFile.FullName, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' has been updated, recompiling.", 
                    fileName);
                return true;
            }

            // check if the names file was changed since the typelib was generated
            if (NamesFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(NamesFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, "'{0}' has been updated, recompiling.", 
                        fileName);
                    return true;
                }
            }

            // if we made it here then we don't have to export the assembly again.
            return false;
        }

        #endregion Protected Instance Methods
    }
}
