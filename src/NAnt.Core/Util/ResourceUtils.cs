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
// Ian MacLean ( imaclean@gmail.com )

using System.Reflection;
using System.Resources;
using System.Globalization;
using System.Collections;
using System.Runtime.CompilerServices;

namespace NAnt.Core.Util {
    /// <summary>
    /// Provides resource support to NAnt assemblies. This class cannot
    /// be inherited from.
    /// </summary>
    public sealed class ResourceUtils {

        #region private fields

        private static ResourceManager _sharedResourceManager;
        private static Hashtable _resourceManagerDictionary = new Hashtable();

        #endregion private fields

        #region public methods

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assembly"></param>
        public static void RegisterAssembly(Assembly assembly) {
            lock (_resourceManagerDictionary) {
                _resourceManagerDictionary.Add(assembly.GetName().Name,
                    new ResourceManager("Strings", assembly));
            }        
        }

        /// <summary>
        /// Register the assemlby to be used as the fallback if resources
        /// aren't found in the local satellite assembly.
        /// </summary>
        /// <param name="assembly"></param>
        private static void RegisterSharedAssembly(Assembly assembly) {
            
            _sharedResourceManager = new ResourceManager(assembly.GetName().Name, assembly); 
            
        }
        /// <summary>
        /// Returns the value of the specified string resource.
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String" /> that contains the name of the
        /// resource to get.
        /// </param>
        /// <returns>
        /// A <see cref="System.String" /> that contains the value of the
        /// resource localized for the current culture.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetString(string name) {
            Assembly assembly = Assembly.GetCallingAssembly();
            return GetString(name, null, assembly);
        }        
        
        /// <summary>
        /// Returns the value of the specified string resource localized for
        /// the specified culture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="culture"></param>
        /// <returns>
        /// A <see cref="System.String" /> that contains the value of the
        /// resource localized for the specified culture. 
        ///</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetString(string name, CultureInfo culture ) {
            Assembly assembly = Assembly.GetCallingAssembly();
            return GetString(name, culture, assembly);
        }

        /// <summary>
        /// Returns the value of the specified string resource localized for
        /// the specified culture for the specified assembly.
        /// </summary>
        /// <param name="name">
        /// A <see cref="System.String" /> that contains the name of the
        /// resource to get.
        /// </param>
        /// <param name="culture">
        /// A <see cref="System.Globalization.CultureInfo" /> that represents
        /// the culture for which the resource is localized.
        /// </param>
        /// <param name="assembly">
        /// A <see cref="System.Reflection.Assembly" />
        /// </param>
        /// <returns>
        /// A <see cref="System.String" /> that contains the value of the
        /// resource localized for the specified culture.
        /// </returns>
        public static string GetString(string name, CultureInfo culture, Assembly assembly) {
            string localizedString = null;
            
            ResourceManager resourceManager = null;
            
            if ( ! _resourceManagerDictionary.Contains(assembly.GetName().Name ) )  {
                lock (_resourceManagerDictionary) 
                {
                    RegisterAssembly(assembly);
                }
            }
            // get the required Manager
            resourceManager = _resourceManagerDictionary[assembly.GetName().Name] as ResourceManager;
            localizedString = resourceManager.GetString(name, culture);
            
            // try the shared resources if we didn't find it in the specific resource manager
            if ( localizedString == null && _sharedResourceManager != null) {
                return _sharedResourceManager.GetString(name, culture);
            }
            return localizedString;

        }
        
        #endregion public methods
    }

}
