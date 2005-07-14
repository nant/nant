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

using NAnt.Core.Attributes;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// Executes the cvs command specified by the command attribute.
    /// </summary>
    /// <example>
    ///   <para>Checkout NAnt.</para>
    ///   <code>
    ///     <![CDATA[
    /// <cvs command="checkout" 
    ///      destination="c:\src\nant\" 
    ///      cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
    ///      module="nant" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs")]
    public class CvsTask : AbstractCvsTask {
        #region Private Instance Fields

        private string _commandName;

        #endregion Private Instance Fields

        #region Override implementation of AbstractCvsTask

        /// <summary>
        /// The cvs command to execute.
        /// </summary>
        [TaskAttribute("command", Required=true)]
        public override string CommandName {
            get { return _commandName; }
            set { _commandName = value; }
        }

        /// <summary>
        /// Specify if the module is needed for this cvs command.  
        /// </summary>
        protected override bool IsModuleNeeded {
            get {
                bool moduleNeeded;
                switch (this.CommandName) {
                    case CheckoutTask.CvsCommandName:
                        moduleNeeded = true;
                        break;
                    case UpdateTask.CvsCommandName:
                        moduleNeeded = false;
                        break;
                    case "commit":
                        moduleNeeded = false;
                        break;
                    case TagTask.CvsCommandName:
                        moduleNeeded = false;
                        break;
                    default:
                        moduleNeeded = true;
                        break;
                }
                return moduleNeeded;
            }
        }

        #endregion Override implementation of AbstractCvsTask
    }
}
