// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Scott Hernandez (ScottHernandez@hotmail.com)

using System.Globalization;
using System.IO;

using NAnt.Core;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Provides the abstract base class for an external SDK program task.
    /// </summary>
    public abstract class SdkExternalProgramBase : NAnt.Core.Tasks.ExternalProgramBase {
        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the filename of the external program to start.
        /// </summary>
        /// <value>The filename of the external program.</value>
        public override string ProgramFileName {
            get { return DetermineFilePath(); }
        }

        #endregion Override implementation of ExternalProgramBase

        #region Private Instance Methods

        /// <summary>
        /// Instead of relying on the .NET external program to be in the user's path, point
        /// to the compiler directly since it lives in the .NET Framework's bin directory.
        /// </summary>       
        /// <returns>A fully qualifies pathname including the program name.</returns>
        /// <exception cref="BuildException">The task is not available or not configured for the current framework.</exception>
        private string DetermineFilePath() {
            if (Project.CurrentFramework != null) {
                if (ExeName != null) {
                    if (Project.CurrentFramework.SdkDirectory != null) {
                        string SdkDirectory = Project.CurrentFramework.SdkDirectory.FullName;
                        return Path.Combine(SdkDirectory, ExeName +  ".exe" );
                    } else {
                        throw new BuildException(
                            string.Format(CultureInfo.InvariantCulture, 
                            "The SDK for the ({0} framework is not available or not configured.", 
                            Project.CurrentFramework.Name));
                    }
                } else {
                    throw new BuildException(
                        string.Format(CultureInfo.InvariantCulture, 
                        "The {0} task is not available or not configured for the {1} framework.", 
                        Name, Project.CurrentFramework.Name));
                }
            } else {
                return ExeName;
            }
        }

        #endregion Private Instance Methods
    }
}
