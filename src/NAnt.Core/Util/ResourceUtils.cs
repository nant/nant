// ResourceUtils.cs
//
// Giuseppe Greco <giuseppe.greco@agamura.com>
// Copyright (C) 2005 Agamura, Inc.
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
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System.Reflection;
using System.Resources;
using System.Globalization;

namespace NAnt.Core.Util {
    /// <summary>
    /// Provides resource support to NAnt assemblies. This class cannot
    /// be inherited.
    /// </summary>
    internal sealed class ResourceUtils {

        #region private fields

        private static volatile ResourceManager resourceManagera;

        #endregion private fields

        #region public methods

        /// <summary>
        /// Returns the value of the specified string resource.
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String" /> that contains the name of the
        /// string resource to get.
        /// </param>
        /// <returns>
        /// A <see cref="System.String" /> that contains the value of the
        /// string resource localized for the current culture.
        /// </returns>
        public static string GetString(string name) {
            return GetString(name, null);
        }

        /// <summary>
        /// Returns the value of the specified string resource localized for
        /// the specified culture.
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String" /> that contains the name of the
        /// string resource to get.
        /// </param>
        /// <param name="culture">
        /// A <see cref="System.Globalization.CultureInfo" /> that represents
        /// the culture for which the string resource should be localized.
        /// </param>
        /// <returns>
        /// A <see cref="System.String" /> that contains the value of the
        /// string resource localized for the specified culture.
        /// </returns>
        public static string GetString(string name, CultureInfo culture) {
            if (resourceManager == null) {
                //
                // prevent more than one instance of the ResourceManager class
                // to be initialized
                //
                lock (typeof(ResourceUtils)) {
                    if (resourceManager == null) {
                        Assembly assembly = Assembly.GetCallingAssembly();
                        resourceManager = new ResourceManager(
                            assembly.GetName().Name, assembly);
                    }
                }
            }
            return resourceManager.GetString(name, culture);
        }
        #endregion public methods
    }
}
