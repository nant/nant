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
//
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions for working with time spans.
    /// </summary>
    [FunctionSet("timespan", "Date/Time")]
    public class TimeSpanFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public TimeSpanFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Returns the total number of days represented by the specified 
        /// <see cref="TimeSpan" />, expressed in whole and fractional days.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The total number of days represented by the given <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-total-days")]
        public static double GetTotalDays(TimeSpan value) {
            return value.TotalDays;
        }

        /// <summary>
        /// Returns the total number of hours represented by the specified 
        /// <see cref="TimeSpan" />, expressed in whole and fractional hours.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The total number of hours represented by the given <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-total-hours")]
        public static double GetTotalHours(TimeSpan value) {
            return value.TotalHours;
        }

        /// <summary>
        /// Returns the total number of minutes represented by the specified 
        /// <see cref="TimeSpan" />, expressed in whole and fractional minutes.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The total number of minutes represented by the given <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-total-minutes")]
        public static double GetTotalMinutes(TimeSpan value) {
            return value.TotalMinutes;
        }

        /// <summary>
        /// Returns the total number of seconds represented by the specified 
        /// <see cref="TimeSpan" />, expressed in whole and fractional seconds.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The total number of seconds represented by the given <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-total-seconds")]
        public static double GetTotalSeconds(TimeSpan value) {
            return value.TotalSeconds;
        }

        /// <summary>
        /// Returns the total number of milliseconds represented by the specified 
        /// <see cref="TimeSpan" />, expressed in whole and fractional milliseconds.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The total number of milliseconds represented by the given 
        /// <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-total-milliseconds")]
        public static double GetTotalMilliseconds(TimeSpan value) {
            return value.TotalMilliseconds;
        }

        /// <summary>
        /// Returns the number of whole days represented by the specified 
        /// <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The number of whole days represented by the given 
        /// <see cref="TimeSpan" />.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Remove all files that have not been modified in the last 7 days from directory "binaries".</para>
        ///   <code>
        ///     <![CDATA[
        /// <foreach item="File" in="binaries" property="filename">
        ///     <if test="${timespan::get-days(datetime::now() - file::get-last-write-time(filename)) >= 7}">
        ///         <delete file="${filename}" />
        ///     </if>
        /// </foreach>
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("get-days")]
        public static int GetDays(TimeSpan value) {
            return value.Days;
        }

        /// <summary>
        /// Returns the number of whole hours represented by the specified 
        /// <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The number of whole hours represented by the given 
        /// <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-hours")]
        public static int GetHours(TimeSpan value) {
            return value.Hours;
        }

        /// <summary>
        /// Returns the number of whole minutes represented by the specified 
        /// <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The number of whole minutes represented by the given 
        /// <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-minutes")]
        public static int GetMinutes(TimeSpan value) {
            return value.Minutes;
        }

        /// <summary>
        /// Returns the number of whole seconds represented by the specified 
        /// <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The number of whole seconds represented by the given 
        /// <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-seconds")]
        public static int GetSeconds(TimeSpan value) {
            return value.Seconds;
        }

        /// <summary>
        /// Returns the number of whole milliseconds represented by the specified
        /// <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The number of whole milliseconds represented by the given 
        /// <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-milliseconds")]
        public static int GetMilliseconds(TimeSpan value) {
            return value.Milliseconds;
        }

        /// <summary>
        /// Returns the number of ticks contained in the specified
        /// <see cref="TimeSpan" />.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" />.</param>
        /// <returns>
        /// The number of ticks contained in the given <see cref="TimeSpan" />.
        /// </returns>
        [Function("get-ticks")]
        public static long GetTicks(TimeSpan value) {
            return value.Ticks;
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan" /> that represents a specified number
        /// of days, where the specification is accurate to the nearest millisecond.
        /// </summary>
        /// <param name="value">A number of days, accurate to the nearest millisecond.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that represents <paramref name="value" />.
        /// </returns>
        [Function("from-days")]
        public static TimeSpan FromDays(double value) {
            return TimeSpan.FromDays(value);
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan" /> that represents a specified number
        /// of hours, where the specification is accurate to the nearest 
        /// millisecond.
        /// </summary>
        /// <param name="value">A number of hours, accurate to the nearest millisecond.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that represents <paramref name="value" />.
        /// </returns>
        [Function("from-hours")]
        public static TimeSpan FromHours(double value) {
            return TimeSpan.FromHours(value);
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan" /> that represents a specified number
        /// of minutes, where the specification is accurate to the nearest 
        /// millisecond.
        /// </summary>
        /// <param name="value">A number of minutes, accurate to the nearest millisecond.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that represents <paramref name="value" />.
        /// </returns>
        [Function("from-minutes")]
        public static TimeSpan FromMinutes(double value) {
            return TimeSpan.FromMinutes(value);
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan" /> that represents a specified number
        /// of seconds, where the specification is accurate to the nearest 
        /// millisecond.
        /// </summary>
        /// <param name="value">A number of seconds, accurate to the nearest millisecond.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that represents <paramref name="value" />.
        /// </returns>
        [Function("from-seconds")]
        public static TimeSpan FromSeconds(double value) {
            return TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan" /> that represents a specified number
        /// of milliseconds.
        /// </summary>
        /// <param name="value">A number of milliseconds.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that represents <paramref name="value" />.
        /// </returns>
        [Function("from-milliseconds")]
        public static TimeSpan FromMilliseconds(double value) {
            return TimeSpan.FromMilliseconds(value);
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan" /> that represents a specified time, 
        /// where the specification is in units of ticks.
        /// </summary>
        /// <param name="value">A number of ticks that represent a time.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that represents <paramref name="value" />.
        /// </returns>
        [Function("from-ticks")]
        public static TimeSpan FromTicks(long value) {
            return TimeSpan.FromTicks(value);
        }

        #endregion Public Static Methods
    }

    /// <summary>
    /// Class which provides NAnt functions to convert strings in date and time span objects and vice versa.
    /// </summary>
    [FunctionSet("timespan", "Conversion")]
    public class TimeSpanConversionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanConversionFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public TimeSpanConversionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Constructs a <see cref="TimeSpan" /> from a time indicated by a 
        /// specified string.
        /// </summary>
        /// <param name="s">A string.</param>
        /// <returns>
        /// A <see cref="TimeSpan" /> that corresponds to <paramref name="s" />.
        /// </returns>
        /// <exception cref="FormatException"><paramref name="s" /> has an invalid format.</exception>
        /// <exception cref="OverflowException">At least one of the hours, minutes, or seconds components is outside its valid range.</exception>
        [Function("parse")]
        public static TimeSpan Parse(string s) {
            return TimeSpan.Parse(s);
        }

        /// <summary>
        /// Converts the specified <see cref="TimeSpan" /> to its equivalent 
        /// string representation.
        /// </summary>
        /// <param name="value">A <see cref="TimeSpan" /> to convert.</param>
        /// <returns>
        /// The string representation of <paramref name="value" />. The format 
        /// of the return value is of the form: [-][d.]hh:mm:ss[.ff].
        /// </returns>
        [Function("to-string")]
        public static string ToString(TimeSpan value) {
            return value.ToString();
        }

        #endregion Public Static Methods
    }
}
