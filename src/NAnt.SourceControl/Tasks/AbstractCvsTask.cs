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
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.SourceControl.Types;

using ICSharpCode.SharpCvsLib.FileSystem;

namespace NAnt.SourceControl.Tasks {
    /// <summary>
    /// A base class for creating tasks for executing CVS client commands on a
    /// CVS repository.
    /// </summary>
    public abstract class AbstractCvsTask : AbstractSourceControlTask {
        #region Private Static Fields
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion Private Static Fields

        #region Protected Static Fields

        /// <summary>
        /// Default value for the recursive directive.  The default is
        /// <see langword="false" />.
        /// </summary>
        protected const bool DefaultRecursive = false;

        /// <summary>
        /// Default value for the quiet command.
        /// </summary>
        protected const bool DefaultQuiet = false;

        /// <summary>
        /// Default value for the really quiet command.
        /// </summary>
        protected const bool DefaultReallyQuiet = false;

        /// <summary>
        /// An environment variable that holds path information about where
        ///     cvs is located.
        /// </summary>
        protected const string CvsHome = "CVS_HOME";

        /// <summary>
        /// Name of the password file that cvs stores pserver
        ///     cvsroot/ password pairings.
        /// </summary>
        protected const String CvsPassfile = ".cvspass";

        /// <summary>
        /// The default compression level to use for cvs commands.
        /// </summary>
        protected const int DefaultCompressionLevel = 3;

        /// <summary>
        /// The default use of binaries, defaults to use sharpcvs.
        /// </summary>
        protected const bool DefaultUseSharpCvsLib = true;

        /// <summary>
        /// The name of the cvs executable.
        /// </summary>
        protected const string CvsExe = "cvs.exe";

        /// <summary>
        /// The temporary name of the sharpcvslib binary file, to avoid
        /// conflicts in the path variable.
        /// </summary>
        protected const string SharpCvsExe = "scvs.exe";

        /// <summary>
        /// Environment variable that holds the executable name that is used for
        /// ssh communication.
        /// </summary>
        protected const string CvsRsh = "CVS_RSH";

        /// <summary>
        /// Property name used to specify on a project level whether sharpcvs is
        /// used or not.
        /// </summary>
        protected const string UseSharpCvsLibProp = "sourcecontrol.usesharpcvslib";

        #endregion "Protected Static Fields

        #region Private Instance Fields

        private string _module;
        private bool _useSharpCvsLib = DefaultUseSharpCvsLib;
        private bool _isUseSharpCvsLibSet = false;
        private FileInfo _cvsFullPath;
        private string _sharpcvslibExeName;

        private CvsFileSet _cvsFileSet = new CvsFileSet();

        #endregion Private Instance Fields

        #region Protected Instance Contructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCvsTask" />
        /// class.
        /// </summary>
        protected AbstractCvsTask () : base() {
            _sharpcvslibExeName = 
                Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, SharpCvsExe);
        }

        #endregion Protected Instance Constructors

        #region Protected Instance Properties

        /// <summary>
        /// The environment name for the ssh variable.
        /// </summary>
        protected override string SshEnv {
            get { return CvsRsh; }
        }

        /// <summary>
        /// The name of the cvs binary, or <c>cvs.exe</c> at the time this
        /// was written.
        /// </summary>
        protected override string VcsExeName {
            get { return CvsExe; }
        }

        /// <summary>
        /// The name of the pass file, or <c>.cvspass</c> at the time
        /// of this writing.
        /// </summary>
        protected override string PassFileName {
            get { return CvsPassfile; }
        }

        /// <summary>
        /// The name of the version control system specific home environment
        /// variable.
        /// </summary>
        protected override string VcsHomeEnv {
            get { return CvsHome; }
        }

        /// <summary>
        /// Specify if the module is needed for this cvs command.  It is
        /// only needed if there is no module information on the local file
        /// system.
        /// </summary>
        protected virtual bool IsModuleNeeded {
            get { return true; }
        }

        #endregion

        #region Protected Instance Methods

