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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Xml;

using NUnit.Framework;
using NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.Core.Tasks {

  using global::NAnt.Core.Tasks;
  using global::NAnt.Core.Types;

    [TestFixture]
    public class ExecTaskTest : BuildTestBase {
      
      private string _buildFileName;
      private string _testProjectName;

      const string _format = @"<?xml version='1.0' ?>
            <project>
                <exec {0}>{1}</exec>
            </project>";

        /// <summary>
        /// The file system operation delay in milliseconds.
        /// Time to wait after any file system operation (create/delete files or directories).
        /// </summary>
        private const int FileSystemOperationDelay = 10;

      /// <summary>
      /// Gets the name of the build file.
      /// </summary>
      /// <value>
      /// The name of the build file.
      /// </value>
      protected string BuildFileName
      {
        get { return _buildFileName; }
        private set { _buildFileName = value; }
      }

      /// <summary>
      /// Gets the name of the test project.
      /// </summary>
      /// <value>
      /// The name of the test project.
      /// </value>
      protected string TestProjectName
      {
        get { return _testProjectName; }
        private set { _testProjectName = value; }
      }

      /// <summary>
      /// This method will be called by NUnit for setup.
      /// </summary>
      public void PrepareTestEnvironment()
        {
          this.TestProjectName = "TestProject";
          this.BuildFileName = "NAnt.build";
          Directory.CreateDirectory(this.TestProjectName);
          Thread.Sleep(FileSystemOperationDelay);
          Environment.CurrentDirectory = Path.GetFullPath(this.TestProjectName);
        }

        /// <summary>Test <arg> option.</summary>
        [Test]
        public void Test_ArgOption() {
            string result = "";
            if (PlatformHelper.IsWin32) {
                result = RunBuild(FormatBuildFile("program='cmd.exe'", "<arg value='/c echo Hello, World!'/>"));
            } else {
                result = RunBuild(FormatBuildFile("program='echo'", "<arg value='Hello, World!'/>"));
            }
            Assert.IsTrue(result.IndexOf("Hello, World!") != -1, "Could not find expected text from external program, <arg> element is not working correctly.");
        }

        /// <summary>Regression test for bug #461732 - ExternalProgramBase.ExecuteTask() hanging</summary>
        /// <remarks>
        /// http://sourceforge.net/tracker/index.php?func=detail&aid=461732&group_id=31650&atid=402868
        /// </remarks>
        [Test]
        public void Test_ReadLargeAmountFromStdout() {

            // create a text file with A LOT of data
            string line = "01234567890123456789012345678901234567890123456789012345678901234567890123456789" + Environment.NewLine;
            StringBuilder contents = new StringBuilder("You can delete this file" + Environment.NewLine);
            for (int i = 0; i < 250; i++) {
                contents.Append(line);
            }
            string tempFileName = Path.Combine(TempDirName, "bigfile.txt");
            TempFile.Create(tempFileName);

            if (PlatformHelper.IsWin32) {
                RunBuild(FormatBuildFile("program='cmd.exe' commandline='/c type &quot;" + tempFileName + "&quot;'", ""));
            } else {
                RunBuild(FormatBuildFile("program='cat' commandline=' &quot;" + tempFileName + "&quot;'", ""));
            }
            // if we get here then we passed, ie, no hang = bug fixed
        }

        private string FormatBuildFile(string attributes, string nestedElements) {
            return String.Format(CultureInfo.InvariantCulture, _format, attributes, nestedElements);
        }

        /// <summary>
        /// Tests the default exit code.
        /// </summary>
        [Test]
        public void TestDefaultExitCode()
        {
            this.PrepareTestEnvironment();
            ExecTask task = this.CreateTaskWithProject();
            if (PlatformHelper.IsUnix)
            {
              task.FileName = "bash";
              task.Arguments.Add(new Argument("-c"));
            }
            else
            {
              task.FileName = @"cmd.exe";
              task.Arguments.Add(new Argument("/c"));
            }
            task.Arguments.Add(new Argument("exit"));
            task.Arguments.Add(new Argument(0.ToString(CultureInfo.InvariantCulture)));
            task.Execute();
        }

        /// <summary>
        /// Tests the expected exit code.
        /// </summary>
        [Test]
        public void TestExpectedExitCode()
        {
            this.PrepareTestEnvironment();
            List<int> exitCodes = new List<int>();
            
            exitCodes.Add(byte.MaxValue);
            exitCodes.Add(byte.MinValue);
            exitCodes.Add(sbyte.MaxValue);

            // Bash supports only exit codes from 0  to 255
            if (PlatformHelper.IsWindows)
            {
                exitCodes.Add(sbyte.MinValue);
                exitCodes.Add(short.MaxValue);
                exitCodes.Add(short.MinValue);
                exitCodes.Add(ushort.MaxValue);
                exitCodes.Add(ushort.MinValue);
                exitCodes.Add(int.MaxValue);
                exitCodes.Add(int.MinValue);
            }

            foreach (int exitCode in exitCodes)
            {
                ExecTask task = this.CreateTaskWithProject();
                if (PlatformHelper.IsUnix)
                {
                    task.FileName = "bash";
                    task.Arguments.Add(new Argument("-c"));
                    task.Arguments.Add(new Argument("\"exit " + exitCode.ToString(CultureInfo.InvariantCulture) + "\""));
                }
                else
                {
                    task.FileName = @"cmd.exe";
                    task.Arguments.Add(new Argument("/c"));
                    task.Arguments.Add(new Argument("exit"));
                    task.Arguments.Add(new Argument(exitCode.ToString(CultureInfo.InvariantCulture)));
                }
                
                task.ExpectedExitCode = exitCode;
                task.Execute();
            }
        }

        /// <summary>
        /// Tests the unexpected exit code.
        /// </summary>
        [Test]
        public void TestUnexpectedExitCode()
        {
            this.PrepareTestEnvironment();
            // Dictonary with test data. Key: produced exit code, value: expected exit code
            Dictionary<int, int> exitCodes = new Dictionary<int, int>();
            exitCodes.Add(byte.MaxValue, byte.MinValue);
            
            // Bash supports only exit codes from 0  to 255
            if (PlatformHelper.IsWindows)
            {
                exitCodes.Add(sbyte.MaxValue, sbyte.MinValue);
                exitCodes.Add(short.MaxValue, short.MinValue);
                exitCodes.Add(ushort.MaxValue, ushort.MinValue);
                exitCodes.Add(int.MaxValue, int.MinValue);
            }

            foreach (KeyValuePair<int, int> exitCode in exitCodes)
            {
                ExecTask task = this.CreateTaskWithProject();
                if (PlatformHelper.IsUnix)
                {
                  task.FileName = "bash";
                  task.Arguments.Add(new Argument("-c"));
                  task.Arguments.Add(new Argument("\"exit " + exitCode.Key.ToString(CultureInfo.InvariantCulture) + "\""));
                }
                else
                {
                  task.FileName = @"cmd.exe";
                  task.Arguments.Add(new Argument("/c"));
                  task.Arguments.Add(new Argument("exit"));
                  task.Arguments.Add(new Argument(exitCode.Key.ToString(CultureInfo.InvariantCulture)));
                }
                
                task.ExpectedExitCode = exitCode.Value;
                BuildException currentBuildException = null;
                try
                {
                    task.Execute();
                }
                catch (BuildException ex)
                {
                    currentBuildException = ex;
                }

                Assert.IsNotNull(currentBuildException);
            }

            foreach (KeyValuePair<int, int> exitCode in exitCodes)
            {
                ExecTask task = this.CreateTaskWithProject();
                if (PlatformHelper.IsUnix)
                {
                  task.FileName = "bash";
                  task.Arguments.Add(new Argument("-c"));
                }
                else
                {
                  task.FileName = @"cmd.exe";
                  task.Arguments.Add(new Argument("/c"));
                }
                task.Arguments.Add(new Argument("exit"));
                task.Arguments.Add(new Argument(exitCode.Value.ToString(CultureInfo.InvariantCulture)));
                task.ExpectedExitCode = exitCode.Key;
                BuildException currentBuildException = null;
                try
                {
                    task.Execute();
                }
                catch (BuildException ex)
                {
                  currentBuildException = ex;
                }

                Assert.IsNotNull(currentBuildException);  
            }
        }

        /// <summary>
        /// Creates the task with a project.
        /// </summary>
        /// <returns>
        /// The task with a project.
        /// </returns>
        protected ExecTask CreateTaskWithProject()
        {
            // Create buildfile
            XmlDocument buildFile = new XmlDocument();
            XmlElement element = buildFile.CreateElement("project");
            XmlAttribute projectName = buildFile.CreateAttribute("name");
            projectName.Value = this.TestProjectName;
            element.Attributes.Append(projectName);
            buildFile.AppendChild(element);
            string buildFileName = Path.GetFullPath(this.BuildFileName);
            buildFile.Save(buildFileName);

            // Create project
            Project project = new Project(buildFileName, Level.Debug, 0);

            // create task
            ExecTask task = new ExecTask();
            task.Project = project;
            task.Parent = project;
            return task;
        }
    }
}
