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
using System.Collections.Specialized;
using System.Globalization;
using System.IO;

using NAnt.Core;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET projects.
    /// </summary>
    public sealed class ProjectFactory {
        #region Private Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectFactory" />
        /// class.
        /// </summary>
        private ProjectFactory() {
        }

        #endregion Private Instance Constructor

        #region Static Constructor

        static ProjectFactory() {
            ClearCache();
        }

        #endregion Static Constructor

        #region Public Static Methods

        public static void ClearCache() {
            _cachedProjects = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _cachedProjectGuids = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        public static ProjectBase LoadProject(Solution sln, SolutionTask slnTask, TempFileCollection tfc, DirectoryInfo outputDir, string path) {
            string projectFileName = ProjectFactory.GetProjectFileName(path);
            string projectExt = Path.GetExtension(projectFileName).ToLower(
                CultureInfo.InvariantCulture);
            
            // check if this a new project?
            if (!_cachedProjects.Contains(projectFileName)) {
                if (projectExt == ".vbproj" || projectExt == ".csproj") {
                    Project p = new Project(slnTask, tfc, outputDir);
                    p.Load(sln, path);
                    _cachedProjects[projectFileName] = p;
                } else if (projectExt == ".vcproj") {
                    VcProject p = new VcProject(slnTask, tfc, outputDir);
                    p.Load(sln, path);
                    _cachedProjects[projectFileName] = p;
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Unknown project file extension '{0}'.", projectExt),
                        Location.UnknownLocation);
                }
            }

            return (ProjectBase) _cachedProjects[projectFileName];
        }

        public static bool IsUrl(string fileName) {
            if (fileName.StartsWith(Uri.UriSchemeFile) || fileName.StartsWith(Uri.UriSchemeHttp) || fileName.StartsWith(Uri.UriSchemeHttps)) {
                return true;
            }

            return false;
        }

        public static string GetProjectFileName(string fileName) {
            string projectPath = null;

            if (ProjectFactory.IsUrl(fileName)) {
                // construct uri for project path
                Uri projectUri = new Uri(fileName);

                // get last segment of the uri (which should be the 
                // project file itself)
                projectPath = projectUri.LocalPath;
            } else {
                projectPath = fileName;
            }

            // return filename part
            return Path.GetFileName(projectPath); 
        }

        public static bool IsSupportedProjectType(string path) {
            string projectFileName = ProjectFactory.GetProjectFileName(path);
            string projectExt = Path.GetExtension(projectFileName).ToLower(
                CultureInfo.InvariantCulture);
            return projectExt == ".vbproj" || projectExt == ".csproj" || projectExt == ".vcproj";
        }

        public static string LoadGuid(string fileName, TempFileCollection tfc) {
            string projectFileName = ProjectFactory.GetProjectFileName(fileName);
            string projectExt = Path.GetExtension(projectFileName).ToLower(
                CultureInfo.InvariantCulture);

            // check if this a new project?
            if (!_cachedProjectGuids.Contains(projectFileName)) {
                if (projectExt == ".vbproj" || projectExt == ".csproj") {
                    _cachedProjectGuids[projectFileName] = Project.LoadGuid(fileName, tfc);
                } else if (projectExt == ".vcproj") {
                    _cachedProjectGuids[projectFileName] = VcProject.LoadGuid(fileName);
                } else
                    throw new BuildException("Unknown project file extension " + projectExt);
            }

            return (string) _cachedProjectGuids[projectFileName];
        }

        #endregion Public Static Methods

        #region Private Static Fields

        private static Hashtable _cachedProjects;
        private static Hashtable _cachedProjectGuids;

        #endregion Private Static Fields
    }
}
