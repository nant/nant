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
	public abstract class AbstractCvsTask : AbstractSourceControlTask {

		#region Protected Static Fields

		/// <summary>
		/// Default value for the recursive directive.  Default is <code>false</code>.
		/// </summary>
		protected const bool DEFAULT_RECURSIVE = false;
		/// <summary>
		/// Default value for the quiet command.
		/// </summary>
		protected const bool DEFAULT_QUIET = false;
		/// <summary>
		/// Default value for the really quiet command.
		/// </summary>
		protected const bool DEFAULT_REALLY_QUIET = false;

		/// <summary>
		/// An environment variable that holds path information about where
		///		cvs is located.
		/// </summary>
		protected const string CVS_HOME = "CVS_HOME";
		/// <summary>
		/// Name of the password file that cvs stores pserver 
		///		cvsroot/ password pairings.
		/// </summary>
		protected const String CVS_PASSFILE = ".cvspass";
		/// <summary>
		/// The default compression level to use for cvs commands.
		/// </summary>
		protected const int DEFAULT_COMPRESSION_LEVEL = 3;
		/// <summary>
		/// The default use of binaries, defaults to use sharpcvs
		///		<code>true</code>.
		/// </summary>
		protected const bool DEFAULT_USE_SHARPCVSLIB = true;

		/// <summary>
		/// The name of the cvs executable.
		/// </summary>
		protected const string CVS_EXE = "cvs.exe";
		/// <summary>
		/// The temporary name of the sharpcvslib binary file, to avoid 
		///		conflicts in the path variable.
		/// </summary>
		protected const string SHARP_CVS_EXE = "scvs.exe";
		/// <summary>
		/// Environment variable that holds the executable name that is used for
		///		ssh communication.
		/// </summary>
		protected const string CVS_RSH = "CVS_RSH";
		/// <summary>
		/// Property name used to specify on a project level whether sharpcvs is
		///		used or not.
		/// </summary>
		protected const string USE_SHARPCVSLIB = "sourcecontrol.usesharpcvslib";

		#endregion

        #region Private Instance Fields

		private string _module;
		private bool _useSharpCvsLib = DEFAULT_USE_SHARPCVSLIB;
		private bool _isUseSharpCvsLibSet = false;

		private FileSet _fileset = new FileSet();

		private string _sharpcvslibExeName;

        #endregion Private Instance Fields

        #region Private Static Fields

		private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AbstractCvsTask" /> 
		/// class.
		/// </summary>
		protected AbstractCvsTask () : base() {
			this._sharpcvslibExeName = 
				Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, SHARP_CVS_EXE);
		}

        #endregion Protected Instance Constructors

		#region Protected Instance Properties

		/// <summary>
		/// The environment name for the ssh variable.
		/// </summary>
		protected override string SshEnv {
			get {return CVS_RSH;}
		}
		#endregion

        #region Public Instance Properties

		/// <summary>
		/// The name of the cvs executable.
		/// </summary>
		public override string ExeName {
			get {
				string _exeNameTemp;
				if (this.UseSharpCvsLib) {
					_exeNameTemp = this._sharpcvslibExeName;
				} else {
					_exeNameTemp = this.DeriveVcsFromEnvironment().FullName;
				}
				Logger.Debug("_sharpcvslibExeName: " + _sharpcvslibExeName);
				Logger.Debug("_exeNameTemp: " + _exeNameTemp);
				Properties[EXE_NAME] = _exeNameTemp;
				return _exeNameTemp;
			}
		}

		/// <summary>
		/// The name of the cvs binary, or <code>cvs.exe</code> at the time this 
		///		was written.
		/// </summary>
		protected override string VcsExeName {
			get {return CVS_EXE;}
		}

		/// <summary>
		/// The name of the pass file, or <code>.cvspass</code> at the time
		///		of this writing.
		/// </summary>
		protected override string PassFileName {
			get {return CVS_PASSFILE;}
		}

		/// <summary>
		/// The name of the version control system specific home environment 
		///		variable.
		/// </summary>
		protected override string VcsHomeEnv {
			get {return CVS_HOME;}
		}

		/// <summary>
		/// <para>
		/// The cvs root variable has the following components.  The examples used is for the
		///     NAnt cvsroot.
		///     
		///     protocol:       ext
		///     username:       [username]
		///     servername:     cvs.sourceforge.net
		///     server path:    /cvsroot/nant
		/// </para>
		/// <para>
		/// Currently supported protocols include:
		/// </para>
		/// <list type="table">
		///     <item>
		///         <term>ext</term>
		///         <description>
		///         Used for securely checking out sources from a cvs repository.  
		///         This checkout method uses a local ssh binary to communicate 
		///         with the repository.  If you would like to secure password 
		///         information then this method can be used along with public/private 
		///         key pairs to authenticate against a remote server.
		///         Please see: http://sourceforge.net/docman/display_doc.php?docid=761&amp;group_id=1
		///         for information on how to do this for http://sourceforge.net.
		///         </description>
		///     </item>
		///     <item>
		///         <term>ssh</term>
		///         <description>
		///         Similar to the ext method.
		///         </description>
		///     </item>
		///     <item>
		///         <term>pserver</term>
		///         <description>
		///         The pserver authentication method is used to checkout sources 
		///         without encryption.  Passwords are stored as plain text and 
		///         all files are transported unencrypted.
		///         </description>
		///     </item>
		/// </list>
		/// </summary>
		/// <example>
		///   <para>NAnt anonymous cvsroot:</para>
		///   <code>
		///   :pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant
		///   </code>
		/// </example>
		/// <example>
		///   <para>Sharpcvslib anonymous cvsroot:</para>
		///   <code>
		///   :pserver:anonymous@cvs.sourceforge.net:/cvsroot/sharpcvslib
		///   </code>
		/// </example>
		[TaskAttribute("cvsroot", Required=true)]
		[StringValidator(AllowEmpty=false)]
		public override string Root {
			get { return base.Root; }
			set { base.Root = StringUtils.ConvertEmptyToNull(value); }
		}

		/// <summary>
		/// The module to perform an operation on.
		/// </summary>
		/// <value>
		/// The module to perform an operation on.  This is a normal file/ folder
		///     name without path information.
		/// </value>
		/// <example>
		///   <para>In Nant the module name would be:</para>
		///   <code>nant</code>
		/// </example>
		[TaskAttribute("module", Required=true)]
		[StringValidator(AllowEmpty=false, Expression=@"^[A-Za-z0-9][A-Za-z0-9._\-]*$")]
		public string Module {
			get { return _module; }
			set { _module = StringUtils.ConvertEmptyToNull(value); }
		}

		/// <summary>
		/// <code>true</code> if the SharpCvsLib binaries that come bundled with 
		///     NAnt should be used to perform the cvs commands, <code>false</code>
		///     otherwise.
		///     
		///     You may also specify an override value for all cvs tasks instead
		///     of specifying a value for each.  To do this set the property
		///     <code>sourcecontrol.usesharpcvslib</code> to <code>false</code>.
		///     
		///     <warn>If you choose not to use SharpCvsLib to checkout from 
		///         cvs you will need to include a cvs.exe binary in your
		///         path.</warn>
		/// </summary>
		/// <example>
		///		To use a cvs client in your path instead of sharpcvslib specify
		///			the property:
		///		&gt;property name="sourcecontrol.usesharpcvslib" value="false"&lt;
		///		
		///		The default settings is to use sharpcvslib and the setting closest
		///		to the task execution is used to determine which value is used
		///		to execute the process.
		///		
		///		For instance if the attribute usesharpcvslib was set to false 
		///		and the global property was set to true, the usesharpcvslib is 
		///		closes to the point of execution and would be used and is false. 
		///		Therefore the sharpcvslib binary would NOT be used.
		/// </example>
		[TaskAttribute("usesharpcvslib", Required=false)]
		public bool UseSharpCvsLib {
			get {return this._useSharpCvsLib;}
			set {
				this._isUseSharpCvsLibSet = true;
				this._useSharpCvsLib = value;
			}
		}

		/// <summary>
		/// The executable to use for ssh communication.
		/// </summary>
		[TaskAttribute("cvsrsh", Required=false)]
		public override FileInfo Ssh {
			get {return base.Ssh;}
			set {base.Ssh = value;}
		}

		/// <summary>
		/// Indicates if the output from the cvs command should be supressed.  Defaults to 
		///		<code>false</code>.
		/// </summary>
		[TaskAttribute("quiet", Required=false)]
		[BooleanValidator()]
		public bool Quiet {
			get {return ((Option)this.GlobalOptions["quiet"]).IfDefined;}
			set {this.SetGlobalOption("quiet", "q", value);}
		}

		/// <summary>
		/// Indicates if the output from the cvs command should be stopped.  Default to 
		///		<code>false</code>.
		/// </summary>
		[TaskAttribute("reallyquiet", Required=false)]
		[BooleanValidator()]
		public bool ReallyQuiet {
			get {return ((Option)this.GlobalOptions["reallyquiet"]).IfDefined;}
			set {this.SetGlobalOption("reallyquiet", "Q", value);}
		}

		/// <summary>
		/// <code>true</code> if the sandbox files should be checked out in
		///		read only mode.
		/// </summary>
		[TaskAttribute("readonly", Required=false)]
		[BooleanValidator()]
		public bool ReadOnly {
			get {return ((Option)this.GlobalOptions["readonly"]).IfDefined;}
			set {this.SetGlobalOption("readonly", "r", value);}
		}

		/// <summary>
		/// <code>true</code> if the sandbox files should be checked out in 
		///		read/ write mode.
		///		
		///		Defaults to <code>true</code>.
		/// </summary>
		[TaskAttribute("readwrite", Required=false)]
		[BooleanValidator()]
		public bool ReadWrite {
			get {return ((Option)this.GlobalOptions["readwrite"]).IfDefined;}
			set {
				if (true == this.ReadOnly && 
					value == true) {
					throw new BuildException ("Cannot set readonly and read/ write.");
				}
				this.SetGlobalOption("readwrite", "w", value);
			}
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
			// Although a global property can be set, take the property closest
			//	to the task execution, which is the attribute on the task itself.
			if (!this._isUseSharpCvsLibSet &&
				(null == Properties || null == Properties[USE_SHARPCVSLIB])) {
				// if not set and the global property is null then use the default
				this._useSharpCvsLib = DEFAULT_USE_SHARPCVSLIB;
			} else if (!this._isUseSharpCvsLibSet &&
				null != Properties[USE_SHARPCVSLIB]){
				try {
					this._useSharpCvsLib =
						System.Convert.ToBoolean(Properties[USE_SHARPCVSLIB]);
				} catch (Exception) {
					throw new BuildException (USE_SHARPCVSLIB + " must be convertable to a boolean.");
				}
			}

			Logger.Debug("number of arguments: " + Arguments.Count);
			if (null == this.Arguments || 0 == this.Arguments.Count) {
				if (IsModuleNeeded) {
					this.Arguments.Add(new Argument(String.Format("-d{0}", this.Root)));
				}
				this.AppendGlobalOptions();
				this.Arguments.Add(new Argument(this.CommandName));

				Logger.Debug("commandline args null: " + ((null == this.CommandLineArguments) ? "yes" : "no"));
				if (null == this.CommandLineArguments) {
					this.AppendCommandOptions();
				}

				this.AppendFiles();
				if (this.IsModuleNeeded) {
					this.Arguments.Add(new Argument(this.Module));
				}
			}
			Logger.Debug("Using sharpcvs" + this.UseSharpCvsLib);

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
			foreach (Option option in this.GlobalOptions.Values) {
//				Log(Level.Verbose, 
//					String.Format("{0} Type '{1}'.", LogPrefix, optionte.GetType()));
				if (!option.IfDefined || option.UnlessDefined) {
					// skip option
					continue;
				}
				this.AddArg(option.Value);
			}
		}

		/// <summary>
		/// Append the command line options or commen names for the options
		///		to the generic options collection.  This is then piped to the
		///		command line as a switch.
		/// </summary>
		private void AppendCommandOptions () {
			foreach (Option option in this.CommandOptions.Values) {
				if (!option.IfDefined || option.UnlessDefined) {
					// skip option
					continue;
				}
				this.AddArg(option.Value);
			}
		}

		private void AddArg (String arg) {
			if (arg.IndexOf("-") != 0) {
				Arguments.Add(new Argument(String.Format("{0}{1}",
					"-", arg)));
			} else {
				Arguments.Add(new Argument(String.Format("{1}",
					arg)));
			}
		}

		private bool IsModuleNeeded {
			get {
				if ("checkout".Equals(this.CommandName)) {
					return true;
				} else {
					return false;
				}
			}
		}

        #endregion Private Instance Methods
    }
}