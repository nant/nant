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
using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.DotNet.Types;

using NAnt.NUnit.Types;
using NAnt.NUnit2.Tasks;

namespace NAnt.NUnit2.Types {
    /// <summary>
    /// Represents a <c>test</c> element of an <see cref="NUnit2Task" />.
    /// </summary>
    [ElementName("test")]
    public class NUnit2Test : Element {
        #region Private Instance Fields

        private FileInfo _assemblyFile;
        private string _testname;
        private bool _haltOnFailure = true;
        private FileInfo _xsltFile;
        private AssemblyFileSet _assemblies = new AssemblyFileSet();
        private AssemblyFileSet _references = new AssemblyFileSet();
        private FileInfo _appConfigFile;
        private Categories _categories = new Categories();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of the assembly to search for tests.
        /// </summary>
        [TaskAttribute("assemblyname")]
        public FileInfo AssemblyFile {
            get { return _assemblyFile; }
            set { _assemblyFile = value; }
        }

        /// <summary>
        /// Name of a specific testfixture to run. If not specified then all 
        /// testfixtures are run.
        /// </summary>
        [TaskAttribute("testname")]
        public string TestName {
            get { return _testname; }
            set { _testname = value; }
        }

        /// <summary>
        /// Assemblies to include in test.
        /// </summary>
        [BuildElement("assemblies")]
        public AssemblyFileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// Assemblies to scan for missing assembly references.
        /// </summary>
        [BuildElement("references")]
        public AssemblyFileSet References {
            get { return _references; }
            set { _references = value; }
        }

        /// <summary>
        /// Categories of test cases to include or exclude.
        /// </summary>
        [BuildElement("categories")]
        public Categories Categories {
            get { return _categories; }
            set { _categories = value; }
        }

        /// <summary>
        /// Build fails on failure. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("haltonfailure")]
        [BooleanValidator()]
        public bool HaltOnFailure {
            get { return _haltOnFailure; }
            set { _haltOnFailure = value; }
        }

        /// <summary>
        /// XSLT transform file to use when using the <see cref="FormatterType.Plain" /> 
        /// formatter.
        /// </summary>
        [TaskAttribute("transformfile")]
        public FileInfo XsltFile {
            get { return _xsltFile; }
            set { _xsltFile = value; }
        }

        /// <summary>
        /// The application configuration file to use for the NUnit test domain.
        /// If not specified, NAnt will try to use a configuration name matching
        /// the file name of the assembly with extension ".config".
        /// </summary>
        [TaskAttribute("appconfig")]
        public FileInfo AppConfigFile {
            get { return _appConfigFile; }
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

                if (AssemblyFile != null) {
                    files.Add(AssemblyFile.FullName);
                } else {
                    files = Assemblies.FileNames;
                }

                return files;
            }
        }

        #endregion Public Instance Properties
    }
}
