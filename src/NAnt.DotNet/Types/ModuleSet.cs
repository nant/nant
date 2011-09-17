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
using System.IO;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.DotNet.Types {
    /// <summary>
    /// <para>
    /// One or more modules to compile into an assembly.
    /// </para>
    /// </summary>
    /// <example>
    ///   <para>
    ///   Define a global <c>&lt;moduleset&gt;</c> that can be referenced by
    ///   other tasks or types.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    ///         <moduleset id="client-modules" dir="${build}">
    ///             <module file="Client.netmodule" />
    ///             <module file="Common.netmodule" />
    ///         </moduleset>
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    [ElementName("moduleset")]
    public class ModuleSet : DataTypeBase {
        #region Private Instance Fields

        private readonly ModuleCollection _modules;
        private DirectoryInfo _dir;

        #endregion Private Instance Fields

         #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleSet" /> class.
        /// </summary>
        public ModuleSet() {
            _modules = new ModuleCollection(this);
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The base of the directory of this <see cref="ModuleSet" />. 
        /// The default is the project base directory.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Dir {
            get {
                if (_dir == null) {
                    if (Project != null) {
                        return new DirectoryInfo(Project.BaseDirectory);
                    }
                }   
                return _dir;
            }
            set { _dir = value; }
        }

        /// <summary>
        /// The modules to add to this <see cref="ModuleSet" />.
        /// </summary>
        [BuildElementArray("module")]
        public ModuleCollection Modules {
            get { return _modules; }
        }

        #endregion Public Instance Properties
    }
}