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

using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Represents the set of command-line options supported by NAnt.
    /// </summary>
    public class CommandLineOptions {
        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the default framework to use (overrides 
        /// NAnt.exe.config settings)
        /// </summary>
        /// <value>
        /// The framework that should be used.
        /// </value>
        /// <remarks>
        /// For a list of possible frameworks, see NAnt.exe.config, possible
        /// values include "net-1.0", "net-1.1", etc.
        /// </remarks>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name="defaultframework", ShortName="k",  Description="use given framework as default")]
        public string DefaultFramework {
            get { return _defaultFramework; }
            set { _defaultFramework = value; }
        }

        /// <summary>
        /// Gets or sets the buildfile that should be executed.
        /// </summary>
        /// <value>
        /// The buildfile that should be executed.
        /// </value>
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
        /// <see langword="true" /> if more information should be displayed; 
        /// otherwise, <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "verbose", ShortName="v", Description = "displays more information during build process")]
        public bool Verbose {
            get { return _verbose; }
            set { _verbose = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether debug information should be
        /// displayed during the build process.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if debug information should be displayed; 
        /// otherwise, <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "debug", Description = "displays debug information during build process")]
        public bool Debug {
            get { return _debug; }
            set { _debug = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether expression evaluator should be disabled
        /// </summary>
        /// <value>
        /// <see langword="true" /> if expression evaluator should be disabled
        /// otherwise, <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "disable-ee", Description = "disables expression evaluator")]
        public bool DisableExpressionEvaluator {
            get { return _disableEE; }
            set { _disableEE = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether only error and debug debug messages should be
        /// displayed during the build process.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if only error or warning messages should be 
        /// displayed; otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "quiet", ShortName="q", Description = "displays only error or warning messages during build process")]
        public bool Quiet {
            get { return _quiet; }
            set { _quiet = value; }
        }

        /// <summary>
        /// Gets a value indicating whether parent directories should be searched
        /// for a buildfile.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if parent directories should be searched for 
        /// a build file; otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "find", Description = "search parent directories for buildfile")]
        public bool FindInParent {
            get { return _findInParent; }
            set { _findInParent = value; }
        }

        /// <summary>
        /// Gets or sets the indentation level of the build output.
        /// </summary>
        /// <value>
        /// The indentation level of the build output. The default is <c>0</c>.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.AtMostOnce, Name = "indent", Description = "indentation level of build output")]
        public int IndentationLevel {
            get { return _indentationLevel; }
            set { _indentationLevel = value; }
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
        /// The <see cref="LoggerType" /> should derive from <see cref="IBuildLogger" />.
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
        /// Gets a collection containing fully qualified type names of classes 
        /// implementating <see cref="IBuildListener" /> that should be added 
        /// to the project as listeners.
        /// </summary>
        /// <value>
        /// A collection of fully qualified type names that should be added as 
        /// listeners to the <see cref="Project" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.MultipleUnique, Name="listener", Description="add an instance of class as a project listener")]
        public StringCollection Listeners {
            get { return _listeners; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="Project" /> help 
        /// should be printed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if <see cref="Project" /> help should be 
        /// printed; otherwise, <see langword="false" />. The default is
        /// <see langword="false" />.
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
        /// <see langword="true" /> if the logo banner should be printed; otherwise, 
        /// <see langword="false" />. The default is <see langword="false" />.
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
        /// <see langword="true" /> if NAnt help should be printed; otherwise, 
        /// <see langword="false" />. The default is <see langword="false" />.
        /// </value>
        [CommandLineArgument(CommandLineArgumentTypes.Exclusive, Name = "help", ShortName = "h", Description = "prints this message")]
        public bool ShowHelp {
            get { return _showHelp; }
            set { _showHelp = value; }
        }

        /// <summary>
        /// Gets a collection containing the targets that should be executed.
        /// </summary>
        /// <value>
        /// A collection that contains the targets that should be executed.
        /// </value>
        [DefaultCommandLineArgument(CommandLineArgumentTypes.MultipleUnique, Name="target")]
        public StringCollection Targets {
            get { return _targets; }
        }

        #endregion Public Instance Properties

        #region Private Instance Fields

        private string _defaultFramework;
        private string _buildFile;
        private bool _noLogo;
        private bool _showHelp;
        private bool _quiet;
        private bool _verbose;
        private bool _debug;
        private bool _disableEE;
        private int _indentationLevel = 0;
        private bool _findInParent;
        private StringCollection _properties = new StringCollection();
        private string _loggerType;
        private FileInfo _logFile;
        private StringCollection _listeners = new StringCollection();
        private StringCollection _targets = new StringCollection();
        private bool _showProjectHelp;

        #endregion Private Instance Fields
    }
}
