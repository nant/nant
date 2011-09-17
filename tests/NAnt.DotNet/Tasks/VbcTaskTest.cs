// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Globalization;
using System.IO;

using NUnit.Framework;

using NAnt.Core;

using NAnt.DotNet.Tasks;
using NAnt.DotNet.Types;

using Tests.NAnt.Core;
using Tests.NAnt.Core.Util;

namespace Tests.NAnt.DotNet.Tasks {
    [TestFixture]
    public class VbcTaskTest : BuildTestBase {
        #region Private Instance Fields

        private string _sourceFileName;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const string _format = @"<?xml version='1.0'?>
            <project>
                <vbc target='exe' output='{0}.exe' {1}>
                    <imports>
                        <import namespace='System,System.Collections' />
                        <import namespace='System.Data' />
                    </imports>
                    <sources basedir='{2}'>
                        <include name='{3}'/>
                    </sources>
                    <references>
                        <include name='System.dll' />
                        <include name='System.Data.dll' />
                    </references>
                    <resources basedir='{2}'>
                        <include name='**/*.resx' />
                    </resources>
                </vbc>
            </project>";

        private const string _sourceCode = @"
            Public Class HelloWorld
                Shared Sub Main()
                    Dim table as DataTable = new DataTable()
                    Dim list as ArrayList = new ArrayList()
                    Console.WriteLine(""Hello World using VB.NET"")
                    Return
                End Sub
            End Class";

        private const string _sourceCodeWithNamespace = @"
            Imports System

            Namespace ResourceTest
                Public Class HelloWorld
                    Shared Sub Main()
                        Console.WriteLine(""Hello World using VB.NET"")
                        Return
                    End sub
                End Class
            End Namespace";

        #endregion Private Static Fields

        #region Override implementation of BuildTestBase

        [SetUp]
        protected override void SetUp() {
            base.SetUp();
            _sourceFileName = Path.Combine(TempDirName, "HelloWorld.vb");
            TempFile.CreateWithContents(_sourceCode, _sourceFileName);
        }

        #endregion Override implementation of BuildTestBase

        #region Public Instance Methods

        /// <summary>
        /// Test to make sure debug option works.
        /// </summary>
        [Test]
        [Category ("NotMono")]
        public void Test_DebugBuild() {
            RunBuild(FormatBuildFile("debug='true'"));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"), _sourceFileName + ".exe does not exists, program did compile.");
            // Comment this for now as its hard to know which framework was used to compile and it was mono there will be no pdb file.
            //Assert.IsTrue(File.Exists(_sourceFileName + ".pdb"), _sourceFileName + ".pdb does not exists, program did compile with debug switch.");
        }

        /// <summary>
        /// Test to make sure debug option works.
        /// </summary>
        [Test]
        [Category ("NotMono")]
        public void Test_ReleaseBuild() {
            RunBuild(FormatBuildFile("debug='false'"));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"), _sourceFileName + ".exe does not exists, program did compile.");
            Assert.IsFalse(File.Exists(_sourceFileName + ".pdb"), _sourceFileName + ".pdb does exists, program did compiled with debug switch.");
        }

        /// <summary>
        /// Test to make sure output can be created, even if the path does not exist yet.
        /// </summary>		
        [Test]
        [Category ("NotMono")]
        public void Test_CreateParentDirectory() {
            _sourceFileName = Path.Combine(TempDirName, 
                Path.Combine("bin", "HelloWorld.vb"));
            TempFile.CreateWithContents(_sourceCode, _sourceFileName);            

            RunBuild(FormatBuildFile(
                Path.Combine("bin", "HelloWorld.vb"), null, null, null));
            Assert.IsTrue(File.Exists(_sourceFileName + ".exe"), _sourceFileName + ".exe does not exists, program did compile.");
        }

        [Test]
        [Category ("NotMono")]
        public void Test_Define() {
            string sourceCode = @"
                Public Class HelloWorld
                #If CONSOLE Then
                    Shared Sub Main()
                #Else
                    Public Sub Whatever ()
                #End If
                #If Not ABC Then
                        SomeError
                #End If
                        Dim table as DataTable = new DataTable()
                        Dim list as ArrayList = new ArrayList()
                        Console.WriteLine(""Hello World using VB.NET"")
                #if DEF Then
                        AnotherError
                #End If
                        Return
                    End Sub
                End Class";

            TempFile.CreateWithContents(sourceCode, _sourceFileName);

            RunBuild(FormatBuildFile("define='CONSOLE=True,DEF=False,ABC=True'"));
        }

        [Test]
        [ExpectedException(typeof(BuildException))]
        public void Test_ManifestResourceName_NonExistingResource() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.DynamicPrefix = true;

            vbcTask.GetManifestResourceName(resources, "I_dont_exist.txt");
        }

        [Test]
        public void Test_ManifestResourceName_Resx_StandAlone_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.DynamicPrefix = true;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.en-US.dunno.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_Resx_StandAlone_DynamicPrefix_With_Prefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = true;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." 
                + "ResourceFile.resources", vbcTask.GetManifestResourceName(
                resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." 
                + "ResourceFile.en-US.resources", vbcTask.GetManifestResourceName(
                resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." 
                + "ResourceFile.en-US.dunno.en-US.resources", vbcTask.GetManifestResourceName(
                resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_Resx_StandAlone_Prefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = false;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.dunno.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_Resx() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
                
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.DynamicPrefix = false;

            PerformDependentResxTests(vbcTask, resources);
        }

        [Test]
        public void Test_ManifestResourceName_Resx_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
                
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.DynamicPrefix = true;

            PerformDependentResxTests(vbcTask, resources);
        }

        [Test]
        public void Test_ManifestResourceName_Resx_Prefix_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = true;

            // prefix should be ignored for resx files
            PerformDependentResxTests(vbcTask, resources);
        }

