// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Chris Jenkin (oneinchhard@hotmail.com)

using System;
using System.Globalization;

using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Sets properties with the current date and time.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   By default the <see cref="TStampTask" /> displays the current date 
    ///   and time and sets the following properties:
    ///   </para>
    ///   <list type="bullet">
    ///     <item><description>tstamp.date to yyyyMMdd</description></item>
    ///     <item><description>tstamp.time to HHmm</description></item>
    ///     <item><description>tstamp.now using the default DateTime.ToString() method</description></item>
    ///   </list>
    ///   <para>
    ///   To set an additional property with a custom date/time use the 
    ///   <see cref="Property" /> and <see cref="Pattern" /> attributes.  
    ///   To set a number of additional properties with the exact same date and 
    ///   time use the <see cref="Formatters" /> nested element (see example).
    ///   </para>
    ///   <para>
    ///   The date and time string displayed by the <see cref="TStampTask" /> 
    ///   uses the computer's default long date and time string format.  You 
    ///   might consider setting these to the 
    ///   <see href="http://www.cl.cam.ac.uk/~mgk25/iso-time.html">ISO 8601 standard 
    ///   for date and time notation</see>.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>Set the <c>build.date</c> property.</para>
    ///   <code>
    ///     <![CDATA[
    /// <tstamp property="build.date" pattern="yyyyMMdd" verbose="true" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Set a number of properties for Ant like compatibility.</para>
    ///   <code>
    ///     <![CDATA[
    /// <tstamp verbose="true">
    ///     <formatter property="TODAY" pattern="dd MMM yyyy"/>
    ///     <formatter property="DSTAMP" pattern="yyyyMMdd" unless="${date.not.needed}" />
    ///     <formatter property="TSTAMP" pattern="HHmm" if="${need.hours}" />
    /// </tstamp>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("tstamp")]
    public class TStampTask : Task {
        #region Private Instance Fields

        private string _property;
        private string _pattern;
        private FormatterCollection _formatters = new FormatterCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// The property to receive the date/time string in the given pattern.
        /// </summary>
        [TaskAttribute("property", Required=false)]
        public string Property {
            get { return _property; }
            set { _property = value; }
        }

        /// <summary>The date/time pattern to be used.</summary>
        /// <remarks>
        ///   <para>The following table lists the standard format characters for each standard pattern. The format characters are case-sensitive; for example, 'g' and 'G' represent slightly different patterns.</para>
        ///   <list type="table">
        ///     <listheader>
        ///       <description>Format Character</description>
        ///       <description>Description Example Format Pattern (en-US)</description>
        ///     </listheader>
        ///     <item><description>d</description><description>MM/dd/yyyy</description></item>
        ///     <item><description>D</description><description>dddd, dd MMMM yyyy</description></item>
        ///     <item><description>f</description><description>dddd, dd MMMM yyyy HH:mm</description></item>
        ///     <item><description>F</description><description>dddd, dd MMMM yyyy HH:mm:ss</description></item>
        ///     <item><description>g</description><description>MM/dd/yyyy HH:mm</description></item>
        ///     <item><description>G</description><description>MM/dd/yyyy HH:mm:ss</description></item>
        ///     <item><description>m, M</description><description>MMMM dd</description></item>
        ///     <item><description>r, R</description><description>ddd, dd MMM yyyy HH':'mm':'ss 'GMT'</description></item>
        ///     <item><description>s</description><description>yyyy'-'MM'-'dd'T'HH':'mm':'ss</description></item>
        ///     <item><description>t</description><description>HH:mm</description></item>
        ///     <item><description>T</description><description>HH:mm:ss</description></item>
        ///     <item><description>u</description><description>yyyy'-'MM'-'dd HH':'mm':'ss'Z'</description></item>
        ///     <item><description>U</description><description>dddd, dd MMMM yyyy HH:mm:ss</description></item>
        ///     <item><description>y, Y</description><description>yyyy MMMM</description></item>
        ///   </list>
        ///   <para>The following table lists the patterns that can be combined to construct custom patterns. The patterns are case-sensitive; for example, "MM" is recognized, but "mm" is not. If the custom pattern contains white-space characters or characters enclosed in single quotation marks, the output string will also contain those characters. Characters not defined as part of a format pattern or as format characters are reproduced literally.</para>
        ///   <list type="table">
        ///     <listheader>
        ///       <description>Format</description>
        ///       <description>Pattern Description</description>
        ///     </listheader>
        ///     <item><description>d</description><description>The day of the month. Single-digit days will not have a leading zero.</description></item>
        ///     <item><description>dd</description><description>The day of the month. Single-digit days will have a leading zero.</description></item>
        ///     <item><description>ddd</description><description>The abbreviated name of the day of the week.</description></item>
        ///     <item><description>dddd</description><description>The full name of the day of the week.</description></item>
        ///     <item><description>M</description><description>The numeric month. Single-digit months will not have a leading zero.</description></item>
        ///     <item><description>MM</description><description>The numeric month. Single-digit months will have a leading zero.</description></item>
        ///     <item><description>MMM</description><description>The abbreviated name of the month.</description></item>
        ///     <item><description>MMMM</description><description>The full name of the month.</description></item>
        ///     <item><description>y</description><description>The year without the century. If the year without the century is less than 10, the year is displayed with no leading zero.</description></item>
        ///     <item><description>yy</description><description>The year without the century. If the year without the century is less than 10, the year is displayed with a leading zero.</description></item>
        ///     <item><description>yyyy</description><description>The year in four digits, including the century.</description></item>
        ///     <item><description>gg</description><description>The period or era. This pattern is ignored if the date to be formatted does not have an associated period or era string.</description></item>
        ///     <item><description>h</description><description>The hour in a 12-hour clock. Single-digit hours will not have a leading zero.</description></item>
        ///     <item><description>hh</description><description>The hour in a 12-hour clock. Single-digit hours will have a leading zero.</description></item>
        ///     <item><description>H</description><description>The hour in a 24-hour clock. Single-digit hours will not have a leading zero.</description></item>
        ///     <item><description>HH</description><description>The hour in a 24-hour clock. Single-digit hours will have a leading zero.</description></item>
        ///     <item><description>m</description><description>The minute. Single-digit minutes will not have a leading zero.</description></item>
        ///     <item><description>mm</description><description>The minute. Single-digit minutes will have a leading zero.</description></item>
        ///     <item><description>s</description><description>The second. Single-digit seconds will not have a leading zero.</description></item>
        ///     <item><description>ss</description><description>The second. Single-digit seconds will have a leading zero.</description></item>
        ///     <item><description>f</description><description>The fraction of a second in single-digit precision. The remaining digits are truncated.</description></item>
        ///     <item><description>ff</description><description>The fraction of a second in double-digit precision. The remaining digits are truncated.</description></item>
        ///     <item><description>fff</description><description>The fraction of a second in three-digit precision. The remaining digits are truncated.</description></item>
        ///     <item><description>ffff</description><description>The fraction of a second in four-digit precision. The remaining digits are truncated.</description></item>
        ///     <item><description>fffff</description><description>The fraction of a second in five-digit precision. The remaining digits are truncated. </description></item>
        ///     <item><description>ffffff</description><description>The fraction of a second in six-digit precision. The remaining digits are truncated. </description></item>
        ///     <item><description>fffffff</description><description>The fraction of a second in seven-digit precision. The remaining digits are truncated. </description></item>
        ///     <item><description>t</description><description>The first character in the AM/PM designator.</description></item>
        ///     <item><description>tt</description><description>The AM/PM designator. </description></item>
        ///     <item><description>z</description><description>The time zone offset ("+" or "-" followed by the hour only). Single-digit hours will not have a leading zero. For example, Pacific Standard Time is "-8".</description></item>
        ///     <item><description>zz</description><description>The time zone offset ("+" or "-" followed by the hour only). Single-digit hours will have a leading zero. For example, Pacific Standard Time is "-08".</description></item>
        ///     <item><description>zzz</description><description>The full time zone offset ("+" or "-" followed by the hour and minutes). Single-digit hours and minutes will have leading zeros. For example, Pacific Standard Time is "-08:00".</description></item>
        ///     <item><description>:</description><description>The default time separator.</description></item>
        ///     <item><description>/</description><description>The default date separator.</description></item>
        ///     <item><description>\ c</description><description>Pattern Where c is any character. Displays the character literally. To display the backslash character, use "\\". </description></item>
        ///   </list>
        /// </remarks>
        [TaskAttribute("pattern", Required=false)]
        public string Pattern {
            get { return _pattern; }
            set { _pattern = value; }
        }

        [BuildElementArray("formatter")]
        public FormatterCollection Formatters {
            get { return _formatters; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            // get and print current date
            DateTime now = DateTime.Now;
            Log(Level.Info, "{0} {1}.", now.ToLongDateString(), now.ToLongTimeString());

            // set default properties
            Properties["tstamp.date"] = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            Properties["tstamp.time"] = now.ToString("HHmm", CultureInfo.InvariantCulture);
            Properties["tstamp.now"] = now.ToString(CultureInfo.InvariantCulture);

            // set custom property
            if (_property != null && _pattern != null) {
                Properties[_property] = now.ToString(_pattern, CultureInfo.InvariantCulture);
                Log(Level.Verbose, "{0} = {1}.", _property, Properties[_property].ToString(CultureInfo.InvariantCulture));
            }

            // set properties set in formatters nested elements
            foreach (Formatter f in Formatters) {
                if (IfDefined && !UnlessDefined) {
                    Properties[f.Property] = now.ToString(f.Pattern, CultureInfo.InvariantCulture);
                    Log(Level.Verbose, "{0} = {1}.", f.Property, Properties[f.Property].ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        #endregion Override implementation of Task

    }
}
