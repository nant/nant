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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.IO;
using System.Globalization;
using System.Xml;

using NUnit.Framework;

using NAnt.Core;
using Tests.NAnt.Core.Util;

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

            // execute build
            RunBuild(string.Format(CultureInfo.InvariantCulture, 
                _projectXml, xmlPokeTaskAttributes, xmlPeekTaskAttributes,
                "${configuration.server}"));
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

        #endregion Public Instance Methods
    }
}
