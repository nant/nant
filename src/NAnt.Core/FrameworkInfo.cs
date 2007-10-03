// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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
// Ian MacLean (imaclean@gmail.com)

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Encalsulates information about installed frameworks incuding version 
    /// information and directory locations for finding tools.
    /// </summary>
    [Serializable()]
    public class FrameworkInfo : ISerializable {
        #region Private Instance Fields

        private readonly XmlNode _frameworkNode;
        private readonly XmlNamespaceManager _nsMgr;
        private readonly string _name;
        private readonly string _family;
        private readonly string _description;
        private Version _version;
        private Version _clrVersion;
        private DirectoryInfo _frameworkDirectory;
        private DirectoryInfo _sdkDirectory;
        private DirectoryInfo _frameworkAssemblyDirectory;
        private FileInfo _runtimeEngine;
        private Project _project;
        private EnvironmentVariableCollection _environmentVariables;
        private FileSet _taskAssemblies;
        private FileSet[] _referenceAssemblies;
        private InitStatus _status = InitStatus.Uninitialized;

        #endregion Private Instance Fields

        #region Internal Instance Constructors

        internal FrameworkInfo(XmlNode frameworkNode, XmlNamespaceManager nsMgr) {
            if (frameworkNode == null) {
                throw new ArgumentNullException("frameworkNode");
            }
            if (nsMgr == null) {
                throw new ArgumentNullException("nsMgr");
            }

            _frameworkNode = frameworkNode;
            _nsMgr = nsMgr;

            _name = GetXmlAttributeValue(frameworkNode, "name");
            _family = GetXmlAttributeValue(frameworkNode, "family");
            _description = GetXmlAttributeValue(_frameworkNode, "description");

            if (_name == null) {
                throw new ArgumentException("Invalid framework configuration.",
                    "name");
            }
            if (_family == null) {
                throw new ArgumentException("Invalid framework configuration.",
                    "family");
            }
            if (_description == null) {
                throw new ArgumentException("Invalid framework configuration.",
                    "description");
            }
        }

        #endregion Internal Instance Constructors

        #region Protected Instance Constructors

        protected FrameworkInfo(SerializationInfo info, StreamingContext context) {
            _name = info.GetString("Name");
            _family = info.GetString("Family");
            _description = info.GetString("Description");
            _version = (Version) info.GetValue("Version", typeof(Version));
            _clrVersion = (Version) info.GetValue("ClrVersion", typeof(Version));
            _frameworkDirectory = (DirectoryInfo) info.GetValue("FrameworkDirectory", typeof(DirectoryInfo));
            _sdkDirectory = (DirectoryInfo) info.GetValue("SdkDirectory", typeof(DirectoryInfo));
            _frameworkAssemblyDirectory = (DirectoryInfo) info.GetValue("FrameworkAssemblyDirectory", typeof(DirectoryInfo));
            _runtimeEngine = (FileInfo) info.GetValue("RuntimeEngine", typeof(FileInfo));
            _project = (Project) info.GetValue("Project", typeof(Project));
            _environmentVariables = (EnvironmentVariableCollection) info.GetValue("EnvironmentVariables", typeof(EnvironmentVariableCollection));
            _taskAssemblies = (FileSet) info.GetValue("TaskAssemblies", typeof(FileSet));
            _referenceAssemblies = (FileSet[]) info.GetValue("ReferenceAssemblies", typeof(FileSet[]));
            _status = (InitStatus) info.GetValue("Status", typeof(InitStatus));
        }

        #endregion Protected Instance Constructors

        #region Private Instance Constructors

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Name", Name);
            info.AddValue("Family", Family);
            if (IsValid) {
                info.AddValue("Description", Description);
                info.AddValue("Version", Version);
                info.AddValue("ClrVersion", ClrVersion);
                info.AddValue("FrameworkDirectory", FrameworkDirectory);
                info.AddValue("SdkDirectory", SdkDirectory);
                info.AddValue("FrameworkAssemblyDirectory", FrameworkAssemblyDirectory);
                info.AddValue("RuntimeEngine", RuntimeEngine);
                info.AddValue("Project", Project);
                info.AddValue("EnvironmentVariables", EnvironmentVariables);
                info.AddValue("TaskAssemblies", TaskAssemblies);
                info.AddValue("ReferenceAssemblies", ReferenceAssemblies);
            }
            info.AddValue("Status", _status);
        }

        #endregion Private Instance Constructors
              
        #region Public Instance Properties

        /// <summary>
        /// Gets the name of the framework.
        /// </summary>
        /// <value>
        /// The name of the framework.
        /// </value>
        public string Name {
            get { return _name; }
        }

        /// <summary>
        /// Gets the family of the framework.
        /// </summary>
        /// <value>
        /// The family of the framework.
        /// </value>
        public string Family {
            get { return _family; }
        }

        /// <summary>
        /// Gets the description of the framework.
        /// </summary>
        /// <value>
        /// The description of the framework.
        /// </value>
        public string Description {
            get { return _description; }
        }
        
        /// <summary>
        /// Gets the version of the framework.
        /// </summary>
        /// <value>
        /// The version of the framework.
        /// </value>
        public Version Version {
            get {
                if (_version == null) {
                    string version = Project.ExpandProperties(
                        GetXmlAttributeValue(_frameworkNode, "version"),
                        Location.UnknownLocation);
                    _version = new Version(version);
                }
                return _version; 
            }
        }

        /// <summary>
        /// Gets the Common Language Runtime version of the framework.
        /// </summary>
        /// <value>
        /// The Common Language Runtime version of the framework.
        /// </value>
        public Version ClrVersion {
            get {
                if (_clrVersion == null) {
                    _clrVersion = new Version(GetXmlAttributeValue(_frameworkNode, "clrversion"));
                }
                return _clrVersion; 
            }
        }
        
        /// <summary>
        /// Gets the Visual Studio version that corresponds with this
        /// framework.
        /// </summary>
        /// <remarks>
        /// The Visual Studio version that corresponds with this framework.
        /// </remarks>
        /// <exception cref="BuildException">There is no version of Visual Studio .NET that corresponds with this framework.</exception>
        public Version VisualStudioVersion {
            get {
                switch (ClrVersion.ToString(2)) {
                    case "1.0":
                        return new Version(7, 0);
                    case "1.1":
                        return new Version(7, 1);
                    case "2.0":
                        return new Version(8, 0);
                    default:
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            ResourceUtils.GetString("NA1055"),
                            Description), Location.UnknownLocation);
                }
            }
        }

        /// <summary>
        /// Gets the base directory of the framework tools for the framework.
        /// </summary>
        /// <value>
        /// The base directory of the framework tools for the framework.
        /// </value>
        public DirectoryInfo FrameworkDirectory {
            get {
                if (_frameworkDirectory == null) {
                    string frameworkDir = Project.ExpandProperties(
                        GetXmlAttributeValue(_frameworkNode, "frameworkdirectory"),
                        Location.UnknownLocation);
                    if (frameworkDir != null) {
                        // ensure the framework directory exists
                        if (Directory.Exists(frameworkDir)) {
                            _frameworkDirectory = new DirectoryInfo(frameworkDir);
                        } else {
                            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                "Framework directory '{0}' does not exist.", frameworkDir));
                        }
                    }
                }
                return _frameworkDirectory;
            }
        }
        /// <summary>
        /// Gets the path to the runtime engine for this framework.
        /// </summary>
        /// <value>
        /// The path to the runtime engine for the framework or <see langword="null" />
        /// if no runtime engine is configured for the framework.
        /// </value>
        public FileInfo RuntimeEngine {
            get {
                EnsureInit ();
                return _runtimeEngine; 
            }
        }
       
        /// <summary>
        /// Gets the directory where the system assemblies for the framework 
        /// are located.
        /// </summary>
        /// <value>
        /// The directory where the system assemblies for the framework are 
        /// located.
        /// </value>
        public DirectoryInfo FrameworkAssemblyDirectory {
            get {
                if (_frameworkAssemblyDirectory == null) {
                    string frameworkAssemblyDir = Project.ExpandProperties(
                        GetXmlAttributeValue(_frameworkNode, "frameworkassemblydirectory"),
                        Location.UnknownLocation);
                    if (frameworkAssemblyDir != null) {
                        // ensure the framework assembly directory exists
                        if (Directory.Exists(frameworkAssemblyDir)) {
                            // only consider framework assembly directory valid if an assembly
                            // named "System.dll" exists in that directory
                            if (!File.Exists(Path.Combine(frameworkAssemblyDir, "System.dll"))) {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                    ResourceUtils.GetString("NA1054"), frameworkAssemblyDir));
                            }
                            _frameworkAssemblyDirectory = new DirectoryInfo(frameworkAssemblyDir);
                        } else {
                            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                "Framework assembly directory '{0}' does not exist.", frameworkAssemblyDir));
                        }
                    }
                }
                return _frameworkAssemblyDirectory;
            }
        }

        /// <summary>
        /// Gets the directory containing the SDK tools for the framework.
        /// </summary>
        /// <value>
        /// The directory containing the SDK tools for the framework or a null 
        /// reference if the configured sdk directory does not exist, or is not
        /// valid.
        /// </value>
        public DirectoryInfo SdkDirectory {
            get {
                EnsureInit ();
                return _sdkDirectory;
            }
        }

        /// <summary>
        /// Gets the <see cref="Project" /> used to initialize this framework.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> used to initialize this framework.
        /// </value>
        public Project Project {
            get {
                EnsureInit ();
                return _project;
            }
        }

        /// <summary>
        /// Gets or sets the collection of environment variables that should be 
        /// passed to external programs that are launched in the runtime engine 
        /// of the current framework.
        /// </summary>
        /// <value>
        /// The collection of environment variables that should be passed to 
        /// external programs that are launched in the runtime engine of the
        /// current framework.
        /// </value>
        public EnvironmentVariableCollection EnvironmentVariables {
            get {
                if (_environmentVariables == null) {
                    // get framework-specific environment nodes
                    XmlNodeList environmentNodes = _frameworkNode.SelectNodes("nant:environment/nant:env", 
                        NamespaceManager);

                    // process framework environment nodes
                    _environmentVariables = ProcessFrameworkEnvironmentVariables(
                        environmentNodes);
                }
                return _environmentVariables; }
        }

        /// <summary>
        /// Gets the set of assemblies and directories that should scanned for
        /// NAnt tasks, types or functions.
        /// </summary>
        /// <value>
        /// The set of assemblies and directories that should be scanned for 
        /// NAnt tasks, types or functions.
        /// </value>
        public FileSet TaskAssemblies {
            get {
                if (_taskAssemblies == null) {
                    // process framework task assemblies
                    _taskAssemblies = new FileSet();
                    _taskAssemblies.Project = Project;
                    _taskAssemblies.NamespaceManager = NamespaceManager;
                    _taskAssemblies.Parent = Project; // avoid warnings by setting the parent of the fileset
                    _taskAssemblies.ID = "internal-task-assemblies"; // avoid warnings by assigning an id
                    XmlNode taskAssembliesNode = _frameworkNode.SelectSingleNode(
                        "nant:task-assemblies", NamespaceManager);
                    if (taskAssembliesNode != null) {
                        _taskAssemblies.Initialize(taskAssembliesNode, 
                            Project.Properties, this);
                    }
                }
                return _taskAssemblies;
            }
        }

        /// <summary>
        /// Returns a value indicating whether the current framework is valid.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the framework is installed and correctly
        /// configured; otherwise, <see langword="false" />.
        /// </value>
        public bool IsValid {
            get {
                try {
                    Validate ();
                    return true;
                } catch {
                    return false;
                }
            }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        internal FileSet [] ReferenceAssemblies {
            get {
                if (_referenceAssemblies == null) {
                    // reference assemblies
                    XmlNodeList referenceAssemblies = _frameworkNode.SelectNodes(
                        "nant:reference-assemblies", NamespaceManager);
                    _referenceAssemblies = new FileSet [referenceAssemblies.Count];
                    for (int i = 0; i < referenceAssemblies.Count; i++) {
                        XmlNode node = referenceAssemblies [i];
                        FileSet fileset = new FileSet();
                        fileset.Project = Project;
                        fileset.NamespaceManager = NamespaceManager;
                        fileset.Parent = Project;
                        fileset.ID = "reference-assemblies-" + i.ToString (CultureInfo.InvariantCulture);
                        fileset.Initialize(node, Project.Properties, this);
                        _referenceAssemblies [i] = fileset;
                    }
                }
                return _referenceAssemblies;
            }
        }

        #endregion Internal Instance Properties

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
        /// Resolves the specified assembly to a full path by matching it
        /// against the reference assemblies.
        /// </summary>
        /// <param name="fileName">The file name of the assembly to resolve (without path information).</param>
        /// <returns>
        /// An absolute path to the assembly, or <see langword="null" /> if the
        /// assembly could not be found.
        /// </returns>
        /// <remarks>
        /// Whether the file name is matched case-sensitive depends on the
        /// operating system.
        /// </remarks>
        public string ResolveAssembly (string fileName) {
            string resolvedAssembly = null;

            foreach (FileSet fileset in ReferenceAssemblies) {
                resolvedAssembly = fileset.Find (fileName);
                if (resolvedAssembly != null) {
                    break;
                }
            }
            return resolvedAssembly;
        }

        internal void Validate() {
            try {
                PerformValidation();
            } catch {
                _status = InitStatus.Invalid;
                throw;
            }
        }

        private void PerformValidation() {
            if (_status == InitStatus.Valid) {
                return;
            }

            EnsureInit();

            // verify is framework directory is configured, and indirectly
            // check if it exists
            if (FrameworkDirectory == null) {
                throw new ArgumentNullException("frameworkDir", string.Format(CultureInfo.InvariantCulture,
                    "Framework directory not configured for framework '{0}'.", Name));
            }

            // verify is framework assembly directory is configured, and 
            // indirectly check if it exists
            if (FrameworkAssemblyDirectory == null) {
                throw new ArgumentNullException("frameworkAssemblyDir", string.Format(CultureInfo.InvariantCulture,
                    "Framework assembly directory not configured for framework '{0}'.", Name));
            }

            _status = InitStatus.Valid;
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void EnsureInit() {
            if (_status != InitStatus.Uninitialized) {
                return;
            }
 
            // get framework-specific project node
            XmlNode projectNode = _frameworkNode.SelectSingleNode("nant:project", 
                NamespaceManager);

            if (projectNode == null) {
                throw new BuildException("<project> node has not been defined.");
            }

            string tempBuildFile = Path.GetTempFileName();
            XmlTextWriter writer = null;
            Project frameworkProject = null;

            try {
                // write project to file
                writer = new XmlTextWriter(tempBuildFile, Encoding.UTF8);
                writer.WriteStartDocument(true);
                writer.WriteRaw(projectNode.OuterXml);
                writer.Flush();
                writer.Close();

                // use StreamReader to load build file from to avoid
                // having location information as part of the error
                // messages
                using (StreamReader sr = new StreamReader(new FileStream(tempBuildFile, FileMode.Open, FileAccess.Read, FileShare.Write), Encoding.UTF8)) {
                    XmlDocument projectDoc = new XmlDocument();
                    projectDoc.Load(sr);

                    // create and execute project
                    frameworkProject = new Project(projectDoc);
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

            // if runtime engine is blank assume we aren't using one
            string runtimeEngine = frameworkProject.ExpandProperties(
                GetXmlAttributeValue(_frameworkNode, "runtimeengine"),
                Location.UnknownLocation);
            if (!StringUtils.IsNullOrEmpty(runtimeEngine)) {
                if (File.Exists(runtimeEngine)) {
                    _runtimeEngine = new FileInfo(runtimeEngine);
                } else {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, "Runtime engine '{0}' does not exist.", runtimeEngine));
                }
            }

            // the sdk directory does not actually have to exist for a
            // framework to be considered valid
            string sdkDir = frameworkProject.ExpandProperties(
                GetXmlAttributeValue(_frameworkNode, "sdkdirectory"),
                Location.UnknownLocation);
            if (sdkDir != null && Directory.Exists(sdkDir)) {
                _sdkDirectory = new DirectoryInfo(sdkDir);
            }

            _project = frameworkProject;
            _status = InitStatus.Initialized;
        }

        /// <summary>
        /// Processes the framework environment variables.
        /// </summary>
        /// <param name="environmentNodes">An <see cref="XmlNodeList" /> representing framework environment variables.</param>
        private EnvironmentVariableCollection ProcessFrameworkEnvironmentVariables(XmlNodeList environmentNodes) {
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
                environmentVariable.Parent = environmentVariable.Project = Project;
                environmentVariable.NamespaceManager = NamespaceManager;

                // configure using xml node
                environmentVariable.Initialize(environmentNode, Project.Properties,
                    this);

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

        private enum InitStatus {
            Uninitialized,
            Initialized,
            Invalid,
            Valid
        }
    }
}
