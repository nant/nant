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
using System.IO;
using System.Text;

using ICSharpCode.SharpCvsLib.Client;
using ICSharpCode.SharpCvsLib.Commands;
using ICSharpCode.SharpCvsLib.Messages;
using ICSharpCode.SharpCvsLib.Misc;
using ICSharpCode.SharpCvsLib.FileSystem;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// A base class for creating tasks for executing CVS client commands on a 
    /// CVS repository.
    /// </summary>
    public abstract class AbstractCvsTask : Task {
        #region Private Instance Fields

        private const bool DEFAULT_USE_SHARPCVSLIB = true;
        private string _cvsRoot;
        private string _module;
        private DirectoryInfo _destinationDirectory;
        private string _password;
        private bool _useSharpCvsLib = DEFAULT_USE_SHARPCVSLIB;

        private CvsRoot _root;
        private WorkingDirectory _workingDirectory;
        private CVSServerConnection _connection;
        private ICommand _command;
        private OptionCollection _commandOptions = new OptionCollection();
        private OptionCollection _globalOptions = new OptionCollection();


        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCvsTask" /> 
        /// class.
        /// </summary>
        protected AbstractCvsTask () {
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

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
        public string CvsRoot {
            get { return this._cvsRoot; }
            set { _cvsRoot = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The module to perform an operation on.
        /// </summary>
        /// <value>
        /// The module to perform an operation on.
        /// </value>
        /// <example>
        ///   <para>In Nant the module name would be:</para>
        ///   <code>nant</code>
        /// </example>
        [TaskAttribute("module", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Module {
            get { return _module; }
            set { _module = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Destination directory for the checked out / updated files.
        /// </summary>
        /// <value>
        /// The destination directory for the checked out or updated files.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the current working directory that will be modifed.
        /// </para>
        /// </remarks>
        [TaskAttribute("destination", Required=true)]
        public DirectoryInfo DestinationDirectory {
            get { return _destinationDirectory; }
            set { _destinationDirectory = value; }
        }

        /// <summary>
        /// The password for logging in to the CVS repository.
        /// </summary>
        /// <value>
        /// The password for logging in to the CVS repository.
        /// </value>
        [TaskAttribute("password")]
        public string Password {
            get { return _password;}
            set { _password = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// A collection of options that can be used to modify cvs 
        /// checkouts/updates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Valid options include:
        /// </para>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Name</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term>sticky-tag</term>
        ///         <description>
        ///         A revision tag or branch tag that has been placed on the 
        ///         repository using the 'rtag' or 'tag' commands.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>override-directory</term>
        ///         <description>
        ///         A directory to substitute for the module name as the top level 
        ///         directory name.  For instance specifying 'nant-cvs' for this
        ///         value while checking out NAnt would checkout the source files 
        ///         into a top level directory named 'nant-cvs' instead of nant.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term>compression-level</term>
        ///         <description>
        ///         The compression level that files will be transported to and 
        ///         from the server at.  Valid numbers include 1 to 9.
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        [BuildElementCollection("commandoptions", "option")]
        public OptionCollection CommandOptions {
            get { return _commandOptions;}
        }

        /// <summary>
        /// Use commandOptions.
        /// </summary>
        [BuildElementCollection("options", "option")]
        public OptionCollection Options {
            get {return _commandOptions;}
        }

        /// <summary>
        /// Holds a collection of globally available cvs options.
        /// </summary>
        [BuildElementCollection("globaloptions", "option")]
        public OptionCollection GlobalOptions {
            get {return this._globalOptions;}
        }

        /// <summary>
        /// <code>true</code> if the SharpCvsLib binaries that come bundled with 
        ///     NAnt should be used to perform the cvs commands, <code>false</code>
        ///     otherwise.  
        ///     
        ///     <warn>If you choose not to use SharpCvsLib to checkout from 
        ///         cvs you will need to include a cvs.exe binary in your
        ///         path.</warn>
        /// </summary>
        [TaskAttribute("usesharpcvslib", Required=false)]
        public bool UseSharpCvsLib {
            get {return _useSharpCvsLib;}
            set {this._useSharpCvsLib = value;}
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

            /// <summary>
            /// Gets or sets the root of the CVS repository.
            /// </summary>
            /// <value>
            /// The root of the CVS repository.
            /// </value>
            protected CvsRoot Root {
            get { return this._root; }
            set { this._root = value; }
        }

        /// <summary>
        /// Gets or sets the directory where checked out sources are placed.
        /// </summary>
        /// <value>
        /// The directory where checked out sources are placed.
        /// </value>
        protected WorkingDirectory WorkingDirectory {
            get { return this._workingDirectory; }
            set { this._workingDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the connection used to connect to the CVS repository.
        /// </summary>
        /// <value>
        /// The connection used to connect to the CVS repository.
        /// </value>
        protected CVSServerConnection Connection {
            get { return this._connection; }
            set { this._connection = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="ICommand" /> to execute.
        /// </summary>
        /// <value>
        /// The <see cref="ICommand" /> to execute.
        /// </value>
        protected ICommand Command {
            get { return this._command; }
            set { this._command = value; }
        }

        #endregion Protected Instance Properties

        #region Protected Instance Methods

        /// <summary>
        /// Executes the CVS command.
        /// </summary>
        protected override void ExecuteTask () {
            if (!this.DestinationDirectory.Exists) {
                Log(Level.Info, LogPrefix + "Creating directory '{0}'.", 
                    this.DestinationDirectory.FullName);
                this.DestinationDirectory.Create();
            }

            this.Root = new CvsRoot(CvsRoot);
            this.WorkingDirectory = new WorkingDirectory(this.Root, 
                this.DestinationDirectory.FullName, this.Module);

            this.SetCommandOptions(this.WorkingDirectory);
            Logger.Debug("this.WorkingDirectory.Revision=[" + this.WorkingDirectory.Revision + "]");

            this.Connection = new CVSServerConnection();
            this.Connection.MessageEvent.MessageEvent +=
                new EncodedMessage.MessageHandler(LogCvsMessage);

            this.Command = this.CreateCommand();

            this.Validate ();

            Logger.Debug("Before trying to get a connection.");
            this.Connection.Connect(this.WorkingDirectory, this.Password);
            Logger.Debug("After trying to get a connection.");

            this.Command.Execute(this.Connection);
            this.Connection.Close();
        }

        /// <summary>
        /// Creates the CVS command object to execute against the specified 
        /// CVS repository.
        /// </summary>
        /// <returns>
        /// The <see cref="ICommand" /> to execute against the CVS repository.
        /// </returns>
        protected abstract ICommand CreateCommand ();

        /// <summary>
        /// Populates the files and folders as well as sub-folders in the given 
        /// path.
        /// </summary>
        /// <param name="path">The path to begin populating folders from.</param>
        /// <returns></returns>
        protected Folders GetFolders (String path) {
            return new Folders();
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Validates that all required information is available.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <para><see cref="CvsRoot" /> is <see langword="null" /></para>
        ///     <para>-or-</para>
        ///     <para><see cref="WorkingDirectory" /> is <see langword="null" /></para>
        ///     <para>-or-</para>
        ///     <para><see cref="Connection" /> is <see langword="null" /></para>
        ///     <para>-or-</para>
        ///     <para><see cref="Command" /> is <see langword="null" /></para>
        /// </exception>
        private void Validate() {
            if (null == this.CvsRoot || null == this.WorkingDirectory || 
                null == this.Connection || null == this.Command) {
                throw new ArgumentNullException(
                    "Cvsroot, working directory, connection and command cannot be null.");
            } 
            if (Logger.IsDebugEnabled) {
                string msg = "In validate. " +
                    ";  Cvsroot=[" + this.WorkingDirectory.CvsRoot + "]" +
                    ";  Local directory=[" + this.WorkingDirectory.LocalDirectory + "]" + 
                    ";  Working directory name=[" + this.WorkingDirectory.WorkingDirectoryName + "]" +
                    "Command=[" + this.Command + "]";
                Logger.Info(msg);
            }
        }

        /// <summary>
        /// Set the checkout/ update options.
        /// </summary>
        /// <param name="workingDirectory">Information about the cvs repository and local sandbox.</param>
        private void SetCommandOptions(WorkingDirectory workingDirectory) {
            Logger.Debug ("Setting options");
            foreach (Option option in _commandOptions) {
                if (!IfDefined || UnlessDefined) {
                    // skip option
                    continue;
                }

                Logger.Debug ("option.OptionName=[" + option.OptionName + "]");
                Logger.Debug ("option.Value=[" + option.Value + "]");
                switch (option.OptionName) {
                    case "sticky-tag":
                    case "-r":
                        workingDirectory.Revision =
                            option.Value;
                        Logger.Debug ("setting sticky-tag=[" + option.Value + "]");
                        break;
                    case "override-directory":
                    case "-d":
                        workingDirectory.OverrideDirectory = 
                            option.Value;
                        Logger.Debug ("setting override-directory=[" + option.Value + "]");
                        break;
                    default:
                        StringBuilder msg = new StringBuilder ();
                        msg.Append(Environment.NewLine + "Unsupported argument.");
                        msg.Append(Environment.NewLine + "\tname=[").Append(option.OptionName).Append ("]");
                        msg.Append(Environment.NewLine + "\tvalue=[").Append(option.Value).Append ("]");
                        throw new NotSupportedException(msg.ToString());
                }
            }
        }

        private void LogCvsMessage(string message) {
            Log(Level.Debug, LogPrefix + message);
        }

        #endregion Private Instance Methods
    }
}