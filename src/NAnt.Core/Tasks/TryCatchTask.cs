// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gert Driesen
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
// Troy Laurin (fiontan@westnet.com.au)

using NAnt.Core.Attributes;
using NAnt.Core.Util;
using System;
using System.Text;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Executes a set of tasks, and optionally catches a build exception to
    /// allow recovery or rollback steps to be taken, or to define some steps
    /// to be taken regardless if the tasks succeed or fail, or both.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The tasks defined in the <c>&lt;<see cref="TryBlock" />&gt;</c> block
    ///   will be executed in turn, as they normally would in a target.
    ///   </para>
    ///   <para>
    ///   If a <c>&lt;<see cref="CatchBlock" />&gt;</c> block is defined, the 
    ///   tasks in that block will be executed in turn only if one of the tasks 
    ///   in the <c>&lt;<see cref="TryBlock" />&gt;</c> block fails. This 
    ///   failure will then be suppressed by the <c>&lt;<see cref="CatchBlock" />&gt;</c>
    ///   block.
    ///   </para>
    ///   <para>
    ///   The message associated with the failure can also be caught in a
    ///   property for use within the <c>&lt;<see cref="CatchBlock" />&gt;</c>
    ///   block.  The original contents of the property will be restored upon 
    ///   exiting the <c>&lt;<see cref="CatchBlock" />&gt;</c> block.
    ///   </para>
    ///   <para>
    ///   If a <c>&lt;<see cref="FinallyBlock" />&gt;</c> block is defined, the 
    ///   tasks in that block will be executed after the tasks in both the 
    ///   <c>&lt;<see cref="TryBlock" />&gt;</c> and <c>&lt;<see cref="CatchBlock" />&gt;</c>
    ///   blocks have been executed, regardless of whether any task fails in 
    ///   either block.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <trycatch>
    ///     <try>
    ///         <echo message="In try" />
    ///         <fail message="Failing!" />
    ///     </try>
    ///     <catch>
    ///         <echo message="In catch" />
    ///     </catch>
    ///     <finally>
    ///         <echo message="Finally done" />
    ///     </finally>
    /// </trycatch>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   The output of this example will be:
    ///   </para>
    ///   <code>
    /// In try
    /// In catch
    /// Finally done
    ///   </code>
    ///   <para>
    ///   The failure in the <c>&lt;<see cref="TryBlock" />&gt;</c> block will 
    ///   not cause the build to fail.
    ///   </para>
    /// </example>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <trycatch>
    ///     <try>
    ///         <echo message="In try" />
    ///         <fail message="Just because..." />
    ///     </try>
    ///     <catch property="failure">
    ///         <echo message="Caught failure: ${failure}" />
    ///         <fail message="Bad catch" />
    ///     </catch>
    ///     <finally>
    ///         <echo message="Finally done" />
    ///     </finally>
    /// </trycatch>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   The output of this example will be:
    ///   </para>
    ///   <code>
    /// In try
    /// Caught failure: Just because...
    /// Finally done
    /// Build failed: Bad catch
    ///   </code>
    ///   <para>
    ///   Like the above, the failure in the <c>&lt;<see cref="TryBlock" />&gt;</c>
    ///   block does not cause the build to fail.  The failure in the 
    ///   <c>&lt;<see cref="CatchBlock" />&gt;</c> block does, however.
    ///   Note that the <c>&lt;<see cref="FinallyBlock" />&gt;</c> block is 
    ///   executed even though the <c>&lt;<see cref="CatchBlock" />&gt;</c>
    ///   block failed.
    ///   </para>
    /// </example>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <trycatch>
    ///     <try>
    ///         <echo message="In try" />
    ///         <fail message="yet again" />
    ///     </try>
    ///     <catch property="failure">
    ///         <echo message="Caught failure ${failure}" />
    ///         <fail message="Bad catch" />
    ///     </catch>
    ///     <finally>
    ///         <echo message="Finally done ${failure}" />
    ///     </finally>
    /// </trycatch>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   The output of this example will be:
    ///   </para>
    ///   <code>
    /// In try
    /// Caught failure yet again
    /// Build failed: Property 'failure' has not been set.
    ///   </code>
    ///   <para>
    ///   The <see cref="EchoTask" /> in the <c>&lt;<see cref="FinallyBlock" />&gt;</c>
    ///   block failed because the &quot;failure&quot; property was not defined 
    ///   after exiting the <c>&lt;<see cref="CatchBlock" />&gt;</c> block.  
    ///   Note that the failure in the <c>&lt;<see cref="FinallyBlock" />&gt;</c> 
    ///   block has eclipsed the failure in the <c>&lt;<see cref="CatchBlock" />&gt;</c>
    ///   block.
    ///   </para>
    /// </example>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <trycatch>
    ///     <try>
    ///         <property name="temp.file" value="${path::get-temp-file-name()}" />
    ///         <do-stuff to="${temp.file}" />
    ///         <fail message="Oops..." />
    ///     </try>
    ///     <finally>
    ///         <echo message="Cleaning up..." />
    ///         <if test="${property::exists('temp.file')}">
    ///             <delete file="${temp.file}" />
    ///         </if>
    ///     </finally>
    /// </trycatch>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   A more concrete example, that will always clean up the generated
    ///   temporary file after it has been created.
    ///   </para>
    /// </example>
    [TaskName("trycatch")]
    public class TryCatchTask : Task {
        #region Private Instance Fields

        private ElementContainer _tryBlock;
        private CatchElement _catchBlock;
        private ElementContainer _finallyBlock;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The tasks in this block will be executed as a normal part of
        /// the build script.
        /// </summary>
        [BuildElement("try", Required=true)]
        public ElementContainer TryBlock {
            get { return _tryBlock; }
            set { _tryBlock = value; }
        }

        /// <summary>
        /// The tasks in this block will be executed if any task in the try
        /// block fails.
        /// </summary>
        [BuildElement("catch", Required=false)]
        public CatchElement CatchBlock {
            get { return _catchBlock; }
            set { _catchBlock = value; }
        }

        /// <summary>
        /// The tasks in this block will always be executed, regardless of
        /// what happens in the try and catch blocks.
        /// </summary>
        /// <remarks>
        /// Note that any failure in any of the tasks in this block will
        /// prevent any subsequent tasks from executing.
        /// </remarks>
        [BuildElement("finally", Required=false)]
        public ElementContainer FinallyBlock {
            get { return _finallyBlock; }
            set { _finallyBlock = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            try {
                if (TryBlock != null) {
                    TryBlock.Execute();
                }
            } catch (BuildException be) {
                if (CatchBlock != null) {
                    CatchBlock.Catch(be);
                } else {
                    throw;
                }
            } finally {
                if (FinallyBlock != null) {
                    FinallyBlock.Execute();
                }
            }
        }

        #endregion Override implementation of Task

        public class CatchElement : ElementContainer {
            #region Private Instance Fields

            private string _property;

            #endregion Private Instance Fields

            #region Public Instance Properties

            /// <summary>
            /// Defines the name of the property to save the message describing
            /// the failure that has been caught.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The failure message is only available in the context of the catch
            /// block.  If you wish to preserve the message, you will need to save
            /// it into another property.
            /// </para>
            /// <para>
            /// Readonly properties cannot be overridden by this mechanism.
            /// </para>
            /// </remarks>
            [TaskAttribute("property", Required=false)]
            [StringValidator(AllowEmpty=false)]
            public string Property {
                get { return _property; }
                set { _property = StringUtils.ConvertEmptyToNull(value); }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods

            public void Catch(BuildException be) {
                bool propertyExists = false;
                string originalPropertyValue = null;

                if (Property != null) {
                    propertyExists = Project.Properties.Contains(Property);
                    originalPropertyValue = Project.Properties[Property];
                    Project.Properties[Property] = GetExceptionMessage(be);
                }

                try {
                    Execute();
                } finally {
                    if (Property != null) {
                        if (!propertyExists) {
                            // if the property did not exist, then remove it again
                            Project.Properties.Remove(Property);
                        } else {
                            // restore original value
                            Project.Properties[Property] = originalPropertyValue;
                        }
                    }
                }
            }

            #endregion Public Instance Methods

            #region Private Instance Methods

            /// <summary>
            /// Parses out the complete exception and inner exception information to be
            /// consumed by the catch element.
            /// </summary>
            /// <param name="e">
            /// The exception object to get the messages from.
            /// </param>
            /// <returns>
            /// The complete exception message.
            /// </returns>
            private string GetExceptionMessage(Exception e)
            {
                StringBuilder sb = new StringBuilder();

                // Get the main message from the exception.
                if (e is BuildException)
                {
                    sb.AppendLine(((BuildException)e).RawMessage);
                }
                else
                {
                    sb.AppendLine(e.Message);
                }

#if NET_4_0
                // If e is an aggregated exception, get the messages from
                // the inner exceptions if they exist.
                if (e is AggregateException)
                {
                    AggregateException agg = e as AggregateException;

                    if (agg.InnerExceptions != null)
                    {
                        foreach (Exception inner in agg.InnerExceptions)
                        {
                            sb.AppendLine();
                            sb.AppendLine(GetExceptionMessage(inner));
                        }
                    }
                }
#endif

                // Get the inner exception information if available.
                if (e.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine(GetExceptionMessage(e.InnerException));
                }
                return sb.ToString().Trim();
            }

            #endregion
        }
    }
}
