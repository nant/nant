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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian_maclean@another.com)

namespace SourceForge.NAnt.Tasks {

    using System;
    using System.Collections;
    using System.IO;
    using System.Xml;
    using System.Text;

    using SourceForge.NAnt.Attributes;

    using NDoc.Core;

    /// <summary>Runs NDoc to create documentation.</summary>
    /// <remarks>
    ///   <para>See the <a href="http://ndoc.sf.net">NDoc home page</a> for more information.</para>
    /// </remarks>
    /// <example>
    ///   <para>Document two assemblies using the MSDN documenter.  The namespaces are documented in <c>NamespaceSummary.xml</c></para>
    ///   <code>
    ///     <![CDATA[
    /// <ndoc>
    ///     <assemblies basedir="${build.dir}">
    ///         <includes name="NAnt.exe"/>
    ///         <includes name="NAnt.Core.dll"/>
    ///     </assemblies>
    ///     <summaries basedir="${build.dir}">
    ///         <includes name="NamespaceSummary.xml"/>
    ///     </summaries>
    ///     <documenters>
    ///         <documenter name="MSDN">
    ///             <property name="OutputDirectory" value="doc\MSDN" />
    ///             <property name="HtmlHelpName" value="NAnt" />
    ///             <property name="HtmlHelpCompilerFilename" value="hhc.exe" />
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
    ///   <para><c>NamespaceSummary.xml</c> contents</para>
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

        XmlNodeList _docNodes;      
        FileSet _assemblies = new FileSet();
        FileSet _summaries = new FileSet();

        /// <summary>The set of assemblies to document.</summary>
        [FileSet("assemblies")]
        public FileSet Assemblies      { get { return _assemblies; } }

        /// <summary>The set of namespace summary files.</summary>
        [FileSet("summaries")]
        public FileSet Summaries       { get { return _summaries; } }

        /// <summary>
        /// Updates the progress bar representing a building step.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnDocBuildingStep(object sender, ProgressArgs e) {
            Log.WriteLine(LogPrefix + e.Status);
        }

        /// <summary>
        /// Initialize taks and verify parameters.
        /// </summary>
        /// <param name="taskNode">Node that contains the XML fragment used to define this task instance.</param>
        protected override void InitializeTask(XmlNode taskNode) {

            // Expand and store the xml node
            _docNodes = taskNode.Clone().SelectNodes("documenters/documenter");
            ExpandPropertiesInNodes(_docNodes);
            // check for valid documenters (any other validation can be done by NDoc itself at project load time)
            foreach( XmlNode node in _docNodes) {
                string documenterName = node.Attributes["name"].Value;
            }
        }

        protected override void ExecuteTask() {
            // write documenter project settings to temp file
            string projectFileName = Path.GetTempFileName(); //@"c:\work\nant\nant.xdp";
            if (Verbose) {
                Log.WriteLine(LogPrefix + "Writing project settings to '{0}'.", projectFileName);
            }

            FileStream file = File.OpenWrite(projectFileName);
            StreamWriter writer = new StreamWriter(file);
            writer.WriteLine("<project>");
            writer.WriteLine("  <assemblies>");

            // Make sure there is at least one included assembly.  This can't
            // be done in the InitializeTask() method because the files might
            // not have been built at startup time.
            if (Assemblies.FileNames.Count == 0) {
                string msg = "There must be at least one included assembly.";
                throw new BuildException(msg, Location);
            }

            foreach (string assemblyPath in Assemblies.FileNames) {
                string docPath = Path.ChangeExtension(assemblyPath, ".xml");
                writer.WriteLine("    <assembly location='{0}' documentation='{1}' />", assemblyPath, docPath);
            }
            writer.WriteLine(@"  </assemblies>");

            StringBuilder sb = new StringBuilder();
            foreach (string summaryPath in Summaries.FileNames) {
                // write out the namespace summary nodes
                try {
                    StreamReader sr = File.OpenText(summaryPath);
                    sb.Append(sr.ReadToEnd());
                    sr.Close();
                } catch (IOException e) {
                    string msg = String.Format("Failed to read ndoc namespace summary file {0}\n{1}", summaryPath, e.Message);
                    throw new BuildException(msg, Location);
                }
            }
            writer.WriteLine(sb.ToString());

            writer.WriteLine(@"  <documenters>");

            // write out the documenter nodes
            foreach( XmlNode node in _docNodes) {
                writer.Write( node.OuterXml );
            }

            writer.WriteLine(@"  </documenters>");
            writer.WriteLine(@"</project>");
            writer.Close();
            file.Close();

            if (Verbose) {
                StreamReader reader = File.OpenText(projectFileName);
                Log.Write(reader.ReadToEnd());
                reader.Close();
            }

            // create Project object
            NDoc.Core.Project project = null;
            try {
                project = new NDoc.Core.Project();
            }
           
            catch(Exception e) {
                //Log.Write("NDoc Assembly is required");
                throw new ApplicationException("NDocTask: Could not create NDoc Project Class!", e);
            }
            project.Read(projectFileName);

            foreach( XmlNode node in _docNodes) {
                string documenterName = node.Attributes["name"].Value;
                IDocumenter documenter = GetDocumenter( project, documenterName);
                if (documenter == null ) {
                    throw new BuildException("Error loading documenter : " + documenterName, Location);
                }
                // hook up events for feedback during the build
                documenter.DocBuildingStep += new DocBuildingEventHandler(OnDocBuildingStep);

                // build documentation
                try {
                    documenter.Build(project);
                } catch (Exception e) {
                    Log.WriteLine(LogPrefix + "Error building documentation.");
                    throw new BuildException(e.Message, Location);
                }
            }
        }

        /// <summary>Returns the documenter instance to use for this task.</summary>
        IDocumenter GetDocumenter(NDoc.Core.Project project, string documenterName ) {
            if (project == null) {
                project = new NDoc.Core.Project();
            }
            IDocumenter documenter = null;
            foreach (IDocumenter d in project.Documenters) {
                // ignore case when comparing documenter names
                if (String.Compare(d.Name, documenterName, true) == 0) {
                    documenter = (IDocumenter)d;
                    break;
                }
            }
            return documenter;
        }

        /// <summary>Perform macro expansion for the given XmlNodeList.</summary>
        void ExpandPropertiesInNodes(XmlNodeList nodes) {
            foreach(XmlNode node in nodes ) {
                ExpandPropertiesInNodes(node.ChildNodes);
                foreach( XmlAttribute attr in node.Attributes ) {
                    attr.Value = Project.ExpandProperties(attr.Value);
                }
            }
        }
    }
}
