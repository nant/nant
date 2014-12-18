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
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Produces an XML report that represents the cvs changes from the given 
    /// start day, to a given end date.
    /// </summary>
    /// <example>
    ///   <para>Report changes in NAnt from 1st of June 2004 until 25th of July 2004.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs-changelog
    ///     destination="e:/test/nant/sourcecontrol/"
    ///     cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant"
    ///     module="nant"
    ///     start="2004/06/01"
    ///     end="2004/07/25"
    ///     xmlfile="e:/test/nant/sourcecontrol/changelog-nant.xml"
    /// />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs-changelog")]
    public class ChangeLogTask : AbstractCvsTask {
        #region Private Static Fields

        /// <summary>
        /// The command being executed.
        /// </summary>
        private const string CvsCommandName = "xml";

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// Name of the xml file that will contain the cvs log information.
        /// </summary>
        [TaskAttribute("xmlfile", Required=true)]
        public FileInfo DestFile {
            get {
                if (!CommandOptions.ContainsKey("destfile")) {
                    return null;
                }
                return new FileInfo(((Option) CommandOptions["destfile"]).Value);
            }
            set {
                if (null == this.DestinationDirectory) {
                    this.DestinationDirectory = 
                        new DirectoryInfo(value.DirectoryName);
                }
                SetCommandOption("destfile", string.Format(CultureInfo.InvariantCulture, 
                    "\"-oxml\" \"{0}\"", value.FullName), true);
            }
        }

        /// <summary>
        /// The earliest change to use in the cvs log command.
        /// </summary>
        [TaskAttribute("start", Required=true)]
        [DateTimeValidator()]
        public DateTime StartDate {
            get { return Convert.ToDateTime(((Option)CommandOptions["start"]).Value); }
            set { SetCommandOption("start", string.Format(CultureInfo.InvariantCulture,"-D \"{0}\"", ToCvsDateTimeString(value)), true); }
        }

        /// <summary>
        /// The latest date to use in the cvs log command.
        /// </summary>
        [TaskAttribute("end", Required=true)]
        [DateTimeValidator()]
        public DateTime EndDate {
            get { return Convert.ToDateTime(((Option)CommandOptions["end"]).Value); }
            set { SetCommandOption("end", string.Format(CultureInfo.InvariantCulture,"-D \"{0}\"", ToCvsDateTimeString(value)), true); }
        }

        #endregion Public Instance Properties

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// The cvs command to execute.
        /// </summary>
        public override string CommandName {
            get { return CvsCommandName; }
        }

        /// <summary>
        /// Override use of sharpcvslib, needs to be true.
        /// </summary>
        public override bool UseSharpCvsLib {
            get { return true; }
            set { /* can only use sharpcvslib for changelog */ }
        }

        /// <summary>
        /// <para>
        /// The cvs root variable has the following components:
        /// </para>
        /// <para>
        ///     <code>[protocol]:[username]@[servername]:[server path]</code>
        ///     <ul>
        ///         <li>protocol:       ext, pserver, ssh (sharpcvslib); if you are not using sharpcvslib consult your cvs documentation.</li>
        ///         <li>username:       [username]</li>
        ///         <li>servername:     cvs.sourceforge.net</li>
        ///         <li>server path:    /cvsroot/nant</li>
        ///     </ul>
        /// </para>
        /// <para>
        /// If the cvsroot is not specified then the directory specified by the 
        /// <see cref="AbstractSourceControlTask.DestinationDirectory" /> attribute 
        /// is searched for CVS\Root.
        /// </para>
        /// </summary>
        /// <example>
        ///   <para>NAnt anonymous cvsroot:</para>
        ///   <code>
        ///   :pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant
        ///   </code>
        /// </example>
        [TaskAttribute("cvsroot", Required=false)]
        public override string Root {
            get {
                if (null == base.Root && this.DestinationDirectory.Exists) {
                    try {
                        ICSharpCode.SharpCvsLib.FileSystem.Manager manager = 
                            new ICSharpCode.SharpCvsLib.FileSystem.Manager(this.DestinationDirectory.FullName);
                        ICSharpCode.SharpCvsLib.FileSystem.Root root =
                            manager.FetchRoot(this.DestinationDirectory.FullName);
                        base.Root = root.FileContents;
                    } catch (Exception e) {
                        throw new BuildException (e.Message);
                    }
                }
                return base.Root;
            }
            set { base.Root = value; }
        }

        #endregion Override implementation of AbstractCvsTask
    }
}
