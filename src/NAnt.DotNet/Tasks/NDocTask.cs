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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian_maclean@another.com)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using NDoc.Core;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.DotNet.Types;
using System.Reflection;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Runs NDoc V1.3.1 to create documentation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   See the <see href="http://ndoc.sourceforge.net/">NDoc home page</see> for more 
    ///   information.
    ///   </para>
    ///   <note>
    ///   By default, only the NDoc MSDN documenter ships as part of the NAnt 
    ///   distribution. To make another NDoc documenter from the NDoc V1.3.1 
    ///   distribution available to the <see cref="NDocTask" />, copy the 
    ///   documenter assembly (and possible dependencies) to the &quot;lib&quot; 
    ///   directory corresponding with the CLR you're running NAnt on 
    ///   (eg. &lt;nant root&gt;/bin/lib/net/1.1).
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Document two assemblies using the MSDN documenter. The namespaces are 
    ///   documented in <c>NamespaceSummary.xml</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <ndoc>
    ///     <assemblies basedir="${build.dir}">
    ///         <include name="NAnt.exe" />
    ///         <include name="NAnt.Core.dll" />
    ///     </assemblies>
    ///     <summaries basedir="${build.dir}">
    ///         <include name="NamespaceSummary.xml" />
    ///     </summaries>
    ///     <documenters>
    ///         <documenter name="MSDN">
    ///             <property name="OutputDirectory" value="doc\MSDN" />
    ///             <property name="HtmlHelpName" value="NAnt" />
    ///             <property name="IncludeFavorites" value="False" />
    ///             <property name="Title" value="An NDoc Documented Class Library" />
    ///             <property name="SplitTOCs" value="False" />
    ///             <property name="DefaulTOC" value="" />
    ///             <property name="ShowVisualBasic" value="True" />
    ///             <property name="ShowMissingSummaries" value="True" />
    ///             <property name="ShowMissingRemarks" value="True" />
    ///             <property name="ShowMissingParams" value="True" />
    ///             <property name="ShowMissingReturns" value="True" />
    ///             <property name="ShowMissingValues" value="True" />
    ///             <property name="DocumentInternals" value="False" />
    ///             <property name="DocumentProtected" value="True" />
    ///             <property name="DocumentPrivates" value="False" />
    ///             <property name="DocumentEmptyNamespaces" value="False" />
    ///             <property name="IncludeAssemblyVersion" value="False" />
    ///             <property name="CopyrightText" value="" />
    ///             <property name="CopyrightHref" value="" />
    ///          </documenter>
    ///     </documenters> 
    /// </ndoc>
    ///     ]]>
    ///   </code>
    ///   <para>Content of <c>NamespaceSummary.xml</c> :</para>
    ///   <code>
    ///     <![CDATA[
    /// <namespaces>
    ///     <namespace name="Foo.Bar">
    ///         The <b>Foo.Bar</b> namespace reinvents the wheel.
    ///     </namespace>
    ///     <namespace name="Foo.Bar.Tests">
    ///         The <b>Foo.Bar.Tests</b> namespace ensures that the Foo.Bar namespace reinvents the wheel correctly.
    ///     </namespace>
    /// </namespaces>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("ndoc")]
    public class NDocTask : Task {
        #region Private Instance Fields

        private XmlNodeList _docNodes;
        private AssemblyFileSet _assemblies = new AssemblyFileSet();
        private FileSet _summaries = new FileSet();
        private RawXml _documenters;
        private DirSet _referencePaths = new DirSet();
        private string _hhcexe;
        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The set of assemblies to document.
        /// </summary>
        [BuildElement("assemblies", Required=true)]
        public AssemblyFileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// The set of namespace summary files.
        /// </summary>
        [BuildElement("summaries")]
        public FileSet Summaries {
            get { return _summaries; }
            set { _summaries = value; }
        }

        /// <summary>
        /// Specifies the formats in which the documentation should be generated.
        /// </summary>
        [BuildElement("documenters", Required=true)]
        public RawXml Documenters {
            get { return _documenters; }
            set { _documenters = value; }
        }

        /// <summary>
        /// Collection of additional directories to search for referenced 
        /// assemblies.
        /// </summary>
        [BuildElement("referencepaths")]
        public DirSet ReferencePaths {
            get { return _referencePaths; }
            set { _referencePaths = value; }
        }
        
        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Initializes the taks and verifies the parameters.
        /// </summary>
        protected override void Initialize() {
            // expand and store clone of the xml node
            _docNodes = Documenters.Xml.Clone().SelectNodes("nant:documenter", 
                NamespaceManager);
            ExpandPropertiesInNodes(_docNodes);

            _hhcexe = ResolveHhcExe();
        }

        /// <summary>
        /// Generates an NDoc project and builds the documentation.
        /// </summary>
        protected override void ExecuteTask() {
            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Assemblies.BaseDirectory == null) {
                Assemblies.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (Summaries.BaseDirectory == null) {
                Summaries.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (ReferencePaths.BaseDirectory == null) {
                ReferencePaths.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            // Make sure there is at least one included assembly.  This can't
            // be done in the Initialize() method because the files might
            // not have been built at startup time.
            if (Assemblies.FileNames.Count == 0) {
                throw new BuildException(ResourceUtils.GetString("NA2020"), Location);
            }

            // create NDoc Project
            NDoc.Core.Project project = null;

            try {
                project = new NDoc.Core.Project();
            } catch (Exception ex) {
                throw new BuildException(ResourceUtils.GetString("NA2021"), Location, ex);
            }

            // set-up probe path, meaning list of directories where NDoc searches
            // for documenters
            // by default, NDoc scans the startup path of the app, so we do not 
            // need to add this explicitly
            string privateBinPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            if (privateBinPath != null) {
                // have NDoc also probe for documenters in the privatebinpath
                foreach (string relativePath in privateBinPath.Split(Path.PathSeparator)) {
                    project.AppendProbePath(Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, relativePath));
                }
            }

            // check for valid documenters (any other validation can be done by NDoc itself at project load time)
            foreach (XmlNode node in _docNodes) {
                //skip non-nant namespace elements and special elements like comments, pis, text, etc.
                if (!(node.NodeType == XmlNodeType.Element) || !node.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                    continue;
                }
                
                string documenterName = node.Attributes["name"].Value;
                CheckAndGetDocumenter(project, documenterName);
            }

            // write documenter project settings to temp file
            string projectFileName = Path.GetTempFileName();
            Log(Level.Verbose, ResourceUtils.GetString("String_WritingProjectSettings"), projectFileName);

            XmlTextWriter writer = new XmlTextWriter(projectFileName, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("project");

            // write assemblies section
            writer.WriteStartElement("assemblies");
            foreach (string assemblyPath in Assemblies.FileNames) {
                string docPath = Path.ChangeExtension(assemblyPath, ".xml");
                writer.WriteStartElement("assembly");
                writer.WriteAttributeString("location", assemblyPath);
                if (File.Exists(docPath)) {
                    writer.WriteAttributeString("documentation", docPath);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            // write summaries section
            StringBuilder sb = new StringBuilder();
            foreach (string summaryPath in Summaries.FileNames) {
                // write out the namespace summary nodes
                try {
                    XmlTextReader tr = new XmlTextReader(summaryPath);
                    tr.MoveToContent();   // skip XmlDeclaration  and Processing Instructions                                               
                    sb.Append(tr.ReadOuterXml());
                    tr.Close();
                } catch (IOException ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA2022"), summaryPath), Location, ex);
                }
            }
            writer.WriteRaw(sb.ToString());

            // write out the documenters section
            writer.WriteStartElement("documenters");
            foreach (XmlNode node in _docNodes) {
                //skip non-nant namespace elements and special elements like comments, pis, text, etc.
                if (!(node.NodeType == XmlNodeType.Element) || !node.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                    continue;
                }
                writer.WriteRaw(node.OuterXml);
            }
            writer.WriteEndElement();

            // end project element
            writer.WriteEndElement();
            writer.Close();

            try {
                // read NDoc project file
                Log(Level.Verbose, ResourceUtils.GetString("String_NDocProjectFile"),
                    Path.GetFullPath(projectFileName));
                project.Read(projectFileName);

                // add additional directories to search for referenced assemblies
                if (ReferencePaths.DirectoryNames.Count > 0) {
                    foreach (string directory in ReferencePaths.DirectoryNames) {
                        project.ReferencePaths.Add(new ReferencePath(directory));
                    }
                }

                foreach (XmlNode node in _docNodes) {
                    //skip non-nant namespace elements and special elements like comments, pis, text, etc.
                    if (!(node.NodeType == XmlNodeType.Element) || !node.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                        continue;
                    }
                
                    string documenterName = node.Attributes["name"].Value;
                    IDocumenter documenter =  CheckAndGetDocumenter(project, documenterName);

                    // hook up events for feedback during the build
                    documenter.DocBuildingStep += new DocBuildingEventHandler(OnDocBuildingStep);
                    documenter.DocBuildingProgress += new DocBuildingEventHandler(OnDocBuildingProgress);

                    // build documentation
                    documenter.Build(project);
                }
            } catch (Exception ex) {
                throw new BuildException(ResourceUtils.GetString("NA2023"), Location, ex);
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Represents the method that will be called to update the overall 
        /// percent complete value and the current step name.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="ProgressArgs" /> that contains the event data.</param>
        private void OnDocBuildingStep(object sender, ProgressArgs e) {
            Log(Level.Info, e.Status);
            if (e.Progress == 25 && null != _hhcexe) {
                // right before progress step 25 HtmlHelp object will be created in MSDN Documentor
                // so we can set path to hhc.exe per reflection
                // determined with ILSpy
                SetHtmlHelpCompiler(sender, _hhcexe);
            }

        }

        /// <summary>
        /// Represents the method that will be called to update the current
        /// step's precent complete value.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="ProgressArgs" /> that contains the event data.</param>
        private void OnDocBuildingProgress(object sender, ProgressArgs e) {
            Log(Level.Verbose, e.Progress + ResourceUtils.GetString("String_PercentageComplete"));
        }

        /// <summary>
        /// Returns the documenter for the given project.
        /// </summary>
        /// <exception cref="BuildException">
        /// Documenter <paramref name="documenterName" /> is not found.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="project" /> is <see langword="null" />.
        /// </exception>
        private IDocumenter CheckAndGetDocumenter(NDoc.Core.Project project, string documenterName){
            IDocumenter documenter = null;

            if (project == null) {
                throw new ArgumentNullException("project");
            }

            StringCollection documenters = new StringCollection();
            foreach (IDocumenter d in project.Documenters) {
                documenters.Add(d.Name);

                // ignore case when comparing documenter names
                if (string.Compare(d.Name, documenterName, true, CultureInfo.InvariantCulture) == 0) {
                    documenter = (IDocumenter) d;
                    break;
                }
            }

            // throw an exception if the documenter could not be found.
            if (documenter == null) {
                if (documenters.Count == 0) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA2024"), documenterName), Location);
                } else {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA2025"), documenterName, 
                        StringUtils.Join(", ", documenters)), Location);
                }
            }
            return documenter;
        }
 
        /// <summary>
        /// Performs macro expansion for the given nodes.
        /// </summary>
        /// <param name="nodes"><see cref="XmlNodeList" /> for which expansion should be performed.</param>
        private void ExpandPropertiesInNodes(XmlNodeList nodes) {
            foreach (XmlNode node in nodes) {
                // do not process comment nodes, or entities and other internal element types.
                if (node.NodeType == XmlNodeType.Element) {
                    ExpandPropertiesInNodes(node.ChildNodes);
                    foreach (XmlAttribute attr in node.Attributes) {
                        // use "this" keyword as workaround for Mono bug #71992
                        attr.Value = this.Project.ExpandProperties(attr.Value, Location);
                    }

                    // convert output directory to full path relative to project base directory
                    XmlNode outputDirProperty = (XmlNode) node.SelectSingleNode("property[@name='OutputDirectory']");
                    if (outputDirProperty != null) {
                        XmlAttribute valueAttribute = (XmlAttribute) outputDirProperty.Attributes.GetNamedItem("value");
                        if (valueAttribute != null) {
                            // use "this" keyword as workaround for Mono bug #71992
                            valueAttribute.Value = this.Project.GetFullPath(valueAttribute.Value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Use Reflection to set HtmplHelp._htmlHelpCompiler private field for MSDN Documentor. 
        /// Ndoc could not handle 64bit installations and is not actively developed anymore.
        /// </summary>
        /// <param name="sender">Active documentor</param>
        /// <param name="hhcexe">Path to hhc.exe</param>
        private void SetHtmlHelpCompiler(object sender, string hhcexe) {

            Log(Level.Debug, "Setting Html Help Compiler per reflection");
            FieldInfo fi = sender.GetType().GetField("htmlHelp", BindingFlags.NonPublic | BindingFlags.Instance);
            if (null == fi)
                return;
            Log(Level.Debug, "Found MSDNDocumenter.htmlHelp field");

            object htmlHelp = fi.GetValue(sender);
            FieldInfo hhc = fi.FieldType.GetField("_htmlHelpCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            if (null == hhc)
                return;

            Log(Level.Debug, "Found HtmlHelp._htmlHelpCompiler field");
            hhc.SetValue(htmlHelp, hhcexe);
            Log(Level.Verbose, "Set  Html Help Compiler to '{0}'", hhcexe);
        }

        /// <summary>
        /// Searches in %ProgramFiles(x86)%\HTML Help Workshop and %ProgramFiles%\HTML Help Workshop
        /// for hhc.exe. If not found let ndoc msdn documentor search itself
        /// </summary>
        /// <returns>the path to hhc.exe if found, null otherwise</returns>
        private string ResolveHhcExe() {
            StringCollection folders = new StringCollection();
            
            string hhwx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!String.IsNullOrEmpty(hhwx86)) {
                folders.Add(Path.Combine(hhwx86, "HTML Help Workshop"));
            }
            string hhw = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!String.IsNullOrEmpty(hhw)) {
                folders.Add(Path.Combine(hhw, "HTML Help Workshop"));
            }

            string[] searchFolders = new string[folders.Count];
            for (int i = 0; i < folders.Count; i++) {
                searchFolders[i] = folders[i];
            }

            return FileUtils.ResolveFile(searchFolders, "hhc.exe", false);
        }
        #endregion Private Instance Methods
    }
}
