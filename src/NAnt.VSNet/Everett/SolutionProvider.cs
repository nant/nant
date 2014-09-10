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
using System.Text.RegularExpressions;
using NAnt.Core.Util;

using NAnt.VSNet.Extensibility;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet.Everett {
    internal class SolutionProvider : ISolutionBuildProvider {
        #region Implementation of ISolutionBuildProvider

        public int IsSupported(string fileContents) {
            Regex reSolutionFormat = new Regex(@"^\s*Microsoft Visual Studio Solution File, Format Version\s+(?<formatVersion>[0-9]+\.[0-9]+)", RegexOptions.Singleline);
            MatchCollection matches = reSolutionFormat.Matches(fileContents);

            if (matches.Count == 0)
                return 0;

            string formatVersion = matches[0].Groups["formatVersion"].Value;
            if (formatVersion == "8.00")
                return 10;
            return 0;
        }

        public SolutionBase GetInstance(string solutionContent, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver) {
            return new Solution(solutionContent, solutionTask, tfc, gacCache, refResolver);
        }

        #endregion Implementation of ISolutionBuildProvider
    }
}
