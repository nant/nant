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
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.ComponentModel;
using NUnit.Framework;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace Tests.NAnt.Core {
    /// <summary>
    /// A simple task with a null element to test failures.
    /// </summary>
    [TaskName("elementTest1")]
    class ElementTest1Task : Task {
        #region Private Instance Fields

        private OutputType _outputType = OutputType.None;
        private Uri _uri;

        #endregion Private Instance Fields

        #region Internal Static Fields

        internal const string UriPropertyName = "elementTest1.uri";

        #endregion Internal Static Fields

        #region Public Instance Properties

        [BuildElement("fileset")]
        public FileSet FileSet {
            get { return null; } // we'll test for null later!
        }

        [TaskAttribute("type")]
        public OutputType Type {
            get { return _outputType; }
            set { _outputType = value; }
        }

        [TaskAttribute("uri")]
        public Uri Uri {
            get { return _uri; }
            set { _uri = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() { 
            Log(Level.Info, "OutputType is \"{0}\".", Type.ToString());

            if (Uri != null) {
                Properties.Add(ElementTest1Task.UriPropertyName, Uri.ToString());
            }
        }

        #endregion Override implementation of Task

        [TypeConverter(typeof(OutputTypeConverter))]
        public enum OutputType {
            None = 0,

            Exe = 1,

            Dll = 2
        }

        public class OutputTypeConverter : EnumConverter {
            public OutputTypeConverter() : base(typeof(OutputType)) {
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
                if (value is string) {
                    if (string.Compare((string) value, "executable", true, culture) == 0) {
                        return OutputType.Exe;
                    }
                }
                return base.ConvertFrom (context, culture, value);
            }
        }
    }

    /// <summary>
    /// This is a test class to make sure that the if/unless conditionals
    /// are processed as expected.
    /// </summary>
    [TaskName("conditionaltest")]
    class ConditionalElementTestTask : Task
    {
        #region Public Static Fields

        internal const string PropName = "quote";

        #endregion

        #region Private Instance Fields

        private string _quote;

        #endregion

        #region Public Instance Properties

        [TaskAttribute("quote")]
        public string Quote
        {
            get { return _quote; }
            set { _quote = value; }
        }

        #endregion
        
        #region Override implementation of Task

        protected override void ExecuteTask() { 
            string result = String.Format("The quote is \"{0}\".", Quote ?? String.Empty);
            Log(Level.Info, result);

            Properties.Add(PropName, result);
        }

        #endregion Override implementation of Task
    }

    /*
    /// <summary>
    /// A simple task with a null element to test failures.
    /// </summary>
    [TaskName("elementTest2")]
    class ElementTest2Task : Task {
        #region Private Instance Fields

        private ArrayList _children = new ArrayList();

        #endregion Private Instance Fields

        #region Public Instance Properties

        [BuildElementCollection("children", "child", ElementType=typeof(object))]
        public ArrayList Children {
            get { return _children; }
            set { _children = value; }
        }

        #endregion Public Instance Properties


        #region Override implementation of Task

        protected override void ExecuteTask() { 
        }

        #endregion Override implementation of Task
    }
    */

    [TestFixture]
    public class ElementTest : BuildTestBase {
        #region Public Instance Methods

        /// <summary>
        /// Test that a read-only property with an element doesn't 
        /// return null when the getter is invoked
        /// </summary>
        [Test]
        public void Test_Null_Element_Prop_Value() {
            const string build = @"<?xml version='1.0' ?>
               <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1>
                           <fileset><include name='*.cs'/></fileset>
                        </elementTest1>
                     </target>
                  </project>";

            try {
                string result = RunBuild(build);
                Assert.Fail("Null property value allowed." + Environment.NewLine + result);
            } catch (TestBuildException e) {
                if (!(e.InnerException is BuildException)) {
                    Assert.Fail("Unexpected exception thrown." + Environment.NewLine + e.ToString());
                }
            }
        }

        [Test]
        public void Test_Enum_Default() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 />
                     </target>
                </project>";

            string result = RunBuild(build);
            Assert.IsTrue(result.IndexOf("OutputType is \"None\".") != -1);
        }

        [Test]
        public void Test_Enum_TypeConverter() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 type='ExeCutable' />
                     </target>
                </project>";

            string result = RunBuild(build);
            Assert.IsTrue(result.IndexOf("OutputType is \"Exe\".") != -1);
        }

        [ExpectedException(typeof(TestBuildException))]
        public void Test_Enum_InvalidValue() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 type='library' />
                     </target>
                </project>";

            RunBuild(build);
        }

        [Test]
        public void Test_Uri_Default() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 />
                     </target>
                </project>";

            Project project = CreateFilebasedProject(build);
            ExecuteProject(project);

            // uri property should not be registered
            Assert.IsFalse(project.Properties.Contains(ElementTest1Task.UriPropertyName));
        }

        [Test]
        public void Test_Uri_RelativeFilePath() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 uri='dir/test.txt' />
                     </target>
                </project>";

            Project project = CreateFilebasedProject(build);
            ExecuteProject(project);

            Uri expectedUri = new Uri(project.GetFullPath("dir/test.txt"));

            // uri property should be registered
            Assert.IsTrue(project.Properties.Contains(ElementTest1Task.UriPropertyName));
            // path should have been resolved to absolute path (in project dir)
            Assert.AreEqual(expectedUri.ToString(), project.Properties[
                ElementTest1Task.UriPropertyName]);
        }

        [Test]
        public void Test_Uri_FileScheme() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 uri='file:///test/file.txt' />
                     </target>
                </project>";

            Project project = CreateFilebasedProject(build);
            ExecuteProject(project);

            // uri property should be registered
            Assert.IsTrue(project.Properties.Contains(ElementTest1Task.UriPropertyName));
            // ensure resulting property matches expected URI 
            Assert.AreEqual(new Uri("file:///test/file.txt"), 
                new Uri(project.Properties[ElementTest1Task.UriPropertyName]));
        }

        [Test]
        public void Test_Uri_HttpScheme() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 uri='http://nant.sourceforge.net' />
                     </target>
                </project>";

            Project project = CreateFilebasedProject(build);
            ExecuteProject(project);

            // uri property should be registered
            Assert.IsTrue(project.Properties.Contains(ElementTest1Task.UriPropertyName));
            // ensure resulting property matches expected URI 
            Assert.AreEqual("http://nant.sourceforge.net/", project.Properties[
                ElementTest1Task.UriPropertyName]);
        }

        [Test]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_Uri_InvalidUri() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest1 uri=':://file.sourceforge.net' />
                     </target>
                </project>";

            RunBuild(build);
        }

        [Test]
        [Ignore ("Re-enable this test once we modified the <nantschema> task to generate a schema for a specified set of assemblies.")]
        [ExpectedException(typeof(TestBuildException))]
        public void Test_Non_StronglyTyped_Element_Collection() {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <elementTest2>
                            <children>
                                <child />
                            </children>
                        </elementTest2>
                     </target>
                </project>";

            RunBuild(build);
        }

        [TestCase("I did not inhale", true)]
        [TestCase("I did inhale", false)]
        [TestCase("${does.not.exist}", false)]
        public void Test_IfAttribute(string quote, bool ifAttr)
        {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <conditionaltest quote='{0}' if='{1}'/>
                     </target>
                </project>";

            Project project = CreateFilebasedProject(String.Format(build, quote, ifAttr));
            ExecuteProject(project);
            
            if (ifAttr)
            {
                Assert.IsTrue(project.Properties.Contains(ConditionalElementTestTask.PropName),
                    String.Format("Project does not contain expected property: '{0}'", 
                        ConditionalElementTestTask.PropName));
                string result = project.Properties[ConditionalElementTestTask.PropName];
                Assert.IsTrue(result.Contains(quote), 
                    String.Format("Result does not contain quote: '{0}' | '{1}'", result, quote));
            }
            else
            {
                Assert.IsFalse(project.Properties.Contains(ConditionalElementTestTask.PropName),
                    String.Format("Project does contain unexpected property: '{0}'", 
                        ConditionalElementTestTask.PropName));
            }
        }

        [TestCase("I did not inhale", false)]
        [TestCase("I did inhale", true)]
        [TestCase("${does.not.exist}", true)]
        public void Test_UnlessAttribute(string quote, bool unlessAttr)
        {
            const string build = @"<?xml version='1.0' ?>
                <project name='testing' default='test'>
                     <target name='test'>
                        <conditionaltest quote='{0}' unless='{1}'/>
                     </target>
                </project>";

            Project project = CreateFilebasedProject(String.Format(build, quote, unlessAttr));
            ExecuteProject(project);
            
            if (unlessAttr)
            {
                Assert.IsFalse(project.Properties.Contains(ConditionalElementTestTask.PropName),
                    String.Format("Project does contain unexpected property: '{0}'", 
                        ConditionalElementTestTask.PropName));
            }
            else
            {
                Assert.IsTrue(project.Properties.Contains(ConditionalElementTestTask.PropName),
                    String.Format("Project does not contain expected property: '{0}'", 
                        ConditionalElementTestTask.PropName));
                string result = project.Properties[ConditionalElementTestTask.PropName];
                Assert.IsTrue(result.Contains(quote), 
                    String.Format("Result does not contain quote: '{0}' | '{1}'", result, quote));
            }
        }

        #endregion Public Instance Methods
    }
}
