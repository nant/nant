// NAnt - A .NET build tool
// Copyright (C) 2004 Thomas Strauss (strausst@arcor.de)
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
// Thomas Strauss (strausst@arcor.de)

using System;

using NAnt.Core;

using Tests.NAnt.Core;

namespace Tests.NAnt.VisualCpp {
    public abstract class VisualCppTestBase : BuildTestBase {
        #region Protected Static Properties

        /// <summary>
        /// Gets a value indicating whether the VC++ compiler is present in the PATH.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the VC++ compiler is present in the PATH;
        /// otherwise, <see langword="false" />.
        /// </value>
        protected static bool CompilerPresent {
            get { return _compilerPresent; }
        }
	    
        /// <summary>
        /// Gets a value indicating whether the VC++ libs are present in the 
        /// LIB environment variable.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the VC++ libs are present in the LIB
        /// environment variable.
        /// </value>
        protected static bool LibsPresent {
            get { return _libsPresent; }
        }

        /// <summary>
        /// Gets a value indicating whether the VC++ header files are present 
        /// in the INCLUDE environment variable.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the VC++ header files are present in the 
        /// INCLUDE environment variable.
        /// </value>
        protected static bool HeaderFilesPresent {
            get { return _headerFilesPresent; }
        }
	    
        /// <summary>
        /// Combined property which allows to check if you can compile and link.
        /// </summary>
        protected static bool CanCompileAndLink {
            get {
                return (LibsPresent && CompilerPresent && HeaderFilesPresent);
            }
        }

        #endregion Protected Static Methods

        #region Private Static Properties

        private static string[] ExpectedLibs {
            get { return _expectedLibs; }
        }

        private static string[] ExpectedHeaderFiles {
            get { return _expectedHeaderFiles; }
        }

        #endregion Private Static Properties

        #region Private Static Methods
        
        /// <summary>
        /// Routine which checks if the libs are present.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the libs are present; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private static bool CheckLibsPresent() {
            foreach (string lib in ExpectedLibs) {
                PathScanner scanner = new PathScanner();
                scanner.Add(lib);
                if (scanner.Scan("LIB").Count == 0) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Routine which checks if the header files are present.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the header files are present; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private static bool CheckHeaderFilesPresent() {
            foreach (string headerFile in ExpectedHeaderFiles) {
                PathScanner scanner = new PathScanner();
                scanner.Add(headerFile);
                if (scanner.Scan("INCLUDE").Count == 0) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Routine which checks if the compiler is present.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the compiler is present; otherwise,
        /// <see langword="false" />.
        /// </returns>
        private static bool CheckCompilerPresent() {
            PathScanner scanner = new PathScanner();
            scanner.Add("cl.exe");
            return scanner.Scan("PATH").Count > 0;
        }
	
        #endregion Private Static Methods

        #region Private Static Fields

        private static string[] _expectedLibs = new string[] {
            "kernel32.lib",
            "user32.lib",
            "gdi32.lib",
            "winspool.lib",
            "comdlg32.lib",
            "advapi32.lib",
            "shell32.lib",
            "ole32.lib",
            "oleaut32.lib",
            "uuid.lib",
            "odbc32.lib",
            "odbccp32.lib"
        };
        private static readonly string[] _expectedHeaderFiles = new string[] {
            "stdio.h",
            "windows.h"
        };
        private static readonly bool _compilerPresent = CheckCompilerPresent();
        private static readonly bool _libsPresent = CheckLibsPresent();
        private static readonly bool _headerFilesPresent = CheckHeaderFilesPresent();

        #endregion Private Static Fields
    }
}
