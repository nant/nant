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
// Ian MacLean (ian_maclean@another.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core {
    internal class ProjectSettingsLoader {
        #region Private Instance Fields

        private Project _project;

        #endregion Private Instance Fields

        #region Private Static Fields

        /// <summary>
        /// Holds a value indicating whether a scan for extensions has already 
        /// been performed for the current runtime framework.
        /// </summary>
        private static bool ScannedExtensions = false;

        /// <summary>
        /// Holds the logger instance for this class.
        /// </summary>
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Internal Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectSettingsLoader" />
        /// class for the given <see cref="Project" />.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> that should be configured.</param>
        internal ProjectSettingsLoader(Project project) {
            _project = project;
        }

        #endregion Internal Instance Constructor

        #region Protected Instance Properties

        /// <summary>
        /// Gets the underlying <see cref="Project" /> instance.
        /// </summary>
        /// <value>
        /// The underlying <see cref="Project" /> instance.
        /// </value>
        protected Project Project {
            get { return _project; }
        }

        #endregion Protected Instance Properties

        #region Private Instance Properties

        private PropertyDictionary Properties {
            get { return Project.Properties; }
        }

        #endregion Private Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Loads and processes settings from the specified <see cref="XmlNode" /> 
        /// of the configuration file.
        /// </summary>
        public void ProcessSettings(XmlNode nantNode) {
            logger.Debug(string.Format(CultureInfo.InvariantCulture, "[{0}].ConfigFile '{1}'",AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));

            if (nantNode == null) { 
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "The NAnt configuration settings in file '{0}' could not be" +
                    " located.  Please ensure this file is available and contains" 
                    + " a 'nant' settings node."));
            }

            // process the framework-neutral properties
            ProcessFrameworkNeutralProperties(nantNode.SelectNodes("frameworks/properties/property"));

            // process the framework nodes of the current platform
            ProcessFrameworks(nantNode.SelectSingleNode("frameworks/platform[@name='" + Project.PlatformName + "']"));

            // only scan the extension assemblies for the runtime framework once
            if (!ScannedExtensions) {
                foreach (string extensionAssembly in Project.RuntimeFramework.Extensions.FileNames) {
                    TypeFactory.ScanAssembly(extensionAssembly);
                }

                foreach (string extensionDir in Project.RuntimeFramework.Extensions.DirectoryNames) {
                    TypeFactory.ScanDir(extensionDir);
                }

                // ensure we don't scan the extension assemblies for the current
                // runtime framework again
                ScannedExtensions = true;
            }

            // TO-DO : should we rename the <loadtasks> task to <load-extensions>
            // and have it scan not only for tasks but also for types and functions ?

            // process global properties
            ProcessGlobalProperties(nantNode.SelectNodes("properties/property"));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Reads the list of global properties specified in the NAnt configuration
        /// file.
        /// </summary>
        /// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing global properties.</param>
        private void ProcessGlobalProperties(XmlNodeList propertyNodes) {
            //deals with xml info from the config file, not build document.
            foreach (XmlNode propertyNode in propertyNodes) {
                //skip special elements like comments, pis, text, etc.
                if (!(propertyNode.NodeType == XmlNodeType.Element)) {
                    continue;	
                }

                string propertyName = GetXmlAttributeValue(propertyNode, "name");
                string propertyValue = GetXmlAttributeValue(propertyNode, "value");
                string propertyReadonly = GetXmlAttributeValue(propertyNode, "readonly");

                if (propertyReadonly != null && propertyReadonly == "true") {
                    Properties.AddReadOnly(propertyName, propertyValue);
                } else {
                    Properties[propertyName] = propertyValue;
                }
            }
        }

        /// <summary>
        /// Reads the list of framework-neutral properties defined in the 
        /// NAnt configuration file.
        /// </summary>
        /// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing framework-neutral properties.</param>
        private void ProcessFrameworkNeutralProperties(XmlNodeList propertyNodes) {
            //deals with xml info from the config file, not build document.
            foreach (XmlNode propertyNode in propertyNodes) {
                //skip elements like comments, pis, text, etc.
                if (!(propertyNode.NodeType == XmlNodeType.Element)) {
                    continue;	
                }

                string propertyName = GetXmlAttributeValue(propertyNode, "name");
                string propertyValue = GetXmlAttributeValue(propertyNode, "value");

                if (propertyName == null) {
                    throw new ArgumentException("A framework-neutral property should at least have a name.");
                }

                if (propertyValue != null) {
                    // expand properties in property value
                    propertyValue = Project.FrameworkNeutralProperties.ExpandProperties(propertyValue, Location.UnknownLocation);

                    // add read-only property to collection of framework-neutral properties
                    Project.FrameworkNeutralProperties.AddReadOnly(propertyName, propertyValue);
                }
            }
        }

        /// <summary>
        /// Processes the framework nodes of the given platform node.
        /// </summary>
        /// <param name="platformNode">An <see cref="XmlNode" /> representing the platform on which NAnt is running.</param>
        private void ProcessFrameworks(XmlNode platformNode) {
            // determine the framework family name
            string frameworkFamily = PlatformHelper.IsMono ? "mono" : "net";
            // determine the version of the current runtime framework
            string frameworkClrVersion = Environment.Version.ToString(3);
            // determine default targetframework
            string defaultTargetFramework = GetXmlAttributeValue(platformNode, "default");

            // deals with xml info from the config file, not build document.
            foreach (XmlNode frameworkNode in platformNode.SelectNodes("framework")) {
                // skip special elements like comments, pis, text, etc.
                if (!(frameworkNode.NodeType == XmlNodeType.Element)) {
                    continue;
                }

                string name = null;
                bool isRuntimeFramework = false;

                try {
                    // get framework attributes
                    name = GetXmlAttributeValue(frameworkNode, "name");

                    string description = GetXmlAttributeValue(frameworkNode, "description");
                    string family = GetXmlAttributeValue(frameworkNode, "family");
                    string version = GetXmlAttributeValue(frameworkNode, "version");
                    string clrVersion = GetXmlAttributeValue(frameworkNode, "clrversion");
                    string runtimeEngine = GetXmlAttributeValue(frameworkNode, "runtimeengine");
                    string frameworkDir = GetXmlAttributeValue(frameworkNode, "frameworkdirectory");
                    string frameworkAssemblyDir = GetXmlAttributeValue(frameworkNode, "frameworkassemblydirectory");
                    string sdkDir = GetXmlAttributeValue(frameworkNode, "sdkdirectory");

                    // get framework-specific property nodes
                    XmlNodeList propertyNodes = frameworkNode.SelectNodes("properties/property");

                    // process framework property nodes
                    PropertyDictionary frameworkProperties = ProcessFrameworkProperties(propertyNodes);

                    // expanded properties in framework attribute values
                    name = frameworkProperties.ExpandProperties(name, Location.UnknownLocation);
                    description = frameworkProperties.ExpandProperties(description, Location.UnknownLocation);
                    version = frameworkProperties.ExpandProperties(version, Location.UnknownLocation);
                    clrVersion = frameworkProperties.ExpandProperties(clrVersion, Location.UnknownLocation);
                    frameworkDir = frameworkProperties.ExpandProperties(frameworkDir, Location.UnknownLocation);
                    frameworkAssemblyDir = frameworkProperties.ExpandProperties(frameworkAssemblyDir, Location.UnknownLocation);

                    try {
                        sdkDir = frameworkProperties.ExpandProperties(sdkDir, Location.UnknownLocation);
                    } catch (BuildException) {
                        // do nothing with this exception as a framework is still
                        // considered valid if the sdk directory is not available
                        // or not configured correctly
                    }

                    // check if we're processing the current runtime framework
                    if (family == frameworkFamily && clrVersion == frameworkClrVersion) {
                        isRuntimeFramework = true;
                    }

                    // create new FrameworkInfo instance, this will throw an
                    // an exception if the framework is not valid
                    FrameworkInfo info = new FrameworkInfo(name, 
                        family,
                        description, 
                        version, 
                        clrVersion,
                        frameworkDir, 
                        sdkDir, 
                        frameworkAssemblyDir, 
                        runtimeEngine, 
                        frameworkProperties);

                    // get framework-specific environment nodes
                    XmlNodeList environmentNodes = frameworkNode.SelectNodes("environment/env");

                    // process framework environment nodes
                    info.EnvironmentVariables = ProcessFrameworkEnvironmentVariables(
                        environmentNodes, info);

                    // process framework extensions
                    info.Extensions.Project = Project;
                    info.Extensions.Parent = Project; // avoid warnings by setting the parent of the fileset
                    info.Extensions.ID = "extensions"; // avoid warnings by assigning an id
                    XmlNode extensionsNode = frameworkNode.SelectSingleNode("extensions");
                    if (extensionsNode != null) {
                        info.Extensions.Initialize(extensionsNode, info.Properties, info);
                    }

                    // framework is valid, so add it to framework dictionary
                    Project.FrameworkInfoDictionary.Add(info.Name, info);

                    if (isRuntimeFramework) {
                        // framework matches current runtime, so set it as 
                        // current target framework
                        Project.RuntimeFramework = Project.TargetFramework = info;
                    }
                } catch (Exception ex) {
                    if (isRuntimeFramework) {
                        // current runtime framework is not correctly configured
                        // in NAnt configuration file
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "The current runtime framework '{0}' is not correctly" 
                            + " configured in the NAnt configuration file.", 
                            name, ex));
                    } else {
                        if (name != null && name == defaultTargetFramework) {
                            Project.Log(Level.Warning, "The default targetframework" +
                                " '{0}' is invalid and has not been loaded : {1}", 
                                name, ex.Message);
                        } else {
                            Project.Log(Level.Verbose, "Framework '{0}' is invalid" 
                                + " and has not been loaded : {1}", name, ex.Message);
                        }
                    }
                }
            }

            if (Project.RuntimeFramework == null) {
                // information about the current runtime framework should
                // be added to the NAnt configuration file
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The NAnt configuration file does not contain a framework" 
                    + " definition for the current runtime framework (family '{0}'" 
                    + ", clrversion '{1}').", frameworkFamily, frameworkClrVersion));
            }

            if (defaultTargetFramework != null && defaultTargetFramework != "auto") {
                if (Project.FrameworkInfoDictionary.ContainsKey(defaultTargetFramework)) {
                    Project.TargetFramework = Project.FrameworkInfoDictionary[defaultTargetFramework];
                } else {
                    Project.Log(Level.Warning, "The default targetframework" +
                        " '{0}' is not valid. Defaulting to the runtime framework" 
                        + " ({1}).", Project.RuntimeFramework.Name);
                }
            }
        }

        /// <summary>
        /// Processes the framework properties.
        /// </summary>
        /// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing framework properties.</param>
        private PropertyDictionary ProcessFrameworkProperties(XmlNodeList propertyNodes) {
            PropertyDictionary frameworkProperties = null;

            // initialize framework-specific properties
            frameworkProperties = new PropertyDictionary(Project);

            // inject framework-neutral properties
            frameworkProperties.Inherit(Project.FrameworkNeutralProperties, (StringCollection)null);

            foreach (XmlNode propertyNode in propertyNodes) {
                //skip non-nant namespace elements and special elements like comments, pis, text, etc.
                if (!(propertyNode.NodeType == XmlNodeType.Element)) {
                    continue;	
                }

                string propertyName = GetXmlAttributeValue(propertyNode, "name");

                // make sure property has atleast a name
                if (propertyName == null) {
                    throw new ArgumentException("A framework property should at least have a name.");
                }

                string propertyValue = null;

                if (GetXmlAttributeValue(propertyNode, "useregistry") == "true") {
                    string regKey = GetXmlAttributeValue(propertyNode, "regkey");

                    if (regKey == null) {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Framework property {0} is configured to be read from the registry but has no regkey attribute set.", propertyName));
                    } else {
                        // expand properties in regkey
                        regKey = frameworkProperties.ExpandProperties(regKey, Location.UnknownLocation);
                    }

                    string regValue = GetXmlAttributeValue(propertyNode, "regvalue");

                    if (regValue == null) {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Framework property {0} is configured to be read from the registry but has no regvalue attribute set.", propertyName));
                    } else {
                        // expand properties in regvalue
                        regValue = frameworkProperties.ExpandProperties(regValue, Location.UnknownLocation);
                    }

                    RegistryKey sdkKey = Registry.LocalMachine.OpenSubKey(regKey);

                    if (sdkKey != null && sdkKey.GetValue(regValue) != null) {
                        propertyValue = sdkKey.GetValue(regValue).ToString();
                    }
                } else {
                    propertyValue = GetXmlAttributeValue(propertyNode, "value");
                }

                if (propertyValue != null) {
                    // expand properties in property value
                    propertyValue = frameworkProperties.ExpandProperties(propertyValue, Location.UnknownLocation);

                    // add read-only property to collection of framework properties
                    frameworkProperties.AddReadOnly(propertyName, propertyValue);
                }
            }

            return frameworkProperties;
        }

        /// <summary>
        /// Processes the framework environment variables.
        /// </summary>
        /// <param name="environmentNodes">An <see cref="XmlNodeList" /> representing framework environment variables.</param>
        /// <param name="framework">The <see cref="FrameworkInfo" /> to obtain framework-specific information from.</param>
        private EnvironmentVariableCollection ProcessFrameworkEnvironmentVariables(XmlNodeList environmentNodes, FrameworkInfo framework) {
            EnvironmentVariableCollection frameworkEnvironment = null;

            // initialize framework-specific environment variables
            frameworkEnvironment = new EnvironmentVariableCollection();

            foreach (XmlNode environmentNode in environmentNodes) {
                //skip non-nant namespace elements and special elements like comments, pis, text, etc.
                if (!(environmentNode.NodeType == XmlNodeType.Element)) {
                    continue;	
                }

                // initialize element
                EnvironmentVariable environmentVariable = new EnvironmentVariable();
                environmentVariable.Project = Project;

                // configure using xml node
                environmentVariable.Initialize(environmentNode, framework.Properties, framework);

                // add to collection of environment variables
                frameworkEnvironment.Add(environmentVariable);
            }

            return frameworkEnvironment;
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Gets the value of the specified attribute from the specified node.
        /// </summary>
        /// <param name="xmlNode">The node of which the attribute value should be retrieved.</param>
        /// <param name="attributeName">The attribute of which the value should be returned.</param>
        /// <returns>
        /// The value of the attribute with the specified name or <see langword="null" />
        /// if the attribute does not exist or has no value.
        /// </returns>
        private static string GetXmlAttributeValue(XmlNode xmlNode, string attributeName) {
            string attributeValue = null;

            if (xmlNode != null) {
                XmlAttribute xmlAttribute = (XmlAttribute)xmlNode.Attributes.GetNamedItem(attributeName);

                if (xmlAttribute != null) {
                    attributeValue = StringUtils.ConvertEmptyToNull(xmlAttribute.Value);
                }
            }

            return attributeValue;
        }

        #endregion Private Static Methods
    }
}
