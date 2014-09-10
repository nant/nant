// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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

using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.MSBuild.Functions {
    /// <summary>
    /// Functions to return information for MSBuild system.
    /// </summary>
    [FunctionSet("msbuild", "MSBuild")]
    public class MSBuildFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <exclude/>
        public MSBuildFunctions(Project project, PropertyDictionary properties) : base(project, properties) {}

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Test whether project is VS2005 project and could be built using &lt;msbuild&gt;
        /// </summary>
        /// <param name="project">The name or path of the project file (csproj, vbproj, ...).</param>
        /// <returns>
        /// True, if it is msbuild project, False otherwise.
        /// </returns>
        [Function("is-msbuild-project")]
        public bool IsMsbuildProject(string project) {
            using(StreamReader str = new StreamReader(File.Open(project,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))) {
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(str);
                string ns = doc.NameTable.Get("http://schemas.microsoft.com/developer/msbuild/2003");
                return ns != null;
            }
        }

        #endregion Public Static Methods
    }
}
