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

        #endregion Public Static Methods

        #region Public Static Methods

        public static ProjectBase LoadProject(Solution sln, SolutionTask slnTask, TempFileCollection tfc, string outputDir, string path) {
            string projectExt = Path.GetExtension(path).ToLower(CultureInfo.InvariantCulture);
            if (projectExt == ".vbproj" || projectExt == ".csproj") {
                Project p = new Project(slnTask, tfc, outputDir);
                p.Load(sln, path);
                return p;
            }
            if (projectExt == ".vcproj") {
                VcProject p = new VcProject(slnTask, tfc, outputDir);
                p.Load(sln, path);
                return p;
            }
            throw new BuildException("Unknown project file extension " + projectExt);
        }

        public static bool IsSupportedProjectType(string path) {
            string projectExt = Path.GetExtension(path).ToLower(CultureInfo.InvariantCulture);
            return projectExt == ".vbproj" || projectExt == ".csproj" || projectExt == ".vcproj";
        }

        public static string LoadGuid(string fileName, TempFileCollection tfc) {
            string projectExt = Path.GetExtension(fileName).ToLower(CultureInfo.InvariantCulture);
            if (projectExt == ".vbproj" || projectExt == ".csproj") {
                return Project.LoadGuid(fileName, tfc);
            }
            if (projectExt == ".vcproj") {
                return VcProject.LoadGuid(fileName);
            }
            throw new BuildException("Unknown project file extension " + projectExt);
        }

        #endregion Public Static Methods
    }
}
