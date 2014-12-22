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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.IO;
using System.Reflection;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Functions that return information about an assembly's identity.
    /// </summary>
    [FunctionSet("assemblyname", "Assembly")]
    public class AssemblyNameFunctions : FunctionSetBase {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyNameFunctions"/> class.
        /// </summary>
        /// <param name="project">The current project.</param>
        /// <param name="properties">The projects properties.</param>
        public AssemblyNameFunctions(Project project, PropertyDictionary properties) : base(project, properties) {}

        #endregion Public Instance Constructors

        #region Public Static Methods
        
        /// <summary>
        /// Gets the location of the assembly as a URL.
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName" /> of the assembly.</param>
        /// <returns>
        /// The location of the assembly as a URL.
        /// </returns>
        /// <seealso cref="AssemblyFunctions.GetName(Assembly)" />
        [Function("get-codebase")]
        public static string GetCodeBase(AssemblyName assemblyName) {
            return assemblyName.CodeBase; 
        }

        /// <summary>
        /// Gets the URI, including escape characters, that represents the codebase.
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName" /> of the assembly.</param>
        /// <returns>
        /// The URI, including escape characters, that represents the codebase.
        /// </returns>
        /// <seealso cref="AssemblyFunctions.GetName(Assembly)" />
        [Function("get-escaped-codebase")]
        public static string GetEscapedCodeBase(AssemblyName assemblyName) {
            return assemblyName.EscapedCodeBase; 
        }

        /// <summary>
        /// Gets the full name of the assembly, also known as the display name.
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName" /> of the assembly.</param>
        /// <returns>
        /// The full name of the assembly, also known as the display name.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Output the full name of the <c>nunit.framework</c> assembly to the
        ///   build log.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="${assemblyname::get-full-name(assemblyname::get-assembly-name('nunit.framework.dll'))}" />
        ///     ]]>
        ///   </code>
        /// </example>
        /// <seealso cref="AssemblyFunctions.GetName(Assembly)" />
        [Function("get-full-name")]
        public static string GetFullName(AssemblyName assemblyName) {
            return assemblyName.FullName; 
        }

        /// <summary>
        /// Gets the simple, unencrypted name of the assembly.
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName" /> of the assembly.</param>
        /// <returns>
        /// The simple, unencrypted name of the assembly.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Output the simple name of the <c>nunit.framework</c> assembly to 
        ///   the build log.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="${assemblyname::get-name(assemblyname::get-assembly-name('nunit.framework.dll'))}" />
        ///     ]]>
        ///   </code>
        /// </example>
        /// <seealso cref="AssemblyFunctions.GetName(Assembly)" />
        [Function("get-name")]
        public static string GetName(AssemblyName assemblyName) {
            return assemblyName.Name; 
        }

        /// <summary>
        /// Gets the version of the assembly.
        /// </summary>
        /// <param name="assemblyName">The <see cref="AssemblyName" /> of the assembly.</param>
        /// <returns>
        /// The version of the assembly.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Output the major version of the <c>nunit.framework</c> assembly 
        ///   to the build log.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="${version::get-major(assemblyname::get-version(assemblyname::get-assembly-name('nunit.framework.dll')))}" />
        ///     ]]>
        ///   </code>
        /// </example>
        /// <seealso cref="AssemblyFunctions.GetName(Assembly)" />
        /// <seealso cref="VersionFunctions" />
        [Function("get-version")]
        public static Version GetVersion(AssemblyName assemblyName) {
            return assemblyName.Version; 
        }

        /// <summary>
        /// Gets the <see cref="AssemblyName" /> for a given file.
        /// </summary>
        /// <param name="assemblyFile">The assembly file for which to get the <see cref="AssemblyName" />.</param>
        /// <returns>
        /// An <see cref="AssemblyName" /> object representing the given file.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="assemblyFile" /> is an empty <see cref="string" />.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="assemblyFile" /> does not exist.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="assemblyFile" /> is not a valid assembly.</exception>
        /// <remarks>
        /// The assembly is not added to this domain.
        /// </remarks>
        /// <example>
        ///   <para>
        ///   Output the full name of the <c>nunit.framework</c> assembly to the
        ///   build log.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="${assemblyname::get-full-name(assemblyname::get-assembly-name('nunit.framework.dll'))}" />
        ///     ]]>
        ///   </code>
        /// </example>
        [Function("get-assembly-name")]
        public AssemblyName GetAssemblyName(string assemblyFile) {
            return AssemblyName.GetAssemblyName(Project.GetFullPath(assemblyFile)); 
        }
        
        #endregion Public Static Methods
    }
}
