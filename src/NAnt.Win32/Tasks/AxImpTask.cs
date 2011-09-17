// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Jayme C. Edwards (jedwards@wi.rr.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Generates a Windows Forms Control that wraps ActiveX Controls defined 
    /// in an OCX.
    /// </summary>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <aximp ocx="MyControl.ocx" output="MyFormsControl.dll" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("aximp")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class AxImpTask : ExternalProgramBase {
        #region Private Instance Fields

        private FileInfo _ocxFile;
        private FileInfo _outputFile;
        private FileInfo _publicKeyFile;
        private FileInfo _keyFile;
        private FileInfo _rcwFile;
        private string _keyContainer;
        private bool _delaySign;
        private bool _generateSource;

        // framework configuration settings
        private bool _supportsRcw = true;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Filename of the .ocx file.
        /// </summary>
        [TaskAttribute("ocx", Required=true)]
        public FileInfo OcxFile {
            get { return _ocxFile; }
            set { _ocxFile = value; }
        }

        /// <summary>
        /// Filename of the generated assembly.
        /// </summary>
        [TaskAttribute("output")]
        public FileInfo OutputFile {
            get { return _outputFile; }
            set { _outputFile = value; }
        }

        /// <summary>
        /// Specifies the file containing the public key to use to sign the 
        /// resulting assembly.
        /// </summary>
        /// <value>
        /// The file containing the public key to use to sign the resulting
        /// assembly.
        /// </value>
        [TaskAttribute("publickey")]
        public FileInfo PublicKeyFile {
            get { return _publicKeyFile; }
            set { _publicKeyFile = value; }
        }

        /// <summary>
        /// Specifies the publisher's official public/private key pair with which 
        /// the resulting assembly should be signed with a strong name.
        /// </summary>
        /// <value>
        /// The keyfile to use to sign the resulting assembly with a strong name.
        /// </value>
        /// <remarks><a href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrftypelibraryimportertlbimpexe.htm">See the Microsoft.NET Framework SDK documentation for details.</a></remarks>
        [TaskAttribute("keyfile")]
        public FileInfo KeyFile {
            get { return _keyFile; }
            set { _keyFile = value; }
        }

        /// <summary>
        /// Specifies the key container in which the public/private key pair 
        /// should be found that should be used to sign the resulting assembly
        /// with a strong name.
        /// </summary>
        /// <value>
        /// The key container containing a public/private key pair that should
        /// be used to sign the resulting assembly.
        /// </value>
        [TaskAttribute("keycontainer")]
        public string KeyContainer {
            get { return _keyContainer; }
            set { _keyContainer = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies to sign the resulting control using delayed signing.
        /// </summary>
        [TaskAttribute("delaysign")]
        [BooleanValidator()]
        public bool DelaySign {
            get { return _delaySign; }
            set { _delaySign = value; }
        }

        /// <summary>
        /// Determines whether C# source code for the Windows Form wrapper should 
        /// be generated. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("generatesource")]
        [BooleanValidator()]
        public bool GenerateSource {
            get { return _generateSource; }
            set { _generateSource = value; }
        }

        /// <summary>
        /// Assembly to use for Runtime Callable Wrapper rather than generating 
        /// new one [.NET 1.1 or higher].
        /// </summary>
        [TaskAttribute("rcw")]
        public FileInfo RcwFile {
            get { return _rcwFile; }
            set { _rcwFile = value; }
        }

        /// <summary>
        /// Indicates whether <c>aximp</c> supports using an existing Runtime
        /// Callable Wrapper for a given target framework. The default is 
        /// <see langword="true" />.
        /// </summary>
        [FrameworkConfigurable("supportsrcw")]
        public bool SupportsRcw {
            get { return _supportsRcw; }
            set { _supportsRcw = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get { return ""; }
        }

        /// <summary>
        /// Import the ActiveX control.
        /// </summary>
        protected override void ExecuteTask() {
            Log(Level.Info, "Generating Windows Forms Control wrapping '{0}'.",
                OcxFile.FullName);

            if (!NeedsCompiling()) {
                return;
            }

            if (DelaySign) {
                Arguments.Add(new Argument("/delaysign"));
            }

            if (GenerateSource) {
                Arguments.Add(new Argument("/source"));
            }

            if (Verbose) {
                Arguments.Add(new Argument("/verbose"));
            } else {
                Arguments.Add(new Argument("/silent"));
            }

            if (OutputFile != null) {
                Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture, 
                    "/out:\"{0}\"", OutputFile.FullName)));
            }

            if (PublicKeyFile != null) {
                Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture, 
                    "/publickey:\"{0}\"", PublicKeyFile.FullName)));
            }

            if (KeyFile != null) {
                Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture, 
                    "/keyfile:\"{0}\"", KeyFile.FullName)));
            }

            if (KeyContainer != null) {
                Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture, 
                    "/keycontainer:\"{0}\"", KeyContainer)));
            }

            if (RcwFile != null) {
                if (SupportsRcw) {
                    Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture, 
                        "/rcw:\"{0}\"", RcwFile.FullName)));
                }
            }

            // suppresses display of the sign-on banner
            Arguments.Add(new Argument("/nologo"));

            Arguments.Add(new Argument(OcxFile));
            
            try {
                base.ExecuteTask();
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error importing ActiveX control from '{0}'.", OcxFile.FullName), 
                    Location, ex);
            }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Determines whether the assembly needs to be created again.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the assembly needs to be created again; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        protected virtual bool NeedsCompiling() {
            // return true as soon as we know we need to compile

            if (!OutputFile.Exists) {
                Log(Level.Verbose, "Output file '{0}' does not exist, recompiling.", 
                    OutputFile.FullName);
                return true;
            }

            // check if the ocx was changed since the assembly was generated
            string fileName = FileSet.FindMoreRecentLastWriteTime(OcxFile.FullName, OutputFile.LastWriteTime);
            if (fileName != null) {
                Log(Level.Verbose, "'{0}' has been updated, recompiling.", 
                    fileName);
                return true;
            }

            // check if the public key file was changed since the assembly was 
            // generated
            if (PublicKeyFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(PublicKeyFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, "'{0}' has been updated, recompiling.", 
                        fileName);
                    return true;
                }
            }

            // check if the key file was changed since the assembly was generated
            if (KeyFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(KeyFile.FullName, OutputFile.LastWriteTime);
                if (fileName != null) {
                    Log(Level.Verbose, "'{0}' has been updated, recompiling.", 
                        fileName);
                    return true;
                }
            }

            // check if the Runtime Callable Wrapper file was changed since the 
            // assembly was generated
            if (RcwFile != null) {
                fileName = FileSet.FindMoreRecentLastWriteTime(RcwFile.FullName, OutputFile.LastWriteTime);
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
