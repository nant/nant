// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Mike Two (2@thoughtworks.com or mike2@nunit.org)
// Scott Hernandez (ScottHernandez@hotmail.com)

using System.Collections.Specialized;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

using NAnt.NUnit2.Tasks;

namespace NAnt.NUnit2.Types {
    /// <summary>
    /// Represents a test element of an <see cref="NUnit2Task" />.
    /// </summary>
    [ElementName("test")]
    public class NUnit2Test : Element {
        #region Private Instance Fields

        private string _assemblyName = null;
        private string _testname = null;
        private bool _haltOnFailure = true;
        private string _transformFile;
        private FileSet _assemblies = new FileSet();
        private string _appConfigFile = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of the assembly to search for tests.
        /// </summary>
        [TaskAttribute("assemblyname")]
        public string AssemblyName {
            get { return (_assemblyName != null) ? Project.GetFullPath(_assemblyName) : null; }
            set { _assemblyName = value; }
        }
        
        /// <summary>
        /// Name of a specific test to run. If not specified then all tests in 
        /// the assembly are run.
        /// </summary>
        [TaskAttribute("testname")]
        public string TestName {
            get { return _testname; }
            set { _testname = value; }
        }

        /// <summary>
        /// Assemblies to include in test.
        /// </summary>
        [FileSet("assemblies")]
        public FileSet Assemblies {
            get { return _assemblies; }
        }

        /// <summary>
        /// Build fails on failure.
        /// </summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure {
            get { return _haltOnFailure; }
            set { _haltOnFailure = value; }
        }

        /// <summary>
        /// XSLT transform file to use when using the Xml formatter.
        /// </summary>
        [TaskAttribute("transformfile")]
        public string TransformFile {
            get { return _transformFile; }
            set { _transformFile = value; }
        }

        /// <summary>
        /// The application configuration file to use for the NUnit test domain.
        /// </summary>
        [TaskAttribute("appconfig")]
        public string AppConfigFile {
            get { return Project.GetFullPath(_appConfigFile); }
            set { _appConfigFile = value; }
        }

        /// <summary>
        /// Gets all assemblies specified for these tests.
        /// </summary>
        /// <returns>
        /// All assemblies specified for these tests.
        /// </returns>
        public StringCollection TestAssemblies {
            get {
                StringCollection files = new StringCollection();

                if (AssemblyName != null) {
                    files.Add(AssemblyName);
                } else {
                    files = Assemblies.FileNames;
                }

                return files;
            }
        }

        #endregion Public Instance Properties
    }
}
