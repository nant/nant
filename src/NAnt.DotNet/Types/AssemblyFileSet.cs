// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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
// Ian MacLean ( ian_maclean@another.com )

using System;
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// A specialized <see cref="FileSet" /> used for setting the lib directories.
    /// </summary>
    /// <remarks>
    /// The primary reason for this class is to allow the <see cref="BaseDirectory" />
    /// to always be the same value as the parent <see cref="AssemblyFileSet" />
    /// </remarks>
    /// <seealso cref="FileSet" />
    [Serializable()]
    public class LibDirectorySet : FileSet {
        #region Public Instance Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LibDirectorySet" /> class.
        /// </summary>
        /// <param name="parent"></param>
        public LibDirectorySet(AssemblyFileSet parent) {
            _parent = parent;
        }

        #endregion Public Instance Constructors

        #region Overrides from FileSet

        /// <summary>
        /// override this. We will always use the base directory of the parent.
        /// overriding without the TaskAttribute attribute prevents it being set 
        /// in the source xml
        /// </summary>
        public override DirectoryInfo BaseDirectory {
            get { return _parent.BaseDirectory; }
           
        }

        #endregion Overrides from FileSet

        #region Private Instance Fields

        private AssemblyFileSet _parent;

        #endregion Private Instance Fields
    }

    /// <summary>
    /// Specialized <see cref="FileSet" /> class for managing assembly files.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   If an include pattern does not contain any wildcard characters then 
    ///   the assembly will be searched for in following locations (in the order listed):
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <description>
    ///       The base directory of the fileset.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///       The directories specified using the nested &lt;lib&gt; element.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///       The list of reference assemblies of the current target framework.
    ///       </description>
    ///     </item>
    ///   </list>
    ///   <para>
    ///   The reference assemblies of a given target framework are defined using
    ///   &lt;reference-assemblies&gt; filesets in the &lt;framework&gt; node
    ///   of the NAnt configuration file.
    ///   </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Define a reference with name &quot;sys.assemblies&quot;, holding
    ///   a set of system assemblies.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <assemblyfileset id="sys.assemblies">
    ///     <include name="System.dll" />
    ///     <include name="System.Data.dll" />
    ///     <include name="System.Xml.dll" />
    /// </assemblyfileset>
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   Use the predefined set of assemblies to compile a C# assembly.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <csc target="exe" output="HelloWorld.exe">
    ///     <sources>
    ///         <include name="**/*.cs" />
    ///     </sources>
    ///     <references refid="sys.assemblies" />
    /// </csc>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Compile a C# assembly using assembly references that are searched for
    ///   in the &quot;Third Party Assemblies&quot; and &quot;Company Assemblies&quot;
    ///   directories.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <csc target="exe" output="HelloWorld.exe">
    ///     <sources>
    ///         <include name="**/*.cs" />
    ///     </sources>
    ///     <references>
    ///         <lib>
    ///             <include name="Third Party Assemblies" />
    ///             <include name="Company Assemblies" />
    ///         </lib>
    ///         <include name="log4net.dll" />
    ///         <include name="Company.Business.dll" />
    ///     </references>
    /// </csc>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <seealso cref="FileSet" />
    [Serializable()]
    [ElementName("assemblyfileset")]
    public class AssemblyFileSet : FileSet, ICloneable {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyFileSet" /> class.
        /// </summary>
        public AssemblyFileSet() : base() {
            // set the parent reference to point back to us
            _lib = new LibDirectorySet(this);
        }
           
        /// <summary>
        /// copy constructor for FileSet. Required in order to 
        /// assign references of FileSet type where 
        /// AssemblyFileSets are used
        /// </summary>
        /// <param name="fs"></param>
        public AssemblyFileSet(FileSet fs) : base(fs) {
           _lib = new LibDirectorySet(this);
        }
                
        #endregion Public Instance Constructors

        #region Public Instance Properties
                
        /// <summary>
        /// Additional directories to search in for assembly references.
        /// </summary>
        /// <remarks>
        /// <para>
        /// loosely Corresponds with the <c>/lib[path]:</c> flag of the various compiler tasks.
        /// </para>
        /// </remarks>
        [BuildElement("lib")]
        public LibDirectorySet Lib {
            get { return _lib; }
            set { _lib = value; }
        }
        
        #endregion Public Instance Properties
        
        #region Overrides from FileSet

        /// <summary>
        /// Do a normal scan and then resolve assemblies.
        /// </summary>
        public override void Scan() {
            base.Scan();
            
            ResolveReferences();
        }

        #endregion Overrides from FileSet
        
        #region private intance methods
        
        /// <summary>
        /// Resolves references to system assemblies and assemblies that can be 
        /// resolved using directories specified in <see cref="Lib" />.
        /// </summary>
        protected void ResolveReferences() {
            foreach (string pattern in Includes) {
                if (Path.GetFileName(pattern) == pattern) {
                    string localPath = Path.Combine(BaseDirectory.FullName, pattern);

                    // check if a file match the pattern exists in the 
                    // base directory of the references fileset
                    if (File.Exists(localPath)) {
                        // the file will already be included as part of
                        // the fileset scan process
                        continue;
                    }

                    foreach (string libPath in Lib.DirectoryNames) {
                        string fullPath = Path.Combine(libPath, pattern);

                        // check whether an assembly matching the pattern
                        // exists in the assembly directory of the current
                        // framework
                        if (File.Exists(fullPath)) {
                            // found a system reference
                            this.FileNames.Add(fullPath);

                            // continue with the next pattern
                            continue;
                        }
                    }

                    if (Project.TargetFramework != null) {
                        string resolveAssembly = Project.TargetFramework.
                            ResolveAssembly (pattern);
                        if (resolveAssembly != null) {
                            // found reference assembly
                            this.FileNames.Add(resolveAssembly);

                            // continue with the next pattern
                            continue;
                        }
                    }
                }
            }
        }

        #endregion private intance methods

        #region Private Instance Fields

        private LibDirectorySet _lib = null;

        #endregion Private Instance Fields
    }
}