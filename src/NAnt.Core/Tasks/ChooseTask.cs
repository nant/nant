//
// NAntContrib
// Copyright (C) 2001-2005 Gerry Shaw
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//
// Gert Driesen (drieseng@users.sourceforge.net)

using System.Collections.Generic;
using System.Globalization;
using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks {
    /// <summary>
    ///   <para>
    ///   Executes an alternate set of task or type definition depending on
    ///   conditions that are individually set on each group.
    ///   </para>
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The <see cref="ChooseTask" /> selects one among a number of possible
    ///   alternatives. It consists of a sequence of <c>&lt;when&gt;</c> elements
    ///   followed by an optional <c>&lt;otherwise&gt;</c> element.
    ///   </para>
    ///   <para>
    ///   Each <c>&lt;when&gt;</c> element has a single attribute, test, which 
    ///   specifies an expression. The content of the <c>&lt;when&gt;</c> and 
    ///   <c>&lt;otherwise&gt;</c> elements is a set of nested tasks.
    ///   </para>
    ///   <para>
    ///   The content of the first, and only the first, <c>&lt;when&gt;</c>
    ///   element whose test is <see langword="true" /> is executed. If no 
    ///   <c>&lt;when&gt;</c> element is <see langword="true" />, the 
    ///   content of the <c>&lt;otherwise&gt;</c> element is executed.
    ///   If no <c>&lt;when&gt;</c> element is <see langword="true" />, and no
    ///   <c>&lt;otherwise&gt;</c> element is present, nothing is done.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Execute alternate set of tasks depending on the configuration being
    ///   built.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <choose>
    ///     <when test="${build.config == 'Debug'}">
    ///         <!-- compile app in debug configuration -->
    ///         ...
    ///     </when>
    ///     <when test="${build.config == 'Release'}">
    ///         <!-- compile app in release configuration -->
    ///         ...
    ///     </when>
    ///     <otherwise>
    ///         <fail>Build configuration '${build.config}' is not supported!</fail>
    ///     </otherwise>
    /// </choose>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Define a <c>sources</c> patternset holding an alternate set of patterns
    ///   depending on the configuration being built.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <choose>
    ///     <when test="${build.config == 'Debug'}">
    ///         <patternset id="sources">
    ///             <include name="**/*.cs" />
    ///         </patternset>
    ///     </when>
    ///     <when test="${build.config == 'Release'}">
    ///         <patternset id="sources">
    ///             <include name="**/*.cs" />
    ///             <exclude name="**/Instrumentation/*.cs" />
    ///         </patternset>
    ///     </when>
    ///     <otherwise>
    ///         <fail>Build configuration '${build.config}' is not supported!</fail>
    ///     </otherwise>
    /// </choose>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("choose")]
    public class ChooseTask : Task {
        #region Private Instance Fields

        private List<ElementContainer> _elementContainers = new List<ElementContainer>();

        #endregion Private Instance Fields

        #region Private Instance Properties

        /// <summary>
        /// Gets a value indicating whether a fallback element is defined.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if a fallback element is defined; otherwise,
        /// <see langword="false" />.
        /// </value>
        private bool IsFallbackDefined {
            get { 
                foreach (ElementContainer container in _elementContainers) {
                    // only allow one fallback container, so check if a otherwise
                    // container was already added
                    if (!(container is When)) {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion Private Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            foreach (ElementContainer container in _elementContainers) {
                When when = container as When;
                // execute the nested tasks of the first matching when element
                if (when != null) {
                    if (when.Test) {
                        when.Execute();
                        break;
                    }
                } else {
                    container.Execute();
                }
            }
        }

        #endregion Override implementation of Task

        #region Public Instance Methods

        /// <summary>
        /// One or more alternative sets of tasks to execute.
        /// </summary>
        /// <param name="when">The set of tasks to add.</param>
        [BuildElement("when", Required=true)]
        public void AddCondition(When when) {
            if (IsFallbackDefined) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "The <otherwise> element must be defined as the last nested"
                    + " element in the <{0} ... /> task.", Name), Location);
            }

            _elementContainers.Add(when);
        }

        /// <summary>
        /// The set of tasks to execute if none of the <see cref="When" />
        /// elements are <see langword="true" />.
        /// </summary>
        [BuildElement("otherwise")]
        public void AddFallback(ElementContainer fallback) {
            if (IsFallbackDefined) {
                throw new BuildException("The <otherwise> element may only"
                    + " be defined once.", Location);
            }

            _elementContainers.Add(fallback);
        }

        #endregion Public Instance Methods

        /// <summary>
        /// Groups a set of tasks to execute when a condition is met.
        /// </summary>
        public class When : ElementContainer {
            #region Private Instance Fields
    
            private bool _test = true;
    
            #endregion Private Instance Fields
    
            #region Public Instance Properties
    
            /// <summary>
            /// Used to test arbitrary boolean expression.
            /// </summary>
            [TaskAttribute("test", Required=true)]
            [BooleanValidator()]
            public bool Test {
                get { return _test; }
                set { _test = value; }
            }
    
            #endregion Public Instance Properties
    
            #region Override implementation of NestedTaskContainer

            /// <summary>
            /// Executes this instance.
            /// </summary>
            public override void Execute() {
                if (!Test) {
                    return;
                }
    
                base.Execute();
            }
    
            #endregion Override implementation of NestedTaskContainer
        }
    }
}
