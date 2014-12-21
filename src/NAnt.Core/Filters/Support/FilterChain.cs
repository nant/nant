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
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Util;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Represent a chain of NAnt filters that can be applied to a <see cref="Task"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A FilterChain represents a collection of one or more filters that can 
    /// be appled to a <see cref="Task"/> such as the <see cref="CopyTask"/>.
    /// In the case of the <see cref="CopyTask"/>, the contents of the copied 
    /// files are filtered through each filter specified in the filter chain. 
    /// Filtering occurs in the order the filters are specified with filtered
    /// output of one filter feeding into another.
    /// </para>
    /// <para>
    ///    :--------:--->:----------:--->:----------: ... :----------:--->:--------:<br/>
    ///    :.Source.:--->:.Filter 1.:--->:.Filter 2.: ... :.Filter n.:--->:.target.:<br/>
    ///    :--------:--->:----------:--->:----------: ... :----------:--->:--------:<br/>
    /// </para>
    /// <para>
    /// A list of all filters that come with NAnt is available <see href="../filters/index.html">here</see>.
    /// </para>
    /// <para>
    /// The following tasks support filtering with a FilterChain:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="T:NAnt.Core.Tasks.CopyTask"/></description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="T:NAnt.Core.Tasks.MoveTask"/></description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Replace all occurrences of @NOW@ with the current date/time and 
    ///   replace tabs with spaces in all copied files.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <property name="NOW" value="${datetime::now()}" />
    /// <copy todir="out">
    ///     <fileset basedir="in">
    ///         <include name="**/*" />
    ///     </fileset>
    ///     <filterchain>
    ///         <replacetokens>
    ///             <token key="NOW" value="${TODAY}" />
    ///         </replacetokens>
    ///         <tabstospaces />
    ///     </filterchain>
    /// </copy>
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable]
    [ElementName("filterchain")]
    public class FilterChain : DataTypeBase {
        #region Private Instance Fields

        private Encoding _inputEncoding;
        private FilterCollection _filters = new FilterCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The filters to apply.
        /// </summary>
        [BuildElementArray("filter", ElementType=typeof(Filter))]
        public FilterCollection Filters {
            get { return _filters; }
        }

        /// <summary>
        /// The encoding to assume when filter-copying files. The default is
        /// system's current ANSI code page.
        /// </summary>
        [TaskAttribute("encoding")]
        [Obsolete("The input encoding should now be specified on the task.", true)]
        public Encoding InputEncoding {
            get { return _inputEncoding; }
            set { _inputEncoding = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element

        /// <summary>
        /// Initializes all build attributes and child elements.
        /// </summary>
        /// <remarks>
        /// <see cref="FilterChain" /> needs to maintain the order in which the
        /// filters are specified in the build file.
        /// </remarks>
        protected override void InitializeXml(XmlNode elementNode, PropertyDictionary properties, FrameworkInfo framework) {
            XmlNode = elementNode;

            FilterChainConfigurator configurator = new FilterChainConfigurator(
                this, elementNode, properties, framework);
            configurator.Initialize();
        }

        #endregion Override implementation of Element

        #region Internal Instance Methods

        /// <summary>
        /// Used to to instantiate and return the chain of stream based filters.
        /// </summary>
        /// <param name="physicalTextReader">The <see cref="PhysicalTextReader" /> that is the source of input to the filter chain.</param>
        /// <remarks>
        /// The <paramref name="physicalTextReader" /> is the first <see cref="Filter" />
        /// in the chain, which is based on a physical stream that feeds the chain.
        /// </remarks>
        /// <returns>
        /// The last <see cref="Filter" /> in the chain.
        /// </returns>
        internal Filter GetBaseFilter(PhysicalTextReader physicalTextReader) {
            // if there is no a PhysicalTextReader then the chain is empty
            if (physicalTextReader == null) {
                return null;
            }

            // the physicalTextReader must be the base filter (Based on a physical stream)
            if (!physicalTextReader.Base) {
                throw new BuildException(ResourceUtils.GetString("String_UseBaseFilter"), Location);
            }

            // build the chain and place the base filter at the beginning.
            Filter parentFilter = physicalTextReader;

            // iterate through the collection of filter elements and instantiate each filter.
            foreach (Filter filter in Filters) {
                if (filter.IfDefined && !filter.UnlessDefined) {
                    filter.Chain(parentFilter);
                    filter.InitializeFilter();
                    parentFilter = filter;
                }
            }

            return parentFilter;
        }

        #endregion Internal Instance Methods

        #region Internal Static Methods

        /// <summary>
        /// Determines whether a given FilterChain is null or empty.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <paramref name="filterChain"/> is null or empty;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name='filterChain'>
        /// The FilterChain to check.
        /// </param>
        internal static bool IsNullOrEmpty(FilterChain filterChain)
        {
            return (filterChain == null || filterChain.Filters.Count <= 0);
        }

        #endregion Internal Static Methods

        /// <summary>
        /// Configurator that initializes filters in the order in which they've
        /// been specified in the build file.
        /// </summary>
        public class FilterChainConfigurator : AttributeConfigurator {
            public FilterChainConfigurator(Element element, XmlNode elementNode, PropertyDictionary properties, FrameworkInfo targetFramework) 
                : base(element, elementNode, properties, targetFramework) {
            }

            protected override bool InitializeBuildElementCollection(System.Reflection.PropertyInfo propertyInfo) {
                Type elementType = typeof(Filter);

                BuildElementArrayAttribute buildElementArrayAttribute = (BuildElementArrayAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(BuildElementArrayAttribute));

                if (buildElementArrayAttribute == null || propertyInfo.PropertyType != typeof(FilterCollection)) {
                    return base.InitializeBuildElementCollection(propertyInfo);
                }

                XmlNodeList collectionNodes = ElementXml.ChildNodes;

                // create new array of the required size - even if size is 0
                ArrayList list = new ArrayList(collectionNodes.Count);

                foreach (XmlNode childNode in collectionNodes) {
                    // skip non-nant namespace elements and special elements like comments, pis, text, etc.
                    if (!(childNode.NodeType == XmlNodeType.Element) || !childNode.NamespaceURI.Equals(NamespaceManager.LookupNamespace("nant"))) {
                        continue;
                    }

                    // remove element from list of remaining items
                    UnprocessedChildNodes.Remove(childNode.Name);

                    // initialize child element (from XML or data type reference)
                    Filter filter = TypeFactory.CreateFilter(childNode, 
                        Element);

                    list.Add(filter);
                }

                MethodInfo addMethod = null;

                // get array of public instance methods
                MethodInfo[] addMethods = propertyInfo.PropertyType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                // search for a method called 'Add' which accepts a parameter
                // to which the element type is assignable
                foreach (MethodInfo method in addMethods) {
                    if (method.Name == "Add" && method.GetParameters().Length == 1) {
                        ParameterInfo parameter = method.GetParameters()[0];
                        if (parameter.ParameterType.IsAssignableFrom(elementType)) {
                            addMethod = method;
                            break;
                        }
                    }
                }

                if (addMethod == null) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1020"), elementType.FullName,
                        propertyInfo.PropertyType.FullName, propertyInfo.Name, Name),
                        Location);
                }

                // if value of property is null, create new instance of collection
                object collection = propertyInfo.GetValue(Element, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                if (collection == null) {
                    if (!propertyInfo.CanWrite) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1093"), 
                            buildElementArrayAttribute.Name, Name), 
                            Location);
                    }
                    object instance = Activator.CreateInstance(
                        propertyInfo.PropertyType, BindingFlags.Public | BindingFlags.Instance, 
                        null, null, CultureInfo.InvariantCulture);
                    propertyInfo.SetValue(Element, instance, 
                        BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
                }

                // add each element of the arraylist to collection instance
                foreach (object childElement in list) {
                    addMethod.Invoke(collection, BindingFlags.Default, null, new object[] {childElement}, CultureInfo.InvariantCulture);
                }
            
                return true;
            }
        }
    }
}
