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
// Matthew Mastracci (matt@aclaro.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System.CodeDom.Compiler;
using NAnt.Core.Util;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Analyses Microsoft Visual Studio .NET 2008 (Orcas) solution files.
    /// </summary>
    internal class OrcasSolution : WhidbeySolution{
        public OrcasSolution(string solutionContent, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver)
            : base(solutionContent, solutionTask, tfc, gacCache, refResolver)
        {
        }
    }
}
