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
// Ian Maclean (ian_maclean@another.com)
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("datetime", "Date/Time")]
    public class DateFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public DateFunctions(Project project, PropertyDictionary propDict ) : base(project, propDict) {
        }

        #endregion Public Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Returns current date and time
        /// </summary>
        /// <returns>
        /// The current date and time.
        /// </returns>
        [Function("now")]
        public static DateTime Now() {
            return DateTime.Now;
        }

        /// <summary>
        /// Return the difference in seconds between two dates.
        /// </summary>
        /// <param name="date1">first date</param>
        /// <param name="date2">second date</param>
        /// <returns>
        /// The difference value.
        /// </returns>
        /// <remarks>
        /// It may be useful to know some magic numbers: One hour is 3600 seconds, 
        /// 24 hours is 86400 seconds.
        /// </remarks>
        [Function("diff")]
        public static int Diff(DateTime date1, DateTime date2) {
            return (int)((date1 - date2).TotalSeconds);
        }

        /// <summary>
        /// Adds the specified number of seconds to the date value.
        /// </summary>
        /// <param name="date">date value</param>
        /// <param name="seconds">number of seconds to add to the date value</param>
        /// <returns>
        /// New date which is <paramref name="seconds" /> seconds later than 
        /// <paramref name="date" />.
        /// </returns>
        /// <remarks>
        /// It may be useful to know some magic numbers: One hour is 3600 seconds, 
        /// 24 hours is 86400 seconds.
        /// </remarks>
        [Function("add")]
        public static DateTime Add(DateTime date, int seconds) {
            return date.AddSeconds(seconds);
        }

        #endregion Public Static Methods
    }
}
