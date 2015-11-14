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
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

using NAnt.Core.Configuration;
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
        private ClrType _clrType;
        private VendorType _vendor;
        private DirectoryInfo _frameworkDirectory;
        private DirectoryInfo _sdkDirectory;
        private DirectoryInfo _frameworkAssemblyDirectory;
        private Runtime _runtime;
        private Project _project;
        private FileSet _taskAssemblies;
        private FileSet[] _referenceAssemblies;
        private string[] _toolPaths;
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
            if (_name == null) {
                throw new ArgumentException("The \"name\" attribute does not " +
                    "exist, or has no value.");
            }

            _family = GetXmlAttributeValue(frameworkNode, "family");
            if (_family == null) {
                throw new ArgumentException("The \"family\" attribute does " +
                    "not exist, or has no value.");
            }

            _description = GetXmlAttributeValue(_frameworkNode, "description");
            if (_description == null) {
                throw new ArgumentException("The \"description\" attribute " +
                    "does not exist, or has no value.");
            }

            string vendor  = GetXmlAttributeValue(_frameworkNode, "vendor");
            if (vendor == null) {
                throw new ArgumentException("The \"vendor\" attribute does " +
                    "not exist, or has no value.");
            }

            try {
                _vendor = (VendorType) Enum.Parse(typeof (VendorType),
                    vendor, true);
            } catch (Exception ex) {
                throw new ArgumentException("The value of the \"vendor\" " +
                    "attribute is not valid.", ex);
            }
        }

        #endregion Internal Instance Constructors

        #region Protected Instance Constructors

        protected FrameworkInfo(SerializationInfo info, StreamingContext context) {
            _name = info.GetString("Name");
            _family = info.GetString("Family");
            _description = info.GetString("Description");
            _status = (InitStatus) info.GetValue("Status", typeof(InitStatus));
            _clrType = (ClrType) info.GetValue("ClrType", typeof(ClrType));
            _version = (Version) info.GetValue("Version", typeof(Version));
            _clrVersion = (Version) info.GetValue("ClrVersion", typeof(Version));
            _vendor = (VendorType) info.GetValue("Vendor", typeof(VendorType));
            if (_status != InitStatus.Valid) {
                return;
            }

            _frameworkDirectory = (DirectoryInfo) info.GetValue("FrameworkDirectory", typeof(DirectoryInfo));
            _sdkDirectory = (DirectoryInfo) info.GetValue("SdkDirectory", typeof(DirectoryInfo));
            _frameworkAssemblyDirectory = (DirectoryInfo) info.GetValue("FrameworkAssemblyDirectory", typeof(DirectoryInfo));
            _runtime = (Runtime) info.GetValue("Runtime", typeof(Runtime));
            _project = (Project) info.GetValue("Project", typeof(Project));
            _taskAssemblies = (FileSet) info.GetValue("TaskAssemblies", typeof(FileSet));
            _referenceAssemblies = (FileSet[]) info.GetValue("ReferenceAssemblies", typeof(FileSet[]));
            _toolPaths = (string[]) info.GetValue("ToolPaths", typeof(string[]));
        }

        #endregion Protected Instance Constructors

        #region Private Instance Constructors

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("Name", Name);
            info.AddValue("Family", Family);
            info.AddValue("Description", Description);
            info.AddValue("ClrType", ClrType);
            info.AddValue("Version", Version);
            info.AddValue("ClrVersion", ClrVersion);
            info.AddValue("Status", _status);
            info.AddValue("Vendor", Vendor);
            if (IsValid) {
                info.AddValue("FrameworkDirectory", FrameworkDirectory);
                info.AddValue("SdkDirectory", SdkDirectory);
                info.AddValue("FrameworkAssemblyDirectory", FrameworkAssemblyDirectory);
                info.AddValue("Runtime", Runtime);
                info.AddValue("Project", Project);
                info.AddValue("TaskAssemblies", TaskAssemblies);
                info.AddValue("ReferenceAssemblies", ReferenceAssemblies);
                info.AddValue("ToolPaths", ToolPaths);
            }
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
        /// Gets the vendor of the framework.
        /// </summary>
        /// <value>
        /// The vendor of the framework.
        /// </value>
        internal VendorType Vendor {
            get { return _vendor; }
        }

        /// <summary>
        /// Gets the version of the framework.
        /// </summary>
        /// <value>
        /// The version of the framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        /// <remarks>
        /// When <see cref="Version" /> is not configured, the framework is not
        /// considered valid.
        /// </remarks>
        public Version Version {
            get {
                if (_version == null) {
                    if (_frameworkNode == null) {
                        throw new ArgumentException("The current framework " +
                            "is not valid.");
                    }

                    string version = GetXmlAttributeValue(_frameworkNode,
                        "version");
                    if (version != null) {
                        _version = new Version(version);
                    }
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
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        /// <remarks>
        /// When <see cref="ClrVersion" /> is <see langword="null" />, the
        /// framework is not considered valid.
        /// </remarks>
        public Version ClrVersion {
            get {
                if (_clrVersion == null) {
                    if (_frameworkNode == null) {
                        throw new ArgumentException("The current framework " +
                            "is not valid.");
                    }

                    string clrVersion = GetXmlAttributeValue(_frameworkNode,
                        "clrversion");
                    if (clrVersion != null) {
                        _clrVersion = new Version(clrVersion);
                    }
                }
                return _clrVersion; 
            }
        }

        /// <summary>
        /// Gets the CLR type of the framework.
        /// </summary>
        /// <value>
        /// The CLR type of the framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        public ClrType ClrType {
            get {
                if (_clrType == 0) {
                    if (_frameworkNode == null) {
                        throw new ArgumentException("The current framework " +
                            "is not valid.");
                    }

                    string clrType = GetXmlAttributeValue(_frameworkNode, "clrtype");
                    if (clrType != null) {
                        try {
                            _clrType = (ClrType) Enum.Parse(typeof (ClrType),
                                clrType, true);
                        } catch (Exception ex) {
                            throw new ArgumentException("The value of the \"clrtype\" " +
                                "attribute is not valid.", ex);
                        }
                    }
                }

                return _clrType;
            }
        }

        /// <summary>
        /// Gets the Visual Studio version that corresponds with this
        /// framework.
        /// </summary>
        /// <value>
        /// The Visual Studio version that corresponds with this framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        /// <exception cref="BuildException">There is no version of Visual Studio that corresponds with this framework.</exception>
        public Version VisualStudioVersion {
            get {
                if (ClrVersion == null) {
                    throw new ArgumentException("The current framework " +
                        "is not valid.");
                }

                switch (ClrVersion.ToString(2)) {
                    case "1.0":
                        return new Version(7, 0);
                    case "1.1":
                        return new Version(7, 1);
                    case "2.0":
                        return new Version(8, 0);
                    case "4.0":
                        return new Version(10, 0);
                    case "4.5":
                        return new Version(11, 0);
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
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        public DirectoryInfo FrameworkDirectory {
            get {
                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

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
        /// Gets the runtime information for this framework.
        /// </summary>
        /// <value>
        /// The runtime information for the framework or <see langword="null" />
        /// if no runtime information is configured for the framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        internal Runtime Runtime {
            get {
                Init();
                return _runtime;
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
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        public DirectoryInfo FrameworkAssemblyDirectory {
            get {
                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

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
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        public DirectoryInfo SdkDirectory {
            get {
                Init();

                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

                return _sdkDirectory;
            }
        }

        /// <summary>
        /// Gets the <see cref="Project" /> used to initialize this framework.
        /// </summary>
        /// <value>
        /// The <see cref="Project" /> used to initialize this framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        public Project Project {
            get {
                Init();

                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

                return _project;
            }
        }

        /// <summary>
        /// Gets the set of assemblies and directories that should scanned for
        /// NAnt tasks, types or functions.
        /// </summary>
        /// <value>
        /// The set of assemblies and directories that should be scanned for 
        /// NAnt tasks, types or functions.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        public FileSet TaskAssemblies {
            get {
                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

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

        /// <summary>
        /// Gets the reference assemblies for the current framework.
        /// </summary>
        /// <value>
        /// The reference assemblies for the current framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        internal FileSet[] ReferenceAssemblies {
            get {
                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

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

        /// <summary>
        /// Gets the tool paths for the current framework.
        /// </summary>
        /// <value>
        /// The tool paths for the current framework.
        /// </value>
        /// <exception cref="ArgumentException">The framework is not valid.</exception>
        internal string[] ToolPaths {
            get {
                // ensure we're not dealing with an invalid framework
                AssertNotInvalid();

                if (_toolPaths == null) {
                    XmlNode node = _frameworkNode.SelectSingleNode(
                        "nant:tool-paths", NamespaceManager);
                    if (node != null) {
                        DirList dirs = new DirList();
                        dirs.Project = Project;
                        dirs.NamespaceManager = NamespaceManager;
                        dirs.Parent = Project;
                        dirs.Initialize(node, Project.Properties, this);
                        _toolPaths = dirs.GetDirectories();
                    } else {
                        _toolPaths = new string[0];
                    }
                }
                return _toolPaths;
            }
        }

        internal string RuntimeEngine {
            get {
                if (Runtime == null) {
                    return string.Empty;
                }

                ManagedExecutionMode mode = Runtime.Modes.GetExecutionMode (ManagedExecution.Auto);
                if (mode != null) {
                    RuntimeEngine engine = mode.Engine;
                    if (engine.Program != null) {
                        return engine.Program.FullName;
                    }
                }
                return string.Empty;
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

        #region Internal Static Properties

        internal static IComparer NameComparer {
            get {
                return new FrameworkNameComparer ();
            }
        }

        #endregion Internal Static Properties

        #region Public Instance Methods

        /// <summary>
        /// Resolves the specified assembly to a full path by matching it
        /// against the reference assemblies.
        /// </summary>
        /// <param name="fileName">The file name of the assembly to resolve (without path information).</param>
        /// <returns>
        /// An absolute path to the assembly, or <see langword="null" /> if the
        /// assembly could not be found or no reference assemblies are configured
        /// for the current framework.
        /// </returns>
        /// <remarks>
        /// Whether the file name is matched case-sensitively depends on the
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

        #endregion Public Instance Methods

        #region Internal Instance Methods

        internal void Validate() {
            if (_status == InitStatus.Valid) {
                return;
            }

            Init();

            // reset status to avoid status check in properties from getting
            // triggered
            _status = InitStatus.Initialized;

            try {
                // verify if framework directory is configured, and indirectly
                // check if it exists
                if (FrameworkDirectory == null) {
                    throw new ArgumentException("The \"frameworkdirectory\" " +
                        "attribute does not exist, or has no value.");
                }

                // verify if framework assembly directory is configured, and 
                // indirectly check if it exists
                if (FrameworkAssemblyDirectory == null) {
                    throw new ArgumentException("The \"frameworkassemblydirectory\" " +
                        "attribute does not exist, or has no value.");
                }

                // verify if version is configured
                if (Version == null) {
                    throw new ArgumentException("The \"version\" attribute " +
                        "does not exist, or has no value.");
                }

                // verify if clrversion is configured
                if (ClrVersion == null) {
                    throw new ArgumentException("The \"clrversion\" attribute " +
                        "does not exist, or has no value.");
                }

                // mark framework valid
                _status = InitStatus.Valid;
            } catch (Exception ex) {
                _status = InitStatus.Invalid;
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "{0} ({1}) is not installed, or not correctly configured.",
                    Description, Name), Location.UnknownLocation, ex);
            }
        }

        /// <summary>
        /// Searches the list of tool paths of the current framework for the
        /// given file, and returns the absolute path if found.
        /// </summary>
        /// <param name="tool">The file name of the tool to search for.</param>
        /// <returns>
        /// The absolute path to <paramref name="tool" /> if found in one of the
        /// configured tool paths; otherwise, <see langword="null" />.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tool" /> is <see langword="null" />.</exception>
        /// <remarks>
        ///   <para>
        ///   The configured tool paths are scanned in the order in which they
        ///   are defined in the framework configuration.
        ///   </para>
        ///   <para>
        ///   The file name of the tool to search should include the extension.
        ///   </para>
        /// </remarks>
        internal string GetToolPath (string tool) {
            if (tool == null)
                throw new ArgumentNullException ("tool");

            return FileUtils.ResolveFile(ToolPaths, tool, false);
        }

        #endregion Internal Instance Methods

        #region Private Instance Methods

        private void Init() {
            if (_status != InitStatus.Uninitialized) {
                return;
            }

            // the framework node is not available when working with a
            // deserialized FrameworkInfo, and as such it's no use
            // attempting to initialize it when the status is invalid
            // since it will not get us the actual reason anyway
            AssertNotInvalid();

            try {
                PerformInit();
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to initialize the '{0}' ({1}) target framework.",
                    Description, Name), Location.UnknownLocation, ex);
            }
        }

        private void PerformInit() {
            // get framework-specific project node
            XmlNode projectNode = _frameworkNode.SelectSingleNode("nant:project", 
                NamespaceManager);

            if (projectNode == null)
                throw new ArgumentException("No <project> node is defined.");

            // create XmlDocument from project node
            XmlDocument projectDoc = new XmlDocument();
            projectDoc.LoadXml(projectNode.OuterXml);

            // create and execute project
            Project frameworkProject = new Project(projectDoc);
            frameworkProject.BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            frameworkProject.Execute();

            XmlNode runtimeNode = _frameworkNode.SelectSingleNode ("runtime",
                NamespaceManager);
            if (runtimeNode != null) {
                _runtime = new Runtime ();
                _runtime.Parent = _runtime.Project = frameworkProject;
                _runtime.NamespaceManager = NamespaceManager;
                _runtime.Initialize(runtimeNode, frameworkProject.Properties, this);
            }

            string sdkDir = GetXmlAttributeValue(_frameworkNode, "sdkdirectory");
            try {
                sdkDir = frameworkProject.ExpandProperties(sdkDir,
                    Location.UnknownLocation);
            } catch (BuildException) {
                // do nothing with this exception as a framework is still
                // considered valid if the sdk directory is not available
                // or not configured correctly
            }

            // the sdk directory does not actually have to exist for a
            // framework to be considered valid
            if (sdkDir != null && Directory.Exists(sdkDir))
                _sdkDirectory = new DirectoryInfo(sdkDir);

            _project = frameworkProject;
            _status = InitStatus.Initialized;
        }

        private void AssertNotInvalid() {
            if (_status == InitStatus.Invalid || (_status == InitStatus.Uninitialized && _frameworkNode == null)) {
                throw new ArgumentException("The current framework " +
                    "is not valid.");
            }
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

        private class FrameworkNameComparer : IComparer {
            public int Compare(object x, object y) {
                FrameworkInfo fix = x as FrameworkInfo;
                FrameworkInfo fiy = y as FrameworkInfo;

                return string.Compare(fix.Name, fiy.Name,false, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    /// Enumeration of CLR types.
    /// </summary>
    public enum ClrType {
        /// <summary>
        /// Desktop CLR is used.
        /// </summary>
        Desktop = 1,
        
        /// <summary>
        /// Compact CLR is used.
        /// </summary>
        Compact = 2,
        
        /// <summary>
        /// Browser CLR is used.
        /// </summary>
        Browser = 3
    }

    /// <summary>
    /// Enumeration of vendors.
    /// </summary>
    public enum VendorType {
        /// <summary>
        /// Vendor Microsoft.
        /// </summary>
        Microsoft = 1,
        
        /// <summary>
        /// Vendor Mono.
        /// </summary>
        Mono = 2
    }
}
