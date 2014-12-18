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
using System.Globalization;

using NAnt.Core.Attributes;
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
    ///     module="nant" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Checkout NAnt revision named <c>0_85</c> to the 
    ///   folder <c>c:\src\nant\v0.85</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-checkout 
    ///     destination="c:\src\nant" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     module="nant"
    ///     revision="0_85"
    ///     overridedir="v0.85" />
    ///     ]]>
    ///   </code>
    ///   <para>So the nant module tagged with revision 0_85 will be checked 
    ///   out in the folder v0.85 under the working/ destination directory.
    ///   <br/>This could be used to work on different 
    ///   branches of a repository at the same time.</para>
    /// </example>
    /// <example>
    ///   <para>
    ///   Checkout NAnt with specified revision date to the 
    ///   folder <c>c:\src\nant\2003_08_16</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-checkout 
    ///     destination="c:\src\nant\" 
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///     module="nant"
    ///     date="2003/08/16"
    ///     overridedir="2003_08_16" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-checkout")]
    public class CheckoutTask : AbstractCvsTask {
        #region Internal Static Fields

        /// <summary>
        /// The command being executed.
        /// </summary>
        internal const string CvsCommandName = "checkout";

        #endregion Internal Static Fields

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
        /// of the file.
        /// </summary>
        [TaskAttribute("revision", Required=false)]
        [StringValidator(AllowEmpty=true, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string Revision {
            get {
                if (CommandOptions.ContainsKey("revision")) {
                    return ((Option)CommandOptions["revision"]).Value;
                }
                return null;
            }
            set { 
                if (String.IsNullOrEmpty(value)) {
                    CommandOptions.Remove("revision");
                } else {
                    SetCommandOption("revision", string.Format(CultureInfo.InvariantCulture,
                        "-r {0}", value), true);
                }
            }
        }

        /// <summary>
        /// Sticky tag or revision to checkout.
        /// </summary>
        [TaskAttribute("sticky-tag", Required=false)]
        [StringValidator(AllowEmpty=true, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
        public string StickyTag {
            get { return Revision; }
            set { Revision = value; }
        }

        /// <summary>
        /// Specify the revision date to checkout.  The date specified is validated
        /// and then passed to the cvs binary in a standard format recognized by
        /// cvs.
        /// </summary>
        [TaskAttribute("date", Required=false)]
        [DateTimeValidator()]
        public DateTime Date {
            get { return Convert.ToDateTime(((Option)CommandOptions["date"]).Value); }
            set { SetCommandOption("date", String.Format(CultureInfo.InvariantCulture,"-D \"{0}\"", ToCvsDateTimeString(value)), true); }
        }

        /// <summary>
        /// Specify a directory name to replace the module name.  Valid names
        /// include any valid filename, excluding path information.
        /// </summary>
        [TaskAttribute("overridedir", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string OverrideDir {
            get { return ((Option)CommandOptions["overridedir"]).Value; }
            set { SetCommandOption("overridedir", String.Format(CultureInfo.InvariantCulture,"-d{0}", value), true); }
        }

        /// <summary>
        /// Specify a directory name to replace the module name.  Valid names
        /// include any valid filename, excluding path information.
        /// </summary>
        [TaskAttribute("override-directory", Required=false)]
        public string OverrideDirectory {
            get { return OverrideDir; }
            set { OverrideDir = value; }
        }

        #endregion

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// The name of the cvs command that is going to be executed.
        /// </summary>
        public override string CommandName {
            get { return CvsCommandName; }
        }

        #endregion Override implementation of AbstractCvsTask
    }
}
