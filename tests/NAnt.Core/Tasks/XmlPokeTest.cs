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
// Gert Driesen (drieseng@users.sourceforge.net)

using System.IO;
using System.Globalization;
using System.Text;
using System.Xml;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]    
    public class XmlPokeTest : BuildTestBase {
        #region Private Instance Fields

        private const string _projectXml = "<?xml version=\"1.0\"?>"
            + "<project>"
                + "<xmlpoke {0} />"
                + "<xmlpeek {1} property=\"configuration.server\" />" 
                + "<echo message=\"configuration.server={2}!\" />"
            + "</project>";
            
        private const string _projectXmlPreserveWhitespace = "<?xml version=\"1.0\"?>"
            + "<project>"
                + "<xmlpoke {0} preserveWhitespace=\"{1}\" />"
                + "<xmlpeek {2} property=\"configuration.server\" />"
                + "<echo message=\"configuration.server={3}!\" />"
            + "</project>";            

        private const string _projectXmlWithNamespace = "<?xml version=\"1.0\"?>"
            + "<project>"
                + "<xmlpoke {0}>"
                    + "<namespaces>"
                        + "<namespace prefix=\"x\" uri=\"http://www.gordic.cz/shared/project-config/v_1.0.0.0\" />"
                    + "</namespaces>"
                + "</xmlpoke>"
                + "<xmlpeek {1} property=\"configuration.server\">" 
                    + "<namespaces>"
                        + "<namespace prefix=\"x\" uri=\"http://www.gordic.cz/shared/project-config/v_1.0.0.0\" />"
                    + "</namespaces>"
                + "</xmlpeek>" 
                + "<echo message=\"configuration.server={2}!\" />"
            + "</project>";

        private const string _validXml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" 
            + "<configuration>" 
                + "<appSettings>"
                    + "<add key=\"server\" value=\"testhost.somecompany.com\" />"
                + "</appSettings>"
            + "</configuration>";

        private const string _validXmlWithNamespace = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>"
            + "<configuration xmlns=\"http://www.gordic.cz/shared/project-config/v_1.0.0.0\">"
                + "<appSettings>"
                    + "<add key=\"server\" value=\"testhost.somecompany.com\" />"
                + "</appSettings>"
            + "</configuration>";
            
        // note the extra whitespace before the nodes
        private const string _validXmlWithWhitespace = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" 
            + " <configuration>" 
                + " <appSettings>"
                    + " <add key=\"server\" value=\"testhost.somecompany.com\" />"
                + " </appSettings>"
            + " </configuration>";            

        #endregion Private Instance Fields

        #region Public Instance Methods

        [Test]
        public void Test_PokeValidXml() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXml);

            // set-up <xmlpoke> task attributes
            string xmlPokeTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"" +
                " value=\"productionhost.somecompany.com\"", xmlFile);

            // set-up <xmlpeek> task attributes
            string xmlPeekTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"",
                xmlFile);

            // execute build
            string buildLog = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _projectXml, xmlPokeTaskAttributes, xmlPeekTaskAttributes, 
                "${configuration.server}"));

            // ensure original value was not retained
            Assert.IsTrue(buildLog.IndexOf("configuration.server=testhost.somecompany.com!") == -1,
                "Value of node was not updated, orignal value is still in xml file.");

            // ensure new value was set
            Assert.IsTrue(buildLog.IndexOf("configuration.server=productionhost.somecompany.com!") != -1,
                "Value of node was not updated correctly, new value does not match.");
        }

        [Test]
        public void Test_PokeValidXmlWithNamespace() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxmlnamespace.xml", _validXmlWithNamespace);

            // set-up <xmlpoke> task attributes
            string xmlPokeTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/x:configuration/x:appSettings/x:add[@key ='server']/@value\"" +
                " value=\"productionhost.somecompany.com\"", xmlFile);

            // set-up <xmlpeek> task attributes
            string xmlPeekTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/x:configuration/x:appSettings/x:add[@key ='server']/@value\"",
                xmlFile);

            // execute build
            string buildLog = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _projectXmlWithNamespace, xmlPokeTaskAttributes, xmlPeekTaskAttributes, 
                "${configuration.server}"));

            // ensure original value was not retained
            Assert.IsTrue(buildLog.IndexOf("configuration.server=testhost.somecompany.com!") == -1,
                "Value of node was not updated, orignal value is still in xml file.");

            // ensure new value was set
            Assert.IsTrue(buildLog.IndexOf("configuration.server=productionhost.somecompany.com!") != -1,
                "Value of node was not updated correctly, new value does not match.");
        }

        [Test]
        public void Test_PokeEmptyValue() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXml);

            // set-up <xmlpoke> task attributes
            string xmlPokeTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"" +
                " value=\"\"", xmlFile);

            // set-up <xmlpeek> task attributes
            string xmlPeekTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"",
                xmlFile);

            // execute build
            string buildLog = RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _projectXml, xmlPokeTaskAttributes, xmlPeekTaskAttributes, 
                "${configuration.server}"));

            // ensure original value was not retained
            Assert.IsTrue(buildLog.IndexOf("configuration.server=testhost.somecompany.com!") == -1,
                "Value of node was not updated, orignal value is still in xml file.");

            // ensure new value was set
            Assert.IsTrue(buildLog.IndexOf("configuration.server=!") != -1,
                "Value of node was not updated correctly, new value does not match.");
        }

        /// <summary>
        /// Ensures no <see cref="BuildException" /> is thrown when no nodes 
        /// match the XPath expression.
        /// </summary>
        [Test]
        public void Test_PokeValidXmlNoMatches() {
            // write xml content to file
            string xmlFile = CreateTempFile("validxml.xml", _validXml);

            // set-up <xmlpoke> task attributes
            string xmlPokeTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='anythingisok']/@value\"" +
                " value=\"productionhost.somecompany.com\"", xmlFile);

            // set-up <xmlpeek> task attributes
            string xmlPeekTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"",
                xmlFile);

            try {
                // execute build
                RunBuild(string.Format(CultureInfo.InvariantCulture,
                    _projectXml, xmlPokeTaskAttributes, xmlPeekTaskAttributes,
                    "${configuration.server}"));
            } catch (TestBuildException ex) {
                // assert that a BuildException was the cause of the TestBuildException
                Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
            }
        }

        [Test]
        public void Test_PokeEmptyFile() {
            // create empty file
            string xmlFile = CreateTempFile("empty.xml");

            // set-up <xmlpoke> task attributes
            string xmlPokeTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='anythingisok']/@value\"" +
                " value=\"productionhost.somecompany.com\"", xmlFile);

            // set-up <xmlpeek> task attributes
            string xmlPeekTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"",
                xmlFile);

            try {
                // execute build
                RunBuild(string.Format(CultureInfo.InvariantCulture, 
                    _projectXml, xmlPokeTaskAttributes, xmlPeekTaskAttributes,
                    "${configuration.server}"));
                // have the test fail
                Assert.Fail("Build should have failed.");
            } catch (TestBuildException ex) {
                // assert that a BuildException was the cause of the TestBuildException
                Assert.IsTrue((ex.InnerException != null && ex.InnerException.GetType() == typeof(BuildException)));
                // assert that an XmlException was the cause of the BuildException
                Assert.IsTrue((ex.InnerException.InnerException != null && ex.InnerException.InnerException.GetType() == typeof(XmlException)));
            }
        }
        
        [Test]
        public void Test_PokePreserveWhitespaceTrue()
        {
            AssertPokePreserveWhitespace(true);
        }

        [Test]
        public void Test_PokePreserveWhitespaceFalse()
        {
            AssertPokePreserveWhitespace(false);
        }        

        #endregion Public Instance Methods
        
        #region Private Instance Methods

        private void AssertPokePreserveWhitespace(bool preserveWhitespace)
        {
            // write xml content to file
            string xmlFile = CreateTempFile("validxmlwithwhitespace.xml", _validXmlWithWhitespace);

            string originalXmlFile = ReadFile(xmlFile);

            // set-up <xmlpoke> task attributes
            string xmlPokeTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"" +
                " value=\"testhost.somecompany.com\"", xmlFile); // don't change anything

            // set-up <xmlpeek> task attributes
            string xmlPeekTaskAttributes = string.Format(CultureInfo.InvariantCulture,
                "file=\"{0}\" xpath=\"/configuration/appSettings/add[@key ='server']/@value\"",
                xmlFile);

            // execute build, preserving whitespace
            RunBuild(string.Format(CultureInfo.InvariantCulture,
                _projectXmlPreserveWhitespace, xmlPokeTaskAttributes, preserveWhitespace, xmlPeekTaskAttributes,
                "${configuration.server}"));

            string currentXmlFile = ReadFile(xmlFile);

            if (preserveWhitespace)
            {
                Assert.AreEqual(originalXmlFile, currentXmlFile);
            }
            else
            {
                Assert.AreNotEqual(originalXmlFile, currentXmlFile);
            }
        }

        private static string ReadFile(string path)
        {
            string contents;

            using (StreamReader sr = new StreamReader(path, Encoding.UTF8, true))
            {
                contents = sr.ReadToEnd();
            }

            return contents;
        }

        #endregion Private Instance Methods      
    }
}
