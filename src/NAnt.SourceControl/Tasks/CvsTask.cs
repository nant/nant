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
using System.Text;
using System.IO;
using System.Diagnostics;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

using ICSharpCode.SharpCvsLib.Commands;

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
    ///      password="" 
    ///      module="nant" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("cvs")]
    public class CvsTask : AbstractCvsTask {

        #region Private Instance Fields
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _commandName;
        #endregion

        #region Public Instance Properties
        /// <summary>
        /// The cvs command to execute.
        /// </summary>
        [TaskAttribute("command", Required=true)]
        public override string CommandName {
            get {return _commandName;}
            set {_commandName = value;}
        }

        #endregion
    }
}
