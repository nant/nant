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
using System.Collections;
using System.Reflection;
using NAnt.Core.Attributes;
using NAnt.Core.Filters;


namespace NAnt.Core.Filters {

    /// <summary>
    /// Represents a chain of NAnt filters that are used to
    /// filter a stream.
    /// </summary>
    /// <remarks>
    /// An element that represents a chain of stream based filters that are used to filter
    /// an input stream as it is read.
    /// </remarks>
    /// <para>Parameters:</para>
    /// <list type="table">
    ///    <listheader>
    ///   <term>Parameter</term>
    ///   <description>Description</description>
    ///  </listheader>
    ///  <item>
    ///   <term><code>na</code></term>
    ///   <description>na</description>
    ///  </item>
    /// </list>
    ///
    /// <example>
    ///  <code>
    ///  <![CDATA[
    ///  <filterchain>
    ///   <replacetokens order="1">
    ///    <token key="DATE" value="${TODAY}"/>
    ///   </replacetokens>
    ///   <tabstospaces order="2"/>
    ///  </filterchain>
    ///  ]]>
    ///  </code>
    /// </example>
    ///
    [Serializable]
    [ElementName("filterchain")]
    public class FilterChain : DataTypeBase {

        //Contains the configuration of each filter reader that belongs
        //to this filter chain. This cillection is used to instantiated the stream
        //based filters.
        FilterElementCollection _filterElements = new FilterElementCollection();

        /// <summary>
        /// Default Constructor
        /// </summary>
        public FilterChain() {}

        /// <summary>
        /// Used to support a generic filter that is instantiated through reflection.
        /// </summary>
        [BuildElementArray("filter", ElementType = typeof(FilterElement))]
        public FilterElementCollection FilterElements {
            get { return _filterElements; }
        }

        #region Convenience Filter Elements

        /// <summary>
        /// Use to support ReplaceCharacterConvenience filters
        /// </summary>
        [BuildElementArray("replacecharacter", ElementType = typeof(ReplaceCharacterConvenience))]
        public FilterElementCollection ReplaceCharacterFilters {
            get { return _filterElements; }
        }

        /// <summary>
        /// Used to support ReplaceTokensConvenience filters
        /// </summary>
        [BuildElementArray("replacetokens", ElementType = typeof(ReplaceTokensConvenience))]
        public FilterElementCollection ReplaceTokenFilters {
            get { return _filterElements; }
        }

        /// <summary>
        /// Used to support TabsToSpacesConvenience filters
        /// </summary>
        [BuildElementArray("tabstospaces", ElementType = typeof(TabsToSpacesConvenience))]
        public FilterElementCollection TabsToSpacesTokenFilters {
            get { return _filterElements; }
        }

        /// <summary>
        /// Used to support ExpandExpressions filters
        /// </summary>
        [BuildElementArray("expandexpressions", ElementType = typeof(ExpandExpressionsConvenience))]
        public FilterElementCollection ExpandExpressionsFilters {
            get { return _filterElements; }
        }

        #endregion
        /// <summary>
        /// Used to to instantiate and return the chain of stream based filters.  The filter that iw the last
        /// filter in the chain. The parameter physicalTextReader is the first filter in the chain which is based on a physical
        /// stream that feeds the chain.
        /// </summary>
        /// <param name="physicalTextReader">The physical TextReader that is the source of input to the filter chain.</param>
        /// <returns>Last filter in the filter chain</returns>
        internal Filter GetBaseFilter(PhysicalTextReader physicalTextReader) {

            //If there is not a physicalTextReader then the chain is empty.
            if (physicalTextReader == null) {
                return null;
            }

            //The physicalTextReader must be the base filter (Based on a physical stream)
            if (!physicalTextReader.Base) {
                throw new BuildException("A base filter must be used", Location);
            }

            //Build the chain and place the base filter at the beginning.
            Filter filter = physicalTextReader;

            //Iterate through the collection of filter elements and instantiate each filter.
            for (int currentIndex = 0 ; currentIndex < _filterElements.Count ; currentIndex++) {
                
                FilterElement element = _filterElements.GetByIndex(currentIndex);

                try {
                    //Load the filter's assembly
                    Assembly targetAssembly = Assembly.Load(element.AssemblyName);

                    //Create the filter and pass the previous filter to the constructor to
                    //chain the filters together.
                    filter = targetAssembly.CreateInstance(
                                 element.ClassName,
                                 true,
                                 BindingFlags.Public | BindingFlags.Instance,
                                 null,
                                 new object[1]{filter},
                                 null,
                                 null)
                             as Filter;
                    if (filter == null) {
                        throw new ApplicationException("CreateInstance returned a null filter");
                    }

                } catch (System.Exception) {
                    throw new BuildException("Unable to load filter: assembly = " + element.AssemblyName + ", class = " + element.ClassName
                                             + "A filter must be derived from the base class NAnt.Core.Filters.Filter", this.Location);
                }


                //Init new filter
                
                filter.Project = base.Project;
                filter.Parameters = element.Parameters;
                filter.Location = element.Location;
                filter.Initialize();
            }
            return filter;
        }
    }
}
