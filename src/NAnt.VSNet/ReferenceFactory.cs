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
using System.IO;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET references.
    /// </summary>
    public sealed class ReferenceFactory {
        #region Private Instance Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceFactory" />
        /// class.
        /// </summary>
        private ReferenceFactory() {
        }

        #endregion Private Instance Constructor

        #region Public Static Methods

        /// <summary>
        /// Creates a new reference.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="xmlDefinition" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="gacCache" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="referencesResolver" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="parent" /> is <see langword="null" />.</para>
        /// </exception>
        public static ReferenceBase CreateReference(SolutionBase solution, ProjectSettings projectSettings, XmlElement xmlDefinition, GacCache gacCache, ReferencesResolver referencesResolver, ProjectBase parent, DirectoryInfo outputDir) {
            if (xmlDefinition == null) {
                throw new ArgumentNullException("elemReference");
            }
            if (gacCache == null) {
                throw new ArgumentNullException("gacCache");
            }
            if (referencesResolver == null) {
                throw new ArgumentNullException("referencesResolver");
            }
            if (parent == null) {
                throw new ArgumentNullException("parent");
            }

            if (xmlDefinition.Attributes["Project"] != null || xmlDefinition.Name == "ProjectReference") {
                // project reference
                return new ProjectReference(xmlDefinition, referencesResolver,
                    parent, solution, projectSettings, gacCache, outputDir);
            } else if (xmlDefinition.Attributes["WrapperTool"] != null) {
                // wrapper
                return new WrapperReference(xmlDefinition, referencesResolver, 
                    parent, gacCache, projectSettings);
            } else {
                // assembly reference
                return new AssemblyReference(xmlDefinition, referencesResolver, 
                    parent, gacCache);
            }
        }

        #endregion Public Static Methods
    }
}
