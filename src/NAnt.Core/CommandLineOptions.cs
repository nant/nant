// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.IO;

namespace SourceForge.NAnt {
    /// <summary>
    /// Represents the set of commandline options supported by NAnt.
    /// </summary>
    public class CommandLineOptions {
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the buildfile that should be executed.
        /// </summary>
        /// <value>The buildfile that should be executed.</value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name="buildfile", ShortName="file",  Description="use given buildfile")]
        public FileInfo BuildFile {
            get { return this._buildFile; }
            set { this._buildFile = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether more information should be
        /// displayed during the build process.
        /// </summary>
        /// <value>
        /// <c>true</c> if more information should be displayed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name = "verbose", ShortName="v", Description = "displays more information during build process")]
        public bool Verbose {
            get { return this._verbose; }
            set { this._verbose = value; }
        }

        /// <summary>
        /// Gets a value indicating whether parent directories should be searched
        /// for a buildfile.
        /// </summary>
        /// <value>
        /// <c>true</c> if parent directories should be searched for a build file;
        /// otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name = "find", Description = "search parent directories for buildfile")]
        public bool FindInParent {
            get { return this._findInParent; }
            set { this._findInParent = value; }
        }

        /// <summary>
        /// Gets or sets the number of characters that build output should be 
        /// indented.
        /// </summary>
        /// <value>
        /// The number of characters that build output should be indented.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name = "indent", Description = "number of characters to indent build output")]
        public int Indent {
            get { return this._indent; }
            set { this._indent = value; }
        }

        /// <summary>
        /// Gets or sets the list of properties that should be set.
        /// </summary>
        /// <value>
        /// The list of properties that should be set.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.MultipleUnique, Name = "D", Description = "use value for given property")]
        public string[] Properties {
            get { return this._properties; }
            set { this._properties = value; }
        }

        /// <summary>
        /// Gets the <see cref="Type" /> of logger to add to the list
        /// of listeners.
        /// </summary>
        /// <value>
        /// The <see cref="Type" /> of logger to add to the list of 
        /// listeners.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name="logger", Description="use given type as logger")]
        public string LoggerType {
            get { return this._loggerType; }
            set { this._loggerType = value; }
        }

        /// <summary>
        /// Gets or sets the name of the file to log output to.
        /// </summary>
        /// <value>
        /// The name of the file to log output to.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name="logfile", ShortName="l", Description="use value as name of log output file")]
        public FileInfo LogFile {
            get { return this._logFile; }
            set { this._logFile = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether project help should be
        /// printed.
        /// </summary>
        /// <value>
        /// <c>true</c> if project help should be printed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name = "projecthelp", Description = "prints project help information")]
        public bool ShowProjectHelp {
            get { return this._showProjectHelp; }
            set { this._showProjectHelp = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the logo banner should be
        /// printed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the logo banner should be printed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.AtMostOnce, Name = "nologo", Description = "surpresses display of the logo banner")]
        public bool NoLogo {
            get { return this._noLogo; }
            set { this._noLogo = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the NAnt help should be
        /// printed.
        /// </summary>
        /// <value>
        /// <c>true</c> if NAnt help should be printed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentType.Exclusive, Name = "help", ShortName = "h", Description = "prints this message")]
        public bool ShowHelp {
            get { return this._showHelp; }
            set { this._showHelp = value; }
        }

        /// <summary>
        /// Gets or sets a list of targets that should be executed.
        /// printed.
        /// </summary>
        /// <value>
        /// The list of targets that should be executed.
        /// </value>
        [DefaultCommandLineArgument(CommandLineArgumentType.MultipleUnique, Name="target")]
        public string[] Targets {
            get { return this._targets; }
            set { this._targets = value; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private FileInfo _buildFile;
        private bool _noLogo;
        private bool _showHelp;
        private bool _verbose;
        private int _indent;
        private bool _findInParent;
        private string[] _properties;
        private string _loggerType;
        private FileInfo _logFile;
        private string[] _targets;
        private bool _showProjectHelp;

        #endregion Private Instance Fields
    }
}