        /// <summary>
        /// Converts a date value to a string representation that can be
        /// interpreted by cvs.
        /// </summary>
        /// <param name="item">Date to convert.</param>
        /// <returns>
        /// String interpretation of <paramref name="item"/>.
        /// </returns>
        protected string ToCvsDateTimeString(DateTime item) {
            return string.Format("{0} {1,2} {2}", item.ToString("ddd MMM"), 
                item.Day.ToString(), item.ToString("HH:mm:ss yyyy"));
        }

        #endregion Protected Instance Methods

        #region Public Instance Properties

        /// <summary>
        /// Used to specify the version control system (VCS) files that are going
        /// to be acted on.
        /// </summary>
        [BuildElement("fileset")]
        public CvsFileSet CvsFileSet {
            get { return this._cvsFileSet; }
            set { this._cvsFileSet = value; }
        }

        /// <summary>
        /// Get the cvs file set.
        /// </summary>
        public override FileSet VcsFileSet{
            get { return this.CvsFileSet; }
        }

        /// <summary>
        /// The name of the cvs executable.
        /// </summary>
        public override string ExeName {
            get {
                if (null != CvsFullPath) {
                    return CvsFullPath.FullName;
                }
                string _exeNameTemp;
                if (UseSharpCvsLib) {
                    _exeNameTemp = _sharpcvslibExeName;
                } else {
                    FileInfo vcsFile = DeriveVcsFromEnvironment();
                    if (vcsFile == null) {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "'{0}' could not be found on the system.", VcsExeName), 
                            Location);
                    }
                    _exeNameTemp = vcsFile.FullName;
                }
                Logger.DebugFormat("_sharpcvslibExeName: {0}", _sharpcvslibExeName);
                Logger.DebugFormat("_exeNameTemp: {0}", _exeNameTemp);
                Properties[PropExeName] = _exeNameTemp;
                return _exeNameTemp;
            }
        }

        /// <summary>
        /// The full path to the cvs binary used.  The cvs tasks will attempt to
        /// "guess" the location of your cvs binary based on your path.  If the
        /// task is unable to resolve the location, or resolves it incorrectly
        /// this can be used to manually specify the path.
        /// </summary>
        /// <value>
        /// A full path (i.e. including file name) of your cvs binary:
        ///     On Windows: c:\vcs\cvs\cvs.exe
        ///     On *nix: /usr/bin/cvs
        /// </value>
        [TaskAttribute("cvsfullpath", Required=false)]
        public FileInfo CvsFullPath {
            get { return _cvsFullPath; }
            set { _cvsFullPath = value; }
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
        /// </summary>
        /// <example>
        ///   <para>NAnt anonymous cvsroot:</para>
        ///   <code>
        ///   :pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant
        ///   </code>
        /// </example>
        [TaskAttribute("cvsroot", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public override string Root {
            get {
                if (null == base.Root) {
                    try {
                        ICSharpCode.SharpCvsLib.FileSystem.Root root = 
                            ICSharpCode.SharpCvsLib.FileSystem.Root.Load(this.DestinationDirectory);
                        this.Root = root.FileContents;
                    } catch (ICSharpCode.SharpCvsLib.Exceptions.CvsFileNotFoundException) {
                        throw new BuildException (string.Format("Cvs/Root file not found in {0}, please perform a checkout.",
                            this.DestinationDirectory.FullName));
                    }
                }
                return base.Root; 
            }
            set { base.Root = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The module to perform an operation on.
        /// </summary>
        /// <value>
        /// The module to perform an operation on.  This is a normal file/folder
        /// name without path information.
        /// </value>
        /// <example>
        ///   <para>In NAnt the module name would be:</para>
        ///   <code>nant</code>
        /// </example>
        [TaskAttribute("module", Required=false)]
        [StringValidator(AllowEmpty=true)]
        public virtual string Module {
            get {
                if (null == _module) {
                    try {
                        Repository repository = Repository.Load(this.DestinationDirectory);
                        this._module = repository.ModuleName;
                    } catch (ICSharpCode.SharpCvsLib.Exceptions.CvsFileNotFoundException) {
                        throw new BuildException (string.Format("Cvs/Repository file not found in {0}, please perform a checkout.",
                            this.DestinationDirectory.FullName));
                    }
                }
                return _module;
            }
            set { _module = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// <para>
        /// <see langword="true" /> if the SharpCvsLib binaries that come bundled
        /// with NAnt should be used to perform the cvs commands, <see langword="false" />
        /// otherwise.
        /// </para>
        /// <para>
        /// You may also specify an override value for all cvs tasks instead
        /// of specifying a value for each.  To do this set the property
        /// <c>sourcecontrol.usesharpcvslib</c> to <see langword="false" />.
        /// </para>
        /// <warn>
        /// If you choose not to use SharpCvsLib to checkout from cvs you will
        /// need to include a cvs.exe binary in your path.
        /// </warn>
        /// </summary>
        /// <example>
        ///     To use a cvs client in your path instead of sharpcvslib specify
        ///         the property:
        ///     &gt;property name="sourcecontrol.usesharpcvslib" value="false"&lt;
        ///
        ///     The default settings is to use sharpcvslib and the setting closest
        ///     to the task execution is used to determine which value is used
        ///     to execute the process.
        ///
        ///     For instance if the attribute usesharpcvslib was set to false
        ///     and the global property was set to true, the usesharpcvslib is
        ///     closes to the point of execution and would be used and is false.
        ///     Therefore the sharpcvslib binary would NOT be used.
        /// </example>
        [TaskAttribute("usesharpcvslib", Required=false)]
        public virtual bool UseSharpCvsLib {
            get { return _useSharpCvsLib; }
            set {
                _isUseSharpCvsLibSet = true;
                _useSharpCvsLib = value;
            }
        }

        /// <summary>
        /// The executable to use for ssh communication.
        /// </summary>
        [TaskAttribute("cvsrsh", Required=false)]
        public override FileInfo Ssh {
            get { return base.Ssh; }
            set { base.Ssh = value; }
        }

        /// <summary>
        /// Indicates if the output from the cvs command should be supressed.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("quiet", Required=false)]
        [BooleanValidator()]
        public bool Quiet {
            get {
                Option option = (Option)GlobalOptions["quiet"];
                return null == option ? false : option.IfDefined;
            }
            set { SetGlobalOption("quiet", "-q", value); }
        }

        /// <summary>
        /// Indicates if the output from the cvs command should be stopped.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("reallyquiet", Required=false)]
        [BooleanValidator()]
        public bool ReallyQuiet {
            get {
                Option option = (Option)GlobalOptions["reallyquiet"];
                return null == option ? false : option.IfDefined;
            }
            set { SetGlobalOption("reallyquiet", "-Q", value); }
        }

        /// <summary>
        /// <see langword="true" /> if the sandbox files should be checked out in
        /// read only mode. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("readonly", Required=false)]
        [BooleanValidator()]
        public bool ReadOnly {
            get {
                Option option = (Option)GlobalOptions["readonly"];
                return null == option ? false : option.IfDefined;
            }
            set { SetGlobalOption("readonly", "-r", value); }
        }

        /// <summary>
        /// <see langword="true" /> if the sandbox files should be checked out in
        /// read/write mode. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("readwrite", Required=false)]
        [BooleanValidator()]
        public bool ReadWrite {
            get {
                Option option = (Option)GlobalOptions["readwrite"];
                return null == option ? false : option.IfDefined;
            }
            set { SetGlobalOption("readwrite", "-w", value); }
        }

        /// <summary>
        /// Compression level to use for all net traffic.  This should be a value from 1-9.
        /// <br />
        /// <br />
        /// <bold>NOTE: This is not available on sharpcvslib.</bold>
        /// </summary>
        [TaskAttribute("compressionlevel")]
        public int CompressionLevel {
            get {
                Option option = (Option)GlobalOptions["compressionlevel"];
                return null == option ? DefaultCompressionLevel : Convert.ToInt32(option.Value);
            }
            set { SetGlobalOption("readwrite", String.Format("-z{0}", value), true); }
        }

        #endregion Public Instance Properties

        #region Override Task Implementation

        /// <summary>
        /// Build up the command line arguments, determine which executable is being
        ///     used and find the path to that executable and set the working
        ///     directory.
        /// </summary>
        /// <param name="process">The process to prepare.</param>
        protected override void PrepareProcess (Process process) {
            // Although a global property can be set, take the property closest
            //  to the task execution, which is the attribute on the task itself.
            if (!_isUseSharpCvsLibSet &&
                (null == Properties || null == Properties[UseSharpCvsLibProp])) {
                // if not set and the global property is null then use the default
                _useSharpCvsLib = UseSharpCvsLib;
            } else if (!_isUseSharpCvsLibSet &&
                null != Properties[UseSharpCvsLibProp]){
                try {
                    _useSharpCvsLib =
                        System.Convert.ToBoolean(Properties[UseSharpCvsLibProp]);
                } catch (Exception) {
                    throw new BuildException (UseSharpCvsLib + " must be convertable to a boolean.");
                }
            }

            Logger.DebugFormat("number of arguments: {0}", Arguments.Count);

            // if set, pass cvsroot to command line tool
            if (Root != null) {
                Arguments.Add(new Argument(string.Format(CultureInfo.InvariantCulture,
                    "-d{0}", Root)));
            }

            if (this.UseSharpCvsLib) {
                Managed = ManagedExecution.Auto;
            }

            // Set verbose logging on the #cvslib client if used.
            if (this.UseSharpCvsLib && this.Verbose) {
                SetGlobalOption("verbose", String.Format("-verbose"), true);
            }

            AppendGlobalOptions();
            Arguments.Add(new Argument(CommandName));

            AppendCommandOptions();

            Log(Level.Debug, "Commandline args are null: {0}", 
                ((null == CommandLineArguments) ? "yes" : "no"));
            Log(Level.Debug, "Commandline: {0}", CommandLineArguments);
            if (null != CommandLineArguments) {
                Arguments.Add(new Argument(CommandLineArguments));
            }

            AppendSubCommandArgs();
            AppendFiles();

            if (IsModuleNeeded && null == Module) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Cvs module is required for this action."), 
                    Location);
            }
            if (IsModuleNeeded) {
                Arguments.Add(new Argument(Module));
            }

            if (!Directory.Exists(DestinationDirectory.FullName)) {
                Directory.CreateDirectory(DestinationDirectory.FullName);
            }
            base.PrepareProcess(process);
            process.StartInfo.FileName = ExeName;

            process.StartInfo.WorkingDirectory = DestinationDirectory.FullName;

            Log(Level.Verbose, "Working directory: {0}", process.StartInfo.WorkingDirectory);
            Log(Level.Verbose, "Executable: {0}", process.StartInfo.FileName);
            Log(Level.Verbose, "Arguments: {0}", process.StartInfo.Arguments);
        }

        /// <summary>
        /// Override to append any commands before the modele and files.
        /// </summary>
        protected virtual void AppendSubCommandArgs() {

        }

        #endregion

        #region Private Instance Methods

        private void AppendGlobalOptions () {
            foreach (Option option in GlobalOptions.Values) {
                // Log(Level.Verbose, "Type '{0}'.", optionte.GetType());
                if (!option.IfDefined || option.UnlessDefined) {
                    // skip option
                    continue;
                }
                AddArg(option.Value);
            }
        }

        /// <summary>
        /// Append the command line options or commen names for the options
        ///     to the generic options collection.  This is then piped to the
        ///     command line as a switch.
        /// </summary>
        private void AppendCommandOptions () {
            foreach (Option option in CommandOptions.Values) {
                if (!option.IfDefined || option.UnlessDefined) {
                    // skip option
                    continue;
                }
                AddArg(option.Value);
            }
        }

        /// <summary>
        /// Add the given argument to the command line options.  Note that are not explicitly
        /// quoted are split into seperate arguments.  This is to resolve a recent issue
        /// with quoting command line arguments.
        /// </summary>
        /// <param name="arg"></param>
        protected void AddArg (String arg) {
            if (arg.IndexOf(" ") > -1 && arg.IndexOf("\"") == -1) {
                string[] args = arg.Split(' ');
                foreach (string targ in args) {
                    Arguments.Add(
                        new Argument(String.Format(CultureInfo.InvariantCulture,"{0}", targ)));
                }
            } else {
                Arguments.Add(new Argument(String.Format(CultureInfo.InvariantCulture,"{0}",
                    arg)));
            }
        }

        #endregion Private Instance Methods
    }
}
