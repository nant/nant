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
        /// Holds a value indicating whether a scan for tasks has already been 
        /// performed on the configured task path.
        /// </summary>
        private static bool ScannedTaskPath = false;

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
            get { return Project.Properties;}	
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
                // todo pull a settings file out of the assembly resource and copy to that location
                Project.Log(Level.Warning, "NAnt settings not found. Defaulting to no known framework.");
                logger.Info("NAnt settings not found. Defaulting to no known framework.");
                return;
            }

            // process the framework-neutral properties
            ProcessFrameworkNeutralProperties(nantNode.SelectNodes("frameworks/properties/property"));

            // process the defined frameworks
            ProcessFrameworks(nantNode.SelectNodes("frameworks/platform[@name='" + Project.PlatformName + "']/framework"));

            // get taskpath setting to load external tasks and types from
            string taskPath = GetXmlAttributeValue(nantNode, "taskpath");

            if (taskPath != null && ScannedTaskPath == false) {
                string[] paths = taskPath.Split(';');

                foreach (string path in paths) {
                    string fullpath = path;

                    if (!Directory.Exists(path)) {
                        // try relative path 
                        fullpath = Path.GetFullPath(Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), path));
                    }

                    TypeFactory.ScanDir(fullpath);
                }

                ScannedTaskPath = true; // so we only load tasks once 
            }

            // determine default framework
            string defaultFramework = GetXmlAttributeValue(nantNode.SelectSingleNode(
                "frameworks/platform[@name='" + Project.PlatformName + "']"), 
                "default");

            if (defaultFramework != null && Project.FrameworkInfoDictionary.ContainsKey(defaultFramework)) {
                Properties.AddReadOnly("nant.settings.defaultframework", defaultFramework);
                Properties.Add("nant.settings.currentframework", defaultFramework);
                Project.DefaultFramework = Project.FrameworkInfoDictionary[defaultFramework];
                Project.CurrentFramework = Project.DefaultFramework;
            } else {
                Project.Log(Level.Warning, "Framework '{0}' does not exist or is not specified in the NAnt configuration file. Defaulting to no known framework.", defaultFramework);
            }

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
        /// Processes the framework nodes.
        /// </summary>
        /// <param name="frameworkNodes">An <see cref="XmlNodeList" /> representing supported frameworks.</param>
        private void ProcessFrameworks(XmlNodeList frameworkNodes) {
            //deals with xml info from the config file, not build document.
            foreach (XmlNode frameworkNode in frameworkNodes) {
                //skip special elements like comments, pis, text, etc.
                if (!(frameworkNode.NodeType == XmlNodeType.Element)) {
                    continue;
                }

                string name = null;

                try {
                    // get framework attributes
                    name = GetXmlAttributeValue(frameworkNode, "name");

                    string description = GetXmlAttributeValue(frameworkNode, "description");
                    string version = GetXmlAttributeValue(frameworkNode, "version");
                    string runtimeEngine = GetXmlAttributeValue(frameworkNode, "runtimeengine");
                    string frameworkDir = GetXmlAttributeValue(frameworkNode, "frameworkdirectory");
                    string frameworkAssemblyDir = GetXmlAttributeValue(frameworkNode, "frameworkassemblydirectory");
                    string sdkDir = GetXmlAttributeValue(frameworkNode, "sdkdirectory");

                    // get framework-specific property nodes
                    XmlNodeList propertyNodes = frameworkNode.SelectNodes("properties/property");

                    // process framework property nodes
                    PropertyDictionary frameworkProperties = ProcessFrameworkProperties(propertyNodes);

                    // create new FrameworkInfo instance, this will throw an
                    // an exception if the framework is not valid
                    FrameworkInfo info = new FrameworkInfo(name, 
                        description, 
                        version, 
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

                    // framework is valid, so add it to framework dictionary
                    Project.FrameworkInfoDictionary.Add(info.Name, info);
                } catch (Exception ex) {
                    string msg = string.Format(CultureInfo.InvariantCulture, 
                        "Framework {0} is invalid and has not been loaded : {1}", 
                        name, ex.Message);

                    Project.Log(Level.Verbose, msg);
                    logger.Info(msg, ex);
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
            frameworkProperties = new PropertyDictionary();

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
