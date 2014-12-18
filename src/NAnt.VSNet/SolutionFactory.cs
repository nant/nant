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
// Gert Driesen (drieseng@users.sourceforge.net)

using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Extensibility;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET solutions.
    /// </summary>
    internal sealed class SolutionFactory {
        #region Private Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFactory" />
        /// class.
        /// </summary>
        private SolutionFactory() {
        }

        #endregion Private Instance Constructor

        #region Internal Static Methods

        internal static SolutionFactory Create () {
            return new SolutionFactory();
        }

        #endregion Internal Static Methods

        #region Public Instance Methods

        public SolutionBase LoadSolution(SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver) {
            if (solutionTask.SolutionFile == null) {
                return new GenericSolution(solutionTask, tfc, gacCache, refResolver);
            } else {
                // determine the solution file format version

                // will hold the content of the solution file
                string fileContents;

                using (StreamReader sr = new StreamReader(solutionTask.SolutionFile.FullName, Encoding.Default, true)) {
                    fileContents = sr.ReadToEnd();
                }

                ISolutionBuildProvider provider = FindProvider(fileContents);
                if (provider != null) {
                    return provider.GetInstance(fileContents, solutionTask, tfc, gacCache, refResolver);
                }
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Solution format of file '{0}' is not supported.", solutionTask.SolutionFile),
                    Location.UnknownLocation);
            }
        }

        public void RegisterProvider(ISolutionBuildProvider provider) {
            _projectProviders.Add(provider);
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private ISolutionBuildProvider FindProvider(string fileContents) {
            int max = 0;
            ISolutionBuildProvider res = null;
            foreach (ISolutionBuildProvider provider in _projectProviders) {
                int pri = provider.IsSupported(fileContents);
                if (pri > max) {
                    max = pri;
                    res = provider;
                }
            }
            return res;
        }

        #endregion Private Instance Methods

        private readonly ArrayList _projectProviders = new ArrayList();
    }
}
