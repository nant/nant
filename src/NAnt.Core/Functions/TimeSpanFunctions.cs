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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    [FunctionSet("timespan", "Date/Time")]
    public class TimeSpanFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public TimeSpanFunctions(Project project, PropertyDictionary propDict ) : base(project, propDict) {
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

        #endregion Public Static Methods
    }
}
