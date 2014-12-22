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
//
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Signs delay-signed .NET Assemblies, or re-signs existing assemblies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The delay-signing mechanism takes a fileset (named targets)
    /// and either a <see cref="KeyFile" /> attribute for a file containing the
    /// public and private keys, or <see cref="KeyContainer" /> to name a key 
    /// container.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>Sign partially-signed <c>foo.dll</c> with <c>bar.snk</c>.</para>
    ///   <code>
    ///     <![CDATA[
    /// <delay-sign keyfile="bar.snk" verbose="false">
    ///     <targets>
    ///         <include name="foo.dll" />
    ///     </targets>
    /// </delay-sign>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("delay-sign")]
    [ProgramLocation(LocationType.FrameworkSdkDir)]
    public class DelaySignTask : ExternalProgramBase {
        #region Private Instance Fields

        private FileSet _targets = new FileSet();
        private FileInfo _keyFile;
        private string _keyContainer;
        private StringBuilder _argumentBuilder;
        
        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// List of assemblies/executables to sign.
        /// </summary>
        [BuildElement("targets")]
        public FileSet Targets {
            get { return _targets; }
            set { _targets = value; }
        }
        
        /// <summary>
        /// Specifies the filesystem path to the signing key.
        /// </summary>
        [TaskAttribute("keyfile")]
        public FileInfo KeyFile {
            get { return _keyFile; }
            set { _keyFile = value; }
        }

        /// <summary>
        /// Specifies the key container.
        /// </summary>
        [TaskAttribute("keycontainer")]
        public string KeyContainer {
            get { return _keyContainer; }
            set { _keyContainer = StringUtils.ConvertEmptyToNull(value); }
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
        /// Converts a single file or group of files.
        /// </summary>
        protected override void ExecuteTask() {
            bool keyAvail = KeyFile != null;
            bool containerAvail = KeyContainer != null;
            string keyname = containerAvail ? KeyContainer : KeyFile.FullName;

            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Targets.BaseDirectory == null) {
                Targets.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            if ((keyAvail && containerAvail) || (! keyAvail && ! containerAvail)) {
                throw new BuildException(ResourceUtils.GetString("NA2012"), Location);
            }

            Log(Level.Info, ResourceUtils.GetString("String_DelaySigningAssemblies"),
                Targets.FileNames.Count);

            foreach (string filename in Targets.FileNames) {
                // Try to guess the buffer length
                // Add 12 for "-R", maybe 'c' and "-q", and spaces/quotes.
                _argumentBuilder = new StringBuilder(9 + filename.Length + keyname.Length);

                if (!Verbose) {
                    _argumentBuilder.Append("-q ");
                }

                // indicate that we want to resign a previously signed or delay-signed assembly
                _argumentBuilder.Append("-R");

                if (containerAvail) {
                    _argumentBuilder.Append('c');
                }

                _argumentBuilder.Append(" \"").Append(filename).Append("\" \"");
                _argumentBuilder.Append(keyname).Append('\"');

                // call base class to do perform the actual call
                base.ExecuteTask();
            }
        }

        #endregion Override implementation of ExternalProgramBase
    }
}
