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
// Ian MacLean (imaclean@gmail.com)

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
        #region Private Static Fields

        private static ResourceManager _sharedResourceManager;
        private static readonly Hashtable _resourceManagerDictionary = new Hashtable();

        #endregion Private Static Fields

        #region Private Instance Constructors

        /// <summary>
        /// Prevents the <see cref="ResourceUtils" /> class from being 
        /// instantiated explicitly.
        /// </summary>
        private ResourceUtils() {}

        #endregion Private Instance Constructors

        #region Public Static Methods

        /// <summary>
        /// Registers the assembly to be used as the fallback if resources
        /// aren't found in the local satellite assembly.
        /// </summary>
        /// <param name="assembly">
        /// A <see cref="T:System.Reflection.Assembly" /> that represents the
        /// assembly to register.
        /// </param>
        /// <example>
        /// The following example shows how to register a shared satellite
        /// assembly.
        /// <code>
        /// <![CDATA[
        /// Assembly sharedAssembly = Assembly.Load("MyResources.dll");
        /// ResourceUtils.RegisterSharedAssembly(sharedAssembly);
        /// ]]>
        /// </code>
        /// </example>
        public static void RegisterSharedAssembly(Assembly assembly) {
            _sharedResourceManager = new ResourceManager(assembly.GetName().Name, assembly); 
        }

        /// <summary>
        /// Returns the value of the specified string resource.
        /// </summary>
        /// <param name="name">
        /// A <see cref="T:System.String" /> that contains the name of the
        /// resource to get.
        /// </param>
        /// <returns>
        /// A <see cref="T:System.String" /> that contains the value of the
        /// resource localized for the current culture.
        /// </returns>
        /// <remarks>
        /// The returned resource is localized for the cultural settings of the
        /// current <see cref="T:System.Threading.Thread" />.
        /// <note>
        /// The <c>GetString</c> method is thread-safe.
        /// </note>
        /// </remarks>
        /// <example>
        /// The following example demonstrates the <c>GetString</c> method using
        /// the cultural settings of the current <see cref="T:System.Threading.Thread" />.
        /// <code>
        /// <![CDATA[
        /// string localizedString = ResourceUtils.GetString("String_HelloWorld");
        /// ]]>
        /// </code>
        /// </example>
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
        /// A <see cref="T:System.String" /> that contains the value of the
        /// resource localized for the specified culture. 
        ///</returns>
        /// <remarks>
        /// <note>
        /// The <c>GetString</c> method is thread-safe.
        /// </note>
        /// </remarks>
        /// <example>
        /// The following example demonstrates the <c>GetString</c> method using
        /// a specific culture.
        /// <code>
        /// <![CDATA[
        /// CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        /// string localizedString = ResourceUtils.GetString("String_HelloWorld", culture);
        /// ]]>
        /// </code>
        /// </example>
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
        /// A <see cref="T:System.String" /> that contains the name of the
        /// resource to get.
        /// </param>
        /// <param name="culture">
        /// A <see cref="T:System.Globalization.CultureInfo" /> that represents
        /// the culture for which the resource is localized.
        /// </param>
        /// <param name="assembly">
        /// A <see cref="T:System.Reflection.Assembly" />
        /// </param>
        /// <returns>
        /// A <see cref="T:System.String" /> that contains the value of the
        /// resource localized for the specified culture.
        /// </returns>
        /// <remarks>
        /// <note>
        /// The <c>GetString</c> method is thread-safe.
        /// </note>
        /// </remarks>
        /// <example>
        /// The following example demonstrates the <c>GetString</c> method using
        /// specific culture and assembly.
        /// <code>
        /// <![CDATA[
        /// CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
        /// Assembly assembly = Assembly.GetCallingAssembly();
        /// string localizedString = ResourceUtils.GetString("String_HelloWorld", culture, assembly);
        /// ]]>
        /// </code>
        /// </example>
        public static string GetString(string name, CultureInfo culture, Assembly assembly) {
            string assemblyName = assembly.GetName().Name;

            if (!_resourceManagerDictionary.Contains(assemblyName)) {
                RegisterAssembly(assembly);
            }

            // retrieve resource manager for assembly
            ResourceManager resourceManager = (ResourceManager) 
                _resourceManagerDictionary[assemblyName];

            // try to get the required string from the given assembly
            string localizedString = resourceManager.GetString(name, culture);

            // if the given assembly does not contain the required string, then
            // try to get it from the shared satellite assembly, if registered
            if (localizedString == null && _sharedResourceManager != null) {
                return _sharedResourceManager.GetString(name, culture);
            }
            return localizedString;
        }

        #endregion Public Static Methods

        #region Private Static Methods

        /// <summary>
        /// Registers the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// A <see cref="T:System.Reflection.Assembly" /> that represents the
        /// assembly to register.
        /// </param>
        private static void RegisterAssembly(Assembly assembly) {
            lock (_resourceManagerDictionary) {
                string assemblyName = assembly.GetName().Name;

                _resourceManagerDictionary.Add(assemblyName,
                    new ResourceManager(GetResourceName(assemblyName), 
                    assembly));
            }
        }

        /// <summary>
        /// Determines the manifest resource name of the resource holding the
        /// localized strings.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <returns>
        /// The manifest resource name of the resource holding the localized
        /// strings for the specified assembly.
        /// </returns>
        /// <remarks>
        /// The manifest resource name of the resource holding the localized
        /// strings should match the name of the assembly, minus <c>Tasks</c>
        /// suffix.
        /// </remarks>
        private static string GetResourceName(string assemblyName) {
            string resourceName = null;
            if (assemblyName.EndsWith("Tasks")) {
                // hack to determine the manifest resource name as our
                // assembly names have a Tasks suffix, while our
                // root namespace in VS.NET does not
                resourceName = assemblyName.Substring(0, assemblyName.Length - 5);
            } else {
                resourceName = assemblyName;
            }
            return resourceName + ".Resources.Strings";
        }

        #endregion Private Static Methods
    }
}
