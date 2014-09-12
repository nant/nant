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
// Ian Maclean (imaclean@gmail.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Globalization;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Class which provides NAnt functions to work with date and time data.
    /// </summary>
    [FunctionSet("datetime", "Date/Time")]
    public class DateTimeFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public DateTimeFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Gets a <see cref="DateTime" /> that is the current local date and 
        /// time on this computer.
        /// </summary>
        /// <returns>
        /// A <see cref="DateTime" /> whose value is the current date and time.
        /// </returns>
        [Function("now")]
        public static DateTime Now() {
            return DateTime.Now;
        }

        /// <summary>
        /// Gets the year component of the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the year component.</param>
        /// <returns>
        /// The year, between 1 and 9999.
        /// </returns>
        [Function("get-year")]
        public static int GetYear(DateTime date) {
            return date.Year;
        }

        /// <summary>
        /// Gets the month component of the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the month component.</param>
        /// <returns>
        /// The month, between 1 and 12.
        /// </returns>
        [Function("get-month")]
        public static int GetMonth(DateTime date) {
            return date.Month;
        }

        /// <summary>
        /// Gets the day of the month represented by the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the day of the month.</param>
        /// <returns>
        /// The day value, between 1 and 31.
        /// </returns>
        [Function("get-day")]
        public static int GetDay(DateTime date) {
            return date.Day;
        }

        /// <summary>
        /// Gets the hour component of the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the hour component.</param>
        /// <returns>
        /// The hour, between 0 and 23.
        /// </returns>
        [Function("get-hour")]
        public static int GetHour(DateTime date) {
            return date.Hour;
        }

        /// <summary>
        /// Gets the minute component of the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the minute component.</param>
        /// <returns>
        /// The minute, between 0 and 59.
        /// </returns>
        [Function("get-minute")]
        public static int GetMinute(DateTime date) {
            return date.Minute;
        }

        /// <summary>
        /// Gets the seconds component of the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the seconds component.</param>
        /// <returns>
        /// The seconds, between 0 and 59.
        /// </returns>
        [Function("get-second")]
        public static int GetSecond(DateTime date) {
            return date.Second;
        }

        /// <summary>
        /// Gets the milliseconds component of the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the milliseconds component.</param>
        /// <returns>
        /// The millisecond, between 0 and 999.
        /// </returns>
        [Function("get-millisecond")]
        public static int GetMillisecond(DateTime date) {
            return date.Millisecond;
        }

        /// <summary>
        /// Gets the number of ticks that represent the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the number of ticks.</param>
        /// <returns>
        /// The number of ticks that represent the date and time of the 
        /// specified date.
        /// </returns>
        [Function("get-ticks")]
        public static long GetTicks(DateTime date) {
            return date.Ticks;
        }

        /// <summary>
        /// Gets the day of the week represented by the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the day of the week.</param>
        /// <returns>
        /// The day of the week, ranging from zero, indicating Sunday, to six, 
        /// indicating Saturday.
        /// </returns>
        [Function("get-day-of-week")]
        public static int GetDayOfWeek(DateTime date) {
            return (int) date.DayOfWeek;
        }

        /// <summary>
        /// Gets the day of the year represented by the specified date.
        /// </summary>
        /// <param name="date">The date of which to get the day of the year.</param>
        /// <returns>
        /// The day of the year, between 1 and 366.
        /// </returns>
        [Function("get-day-of-year")]
        public static int GetDayOfYear(DateTime date) {
            return (int) date.DayOfYear;
        }

        /// <summary>
        /// Returns the number of days in the specified month of the specified 
        /// year.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month (a number ranging from 1 to 12).</param>
        /// <returns>
        /// The number of days in <paramref name="month" /> for the specified 
        /// <paramref name="year" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="month" /> is less than 1 or greater than 12.</exception>
        [Function("get-days-in-month")]
        public static int GetDaysInMonth(int year, int month) {
            return DateTime.DaysInMonth(year, month);
        }

        /// <summary>
        /// Returns an indication whether the specified year is a leap year.
        /// </summary>
        /// <param name="year">A 4-digit year.</param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="year" /> is a leap year; 
        /// otherwise, <see langword="false" />.
        /// </returns>
        [Function("is-leap-year")]
        public static bool IsLeapYear(int year) {
            return DateTime.IsLeapYear(year);
        }

        #endregion Public Static Methods
    }

    /// <summary>
    /// Class which provides NAnt functions to convert strings in date and time objects and vice versa.
    /// </summary>
    [FunctionSet("datetime", "Conversion")]
    public class DateTimeConversionFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeConversionFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public DateTimeConversionFunctions(Project project, PropertyDictionary properties) : base(project, properties) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Converts the specified string representation of a date and time to 
        /// its <see cref="DateTime" /> equivalent.
        /// </summary>
        /// <param name="s">A string containing a date and time to convert.</param>
        /// <returns>
        /// A <see cref="DateTime" /> equivalent to the date and time contained 
        /// in <paramref name="s" />.
        /// </returns>
        /// <exception cref="FormatException"><paramref name="s" /> does not contain a valid string representation of a date and time.</exception>
        /// <remarks>
        /// The <see cref="DateTimeFormatInfo" /> for the invariant culture is 
        /// used to supply formatting information about <paramref name="s" />.
        /// </remarks>
        [Function("parse")]
        public static DateTime Parse(string s) {
            return DateTime.Parse(s, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the specified <see cref="DateTime" /> to its equivalent
        /// string representation.
        /// </summary>
        /// <param name="value">A <see cref="DateTime" /> to convert.</param>
        /// <returns>
        /// A string representation of <paramref name="value" /> formatted using
        /// the general format specifier ("G").
        /// </returns>
        /// <remarks>
        /// <paramref name="value" /> is formatted with the 
        /// <see cref="DateTimeFormatInfo" /> for the invariant culture.
        /// </remarks>
        [Function("to-string")]
        public static string ToString(DateTime value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the specified <see cref="DateTime" /> to its equivalent
        /// string representation.
        /// </summary>
        /// <param name="value">A <see cref="DateTime" /> to convert.</param>
        /// <param name="format">A format string.</param>
        /// <returns>
        /// A string representation of <paramref name="value" /> formatted
        ///	using the specified format
        /// </returns>
        /// <remarks>
        /// <paramref name="value" /> is formatted with the 
        /// <see cref="DateTimeFormatInfo" /> for the invariant culture.
        /// </remarks>
        [Function("format-to-string")]
        public static string ToString(DateTime value, string format) {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
        #endregion Public Static Methods
    }
}
