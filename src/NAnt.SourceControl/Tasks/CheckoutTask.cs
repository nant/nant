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
// Clayton Harbour (claytonharbour@sporadicism.com)

using System;

using ICSharpCode.SharpCvsLib.Commands;

using NAnt.Core.Attributes;
using NAnt.Core.Tasks;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Checks out a CVS module to the required directory.
    /// </summary>
    /// <example>
    ///   <para>Checkout NAnt.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-checkout 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     password="" 
    ///     module="nant" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Checkout NAnt revision named <c>your_favorite_revision_here</c> to the 
    ///   folder <c>c:\src\nant\replacement_for_module_directory_name</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-checkout 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     password="" 
    ///     module="nant">
    ///     <options>
    ///         <option name="sticky-tag" value="your_favorite_revision_here" />
    ///         <option name="override-directory" value="replacement_for_module_directory_name" />
    ///     </options>
    /// </cvs-checkout>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Checkout NAnt revision named <c>your_favorite_revision_here</c> to the 
    ///   folder <c>c:\src\nant\replacement_for_module_directory_name</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-checkout 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     password="" 
    ///     module="nant">
    ///     <options>
    ///         <option name="-r" value="your_favorite_revision_here" />
    ///         <option name="-d" value="replacement_for_module_directory_name" />
    ///     </options>
    /// </cvs-checkout>
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-checkout")]
    public class CheckoutTask : AbstractCvsTask {
		private const string COMMAND_NAME = "checkout";
		/// <summary>
		/// The name of the cvs command that is going to be executed.
		/// </summary>
		public override string CommandName {
			get {return COMMAND_NAME;}
		}

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutTask" /> class.
        /// </summary>
        public CheckoutTask() {
        }

        #endregion Public Instance Constructors
    }
}
