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

using ICSharpCode.SharpCvsLib.Util;

using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

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
    ///     module="nant"
    ///     revision="your_favorite_revision_here"
    ///     overridedir="replacement_for_module_directory_name">
    /// </cvs-checkout>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    /// <cvs-checkout 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     password="" 
    ///     module="nant"
    ///     date="2003/08/16"
    ///     overridedir="2003_08_16"
    ///     usesharpcvslib="false">
    /// </cvs-checkout>
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

        #region Public Instance Properties

        /// <summary>
        /// Specify the revision to checkout.  This corresponds to the "sticky-tag"
        ///     of the file.
        /// </summary>
        [TaskAttribute("revision", Required=false)]
        public string Revision {
            get {return ((Option)this.CommandOptions["revision"]).Value;}
            set {this.SetCommandOption("revision", String.Format("r {0}", value), true);}
        }

        /// <summary>
        /// Sticky tag or revision to checkout.
        /// </summary>
        [TaskAttribute("sticky-tag", Required=false)]
        public string StickyTag {
            get {return this.Revision;}
            set {this.Revision = value;}
        }

        /// <summary>
        /// Specify a directory name to replace the module name.  Valid names
        ///     include any valid filename, excluding path information.
        /// </summary>
        [TaskAttribute("overridedir", Required=false)]
        public string OverrideDir {
            get {return ((Option)this.CommandOptions["overridedir"]).Value;}
            set {this.SetCommandOption("overridedir", String.Format("d{0}", value), true);}
        }

        /// <summary>
        /// Specify a directory name to replace the module name.  Valid names
        ///     include any valid filename, excluding path information.
        /// </summary>
        [TaskAttribute("override-directory", Required=false)]
        public string OverrideDirectory {
            get {return this.OverrideDir;}
            set {this.OverrideDir = value;}
        }


        /// <summary>
        /// Specify the revision date to checkout.  The date specified is validated
        ///     and then passed to the cvs binary in a standard format recognized by
        ///     cvs.
        /// </summary>
        [TaskAttribute("date", Required=false)]
        [DateTimeValidator()]
        public DateTime Date {
            get {return Convert.ToDateTime(((Option)this.CommandOptions["date"]).Value);}
            set {this.SetCommandOption("date", String.Format("D {0}", DateParser.GetCvsDateString(value)), true);}
        }

        #endregion
    }
}
