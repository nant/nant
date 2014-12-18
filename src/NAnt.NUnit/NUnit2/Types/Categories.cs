// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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

using NAnt.Core;
using NAnt.Core.Attributes;

using NAnt.NUnit2.Tasks;

namespace NAnt.NUnit2.Types {
    /// <summary>
    /// Controls the categories of tests to execute using the <see cref="NUnit2Task" />.
    /// </summary>
    /// <example>
    ///   <para>
    ///   Only include test cases and fixtures that require no internet access.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <categories>
    ///     <include name="NoInternetAccess" />
    /// </categories>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Exclude test cases and fixtures that are known to fail.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <categories>
    ///     <exclude name="NotWorking" />
    /// </categories>
    ///     ]]>
    ///   </code>
    /// </example>
    [ElementName("categories")]
    public class Categories : DataTypeBase {
        #region Private Instance Fields

        private CategoryCollection _includes = new CategoryCollection();
        private CategoryCollection _excludes = new CategoryCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies a list of categories to include.
        /// </summary>
        [BuildElementArray("include")]
        public CategoryCollection Includes {
            get { return _includes; }
        }

        /// <summary>
        /// Specifies a list of categories to exclude.
        /// </summary>
        [BuildElementArray("exclude")]
        public CategoryCollection Excludes {
            get { return _excludes; }
        }

        #endregion Public Instance Properties
    }
}
