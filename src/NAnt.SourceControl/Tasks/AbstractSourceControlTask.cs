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
        ///     in a *nix environment.
        /// </summary>
        protected const String EnvHome = "HOME";
        /// <summary>
        /// Used on windows to specify the location of application data.
        /// </summary>
        protected const string AppData = "APPDATA";
        /// <summary>
        /// The environment variable that holds path information.
        /// </summary>
        protected const String PathVariable = "PATH";
        /// <summary>
        /// The environment variable that holds the location of the
        /// .cvspass file.
        /// </summary>
        protected const string CvsPassFileVariable = "CVS_PASSFILE";
        /// <summary>
        /// Property name used to specify the source control executable.  This is 
        ///     used as a readonly property.
        /// </summary>
        protected const string PropExeName = "sourcecontrol.exename";

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
        ///     this can be where the binary files are kept, or other app
        ///     information.
        /// </summary>
        protected DirectoryInfo VcsHome {
            get {
                string vcsHome =
                    Environment.GetEnvironmentVariable(VcsHomeEnv);
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
        ///     (VCS) home variable is kept.
        /// </summary>
        protected abstract string VcsHomeEnv {get;}

        /// <summary>
        /// The name of the version control system (VCS) executable file.
        /// </summary>
        protected abstract string VcsExeName {get;}

        #endregion

        #region Public Instance Properties

        /// <summary>
        /// <para>
        /// The root variable contains information on how to locate a repository.  
        ///     Although this information is in different formats it typically must
        ///     define the following:
        ///     <list type="table">
        ///         <item>server location</item>
        ///         <item>protocol used to communicate with the repository</item>
        ///         <item>repository location on the server</item>
        ///         <item>project location in the repository</item>
        ///     </list>
        /// </para>
        /// </summary>
        [StringValidator(AllowEmpty=false)]
        public virtual string Root {
            get {return _root;}
            set {_root = value;}
        }

        /// <summary>
        /// Destination directory for the local sandbox.  If destination is not specified
        /// then the current directory is used.
        /// </summary>
        /// <value>
        /// Root path of the local sandbox.
        /// </value>
        /// <remarks>
        /// <para>
        /// Root path of the local sandbox.
        /// </para>
        /// </remarks>
        [TaskAttribute("destination", Required=false)]
        public virtual DirectoryInfo DestinationDirectory {
            get { 
                if (null == this._destinationDirectory) {
                    this._destinationDirectory = new DirectoryInfo(Environment.CurrentDirectory);
                }
                return this._destinationDirectory;
            }
            set { this._destinationDirectory = value; }
        }

        /// <summary>
        /// The password for logging in to the repository.
        /// </summary>
        /// <value>
        /// The password for logging in to the repository.
        /// </value>
        [TaskAttribute("password", Required=false)]
        [Obsolete("Use <cvs-pass> task instead.", true)]
        public virtual string Password {
            get { return _password;}
            set { _password = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The full path to the cached password file.  If not specified then the
        /// environment variables are used to try and locate the file.
        /// </summary>
        [TaskAttribute("passfile")]
        public virtual FileInfo PassFile {
            get { return _passFile; }
            set { _passFile = value; }
        }

        /// <summary>
        /// Holds a collection of globally available options.
        /// </summary>
        public Hashtable GlobalOptions {
            get {return _globalOptions;}
            set {_globalOptions = value;}
        }

        /// <summary>
        /// A collection of options that can be used to modify the default behavoir
        /// of the version control commands.  See the sub-tasks for implementation
        /// specifics.
        /// </summary>
        public Hashtable CommandOptions {
            get { return _commandOptions;}
            set { _commandOptions = value; }
        }

        /// <summary>
        /// Command-line arguments for the program.  The command line arguments are used to specify
        /// any cvs command options that are not available as attributes.  These are appended
        /// after the command itself and are additive to whatever attributes are currently specified.
        /// </summary>
        /// <example>
        ///     &lt;cvs-checkout    cvsroot=":pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant" 
        ///                         module="nant"
        ///                         destination="e:\test\merillcornish\working"
        ///                         readonly="true"
        ///                         quiet="true"
        ///                         commandline="-n"
        ///                         cvsfullpath="C:\Program Files\TortoiseCVS\cvs.exe"
        ///     /&gt;
        ///     <br />
        ///     Produces the cvs command:
        ///     <code>c:\Program Files\TortoiseCVS\cvs.exe -d:pserver:anonymous@cvs.sourceforge.net:/cvsroot/nant -q checkout -n nant</code>
        /// </example>
        [TaskAttribute("commandline")]
        public string CommandLineArguments {
            get {return _commandLineArguments;}
            set {_commandLineArguments = StringUtils.ConvertEmptyToNull(value);}
        }

        /// <summary>
        /// The name of the command that is going to be executed.
        /// </summary>
        public virtual string CommandName {
            get {return _commandName;}
            set {_commandName = value;}
        }

        /// <summary>
        /// Used to specify the version control system (VCS) files that are going
        /// to be acted on.
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
            get {return _ssh;}
            set {_ssh = value;}
        }

        #endregion Public Instance Properties

        #region Protected Instance Properties

        /// <summary>
        /// The environment name for the ssh variable.
        /// </summary>
        protected abstract string SshEnv {
            get;
        }

        #endregion Protected Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// The name of the version control system executable.
        /// </summary>
        public override string ExeName {
            get {return _exeName;}
            set {_exeName = value;}
        }

        /// <summary>
        /// Get the command line arguments for the task.
        /// </summary>
        public override string ProgramArguments {
            get {return _commandLine;}
        }

        /// <summary>
        /// Build up the command line arguments, determine which executable is being
        /// used and find the path to that executable and set the working 
        /// directory.
        /// </summary>
        /// <param name="process">The process to prepare.</param>
        protected override void PrepareProcess (Process process) {
            base.PrepareProcess(process);
            SetEnvironment(process);
        }

        #endregion Override implementation of ExternalProgramBase

        #region Protected Instance Methods

        /// <summary>
        /// Adds a new global option if none exists.  If one does exist then
        /// the use switch is toggled on or of.
        /// </summary>
        /// <param name="name">The common name of the option.</param>
        /// <param name="value">The option value or command line switch
        ///     of the option.</param>
        /// <param name="on"><code>true</code> if the option should be
        ///     appended to the commandline, otherwise <code>false</code>.</param>
        protected void SetGlobalOption (String name, String value, bool on) {
            Option option;
            Log(Level.Debug, "Name: {0}", name);
            Log(Level.Debug, "Value: {0}",value);
            Log(Level.Debug, "On: {0}", on);

            if (GlobalOptions.Contains(name)) {
                option = (Option)GlobalOptions[name];
            } else {
                option = new Option();
                option.OptionName = name;
                option.Value = value;
                GlobalOptions.Add(option.OptionName, option);
            } 
            option.IfDefined = on;
        }

        /// <summary>
        /// Adds a new command option if none exists.  If one does exist then
        ///     the use switch is toggled on or of.
        /// </summary>
        /// <param name="name">The common name of the option.</param>
        /// <param name="value">The option value or command line switch
        ///     of the option.</param>
        /// <param name="on"><code>true</code> if the option should be
        ///     appended to the commandline, otherwise <code>false</code>.</param>
        protected void SetCommandOption (String name, String value, bool on) {
            Option option;
            if (CommandOptions.Contains(name)) {
                option = (Option)CommandOptions[name];
            } else {
                option = new Option();
                option.OptionName = name;
                option.Value = value;
                CommandOptions.Add(name, option);
            } 
            option.IfDefined = on;
        }

        /// <summary>
        /// Set up the environment variables for a process.
        /// </summary>
        /// <param name="process">A process to setup.</param>
        protected virtual void SetEnvironment (Process process) {
            if (Ssh != null && !Ssh.Exists) {
                FileInfo tempLookup = DeriveFullPathFromEnv(PathVariable, Ssh.Name);
                if (null == tempLookup) {
                    tempLookup = DeriveFullPathFromEnv(PathVariable, Ssh.Name + ".exe");
                } 

                if (null != tempLookup) {
                    Ssh = tempLookup;
                }
            }
            if (Ssh != null) {
                try {
                    process.StartInfo.EnvironmentVariables.Add(SshEnv, Ssh.FullName);
                } catch (System.ArgumentException e) {
                    Logger.Warn("Possibility cvs_rsh key has already been added.", e);
                }
            }
            if (null != this.PassFile) {
                if (process.StartInfo.EnvironmentVariables.ContainsKey(CvsPassFileVariable)) {
                    process.StartInfo.EnvironmentVariables[CvsPassFileVariable] = this.PassFile.FullName;
                } else {
                    process.StartInfo.EnvironmentVariables.Add(CvsPassFileVariable, PassFile.FullName);
                }
            }
            Log(Level.Verbose, "Using ssh binary: {0}", process.StartInfo.EnvironmentVariables[SshEnv]);
            Log(Level.Verbose, "Using .cvspass file: {0}", process.StartInfo.EnvironmentVariables[CvsPassFileVariable]);
        }

        /// <summary>
        /// Append the files specified in the fileset to the command line argument.
        /// Files are changed to use a relative path from the working directory
        /// that the task is spawned in.
        /// </summary>
        protected void AppendFiles () {
            foreach (string pathname in VcsFileSet.FileNames) {
                string relativePath = pathname.Replace(DestinationDirectory.FullName, "");
                if (relativePath.IndexOf('/') == 0 || relativePath.IndexOf('\\') == 0) {
                    relativePath = relativePath.Substring(1, relativePath.Length - 1);
                }
                relativePath = relativePath.Replace("\\", "/");
                Arguments.Add(new Argument("\"" + relativePath + "\""));
            }
        }

        /// <summary>
        /// Derive the location of the version control system from the environment
        ///     variable <code>PATH</code>.
        /// </summary>
        /// <returns>The file information of the version control system, 
        ///     or <code>null</code> if this cannot be found.</returns>
        protected FileInfo DeriveVcsFromEnvironment () {
            FileInfo vcsFile =
                DeriveFullPathFromEnv(VcsHomeEnv, VcsExeName);
            if (null == vcsFile) {
                vcsFile = DeriveFullPathFromEnv(PathVariable, VcsExeName);
            }
            return vcsFile;
        }

        #endregion Protected Instance Methods

        #region Private Instance Methods

        private FileInfo DeriveFullPathFromEnv(string environmentVar, string fileName) {
            string environmentValue = StringUtils.ConvertEmptyToNull(
                System.Environment.GetEnvironmentVariable(environmentVar));

            Log(Level.Debug, "Environment variable: {0}", environmentVar);
            Log(Level.Debug, "Environment value: {0}", environmentValue);

            if (environmentValue != null) {
				string[] environmentPaths = environmentValue.Split(Path.PathSeparator);
                foreach (string environmentPath in environmentPaths) {
                    if (environmentPath == null) {
                        continue;
                    }

                    // remove leading or trailing quotes, which are valid for
                    // individual entries in PATH but are considered invalid 
                    // path characters
                    string cleanPath = environmentPath.Trim('\"');

                    Log(Level.Debug, "Environment Path: {0}", cleanPath);
                    Log(Level.Debug, "FileName: {0}", fileName);

                    string fileFullName = Path.Combine(cleanPath, fileName);
                    Log(Level.Debug, "FileFullName: {0}", fileFullName);
                    if (environmentPath.IndexOf(fileName) > -1 && File.Exists(fileName)) {
                        if (!(Path.GetDirectoryName(fileName).IndexOf(
                            Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)) > 1)) {
                            return new FileInfo(fileName);
                        }
                    }
                    if (fileFullName.IndexOf(fileName) > -1 && File.Exists(fileFullName)) {
                        if (Path.GetDirectoryName(fileFullName).IndexOf(
                            Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)) == -1) {
                            return new FileInfo(fileFullName);
                        }
                    }
                }
            }
            return null;
        }

        #endregion Private Instance Methods
    }
}