        [Test]
        public void Test_ManifestResourceName_Resx_Prefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = false;

            // prefix should be ignored for resx files
            PerformDependentResxTests(vbcTask, resources);
        }

        [Test]
        public void Test_ManifestResourceName_NonResx_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.DynamicPrefix = true;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.en-US.dunno.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_NonResx_Prefix_With_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = true;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." 
                + "ResourceFile.en-US.dunno.txt", vbcTask.GetManifestResourceName(
                resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_NonResx_Prefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = false;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.txt");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.txt", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_CompiledResource_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.DynamicPrefix = true;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual("SubDir" + "." + "ResourceFile.en-US.dunno.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_CompiledResource_Prefix_With_DynamicPrefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = true;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "SubDir" + "." 
                + "ResourceFile.en-US.dunno.en-US.resources", vbcTask.GetManifestResourceName(
                resources, resourceFile));
        }

        [Test]
        public void Test_ManifestResourceName_CompiledResource_Prefix() {
            VbcTask vbcTask = new VbcTask();
            vbcTask.Project = CreateEmptyProject();
            vbcTask.RootNamespace = "TestRootNamespace";
            
            ResourceFileSet resources = new ResourceFileSet();
            resources.BaseDirectory = TempDirectory;
            resources.Prefix = "TestNamespace";
            resources.DynamicPrefix = false;

            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resources");
            // create resource file
            CreateTempFile(resourceFile);
            // assert manifest resource name
            Assert.AreEqual(resources.Prefix + "." + "ResourceFile.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void PerformDependentResxTests(VbcTask vbcTask, ResourceFileSet resources) {
            // holds the path to the resource file
            string resourceFile = null;

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCodeWithNamespace, Path.Combine(
                resources.BaseDirectory.FullName, "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".ResourceTest.HelloWorld.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCode, Path.Combine(
                resources.BaseDirectory.FullName, "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".HelloWorld.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCodeWithNamespace, Path.Combine(
                resources.BaseDirectory.FullName, "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".ResourceTest.HelloWorld.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, 
                "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCode, Path.Combine(
                resources.BaseDirectory.FullName, "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".HelloWorld.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCodeWithNamespace, Path.Combine(
                resources.BaseDirectory.FullName, "SubDir" + Path.DirectorySeparatorChar 
                + "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".ResourceTest.HelloWorld.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCode, Path.Combine(
                resources.BaseDirectory.FullName, "SubDir" + Path.DirectorySeparatorChar 
                + "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".HelloWorld.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCodeWithNamespace, Path.Combine(
                resources.BaseDirectory.FullName, "SubDir" + Path.DirectorySeparatorChar 
                + "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".ResourceTest.HelloWorld.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCode, Path.Combine(
                resources.BaseDirectory.FullName, "SubDir" + Path.DirectorySeparatorChar 
                + "ResourceFile." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".HelloWorld.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCodeWithNamespace, Path.Combine(
                resources.BaseDirectory.FullName, "SubDir" + Path.DirectorySeparatorChar 
                + "ResourceFile.en-US.dunno." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".ResourceTest.HelloWorld.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));

            // initialize resource file
            resourceFile = Path.Combine(resources.BaseDirectory.FullName, "SubDir" 
                + Path.DirectorySeparatorChar + "ResourceFile.en-US.dunno.en-US.resx");
            // create resource file
            CreateTempFile(resourceFile);
            // create dependent file
            TempFile.CreateWithContents(_sourceCode, Path.Combine(
                resources.BaseDirectory.FullName, "SubDir" + Path.DirectorySeparatorChar 
                + "ResourceFile.en-US.dunno." + vbcTask.Extension));
            // assert manifest resource name
            Assert.AreEqual(vbcTask.RootNamespace + ".HelloWorld.en-US.resources", 
                vbcTask.GetManifestResourceName(resources, resourceFile));
        }

        private string FormatBuildFile(string attributes) {
            return FormatBuildFile(
                null,
                attributes,
                null,
                null);
        }

        private string FormatBuildFile(
            string output, 
            string attributes, 
            string basedir,
            string includefiles) {
            return string.Format(CultureInfo.InvariantCulture, _format, 
                output       != null ? output : Path.GetFileName(_sourceFileName), 
                attributes   != null ? attributes : "",
                basedir      != null ? basedir : Path.GetDirectoryName(_sourceFileName), 
                includefiles != null ? includefiles : Path.GetFileName(_sourceFileName));
        }

        #endregion Private Instance Methods

        /// <summary>
        /// Unit tests for FileParser
        /// </summary>
        [TestFixture]
        public class TestResourceLinkage {
            /// <summary>
            /// Uses a representative sampling of classname inputs to verify that the classname line can be found
            /// </summary>
            [Test]
            public void TestFindClassname() {
                // Positive test cases - classname should be found
                VerifyFindClassname("Public Abstract Class CompilerBase\r\n{} \r\n}", "CompilerBase");
                VerifyFindClassname("Public Abstract Class Conference \r\n{}", "Conference");
        
                // Negative test cases - no classname should be found
                VerifyFindClassname("' this is some Class here\r\n", "");
            }
                
            /// <summary>
            /// Parses the input, ensuring the class name is found
            /// </summary>
            public void VerifyFindClassname( string input, string expectedClassname ) {
                VbcTask vbTask = new VbcTask();
                StringReader reader = new StringReader( input );
                CompilerBase.ResourceLinkage linkage = vbTask.PerformSearchForResourceLinkage( reader );
                
                Assert.IsNotNull(linkage, "no resourcelinkage found for " + input);
                Assert.AreEqual(expectedClassname, linkage.ClassName);
            }
        }
    }
}
