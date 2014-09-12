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

using System.CodeDom.Compiler;
using System.IO;
using System.Xml;
using NAnt.Core.Extensibility;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet.Extensibility {
    public interface IProjectBuildProvider : IPlugin {
        /// <summary>
        /// Returns a number representing how much this file fits this project type.
        /// </summary>
        /// <param name="projectExt"></param>
        /// <param name="xmlDefinition"></param>
        /// <returns></returns>
        /// <remarks>
        /// This enables the override in other providers. Do not return big numbers, mainly when compring only on filename.
        /// </remarks>
        int IsSupported(string projectExt, XmlElement xmlDefinition);
        ProjectBase GetInstance(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir);
        string LoadGuid(XmlElement xmlDefinition);
    }
}
