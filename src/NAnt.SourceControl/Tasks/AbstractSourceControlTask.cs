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
using System.Collections;
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
    public abstract class AbstractSourceControlTask : ExternalProgramBase {
		#region Protected Static Fields

		/// <summary>
		/// Name of the environmental variable specifying a users' home
		///		in a *nix environment.
		/// </summary>
		protected const String HOME = "HOME";
		/// <summary>
		/// Used on windows to specify the location of application data.
		/// </summary>
		protected const string APP_DATA = "APPDATA";
		/// <summary>
		/// The environment variable that holds path information.
		/// </summary>
		protected const String PATH = "PATH";
		/// <summary>
		/// Property name used to specify the source control executable.  This is 
		///		used as a readonly property.
		/// </summary>
		protected const string EXE_NAME = "sourcecontrol.exename";

		#endregion

        #region Private Instance Fields

		private string _exeName;
        private string _root;
        private DirectoryInfo _destinationDirectory;
        private string _password;
        private FileInfo _passFile;

		private string _commandName;
		private string _commandLine = null;
        private Hashtable _commandOptions = new Hashtable();
		private string _commandLineArguments;
        private Hashtable _globalOptions = new Hashtable();

		private FileInfo _ssh;
        private FileSet _fileset = new FileSet();

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog Logger = 
			log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCvsTask" /> 
        /// class.
        /// </summary>
        protected AbstractSourceControlTask () : base() {
        }

        #endregion Protected Instance Constructors

		#region Protected Instance Properties
		/// <summary>
		/// The name of the passfile, overriden for each version control system (VCS).
		/// </summary>
		protected abstract string PassFileName {get;}

		/// <summary>
		/// The path to the specific home directory of the version control system,
		///		this can be where the binary files are kept, or other app
		///		information.
		/// </summary>
		protected DirectoryInfo VcsHome {
			get {
				string vcsHome =
					Environment.GetEnvironmentVariable(this.VcsHomeEnv);
				if (null != vcsHome) {
					if (Directory.Exists(vcsHome)) {
						return new DirectoryInfo(vcsHome);
					}
				}
				return null;
			}
		}

		/// <summary>
		/// The environment variable that defines where the version control system
		///		(VCS) home variable is kept.
		/// </summary>
		protected abstract string VcsHomeEnv {get;}

		/// <summary>
		/// The name of the version control system (VCS) executable file.
		/// </summary>
		protected abstract string VcsExeName {get;}

		#endregion

        #region Public Instance Properties

		/// <summary>
		/// The name of the version control system executable.
		/// </summary>
		public override string ExeName {
			get {return this._exeName;}
			set {this._exeName = value;}
		}

        /// <summary>
        /// <para>
        /// The root variable contains information on how to locate a repository.  
        ///		Although this information is in different formats it typically must
        ///		define the following:
        ///		<list type="table">
        ///			<item>server location</item>
        ///			<item>protocol used to communicate with the repository</item>
        ///			<item>repository location on the server</item>
        ///			<item>project location in the repository</item>
        ///		</list>
        /// </para>
        /// </summary>
		[StringValidator(AllowEmpty=false)]
		public virtual string Root {
			get {return this._root;}
			set {this._root = value;}
		}

        /// <summary>
        /// Destination directory for the local sandbox.
        /// </summary>
        /// <value>
        /// Root path of the local sandbox.
        /// </value>
        /// <remarks>
        /// <para>
        /// Root path of the local sandbox.
        /// </para>
        /// </remarks>
        [TaskAttribute("destination", Required=true)]
        public DirectoryInfo DestinationDirectory {
            get { return _destinationDirectory; }
            set { _destinationDirectory = value; }
        }

        /// <summary>
        /// The password for logging in to the repository.
        /// </summary>
        /// <value>
        /// The password for logging in to the repository.
        /// </value>
        [TaskAttribute("password", Required=false)]
        public virtual string Password {
            get { return _password;}
            set { _password = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The full path to the cached password file.  If not specified then the
        ///		environment variables are used to try and locate the file.
        /// </summary>
        [TaskAttribute("passfile")]
        public FileInfo PassFile {
            get {
				if (null == this._passFile) {
					this._passFile = DerivePassFile();
				}
				return this._passFile;}
            set {this._passFile = value;}
        }

		/// <summary>
		/// Holds a collection of globally available options.
		/// </summary>
		public Hashtable GlobalOptions {
			get {return this._globalOptions;}
			set {this._globalOptions = value;}
		}

        /// <summary>
        /// A collection of options that can be used to modify the default behavoir
        ///		of the version control commands.  See the sub-tasks for implementation
        ///		specifics.
        /// </summary>
        public Hashtable CommandOptions {
            get { return _commandOptions;}
			set { this._commandOptions = value; }
        }

		/// <summary>
		/// The command-line arguments for the program.
		/// </summary>
        [TaskAttribute("commandline")]
		public string CommandLineArguments {
			get {return this._commandLineArguments;}
			set {this._commandLineArguments = StringUtils.ConvertEmptyToNull(value);}
		}

		/// <summary>
		/// Get the command line arguments for the task.
		/// </summary>
		public override string ProgramArguments {
			get {return this._commandLine;}
		}

		/// <summary>
		/// The name of the command that is going to be executed.
		/// </summary>
		public virtual string CommandName {
			get {return this._commandName;}
			set {this._commandName = value;}
		}

		/// <summary>
		/// Used to specify the version control system (VCS) files that are going
		///		to be acted on.
		/// </summary>
		[BuildElement("fileset")]
		public virtual FileSet VcsFileSet {
			get { return _fileset; }
			set { _fileset = value; }
		}

		/// <summary>
		/// The executable to use for ssh communication.
		/// </summary>
		[TaskAttribute("ssh", Required=false)]
		public virtual FileInfo Ssh {
			get {return this._ssh;}
			set {this._ssh = value;}
		}

		/// <summary>
		/// The environment name for the ssh variable.
		/// </summary>
		protected abstract string SshEnv {get;}

		/// <summary>
		/// Adds a new global option if none exists.  If one does exist then
		///		the use switch is toggled on or of.
		/// </summary>
		/// <param name="name">The common name of the option.</param>
		/// <param name="value">The option value or command line switch
		///		of the option.</param>
		/// <param name="on"><code>true</code> if the option should be
		///		appended to the commandline, otherwise <code>false</code>.</param>
		protected void SetGlobalOption (String name, String value, bool on) {
			Option option;
			if (this.GlobalOptions.Contains(name)) {
				option = (Option)this.GlobalOptions[name];
			} else {
				option = new Option();
				option.OptionName = name;
				option.Value = value;
				this.GlobalOptions.Add(option.Name, option);
			} 
			option.IfDefined = on;
		}

		/// <summary>
		/// Adds a new command option if none exists.  If one does exist then
		///		the use switch is toggled on or of.
		/// </summary>
		/// <param name="name">The common name of the option.</param>
		/// <param name="value">The option value or command line switch
		///		of the option.</param>
		/// <param name="on"><code>true</code> if the option should be
		///		appended to the commandline, otherwise <code>false</code>.</param>
		protected void SetCommandOption (String name, String value, bool on) {
			Option option;
			if (this.CommandOptions.Contains(name)) {
				option = (Option)this.CommandOptions[name];
			} else {
				option = new Option();
				option.OptionName = name;
				option.Value = value;
				this.CommandOptions.Add(name, option);
			} 
			option.IfDefined = on;
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
			base.PrepareProcess(process);
			this.SetEnvironment(process);
		}

		/// <summary>
		/// Set up the environment variables for a process.
		/// </summary>
		/// <param name="process">A process to setup.</param>
		protected virtual void SetEnvironment (Process process) {
			if (this.Ssh != null && !this.Ssh.Exists) {
				FileInfo tempLookup = this.DeriveFullPathFromEnv(PATH, this.Ssh.Name);
				if (null == tempLookup) {
					tempLookup = this.DeriveFullPathFromEnv(PATH, this.Ssh.Name + ".exe");
				} 

				if (null != tempLookup) {
					this.Ssh = tempLookup;
				}
			}
			if (this.Ssh != null) {
				try {
					process.StartInfo.EnvironmentVariables.Add(this.SshEnv, this.Ssh.FullName);
				} catch (System.ArgumentException e) {
					Logger.Warn("Possibility cvs_rsh key has already been added.", e);
				}
			}
		}

		#endregion

		#region Protected Instance Methods

		#endregion

        #region Private Instance Methods

		/// <summary>
		/// Get the password file location derived by looking for the file name/ 
		///		path specified by the virtual property <code>PassFileName</code>.  
		///		The following search algorithm is applied:
		///		<list type="list">
		///			<item>Search in the <code>Home</code> path of the user.</item>
		///			<item>Search in the <code>APPDATA</code> path of the user.</item>
		///			<item>Search in the root directory of the current executing process</item>
		///			<item>Fail with an 'E' for effort.</item>
		///		</list>
		/// </summary>
		/// <returns>A <code>FileInfo</code> object that specifies the location
		///		of the passfile or <code>null</code> if this cannot be found.</returns>
		protected FileInfo DerivePassFile () {
			if (this._passFile == null) {
				FileInfo passFile = this.DerivePassFile(HOME);

				// only valid on a windows machine, but should not hurt to look
				//	for this on a linux machine
				if (null == passFile) {
					passFile = this.DerivePassFile(APP_DATA);
				}

				// finally search in the root directory of the current process
				if (null == passFile) {
					string rootDir = 
						Path.GetPathRoot(System.AppDomain.CurrentDomain.BaseDirectory);
					string passFileFullName =
						Path.Combine(rootDir, this.PassFileName);
					if (File.Exists(passFileFullName)) {
						passFile = new FileInfo(passFileFullName);
					} else {
						passFile = null;
					}
				}
				return passFile;
			}
			return this._passFile;
		}

		private FileInfo DerivePassFile(String environmentVar) {
			return this.DeriveFullPathFromEnv(environmentVar, this.PassFileName);
		}

		private FileInfo DeriveFullPathFromEnv(string environmentVar, string fileName) {
			string environmentValue = StringUtils.ConvertEmptyToNull(
				System.Environment.GetEnvironmentVariable(environmentVar));

			Log(Level.Debug, String.Format("{0} Environment variable: {1}",
				LogPrefix, environmentVar));
			Log(Level.Debug, String.Format("{0} Environment value: {1}",
				LogPrefix, environmentValue));

			string [] environmentPaths = null;
			if (null != environmentValue) {
				if (PlatformHelper.IsUnix) {
					environmentPaths = environmentValue.Split(':');
				} else if (PlatformHelper.IsWin32) {
					environmentPaths = environmentValue.Split(';');
				} else {
					environmentPaths = new string[] {environmentValue};
				}

				foreach (string environmentPath in environmentPaths) {
					if (null != environmentPath) {
						string fileFullName = Path.Combine(environmentPath, fileName);
						Log(Level.Debug, String.Format("{0} environmentPath: {1}", LogPrefix, environmentPath));
						Log(Level.Debug, String.Format("{0} fileName: {1}", LogPrefix, fileName));
						Log(Level.Debug, String.Format("{0} fileFullName: {1}", LogPrefix, fileFullName));
						if (environmentPath.IndexOf(fileName) > -1 &&
							File.Exists(fileName)) {
							if (!(Path.GetDirectoryName(fileName).IndexOf(
								Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)) > 1)) {
								return new FileInfo(fileName);
							}
						}
						if (fileFullName.IndexOf(fileName) > -1 &&
							File.Exists(fileFullName)) {
							if (Path.GetDirectoryName(fileFullName).IndexOf(
								Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)) == -1) {
								return new FileInfo(fileFullName);
							}
						}
					}
				}
			}
			return null;
		}

        /// <summary>
        /// Append the files specified in the fileset to the command line argument.
        /// </summary>
        protected void AppendFiles () {
            foreach (string pathname in this.VcsFileSet.FileNames) {
                string relativePath = pathname;
                if (relativePath.IndexOf('/') == 0 || relativePath.IndexOf('\\') == 0) {
                    relativePath = relativePath.Substring(1, relativePath.Length - 1);
                }
                relativePath = pathname.Replace(this.DestinationDirectory.FullName, "").Replace("\\", "/");
                try {
                    Arguments.Add(new Argument(relativePath));
                } catch (Exception e) {
                    System.Console.WriteLine("Unable to parse file: " + e.Message);
                }
            }
        }

		/// <summary>
		/// Derive the location of the version control system from the environment
		///		variable <code>PATH</code>.
		/// </summary>
		/// <returns>The file information of the version control system, 
		///		or <code>null</code> if this cannot be found.</returns>
		protected FileInfo DeriveVcsFromEnvironment () {
			FileInfo vcsFile =
				this.DeriveFullPathFromEnv(this.VcsHomeEnv, this.VcsExeName);
			if (null == vcsFile) {
				vcsFile = this.DeriveFullPathFromEnv(PATH, this.VcsExeName);
			}
			return vcsFile;
		}

        #endregion Private Instance Methods
    }
}