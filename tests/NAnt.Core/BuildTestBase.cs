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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Text;
using System.Xml;

using NUnit.Framework;
using SourceForge.NAnt.Tasks;

namespace SourceForge.NAnt.Tests {

    /// <summary>Base class for running build files and checking results.</summary>
    /// <remarks>
    ///   <para>Provides support for quickly running a build and capturing the output.</para>
    /// </remarks>
    public abstract class BuildTestBase : TestCase {

        string _tempDirName = null;
       
        public BuildTestBase(string name) : base(name) {
        }

        /// <summary>
        /// The Temp Directory name for this test case. Should be in the form %temp%\ClassName (ex. c:\temp\SourceForge.NAnt.Test.BuildTestBase).
        /// </summary>
        public string TempDirName {
            get { return _tempDirName; }
        }

        /// <remarks>
        ///   <para>Super classes that override SetUp must call the base class first.</para>
        /// </remarks>
        protected override void SetUp() {
            _tempDirName = TempDir.Create(this.GetType().FullName);
        }

        /// <remarks>
        ///   <para>Super classes that override must call the base class last.</para>
        /// </remarks>
        protected override void TearDown() {
            TempDir.Delete(_tempDirName);
        }

        /// <summary>
        /// run the xml as nant project and return the console output as a string
        /// </summary>
        /// <param name="xml">xml representing the build file contents</param>
        /// <returns>The console output</returns>
        public string RunBuild(string xml) {
            Project p = CreateFilebasedProject(xml);
            return ExecuteProject(p);
        }

        /// <summary>
        /// executes the project return the console output as a string
        /// </summary>
        /// <param name="p">The Project to Execute()</param>
        /// <returns>The console output</returns>
        public string ExecuteProject(Project p) {
            using (ConsoleCapture c = new ConsoleCapture()) {
                string output = null;
                try{
                    p.Execute();
                }
                catch {
                    output = c.Close();
                    if(!(output == null || output.Equals(string.Empty) || output.Equals(""))){
                        Console.WriteLine("+++++++++++++++++++++++++++++++++++++");
                        Console.WriteLine("+++++     Output From Test      +++++");
                        Console.WriteLine("+++++++++++++++++++++++++++++++++++++");
                        Console.Write (output);
                        Console.WriteLine("+++++++++++++++++++++++++++++++++++++");
                    }
                    throw;
                }
                finally {
                    if(output == null)
                        output = c.Close();
                }
                
                return output;
            }
        }

        /// <summary>
        /// Creates a new Project
        /// </summary>
        /// <param name="xml">The xml of the build file</param>
        /// <returns>The new project</returns>
        public Project CreateFilebasedProject(string xml) {
            // create the build file in the temp folder
            string buildFileName = Path.Combine(TempDirName, "test.build");
            TempFile.CreateWithContents(xml, buildFileName);

            return new Project(buildFileName);
        }

        /// <summary>
        /// Creates a tempfile in the test temp directory
        /// </summary>
        /// <param name="name">the filename, should not be an absolute</param>
        /// <returns>The full FilePath to the new file</returns>
        public string CreateTempFile(string name) {
            return CreateTempFile(name, null);
        }
        /// <summary>
        /// Creates a tempfile in the test temp directory
        /// </summary>
        /// <param name="name">the filename, should not be an absolute</param>
        /// <param name="contents">What you want in the file</param>
        /// <returns>The full FilePath to the new file</returns>
        /// <remarks>The file is created and existance is checked</remarks>
        public string CreateTempFile(string name, string contents) {
            string filename = Path.Combine(TempDirName, name);
            
            if(Path.IsPathRooted(name))
                filename=name;

            if(contents == null)
                return TempFile.Create(filename);
            
            return TempFile.CreateWithContents(contents, filename);
        }
        /// <summary>
        /// Creates a temp directory
        /// </summary>
        /// <param name="name">the name of the directory to create (name only, no path info)</param>
        /// <returns>The full path to the new directory</returns>
        /// <remarks>The dir is created and existance is checked</remarks>
        public string CreateTempDir(string name) {
            return TempDir.Create(Path.Combine(TempDirName, name));
        }
    }
}
