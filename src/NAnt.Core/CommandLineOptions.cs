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
using System.Collections.Specialized;
using System.IO;

namespace SourceForge.NAnt {
    /// <summary>
    /// Represents the set of command-line options supported by NAnt.
    /// </summary>
    public class CommandLineOptions {
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the buildfile that should be executed.
        /// </summary>
        /// <value>The buildfile that should be executed.</value>
        /// <remarks>
        /// Can be both a file or an URI.
        /// </remarks>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name="buildfile", ShortName="f",  Description="use given buildfile")]
        public string BuildFile {
            get { return _buildFile; }
            set { _buildFile = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether more information should be
        /// displayed during the build process.
        /// </summary>
        /// <value>
        /// <c>true</c> if more information should be displayed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "verbose", ShortName="v", Description = "displays more information during build process")]
        public bool Verbose {
            get { return _verbose; }
            set { _verbose = value; }
        }

        /// <summary>
        /// Gets a value indicating whether parent directories should be searched
        /// for a buildfile.
        /// </summary>
        /// <value>
        /// <c>true</c> if parent directories should be searched for a build file;
        /// otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "find", Description = "search parent directories for buildfile")]
        public bool FindInParent {
            get { return _findInParent; }
            set { _findInParent = value; }
        }

        /// <summary>
        /// Gets or sets the number of characters that build output should be
        /// indented.
        /// </summary>
        /// <value>
        /// The number of characters that the build output should be indented.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "indent", Description = "number of characters to indent build output")]
        public int Indent {
            get { return _indent; }
            set { _indent = value; }
        }

        /// <summary>
        /// Gets or sets the list of properties that should be set.
        /// </summary>
        /// <value>
        /// The list of properties that should be set.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.MultipleUnique, Name = "D", Description = "use value for given property")]
        public StringCollection Properties {
            get { return _properties; }
        }

        /// <summary>
        /// Gets or sets the <see cref="Type" /> of logger to add to the list
        /// of listeners.
        /// </summary>
        /// <value>
        /// The <see cref="Type" /> of logger to add to the list of
        /// listeners.
        /// </value>
        /// <remarks>
        /// The <see cref="LoggerType" /> should derive from <see cref="LogListener" />.
        /// </remarks>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name="logger", Description="use given type as logger")]
        public string LoggerType {
            get { return _loggerType; }
            set { _loggerType = value; }
        }

        /// <summary>
        /// Gets or sets the name of the file to log output to.
        /// </summary>
        /// <value>
        /// The name of the file to log output to.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name="logfile", ShortName="l", Description="use value as name of log output file")]
        public FileInfo LogFile {
            get { return _logFile; }
            set { _logFile = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether project help should be
        /// printed.
        /// </summary>
        /// <value>
        /// <c>true</c> if project help should be printed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "projecthelp", Description = "prints project help information")]
        public bool ShowProjectHelp {
            get { return _showProjectHelp; }
            set { _showProjectHelp = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the logo banner should be
        /// printed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the logo banner should be printed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "nologo", Description = "surpresses display of the logo banner")]
        public bool NoLogo {
            get { return _noLogo; }
            set { _noLogo = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the NAnt help should be
        /// printed.
        /// </summary>
        /// <value>
        /// <c>true</c> if NAnt help should be printed; otherwise, <c>false</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.Exclusive, Name = "help", ShortName = "h", Description = "prints this message")]
        public bool ShowHelp {
            get { return _showHelp; }
            set { _showHelp = value; }
        }

        /// <summary>
        /// Gets or sets a list of targets that should be executed.
        /// </summary>
        /// <value>
        /// The list of targets that should be executed.
        /// </value>
        [DefaultCommandLineArgument(CommandLineArgumentTypes.MultipleUnique, Name="target")]
        public StringCollection Targets {
            get { return _targets; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _buildFile;
        private bool _noLogo;
        private bool _showHelp;
        private bool _verbose;
        private int _indent;
        private bool _findInParent;
        private StringCollection _properties = new StringCollection();
        private string _loggerType;
        private FileInfo _logFile;
        private StringCollection _targets = new StringCollection();
        private bool _showProjectHelp;

        #endregion Private Instance Fields
    }
}
