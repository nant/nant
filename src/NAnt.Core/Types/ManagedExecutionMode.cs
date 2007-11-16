using System;
using System.ComponentModel;
using System.Globalization;

namespace NAnt.Core.Types {
    /// <summary>
    /// Specifies the execution mode for managed applications.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   For backward compatibility, the following string values can also be
    ///   used in build files:
    ///   </para>
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Value</term>
    ///       <description>Corresponding field</description>
    ///     </listheader>
    ///     <item>
    ///       <term>&quot;true&quot;</term>
    ///       <description><see cref="Auto" /></description>
    ///     </item>
    ///     <item>
    ///       <term>&quot;false&quot;</term>
    ///       <description><see cref="Default" /></description>
    ///     </item>
    ///   </list>
    ///   <para>
    ///   Even if set to <see cref="Default" />, the operating system can still
    ///   run the program as a managed application.
    ///   </para>
    ///   <para>On Linux this can be done through <b>binfmt_misc</b>, while on
    ///   Windows installing the .NET Framework redistributable caused managed
    ///   applications to run on the MS CLR by default.
    ///   </para>
    /// </remarks>
    [TypeConverter(typeof(ManagedExecutionConverter))]
    public enum ManagedExecution {
        /// <summary>
        /// Do not threat the program as a managed application.
        /// </summary>
        Default,

        /// <summary>
        /// Leave it up to the CLR to determine which specific version of
        /// the CLR will be used to run the application.
        /// </summary>
        Auto,

        /// <summary>
        /// Forces an application to run against the currently targeted
        /// version of a given CLR.
        /// </summary>
        Strict
    }

    /// <summary>
    /// Specialized <see cref="EnumConverter" /> that also supports 
    /// case-insensitive conversion of &quot;true&quot; to 
    /// <see cref="ManagedExecution.Auto" /> and &quot;false&quot; to
    /// <see cref="ManagedExecution.Default" />.
    /// </summary>
    public class ManagedExecutionConverter : EnumConverter {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedExecutionConverter" />
        /// class.
        /// </summary>
        public ManagedExecutionConverter() : base(typeof(ManagedExecution)) {
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the 
        /// specified context and culture information.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="culture">A <see cref="CultureInfo"/> object. If a <see langword="null"/> is passed, the current culture is assumed.</param>
        /// <param name="value">The <see cref="Object"/> to convert.</param>
        /// <returns>
        /// An <see cref="Object"/> that represents the converted value.
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string) {
                string stringValue = (string) value;
                if (string.Compare(stringValue, Boolean.TrueString, true, culture) == 0) {
                    return ManagedExecution.Auto;
                }
                if (string.Compare(stringValue, Boolean.FalseString, true, culture) == 0) {
                    return ManagedExecution.Default;
                }

                return Enum.Parse(typeof(ManagedExecution), stringValue, true);
            }

            // default to EnumConverter behavior
            return base.ConvertFrom(context, culture, value);
        }
    }
}
