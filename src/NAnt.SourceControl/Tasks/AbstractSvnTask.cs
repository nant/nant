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
using System.Diagnostics;
using System.IO;
using System.Text;

using ICSharpCode.SharpCvsLib.Client;
using ICSharpCode.SharpCvsLib.Commands;
using ICSharpCode.SharpCvsLib.Messages;
using ICSharpCode.SharpCvsLib.Misc;
using ICSharpCode.SharpCvsLib.FileSystem;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// A base class for creating tasks for executing CVS client commands on a 
    /// CVS repository.
    /// </summary>
    public abstract class AbstractSvnTask : AbstractSourceControlTask {
		#region Protected Static Fields
		/// <summary>
		/// An environment variable that holds path information about where
		///		svn is located.
		/// </summary>
		protected const string SVN_HOME = "SVN_HOME";
		/// <summary>
		/// Name of the password file that is used to cash password settings.
		/// </summary>
		protected static readonly String SVN_PASSFILE = 
			Path.Combine(".subversion", "auth");
		/// <summary>
		/// The name of the svn executable.
		/// </summary>
		protected const string SVN_EXE = "svn.exe";
		/// <summary>
		/// Environment variable that holds the executable name that is used for
		///		ssh communication.
		/// </summary>
		protected const string SVN_RSH = "RSH";

		#endregion

        #region Private Static Fields

        private static readonly log4net.ILog Logger = 
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCvsTask" /> 
        /// class.
        /// </summary>
        protected AbstractSvnTask () : base() {
        }

        #endregion Protected Instance Constructors

		#region Protected Instance Properties

		/// <summary>
		/// The name of the executable.
		/// </summary>
		protected override string VcsExeName {
			get {return SVN_EXE;}
		}

		/// <summary>
		/// The name of the password file.
		/// </summary>
		protected override string PassFileName {
			get {return SVN_PASSFILE;}
		}

		/// <summary>
		/// Name of the home environment variable.
		/// </summary>
		protected override string VcsHomeEnv {
			get {return SVN_HOME;}
		}

		/// <summary>
		/// The name of the ssh/ rsh environment variable.
		/// </summary>
		protected override string SshEnv {
			get {return SVN_RSH;}
		}

		#endregion

        #region Public Instance Properties

		/// <summary>
		/// The full path of the svn executable.
		/// </summary>
		public override string ExeName {
			get {return this.DeriveVcsFromEnvironment().FullName;}
		}

        /// <summary>
        /// <para>
        /// TODO: Add more documentation when I understand all svn root possibilities/
        ///		protocols.
        /// The svn root is usually in the form of a URL from which the server, protocol
        ///		and path information can be derived.  Although the path to the repository
        ///		can also be determined from this the information is more implicit
        ///		than explicit.  For example a subversion root URL of:
        ///		
        ///		http://svn.collab.net/repos/svn/trunk/doc/book/tools
        ///		
        ///		would have the following components:
        ///     protocol:       http/ web_dav
        ///     username:       anonymous
        ///     servername:     svn.collab.net
        ///     repository:		/repos/svn
        ///     server path:    /trunk/doc/book/tools
        ///     
        ///     In addition the revision path or branch can also be determined as
        ///     subversion stores this information as a seperate physical directory.
        ///     In this example:
        ///     
        ///     revision:		trunk
        /// </para>
        /// </summary>
        [TaskAttribute("svnroot", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public override string Root {
            get { return base.Root; }
            set { base.Root = StringUtils.ConvertEmptyToNull(value); }
        }

		/// <summary>
		/// The executable to use for ssh communication.
		/// </summary>
		[TaskAttribute("rsh", Required=false)]
		public override FileInfo Ssh {
			get {return base.Ssh;}
			set {base.Ssh = value;}
		}

		/// <summary>
		/// The executable to use for ssh communication.
		/// </summary>
		[TaskAttribute("command", Required=false)]
		public override string CommandName {
			get {return base.CommandName;}
			set {base.CommandName = value;}
		}

        #endregion Public Instance Properties

		#region Override Task Implementation

		/// <summary>
		/// Build up the command line arguments, determine which executable is being
		///		used and find the path to that executable and set the working 
		///		directory.
		/// </summary>
		/// <param name="process">The process to prepare.</param>
		protected override void PrepareProcess (Process process) {
			Log(Level.Info, String.Format("{0} command name: {1}", 
				LogPrefix, this.CommandName));
			if (null == this.Arguments || 0 == this.Arguments.Count) {
				this.AppendGlobalOptions();
				this.Arguments.Add(new Argument(this.CommandName));
				if (!this.CommandName.Equals("cleanup")) {
					this.Arguments.Add(new Argument(this.Root));
				}

				Logger.Debug("commandline args null: " + ((null == this.CommandLineArguments) ? "yes" : "no"));
				if (null == this.CommandLineArguments) {
					this.AppendCommandOptions();
				}

				this.AppendFiles();
			}
			if (!Directory.Exists(this.DestinationDirectory.FullName)) {
				Directory.CreateDirectory(this.DestinationDirectory.FullName);
			}
			base.PrepareProcess(process);
			process.StartInfo.FileName = this.ExeName;

			process.StartInfo.WorkingDirectory = 
				this.DestinationDirectory.FullName;
			Logger.Debug("working directory: " + process.StartInfo.WorkingDirectory);
			Logger.Debug("executable: " + process.StartInfo.FileName);
			Logger.Debug("arguments: " + process.StartInfo.Arguments);

			Log(Level.Info, String.Format("{0} working directory: {1}", 
				LogPrefix, process.StartInfo.WorkingDirectory));
			Log(Level.Info, String.Format("{0} executable: {1}", 
				LogPrefix, process.StartInfo.FileName));
			Log(Level.Info, String.Format("{0} arguments: {1}", 
				LogPrefix, process.StartInfo.Arguments));

		}

		#endregion

        #region Private Instance Methods

		private void AppendGlobalOptions () {
			foreach (Option option in this.GlobalOptions) {
				if (!IfDefined || UnlessDefined) {
					// skip option
					continue;
				}

				Logger.Debug ("option.OptionName=[" + option.OptionName + "]");
				Logger.Debug ("option.Value=[" + option.Value + "]");
				switch (option.OptionName) {
					case "cvsroot-prefix":
					case "-D":
					case "temp-dir":
					case "-T":
					case "editor":
					case "-e":
					case "compression":
					case "-z":
					case "variable":
					case "-s": 
						Arguments.Add(new Argument(option.OptionName));
						Arguments.Add(new Argument(option.Value));
						Logger.Debug ("setting option" + option.OptionName + 
							"=[" + option.Value + "]");
						break;
					default:
						Arguments.Add(new Argument(option.OptionName));
						Logger.Debug("setting prune to true.");
						break;
				}
																																																								}
		}

		private void AppendCommandOptions () {
			foreach (Option option in this.CommandOptions) {
				if (!IfDefined || UnlessDefined) {
					// skip option
					continue;
				}

				Logger.Debug ("option.OptionName=[" + option.OptionName + "]");
				Logger.Debug ("option.Value=[" + option.Value + "]");
				switch (option.OptionName) {
					case "sticky-tag":
					case "-r":
					case "override-directory":
					case "-d":
					case "join":
					case "-j":
					case "revision-date":
					case "-D":
					case "rcs-kopt":
					case "-k":
					case "message":
					case "-m":
						Arguments.Add(new Argument(option.OptionName));
						Arguments.Add(new Argument(option.Value));
						Logger.Debug ("setting option" + option.OptionName + 
							"=[" + option.Value + "]");
						break;
					default:
						Arguments.Add(new Argument(option.OptionName));
						Logger.Debug("adding command option: " + option.OptionName);
						break;
				}
			}
		}

        #endregion Private Instance Methods
    }
}