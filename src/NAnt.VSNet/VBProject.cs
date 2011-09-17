// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.DotNet.Types;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class VBProject : ManagedProjectBase {
        #region Public Instance Constructors

        public VBProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(solution, projectPath, xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            _imports = new NamespaceImportCollection();

            // process namespace imports
            XmlNodeList imports = xmlDefinition.SelectNodes("//Imports/Import");
            foreach (XmlElement import in imports) {
                XmlAttribute nsAttribute = import.Attributes["Namespace"];
                if (nsAttribute != null) {
                    string nameSpace = nsAttribute.Value.ToString(
                        CultureInfo.InvariantCulture);
                    _imports.Add(new NamespaceImport(nameSpace));
                }
            }
        }

        #endregion Public Instance Constructors

        #region Override implementation of ManagedProjectBase

        protected override void WriteProjectOptions(StreamWriter sw) {
            // write namespace imports
            if (_imports.Count > 0) {
                sw.WriteLine("/imports:{0}", _imports.ToString());
            }
        }

        #endregion Override implementation of ManagedProjectBase

        #region Override implementation of ProjectBase

        /// <summary>
        /// Gets the type of the project.
        /// </summary>
        /// <value>
        /// The type of the project.
        /// </value>
        public override ProjectType Type {
            get { return ProjectType.VB; }
        }

        /// <summary>
        /// Verifies whether the specified XML fragment represents a valid project
        /// that is supported by this <see cref="ProjectBase" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <exception cref="BuildException">
        ///   <para>The XML fragment is not supported by this <see cref="ProjectBase" />.</para>
        ///   <para>-or-</para>
        ///   <para>The XML fragment does not represent a valid project (for this <see cref="ProjectBase" />).</para>
        /// </exception>
        protected override void VerifyProjectXml(XmlElement docElement) {
            if (!IsSupported(docElement)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project '{0}' is not a valid VB.NET project.", ProjectPath),
                    Location.UnknownLocation);
            }
        }

        /// <summary>
        /// Returns the Visual Studio product version of the specified project
        /// XML fragment.
        /// </summary>
        /// <param name="docElement">The document element of the project.</param>
        /// <returns>
        /// The Visual Studio product version of the specified project XML 
        /// fragment.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The product version could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The product version is not supported.</para>
        /// </exception>
        /// <remarks>
        /// This method is called from the <see cref="ProjectBase" /> ctor, and
        /// at that time we're not sure the XML that is passed in, is indeed a
        /// valid Visual Basic project.
        /// </remarks>
        protected override ProductVersion DetermineProductVersion(XmlElement docElement) {
            if (docElement == null) {
                throw new ArgumentNullException("docElement");
            }
            return GetProductVersion(docElement.SelectSingleNode("./VisualBasic"));
        }

        #endregion Override implementation of ProjectBase

        #region Override implemenation of ManagedProjectBase

        /// <summary>
        /// Gets the default file extension of sources for this project.
        /// </summary>
        /// <value>
        /// For VB projects, the default file extension is &quot;.vb&quot;.
        /// </value>
        protected override string FileExtension {
            get { return ".vb"; }
        }

        /// <summary>
        /// Returns the project location from the specified project XML fragment.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project file.</param>
        /// <returns>
        /// The project location of the specified project XML file.
        /// </returns>
        /// <exception cref="BuildException">
        ///   <para>The project location could not be determined.</para>
        ///   <para>-or-</para>
        ///   <para>The project location is invalid.</para>
        /// </exception>
        protected override ProjectLocation DetermineProjectLocation(XmlElement docElement) {
            return GetProjectLocation(docElement.SelectSingleNode("./VisualBasic"));
        }

        /// <summary>
        /// Returns a <see cref="ProcessStartInfo" /> for launching the compiler
        /// for this project.
        /// </summary>
        /// <param name="config">The configuration to build.</param>
        /// <param name="responseFile">The response file for the compiler.</param>
        /// <returns>
        /// A <see cref="ProcessStartInfo" /> for launching the compiler for 
        /// this project.
        /// </returns>
        protected override ProcessStartInfo GetProcessStartInfo(ConfigurationBase config, string responseFile) {
            ProcessStartInfo psi = new ProcessStartInfo(FileUtils.CombinePaths(SolutionTask.
                Project.TargetFramework.FrameworkDirectory.FullName, "vbc.exe"), 
                "@\"" + responseFile + "\"");

            // Visual Basic.NET uses the directory from which VS.NET 
            // was launched as working directory, the closest match
            // and best behaviour for us is to use the <solution dir>
            // as working directory and fallback to the <project dir>
            // if the project was explicitly specified
            if (SolutionTask.SolutionFile != null) {
                psi.WorkingDirectory = SolutionTask.SolutionFile.DirectoryName;
            } else {
                psi.WorkingDirectory = ProjectDirectory.FullName;
            }
            return psi;
        }

        #endregion Override implemenation of ManagedProjectBase

        #region Public Static Methods

        /// <summary>
        /// Returns a value indicating whether the project represented by the
        /// specified XML fragment is supported by <see cref="VBProject" />.
        /// </summary>
        /// <param name="docElement">XML fragment representing the project to check.</param>
        /// <returns>
        /// <see langword="true" /> if <see cref="VBProject" /> supports the 
        /// specified project; otherwise, <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// <para>
        /// A project is identified as as Visual Basic project, if the XML 
        /// fragment at least has the following information:
        /// </para>
        /// <code>
        ///   <![CDATA[
        /// <VisualStudioProject>
        ///     <VisualBasic
        ///         ProductVersion="..."
        ///         ....
        ///     >
        ///         ...
        ///     </VisualBasic>
        /// </VisualStudioProject>
        ///   ]]>
        /// </code>
        /// </remarks>
        public static bool IsSupported(XmlElement docElement) {
            if (docElement == null) {
                return false;
            }

            if (docElement.Name != "VisualStudioProject") {
                return false;
            }

            XmlNode projectNode = docElement.SelectSingleNode("./VisualBasic");
            if (projectNode == null) {
                return false;
            }

            try {
                GetProductVersion(projectNode);
                // no need to perform version check here as this is done in 
                // GetProductVersion
            } catch {
                // product version could not be determined or is not supported
                return false;
            }

            return true;
        }

        #endregion Public Static Methods

        #region Private Instance Fields

        private readonly NamespaceImportCollection _imports;

        #endregion Private Instance Fields
    }
}
