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

// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections.Specialized;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Provides the abstract base class for a Microsoft .Net Framework compiler task.</summary>
    public abstract class MsftFXCompilerBase : CompilerBase {
        public override string ProgramFileName  {
            get {
                return determineFilePath();
            } 
        }
        /// <summary>
        /// Instead of relying on the .NET external program to be in the user's path, point
        /// to the compiler directly since it lives in the .NET Framework's bin directory.
        /// <note>If the file path returned does not exist then there is some issue with the users framework setup</note>
        /// </summary>       
        /// <returns>A fully qualifies pathname including the program name.</returns>
        private string determineFilePath() {
            if (Project.CurrentFramework != null ) {
                string FrameworkDir = "";           
                FrameworkDir = Project.CurrentFramework.FrameworkDirectory.FullName;
                              
                return Path.Combine(FrameworkDir, ExeName +  ".exe" );               
            }
            else {
                return ExeName;
            }                         
        }      
    }
}
