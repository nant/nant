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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    public class VBProject : ManagedProjectBase {
        #region Public Instance Constructors

        public VBProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir) : base(solution, projectPath, xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            // ensure the specified project is actually supported by this class
            if (!IsSupported(xmlDefinition)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project '{0}' is not a valid VB project.", ProjectPath),
                    Location.UnknownLocation);
            }

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

        protected override void WriteProjectOptions(StreamWriter sw, ConfigurationSettings config) {
            // write namespace imports
            if (_imports.Count > 0) {
                sw.WriteLine("/imports:\"{0}\"", _imports.ToString());
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

        #endregion Override implementation of ProjectBase

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
        ///     <VisualBasic ... />
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

            // TODO: add ProductVersion (and SchemaVersion) check once we add 
            // more specific implementation classes
            return true;
        }

        #endregion Public Static Methods

        #region Private Instance Fields

        private readonly NamespaceImportCollection _imports;

        #endregion Private Instance Fields
    }
}
