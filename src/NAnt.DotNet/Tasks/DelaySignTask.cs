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

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.DotNet.Tasks;
using StringBuilder = System.Text.StringBuilder;

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
    public class DelaySignTask : ExternalProgramBase  {
        private FileSet _targets        = new FileSet();
        private string  _keyFilename    = null;
        private string  _keyContainer   = null;
        private string  _arguments      = null;
        private bool    _quiet          = true;
        
        /// <summary>
        /// Takes a list of assemblies/executables to sign
        /// </summary>
        [FileSet("targets")]
        public FileSet Targets {
            get { return _targets; }
            set { _targets = value; }
        }
        
        /// <summary>
        /// Specifies the filesystem path to the signing key
        /// </summary>
        [TaskAttribute("keyfile")]
        public string KeyFile {
            get { return _keyFilename;  }
            set { _keyFilename = value; }
        }

        /// <summary>
        /// Specifies the filesystem path to the signing key
        /// </summary>
        [TaskAttribute("keycontainer")]
        public string KeyContainer {
            get { return _keyContainer;  }
            set { _keyContainer = value; }
        }

        /// <summary>
        /// Specifies whether non-error output should be suppressed.
        /// </summary>
        [TaskAttribute("quiet")]
        public bool Quiet {
            get { return _quiet;  }
            set { _quiet = value; }
        }

        /// <summary>
        /// Gets the command line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command line arguments for the external program.
        /// </value>
        public override string ProgramArguments  { 
            get { return _arguments; } 
        }

        /// <summary>
        /// Override the ExeName paramater for sn.exe
        /// </summary>       
        public override string ExeName {
            get { return "sn"; }
        }
        /// <summary>
        /// Converts a single file or group of files.
        /// </summary>
        protected override void ExecuteTask() {
            bool keyAvail =
                (_keyFilename != null && _keyFilename.Length > 0);
            bool containerAvail = 
                (_keyContainer != null && _keyContainer.Length > 0);
            if ((keyAvail && containerAvail) ||
                (!keyAvail && !containerAvail)) {
                throw new BuildException
                    ("either 'keyfile' or 'keycontainer' must be specified");
            }

            string keyname =  (containerAvail ? _keyContainer : _keyFilename);

            foreach (string filename in Targets.FileNames)  {
                // Try to guess the buffer length
                // Add 12 for "-R", maybe 'c' and "-q", and spaces/ quotes.
                StringBuilder arguments = new StringBuilder (9 + filename.Length + keyname.Length);
                if (Quiet) {
                    arguments.Append("-q ");
                }
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
    }
}