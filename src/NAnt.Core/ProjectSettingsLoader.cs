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
// Ian MacLean (imaclean@gmail.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using NAnt.Core.Tasks;
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
        private static bool ScannedTasks;

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

            // process global properties
            ProcessGlobalProperties(Project.ConfigurationNode.SelectNodes(
                "nant:properties/nant:property", NamespaceManager));
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void ProcessPlatform(XmlNode platformNode) {
            if (platformNode == null) {
                throw new ArgumentNullException("platformNode");
            }

            // process platform task assemblies
            if (!ScannedTasks) {
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

                // scan platform extensions assemblies
                LoadTasksTask loadTasks = new LoadTasksTask();
                loadTasks.Project = Project;
                loadTasks.NamespaceManager = NamespaceManager;
                loadTasks.Parent = Project;
                loadTasks.TaskFileSet = platformTaskAssemblies;
                loadTasks.FailOnError = false;
                loadTasks.Threshold = (Project.Threshold == Level.Debug) ? 
                    Level.Debug : Level.Warning;
                loadTasks.Execute();

                // scan NAnt.Core
                TypeFactory.ScanAssembly(Assembly.GetExecutingAssembly(), loadTasks);
            }

            // process the framework nodes of the current platform
            ProcessFrameworks(platformNode);

            // configure the runtime framework
            Project.RuntimeFramework = ConfigureRuntimeFramework();

            // configure the default target framework
            Project.TargetFramework = ConfigureTargetFramework(platformNode);

            // process runtime framework task assemblies
            if (!ScannedTasks) {
                LoadTasksTask loadTasks = new LoadTasksTask();
                loadTasks.Project = Project;
                loadTasks.NamespaceManager = NamespaceManager;
                loadTasks.Parent = Project;
                loadTasks.TaskFileSet = Project.RuntimeFramework.TaskAssemblies;
                loadTasks.FailOnError = false;
                loadTasks.Threshold = (Project.Threshold == Level.Debug) ? 
                    Level.Debug : Level.Warning;
                loadTasks.Execute();

                // ensure we don't scan task assemblies for the current
                // runtime framework and platform again
                ScannedTasks = true;
            }
        }

        /// <summary>
        /// Processes the framework nodes of the given platform node.
        /// </summary>
        /// <param name="platformNode">An <see cref="XmlNode" /> representing the platform on which NAnt is running.</param>
        private void ProcessFrameworks(XmlNode platformNode) {
            if (platformNode == null) {
                throw new ArgumentNullException("platformNode");
            }

            // deals with xml info from the config file, not build document.
            foreach (XmlNode frameworkNode in platformNode.SelectNodes("nant:framework", NamespaceManager)) {
                // skip special elements like comments, pis, text, etc.
                if (!(frameworkNode.NodeType == XmlNodeType.Element)) {
                    continue;
                }

                FrameworkInfo framework = new FrameworkInfo(frameworkNode,
                    NamespaceManager);

                // add framework before it's considered valid, since we
                // want to inform users of possible configuration or
                // installation issues when they explicitly target that
                // framework
                Project.Frameworks.Add(framework.Name, framework);
            }
        }

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

                // initialize task
                PropertyTask propertyTask = new PropertyTask();
                propertyTask.Parent = propertyTask.Project = Project;
                propertyTask.NamespaceManager = NamespaceManager;
                propertyTask.InitializeTaskConfiguration();
                // configure using xml node
                propertyTask.Initialize(propertyNode);
                // execute task
                propertyTask.Execute();
            }
        }

        private FrameworkInfo ConfigureRuntimeFramework() {
            ArrayList candidates = new ArrayList();

            // determine the framework family name
            string frameworkFamily = PlatformHelper.IsMono ? "mono" : "net";
            // determine the version of the current runtime framework
            Version frameworkClrVersion = new Version(Environment.Version.ToString(3));

            // determine which framework configuration matches the host CLR
            foreach (FrameworkInfo framework in Project.Frameworks) {
                if (framework.Family != frameworkFamily)
                    continue;
                if (framework.ClrVersion != frameworkClrVersion) {
                    continue;
                }
                candidates.Add(framework);
            }

            FrameworkInfo selected = null;

            for (int i = 0; i < candidates.Count; i++) {
                FrameworkInfo current = (FrameworkInfo) candidates[i];
                try {
                    // validate
                    current.Validate();
                    selected = current;
                    if (selected.SdkDirectory != null) {
                        // if we found a matching framework with a valid
                        // SDK, then skip further candidates
                        break;
                    }
                } catch {
                    // only rethrow exception if we haven't yet found a valid
                    // framework and we're dealing with the last candidate
                    if (selected == null && i == (candidates.Count -1)) {
                        throw;
                    }
                }
            }

            if (selected == null) {
                // information about the current runtime framework should
                // be added to the NAnt configuration file
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1062"), frameworkFamily, 
                    frameworkClrVersion.ToString()));
            }

            return selected;
        }

        private FrameworkInfo ConfigureTargetFramework(XmlNode platformNode) {
            // determine default targetframework
            string defaultTargetFramework = GetXmlAttributeValue(platformNode, "default");

            if (defaultTargetFramework == null || defaultTargetFramework == "auto") {
                // no need to validate the framework since this was done in
                // ConfigureRuntimeFramework
                return Project.RuntimeFramework;
            }

            FrameworkInfo framework = Project.Frameworks [defaultTargetFramework];
            if (framework == null) {
                Project.Log(Level.Warning, ResourceUtils.GetString("NA1178"),
                    defaultTargetFramework, Project.RuntimeFramework.Name);
                Project.Log(Level.Warning, "");
                return null;
            }

            return framework;
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
