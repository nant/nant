// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
using System.IO;
using System.Reflection;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Functions to return information for a given assembly.
    /// </summary>
    [FunctionSet("assembly", "Assembly")]
    public class AssemblyFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public AssemblyFunctions(Project project, PropertyDictionary properties) : base(project, properties) {}

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Loads an assembly given its file name or path.
        /// </summary>
        /// <param name="assemblyFile">The name or path of the file that contains the manifest of the assembly.</param>
        /// <returns>
        /// The loaded assembly.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="assemblyFile" /> is an empty <see cref="string" />.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="assemblyFile" /> is not found, or the module you are trying to load does not specify a filename extension.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="assemblyFile" /> is not a valid assembly.</exception>
        /// <exception cref="PathTooLongException">An assembly or module was loaded twice with two different evidences, or the assembly name is longer than MAX_PATH characters.</exception>
        [Function("load-from-file")]
        public Assembly LoadFromFile(string assemblyFile) {
            return Assembly.LoadFrom(Project.GetFullPath(assemblyFile));

        }

        /// <summary>
        /// Loads an assembly given the long form of its name.
        /// </summary>
        /// <param name="assemblyString">The long form of the assembly name.</param>
        /// <returns>
        /// The loaded assembly.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyString" /> is a <see langword="null" />.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="assemblyString" /> is not found.</exception>
        /// <example>
        ///   <para>
        ///   Determine the location of the Microsoft Access 11 Primary Interop 
        ///   Assembly by loading it using its fully qualified name, and copy it
        ///   to the build directory.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <property name="access.pia.path" value="${assembly::get-location(assembly::load('Microsoft.Office.Interop.Access, Version=11.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c'))}" />
        /// <copy file="${access.pia.path}" todir="${build.dir}" />
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("load")]
        public Assembly Load(string assemblyString) {
            return Assembly.Load(assemblyString);
        }

        #endregion Public Instance Methods

        #region Public Static Methods
        
        /// <summary>
        /// Gets the full name of the assembly, also known as the display name.
        /// </summary>
        /// <param name="assembly">The assembly to get the full name for.</param>
        /// <returns>
        /// The full name of the assembly, also known as the display name.
        /// </returns>
        [Function("get-full-name")]
        public static string GetFullName(Assembly assembly) {
            return assembly.FullName;
        }
        
        /// <summary>
        /// Gets an <see cref="AssemblyName" /> for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get an <see cref="AssemblyName" /> for.</param>
        /// <returns>
        /// An <see cref="AssemblyName" /> for the specified assembly.
        /// </returns>
        /// <seealso cref="AssemblyNameFunctions" />
        [Function("get-name")]
        public static AssemblyName GetName(Assembly assembly) {
            return assembly.GetName(false);
        }

        /// <summary>
        /// Gets the physical location, in codebase format, of the loaded file 
        /// that contains the manifest.
        /// </summary>
        /// <param name="assembly">The assembly to get the location for.</param>
        /// <returns>
        /// The location of the specified assembly.
        /// </returns>
        [Function("get-location")]
        public static string GetLocation(Assembly assembly) {
            return assembly.Location;
        }

        /// <summary>
        /// Gets the value of the <see cref="AssemblyInformationalVersionAttribute"/> for the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get an <see cref="AssemblyInformationalVersionAttribute"/> for.</param>
        /// <returns>The InformationalVersion from the <see cref="AssemblyInformationalVersionAttribute"/>.</returns>
        [Function("get-informational-version")]
        public static string GetInformationalVersion(Assembly assembly) {
            return GetAttributeProperty(assembly, typeof(AssemblyInformationalVersionAttribute), "InformationalVersion");
        }

        /// <summary>
        /// Gets a custom attribute from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly from which to get the custom attribute.</param>
        /// <param name="attributeAssemblyPath">The assembly which contains the custom attribute type.</param>
        /// <param name="attributeTypeName">The name of the custom attribute type.</param>
        /// <param name="propertyName">The name of the property to retreive from the custom attribute instance.</param>
        /// <returns>The specified property from the custom attribute, as a string.</returns>
        [Function("get-attribute-property")]
        public static string GetAttributeProperty(Assembly assembly, string attributeAssemblyPath, string attributeTypeName, string propertyName) {
            Assembly attributeAssembly = Assembly.LoadFile(attributeAssemblyPath);

            return GetAttributeProperty(assembly, attributeAssembly, attributeTypeName, propertyName);
        }

        private static string GetAttributeProperty(Assembly assembly, Assembly attributeAssembly, string attributeTypeName, string propertyName) {
            Type attributeType = attributeAssembly.GetType(attributeTypeName, true);

            return GetAttributeProperty(assembly, attributeType, propertyName);
        }

        private static string GetAttributeProperty(Assembly assembly, Type attributeType, string propertyName) {
            PropertyInfo property = attributeType.GetProperty(propertyName, BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            object[] attributes = assembly.GetCustomAttributes(attributeType, true);

            if (attributes != null && attributes.Length > 0) {
                object attribute = attributes[0];
                object value = property.GetValue(attribute, null);

                return Convert.ToString(value);
            }

            return null;
        }

        #endregion Public Static Methods
    }
}
