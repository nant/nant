// NAntContrib
// Copyright (C) 2003 David Waite
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Library General Public License as
// published by the Free Software Foundation; either version 2 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Library General Public License for more details.
//
// You should have received a copy of the GNU Library General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.DotNet.Tasks;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Signs Delay-Signed .NET Assemblies, or re-signs existing assemblies.
    /// </summary>
    /// <remarks>
    /// The delay-signing mechanism takes a fileset (named targets)
    /// and either a 'keyfile' attribute for a file containing the
    /// public and private keys, or 'container' to name a key container.
    /// 
    /// The attribute 'quiet' indicates whether the full output should be
    /// displayed. The default is to suppress all non-error output.
    /// </remarks>
    /// <example>
    ///   <para>Sign partially-signed <c>foo.dll</c> with <c>bar.snk</c></para>
    ///   <code>
    ///     <![CDATA[
    /// <delay-sign keyfile="bar.snk" quiet='false'>
    ///   <targets>
    ///     <includes name="foo.dll"/>
    ///   </targets>
    /// </delay-sign>
    ///     ]]>
    ///   </code>
    ///   <para>The parameter keycontainer can also be used.</para>
    /// </example>
    [TaskName("delay-sign")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class DelaySignTask : ExternalProgramBase {
        #region Private Instance Fields

        private FileSet _targets        = new FileSet();
        private string  _keyFilename    = null;
        private string  _keyContainer   = null;
        private string  _arguments      = null;
        private bool    _quiet          = true;
        private string _exeName         = "sn"; 
        
        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Takes a list of assemblies/executables to sign.
        /// </summary>
        [FileSet("targets")]
        public FileSet Targets {
            get { return _targets; }
            set { _targets = value; }
        }
        
        /// <summary>
        /// Specifies the filesystem path to the signing key.
        /// </summary>
        [TaskAttribute("keyfile")]
        public string KeyFile {
            get { return (_keyFilename != null) ? Project.GetFullPath(_keyFilename) : null; }
            set { _keyFilename = SetStringValue(value); }
        }

        /// <summary>
        /// Specifies the filesystem path to the signing key
        /// </summary>
        [TaskAttribute("keycontainer")]
        public string KeyContainer {
            get { return _keyContainer; }
            set { _keyContainer = SetStringValue(value); }
        }

        /// <summary>
        /// Specifies whether non-error output should be suppressed. Default
        /// is <c>true</c>.
        /// </summary>
        [TaskAttribute("quiet")]
        public bool Quiet {
            get { return _quiet; }
            set { _quiet = value; }
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
            get { return _arguments; }
        }

        /// <summary>
        /// Override the ExeName paramater for sn.exe
        /// </summary>
        [FrameworkConfigurable("exename")]
        public override string ExeName {
            get { return _exeName; }
            set { _exeName = SetStringValue(value); }
        }

        /// <summary>
        /// Converts a single file or group of files.
        /// </summary>
        protected override void ExecuteTask() {
            bool keyAvail = KeyFile != null;
            bool containerAvail = KeyContainer != null;
            string keyname = containerAvail ? KeyContainer : KeyFile;

            if ((keyAvail && containerAvail) || (! keyAvail && ! containerAvail)) {
                throw new BuildException("Either 'keyfile' or 'keycontainer' must be specified.");
            }

            foreach (string filename in Targets.FileNames) {
                // Try to guess the buffer length
                // Add 12 for "-R", maybe 'c' and "-q", and spaces/ quotes.
                StringBuilder arguments = new StringBuilder (9 + filename.Length + keyname.Length);

                if (Quiet) {
                    arguments.Append("-q ");
                }

                // indicate that we want to resign a previously signed or delay-signed assembly
                arguments.Append("-R");

                if (containerAvail) {
                    arguments.Append('c');
                }

                arguments.Append(" \"").Append(filename).Append("\" \"");
                arguments.Append(keyname).Append('\"');
                _arguments = arguments.ToString();

                // call base class to do perform the actual call
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase
    }
}