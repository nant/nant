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
using System.CodeDom.Compiler;
using NAnt.Core.Util;

using NAnt.VSNet;

namespace NAnt.MSBuild {
    internal class MSBuildProjectReference : ProjectReferenceBase {
        private readonly ProjectBase _project;
        private readonly MSBuildReferenceHelper _helper;

        public MSBuildProjectReference(
            ReferencesResolver referencesResolver, ProjectBase parent,
            ProjectBase project, bool isPrivateSpecified, bool isPrivate)

            :base(referencesResolver, parent) {
            _helper = new MSBuildReferenceHelper(isPrivateSpecified, isPrivate);
            _project = project;
        }

        public MSBuildProjectReference(
            ReferencesResolver referencesResolver, ProjectBase parent,
            SolutionBase solution, TempFileCollection tfc,
            GacCache gacCache, DirectoryInfo outputDir,
            string pguid, string pname, string rpath, string priv)
            
            : base(referencesResolver, parent) {
            _helper = new MSBuildReferenceHelper(priv, true);
            string projectFile = solution.GetProjectFileFromGuid(pguid);
            _project = LoadProject(solution, tfc, gacCache, outputDir, projectFile);
        }

        protected override bool IsPrivate {
            get { return _helper.IsPrivate; }
        }

        protected override bool IsPrivateSpecified {
            get { return _helper.IsPrivateSpecified; }
        }

        public override ProjectBase Project {
            get { return _project; }
        }

        public override bool IsManaged(Configuration solutionConfiguration) {
            return true;
        }
    }
}