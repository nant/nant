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
        private XmlNamespaceManager _nsMgr;

        #endregion Private Instance Fields

        #region Private Static Fields

        /// <summary>
        /// Holds a value indicating whether a scan for tasks, types and functions
        /// has already been performed for the current runtime framework.
        /// </summary>
        private static bool ScannedTasks = false;

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

            // setup namespace manager
            _nsMgr = new XmlNamespaceManager(new NameTable());
            _nsMgr.AddNamespace("nant", _nsMgr.DefaultNamespace);
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

        /// <summary>
        /// Gets the <see cref="XmlNamespaceManager" />.
        /// </summary>
        /// <value>
        /// The <see cref="XmlNamespaceManager" />.
        /// </value>
        /// <remarks>
        /// The <see cref="NamespaceManager" /> defines the current namespace 
        /// scope and provides methods for looking up namespace information.
        /// </remarks>
        private XmlNamespaceManager NamespaceManager {
            get { return _nsMgr; }
        }

        #endregion Private Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Loads and processes settings from the specified <see cref="XmlNode" /> 
        /// of the configuration file.
        /// </summary>
        public void ProcessSettings() {
            if (Project.ConfigurationNode == null) {
                return;
            }

            // process platform configuration
            ProcessPlatform(Project.ConfigurationNode.SelectSingleNode(
                "nant:frameworks/nant:platform[@name='" + Project.PlatformName + "']",
                NamespaceManager));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void ProcessPlatform(XmlNode platformNode) {
            // process platform task assemblies
            FileSet platformTaskAssemblies = new FileSet();
            platformTaskAssemblies.BaseDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            platformTaskAssemblies.Project = Project;
            platformTaskAssemblies.NamespaceManager = NamespaceManager;
            platformTaskAssemblies.Parent = Project; // avoid warnings by setting the parent of the fileset
            platformTaskAssemblies.ID = "platform-task-assemblies"; // avoid warnings by assigning an id
            XmlNode taskAssembliesNode = platformNode.SelectSingleNode(
                "nant:task-assemblies", NamespaceManager);
            if (taskAssembliesNode != null) {
                platformTaskAssemblies.Initialize(taskAssembliesNode, 
                    Project.Properties, null);
            }

            if (!ScannedTasks) {
                foreach (string taskAssembly in platformTaskAssemblies.FileNames) {
                    TypeFactory.ScanAssembly(taskAssembly);
                }

                foreach (string taskDir in platformTaskAssemblies.DirectoryNames) {
                    TypeFactory.ScanDir(taskDir);
                }
            }

            // process the framework nodes of the current platform
            ProcessFrameworks(platformNode);

            // only scan the task assemblies for the runtime framework once
            if (!ScannedTasks) {
                /*
                foreach (string scannedDir in Project.RuntimeFramework.TaskAssemblies.ScannedDirectories) {
                    // check if directory is subdirectory of AppDomain base dir
                    if (scannedDir.StartsWith(AppDomain.CurrentDomain.BaseDirectory)) {
                        string relPath = scannedDir.Remove(0, 
                            AppDomain.CurrentDomain.BaseDirectory.Length);
                        if (relPath.StartsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture))) {
                            relPath = relPath.Remove(0, 1);
                        }

                        if (relPath.Length != 0) {
                            AppDomain.CurrentDomain.AppendPrivatePath(relPath);
                        }
                    }
                }
                */

                foreach (string taskAssembly in Project.RuntimeFramework.TaskAssemblies.FileNames) {
                    TypeFactory.ScanAssembly(taskAssembly);
                }

                foreach (string taskDir in Project.RuntimeFramework.TaskAssemblies.DirectoryNames) {
                    TypeFactory.ScanDir(taskDir);
                }

                // ensure we don't scan the task assemblies for the current
                // runtime framework again
                ScannedTasks = true;
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
            foreach (XmlNode frameworkNode in platformNode.SelectNodes("nant:framework", NamespaceManager)) {
                // skip special elements like comments, pis, text, etc.
                if (!(frameworkNode.NodeType == XmlNodeType.Element)) {
                    continue;
                }

                string name = null;
                bool isRuntimeFramework = false;

                try {
                    // get framework attributes
                    name = GetXmlAttributeValue(frameworkNode, "name");

                    string family = GetXmlAttributeValue(frameworkNode, "family");
                    string clrVersion = GetXmlAttributeValue(frameworkNode, "clrversion");

                    // check if we're processing the current runtime framework
                    if (family == frameworkFamily && clrVersion == frameworkClrVersion) {
                        isRuntimeFramework = true;
                    }

                    // get framework-specific project node
                    XmlNode projectNode = frameworkNode.SelectSingleNode("nant:project", 
                        NamespaceManager);

                    if (projectNode == null) {
                        throw new BuildException("<project> node has not been defined.");
                    }

                    string tempBuildFile = Path.GetTempFileName();
                    XmlTextWriter writer = null;
                    Project frameworkProject = null;

                    try {
                        writer = new XmlTextWriter(tempBuildFile, Encoding.UTF8);
                        writer.WriteStartDocument(true);
                        writer.WriteRaw(projectNode.OuterXml);
                        writer.Flush();
                        writer.Close();

                        using (StreamReader sr = new StreamReader(new FileStream(tempBuildFile, FileMode.Open, FileAccess.Read, FileShare.Write), Encoding.UTF8)) {
                            XmlDocument projectDoc = new XmlDocument();
                            projectDoc.Load(sr);

                            frameworkProject = new Project(projectDoc, Level.None, 0, (XmlNode) null);
                            frameworkProject.BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                            frameworkProject.Execute();
                        }
                    } finally {
                        if (writer != null) {
                            writer.Close();
                        }

                        if (File.Exists(tempBuildFile)) {
                            File.Delete(tempBuildFile);
                        }
                    }

                    string description = frameworkProject.ExpandProperties(
                        GetXmlAttributeValue(frameworkNode, "description"),
                        Location.UnknownLocation);
                    string version = frameworkProject.ExpandProperties(
                        GetXmlAttributeValue(frameworkNode, "version"),
                        Location.UnknownLocation);
                    string runtimeEngine = frameworkProject.ExpandProperties(
                        GetXmlAttributeValue(frameworkNode, "runtimeengine"),
                        Location.UnknownLocation);
                    string frameworkDir = frameworkProject.ExpandProperties(
                        GetXmlAttributeValue(frameworkNode, "frameworkdirectory"),
                        Location.UnknownLocation);
                    string frameworkAssemblyDir = frameworkProject.ExpandProperties(
                        GetXmlAttributeValue(frameworkNode, "frameworkassemblydirectory"),
                        Location.UnknownLocation);
                    string sdkDir = GetXmlAttributeValue(frameworkNode, "sdkdirectory");

                    try {
                        sdkDir = frameworkProject.ExpandProperties(sdkDir, 
                            Location.UnknownLocation);
                    } catch (BuildException) {
                        // do nothing with this exception as a framework is still
                        // considered valid if the sdk directory is not available
                        // or not configured correctly
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
                        frameworkProject);

                    // get framework-specific environment nodes
                    XmlNodeList environmentNodes = frameworkNode.SelectNodes("nant:environment/nant:env", 
                        NamespaceManager);

                    // process framework environment nodes
                    info.EnvironmentVariables = ProcessFrameworkEnvironmentVariables(
                        environmentNodes, info);

                    // process framework task assemblies
                    info.TaskAssemblies.Project = frameworkProject;
                    info.TaskAssemblies.NamespaceManager = NamespaceManager;
                    info.TaskAssemblies.Parent = frameworkProject; // avoid warnings by setting the parent of the fileset
                    info.TaskAssemblies.ID = "internal-task-assemblies"; // avoid warnings by assigning an id
                    XmlNode taskAssembliesNode = frameworkNode.SelectSingleNode(
                        "nant:task-assemblies", NamespaceManager);
                    if (taskAssembliesNode != null) {
                        info.TaskAssemblies.Initialize(taskAssembliesNode, 
                            frameworkProject.Properties, info);
                    }

                    // framework is valid, so add it to framework dictionary
                    Project.Frameworks.Add(info.Name, info);

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
                            name), ex);
                    } else {
                        if (name != null && name == defaultTargetFramework) {
                            Project.Log(Level.Warning, "The default targetframework" +
                                " '{0}' is invalid and has not been loaded : {1}", 
                                name, ex.Message);
                            Project.Log(Level.Debug, ex.ToString());
                            Project.Log(Level.Warning, "");
                        } else {
                            Project.Log(Level.Verbose, "Framework '{0}' is invalid" 
                                + " and has not been loaded : {1}", name, ex.Message);
                            Project.Log(Level.Debug, ex.ToString());
                            Project.Log(Level.Verbose, "");
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
                if (Project.Frameworks.ContainsKey(defaultTargetFramework)) {
                    Project.TargetFramework = Project.Frameworks[defaultTargetFramework];
                } else {
                    Project.Log(Level.Warning, "The default targetframework" +
                        " '{0}' is not valid. Defaulting to the runtime framework" 
                        + " ({1}).", defaultTargetFramework, Project.RuntimeFramework.Name);
                    Project.Log(Level.Warning, "");
                }
            }
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
                // skip non-nant namespace elements and special elements like comments, pis, text, etc.
                if (!(environmentNode.NodeType == XmlNodeType.Element)) {
                    continue;
                }

                // initialize element
                EnvironmentVariable environmentVariable = new EnvironmentVariable();
                environmentVariable.Project = framework.Project;
                environmentVariable.NamespaceManager = NamespaceManager;

                // configure using xml node
                environmentVariable.Initialize(environmentNode, framework.Project.Properties, 
                    framework);

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
