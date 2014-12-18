using NAnt.Core;
using NAnt.Core.Attributes;

using NAnt.DotNet.Tasks;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// Controls the behaviour of a compiler with regards to the reporting of
    /// warnings.
    /// </summary>
    /// <example>
    ///   <para>
    ///   Instruct a compiler to report warning 0519 as an error.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <warnaserror>
    ///     <include number="0519" />
    /// </warnaserror>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Instruct a compiler not to report warning 0519 as an error, if the
    ///   <c>release</c> property is <see langword="true" />.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <warnaserror>
    ///     <exclude number="0519" if="${release}" />
    /// </warnaserror>
    ///     ]]>
    ///   </code>
    /// </example>
    [ElementName("warnaserror")]
    public class WarningAsError : DataTypeBase {
        #region Private Instance Fields

        private CompilerWarningCollection _includes = new CompilerWarningCollection();
        private CompilerWarningCollection _excludes = new CompilerWarningCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Specifies a list of warnings that the compiler should treat as 
        /// errors. This overrides the <see cref="CompilerBase.WarnAsError" /> 
        /// attribute. Only supported when targeting .NET 2.0 or higher.
        /// </summary>
        [BuildElementArray("include")]
        public CompilerWarningCollection Includes {
            get { return _includes; }
        }

        /// <summary>
        /// Specifies a list of warnings that the compiler should NOT treat as 
        /// errors. This is only useful if <see cref="CompilerBase.WarnAsError" /> 
        /// is <see langword="true" />. Only supported when targeting .NET 2.0
        /// or higher.
        /// </summary>
        [BuildElementArray("exclude")]
        public CompilerWarningCollection Excludes {
            get { return _excludes; }
        }

        #endregion Public Instance Properties
    }
}
