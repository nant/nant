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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET solutions.
    /// </summary>
    public sealed class SolutionFactory {
        #region Private Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFactory" />
        /// class.
        /// </summary>
        private SolutionFactory() {
        }

        #endregion Private Instance Constructor

        #region Public Static Methods

        public static SolutionBase LoadSolution(SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver) {
            if (solutionTask.SolutionFile == null) {
                return new GenericSolution(solutionTask, tfc, gacCache, refResolver);
            } else {
                // determine the solution file format version

                // will hold the content of the solution file
                string fileContents;

                using (StreamReader sr = new StreamReader(solutionTask.SolutionFile.FullName, Encoding.Default, true)) {
                    fileContents = sr.ReadToEnd();
                }

                Regex reSolutionFormat = new Regex(@"^\s*Microsoft Visual Studio Solution File, Format Version\s+(?<formatVersion>[0-9]+\.[0-9]+?)", RegexOptions.Singleline);
                MatchCollection matches = reSolutionFormat.Matches(fileContents);

                if (matches.Count == 0) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "The format version of solution file '{0}' could not"
                        + " be determined.", solutionTask.SolutionFile.FullName), 
                        Location.UnknownLocation);
                } else {
                    string formatVersion = matches[0].Groups["formatVersion"].Value;
                    switch (formatVersion) {
                        case "7.0":
                            return new Rainier.Solution(fileContents, solutionTask, 
                                tfc, gacCache, refResolver);
                        case "8.0":
                            return new Everett.Solution(fileContents, solutionTask, 
                                tfc, gacCache, refResolver);
                        case "9.0":
                            throw new BuildException("Microsoft Visual Studio.NET"
                                + " 2005 solutions are not supported.", 
                                Location.UnknownLocation);
                        default:
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                                "Visual Studio Solution format version '{0}' is"
                                + " not supported.", formatVersion), Location.UnknownLocation);
                    }
                }
            }
        }

        #endregion Public Static Methods
    }
}
