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
// Dmitry Jemerov <yole@yole.ru>

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET projects.
    /// </summary>
    public sealed class ProjectFactory {
        #region Public Static Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFactory" />
        /// class.
        /// </summary>
        private ProjectFactory() {
        }

        static ProjectFactory() {
            ClearCache();
        }

        #endregion Public Static Methods

        #region Public Static Methods

        public static void ClearCache() {
            _cachedProjects = new Hashtable();
            _cachedProjectGuids = new Hashtable();
        }

        public static ProjectBase LoadProject(Solution sln, SolutionTask slnTask, TempFileCollection tfc, string outputDir, string path) {
            string projectName = Path.GetFullPath(path).ToLower(CultureInfo.InvariantCulture);
            string projectExt = Path.GetExtension(projectName);
            
            // Is this a new project?
            if (!_cachedProjects.Contains(projectName)) {
                if (projectExt == ".vbproj" || projectExt == ".csproj") {
                    Project p = new Project(slnTask, tfc, outputDir);
                    p.Load(sln, path);
                    _cachedProjects[projectName] = p;
                } else if (projectExt == ".vcproj") {
                    VcProject p = new VcProject(slnTask, tfc, outputDir);
                    p.Load(sln, path);
                    _cachedProjects[projectName] = p;
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Unknown project file extension {0}.", projectExt),
                        Location.UnknownLocation);
                }
            }

            return (ProjectBase)_cachedProjects[projectName];
        }

        public static bool IsSupportedProjectType(string path) {
            string projectExt = Path.GetExtension(path).ToLower(CultureInfo.InvariantCulture);
            return projectExt == ".vbproj" || projectExt == ".csproj" || projectExt == ".vcproj";
        }

        public static string LoadGuid(string fileName, TempFileCollection tfc) {
            string projectName = Path.GetFullPath(fileName).ToLower(CultureInfo.InvariantCulture);
            string projectExt = Path.GetExtension(projectName);

            // Is this a new project?
            if (!_cachedProjectGuids.Contains(projectName)) {
                if (projectExt == ".vbproj" || projectExt == ".csproj") {
                    _cachedProjectGuids[projectName] = Project.LoadGuid(fileName, tfc);
                } else if (projectExt == ".vcproj") {
                    _cachedProjectGuids[projectName] = VcProject.LoadGuid(fileName);
                } else
                    throw new BuildException("Unknown project file extension " + projectExt);
            }

            return (string)_cachedProjectGuids[projectName];
        }

        #endregion Public Static Methods

        #region Private Static Fields
        private static Hashtable _cachedProjects;
        private static Hashtable _cachedProjectGuids;
        #endregion Private Static Fields
    }
}
