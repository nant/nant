// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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


using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Generic representation of a filter.
    /// </summary>
    ///
    /// <remarks>
    /// Represents an generic Filter XML element. A filter element is used
    /// to represent a filter by providing the information that
    /// is necessary to instaniate it as well as providing
    /// its parameters. All Convienece filter elements are derived from
    /// this class.
    ///
    /// It is possiable to write custom filters by using the &lt;assembly&gt; and class &lt;attributes&gt;.
    ///
    /// <para>Parameters:</para>
    /// <list type="table">
    ///    <listheader>
    ///   <term>Parameter</term>
    ///   <description>Description</description>
    ///  </listheader>
    ///  <item>
    ///   <term><code>&lt;order&gt;</code></term>
    ///   <description>The order this filter will be in the <see cref="FilterChain"></see></description>
    ///  </item>
    ///  <item>
    ///   <term><code>&lt;assembly&gt;</code></term>
    ///   <description>Assembly that the contains the filter class</description>
    ///  </item>
    ///  <item>
    ///   <term><code>&lt;class&gt;</code></term>
    ///   <description>Name of the class that implements the filter. Must be derived from <see cref="Filter" /></description>
    ///  </item>
    /// </list>
    /// </remarks>
    ///
    /// <example>
    ///  <code>
    ///  <![CDATA[
    ///  <filter assembly="NAnt.Core" class="NAnt.Core.Filters.ReplaceTokens" order="1">
    ///    <parameter name="begintoken" value="@"/>
    ///    <parameter name="endtoken" value="@"/>
    ///  </filter>
    ///  ]]>
    ///  </code>
    /// </example>
    ///
    [ElementName("filter")]
    public class FilterElement : Element {

        #region Private Instance Fields

        string _className;
        string _assemblyName;
        int _order = 0;
        FilterElementParameterCollection _parameters = new FilterElementParameterCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Parameters to be passed to the filter.
        /// </summary>
        [BuildElementArray("param")]
        public FilterElementParameterCollection Parameters {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// Name of the class which is the filter
        /// </summary>
        [TaskAttribute("class")]
        [StringValidator(AllowEmpty = false)]
        public string ClassName {
            get { return _className; }
            set { _className = value; }
        }

        /// <summary>
        /// Name of the assembly that contain's the filters class
        /// </summary>
        [TaskAttribute("assembly")]
        [StringValidator(AllowEmpty = false)]
        public string AssemblyName {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        /// <summary>
        /// Since elements are not processed in order it is necessary
        /// to specify the order.
        /// </summary>
        [TaskAttribute("order", Required = true)]
        [Int32Validator(0, int.MaxValue)]
        public int Order {
            get { return _order; }
            set { _order = value; }
        }

        /// <summary>
        ///Used to get the location so it can be
        ///passed to the filter.
        /// </summary>
        public new Location Location {
            get { return base.Location; }
        }

        #endregion Public Instance Properties

        #region Public Instance Counstrucors

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterElement() : base() {}
        #endregion Public Instance Counstrucors
    }
}
