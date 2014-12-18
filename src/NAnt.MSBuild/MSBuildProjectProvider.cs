// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System.Xml;
using System.CodeDom.Compiler;
using System.IO;
using NAnt.Core.Util;

using NAnt.VSNet;
using NAnt.VSNet.Extensibility;
using NAnt.VSNet.Tasks;

namespace NAnt.MSBuild {
    internal class MSBuildProjectProvider : IProjectBuildProvider {
        #region IProjectBuildProvider Members

        public int IsSupported(string projectExt, XmlElement xmlDefinition) {
            if (MSBuildProject.IsMSBuildProject(xmlDefinition))
                return 20;
            return 0;
        }

        public ProjectBase GetInstance(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) {
            return new MSBuildProject(solution, projectPath, xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir);
        }

        public string LoadGuid(XmlElement xmlDefinition) {
            return MSBuildProject.LoadGuid(xmlDefinition);
        }

        #endregion
    }
}
