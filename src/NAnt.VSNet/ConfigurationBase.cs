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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public abstract class ConfigurationBase {
        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBase" /> 
        /// class with the given <see cref="ProjectBase" />.
        /// </summary>
        /// <param name="project">The project of the configuration.</param>
        protected ConfigurationBase(ProjectBase project) {
            if (project == null) {
                throw new ArgumentNullException("project");
            }

            _project = project;
            _extraOutputFiles = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets the project.
        /// </summary>
        public ProjectBase Project {
            get { return _project;}
        }

        /// <summary>
        /// Gets the name of the configuration.
        /// </summary>
        public abstract string Name {
            get;
        }

        /// <summary>
        /// Get the directory in which intermediate build output will be stored 
        /// for this configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a directory relative to the project directory named 
        /// <c>obj\&lt;configuration name&gt;</c>.
        /// </para>
        /// <para>
        /// <c>.resx</c> and <c>.licx</c> files will only be recompiled if the
        /// compiled resource files in the <see cref="ObjectDir" /> are not 
        /// uptodate.
        /// </para>
        /// </remarks>
        public virtual DirectoryInfo ObjectDir {
            get { 
                return new DirectoryInfo(FileUtils.CombinePaths(Project.ObjectDir.FullName, 
                    Name));
            }
        }

        /// <summary>
        /// Gets the output directory.
        /// </summary>
        public abstract DirectoryInfo OutputDir {
            get;
        }

        /// <summary>
        /// Gets the path for the output file.
        /// </summary>
        public abstract string OutputPath {
            get;
        }


        /// <summary>
        /// Gets the path in which the output file will be created before its
        /// copied to the actual output path.
        /// </summary>
        public abstract string BuildPath {
            get;
        }

        /// <summary>
        /// Get the path of the output directory relative to the project
        /// directory.
        /// </summary>
        public abstract string RelativeOutputDir {
            get;
        }

        /// <summary>
        /// Gets the platform that the configuration targets.
        /// </summary>
        /// <value>
        /// The platform targeted by the configuration.
        /// </value>
        public abstract string PlatformName {
            get;
        }

        /// <summary>
        /// Gets the set of output files that is specific to the project
        /// configuration.
        /// </summary>
        /// <value>
        /// The set of output files that is specific to the project
        /// configuration.
        /// </value>
        /// <remarks>
        /// The key of the case-insensitive <see cref="Hashtable" /> is the 
        /// full path of the output file and the value is the path relative to
        /// the output directory.
        /// </remarks>
        public Hashtable ExtraOutputFiles {
            get { return _extraOutputFiles; }
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        protected SolutionTask SolutionTask {
            get { return Project.SolutionTask; }
        }

        #endregion Protected Instance Properties

        #region Public Instance Methods

        public string ExpandMacros(string s) {
            if (s == null) {
                return s;
            }

            return _rxMacro.Replace(s, new MatchEvaluator(EvaluateMacro));
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>
        /// Expands the given macro.
        /// </summary>
        /// <param name="macro">The macro to expand.</param>
        /// <returns>
        /// The expanded macro.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The macro is not supported.</para>
        ///   <para>-or-</para>
        ///   <para>The macro is not implemented.</para>
        ///   <para>-or-</para>
        ///   <para>The macro cannot be expanded.</para>
        /// </exception>
        /// <exception cref="NotImplementedException">
        ///   <para>Expansion of a given macro is not yet implemented.</para>
        /// </exception>
        protected internal virtual string ExpandMacro(string macro) {
            // perform case-insensitive expansion of macros 
            switch (macro.ToLower(CultureInfo.InvariantCulture)) {
                case "outdir": // E.g. bin\Debug\
                    return RelativeOutputDir;
                case "configurationname": // E.g. Debug
                    return Name;
                case "targetname": // E.g. WindowsApplication1
                    return Path.GetFileNameWithoutExtension(Path.GetFileName(
                        OutputPath));
                case "targetpath": // E.g. C:\Doc...\Visual Studio Projects\WindowsApplications1\bin\Debug\WindowsApplications1.exe
                    return OutputPath;
                case "targetext": // E.g. .exe
                    return Path.GetExtension(OutputPath);
                case "targetfilename": // E.g. WindowsApplications1.exe
                    return Path.GetFileName(OutputPath);
                case "targetdir": // Absolute path to OutDir
                    return OutputDir.FullName + (OutputDir.FullName.EndsWith(
                        Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) 
                        ? string.Empty : Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture));
                case "platformname": // .NET, does this value ever change?
                    return PlatformName;
            }

            // expand using solution level macro's
            string expandedMacro = Project.SolutionTask.ExpandMacro(macro);
            if (expandedMacro != null) {
                return expandedMacro;
            }

            // expand using project level macro's
            expandedMacro = Project.ExpandMacro(macro);
            if (expandedMacro != null) {
                return expandedMacro;
            }

            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Macro \"{0}\" is not supported.", macro), Location.UnknownLocation);
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Is called each time a regular expression match is found during a 
        /// <see cref="M:Regex.Replace(string, MatchEvaluator)" /> operation.
        /// </summary>
        /// <param name="m">The <see cref="Match" /> resulting from a single regular expression match during a <see cref="M:Regex.Replace(string, MatchEvaluator)" />.</param>
        /// <returns>
        /// The expanded <see cref="Match" />.
        /// </returns>
        private string EvaluateMacro(Match m) {
            return ExpandMacro(m.Groups[1].Value);
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private readonly ProjectBase _project;
        private readonly Regex _rxMacro = new Regex(@"\$\((\w+)\)");
        private Hashtable _extraOutputFiles;

        #endregion Private Instance Fields
    }
}
