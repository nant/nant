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
        private int DEFAULT_COMPRESSION_LEVEL = 3;

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string _commandName;
        /// <summary>
        /// The cvs command to execute.
        /// </summary>
        [TaskAttribute("command", Required=true)]
        public string CommandName {
            get {return this._commandName;}
            set {this._commandName = value;}
        }

        /// <summary>
        /// Execute the cvs command using either sharpcvslib or the binary file in 
        ///     the path variable.
        /// </summary>
        protected override void ExecuteTask () {
            // Get Executable name
            Log(Level.Debug, LogPrefix + "Starting CvsTask.");
            String fileName = this.GetFileName();

            Log(Level.Debug, LogPrefix + "Executing cvs binary: " + fileName);
            Log(Level.Debug, LogPrefix + "Cvs Version Info: " + this.GetCvsVersion(fileName));

            String commandLine = this.CreateCommandLine();
            Log(Level.Debug, LogPrefix + "Executing command line: " + commandLine);

            // Create new process
            ProcessStartInfo cvsProcessInfo = 
                new ProcessStartInfo(fileName, commandLine);
            cvsProcessInfo.UseShellExecute = false;
            cvsProcessInfo.WorkingDirectory = this.DestinationDirectory.FullName;
            cvsProcessInfo.RedirectStandardOutput = true;
            cvsProcessInfo.CreateNoWindow = true;

            // Run the process
            Process cvsProcess = new Process();
            cvsProcess.StartInfo = cvsProcessInfo;
            try {
                cvsProcess.Start();
            } catch (Exception) {
                throw new Exception ("Unable to start process: " + fileName);
            }

            string output = cvsProcess.StandardOutput.ReadToEnd();
            Log(Level.Debug, LogPrefix + output);
            cvsProcess.WaitForExit();
        }

        private String GetCvsVersion (String fileName) {
            ProcessStartInfo versionStartInfo = 
                new ProcessStartInfo(fileName, "--version");
            versionStartInfo.UseShellExecute = false;
            versionStartInfo.WorkingDirectory = this.DestinationDirectory.FullName;
            versionStartInfo.RedirectStandardOutput = true;
            versionStartInfo.CreateNoWindow = true;

            // Run the process
            Process cvsProcess = new Process();
            cvsProcess.StartInfo = versionStartInfo;
            try {
                cvsProcess.Start();
            } catch (Exception e) {
                Log(Level.Debug, LogPrefix + "Exception getting version.  Exception: " +
                    e);
            }

            string versionInfo = cvsProcess.StandardOutput.ReadToEnd();

            return versionInfo;
        }

        private String GetFileName () {
            String fileName;
            if (this.UseSharpCvsLib) {
                fileName = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, "cvs.exe");
            } else {
                fileName = this.GetCvsFromPath();
            }
            return fileName;
        }

        private String GetCvsFromPath () {
            String fileName = null;

            String path = Environment.GetEnvironmentVariable("PATH");
            String[] pathElements = path.Split(';');
            foreach (String pathElement in pathElements) {
                try {
                    String[] files = Directory.GetFiles(pathElement, "*.exe");
                    foreach (String file in files) {
                        if (Path.GetFileName(file).ToLower().IndexOf("cvs") >= 0) {
                            Log(Level.Debug, LogPrefix + "Using file " + file + 
                                "; file.ToLower().IndexOf(\"cvs\") >=0: " + file.ToLower().IndexOf("cvs"));
                            fileName = file;
                            break;
                        }
                    }
                } catch (DirectoryNotFoundException) {
                    // expected, happens if the path contains an old directory.
                    Log(Level.Debug, LogPrefix + "Path does not exist: " + pathElement);
                } catch (ArgumentException) {
                    Log(Level.Debug, LogPrefix + "Path does not exist: " + pathElement);
                }
                if (null != fileName) {
                    break;
                }
            }

            if (null == fileName) {
                throw new BuildException ("Cvs binary not specified.");
            }
            return fileName;
        }

        /// <summary>
        /// Create a command object.
        /// </summary>
        /// <returns>A new command object.</returns>
        /// <exception cref="NotImplementedException">Not implemented, just 
        ///     here to fulfill the requirements of the AbstractCvsTask command.</exception>
        protected override ICommand CreateCommand () {
            throw new NotImplementedException ("Not implemented.");
        }

        private String CreateCommandLine () {
            StringBuilder commandLine = new StringBuilder ();

            commandLine.Append("-d").Append(this.CvsRoot).Append(" ");

            String globalArgs = this.CreateGlobalArgs(this.GlobalOptions);

            if (null != globalArgs && String.Empty != globalArgs) {
                commandLine.Append("-").Append(globalArgs);
                commandLine.Append(" ");
            }

            commandLine.Append(this._commandName).Append(" ");

            String commandArgs = this.CreateCommandArgs(this.CommandOptions);

            if (null != commandArgs && String.Empty != commandArgs) {
                commandLine.Append(commandArgs);
                commandLine.Append(" ");
            }

            commandLine.Append(this.Module);

            return commandLine.ToString();
        }

        private String CreateGlobalArgs (OptionCollection globalOptions) {
            StringBuilder globalArgs = new StringBuilder ();

            foreach (Option option in globalOptions) {
                if (!IfDefined || UnlessDefined) {
                    // skip option
                    continue;
                }
                switch (option.OptionName) {
                    case "really-quiet":
                    case "-Q":
                        globalArgs.Append("-Q").Append(" ");;
                        break;
                    case "somewhat-quiet":
                    case "-q":
                        globalArgs.Append("-q").Append(" ");;
                        break;
                    case "compression-level":
                    case "-z":
                        globalArgs.Append("-z").Append(" ");;
                        int compressionLevel = DEFAULT_COMPRESSION_LEVEL;
                        if (option.Value == null || String.Empty == option.Value) {
                            compressionLevel = System.Convert.ToInt32(option.Value);
                        }
                        globalArgs.Append(compressionLevel).Append(" ");
                        break;
                    case "no-execute":
                    case "-n":
                        globalArgs.Append("-n").Append(" ");
                        break;
                    case "cvs-rsh":
                        String cvsRsh = Environment.GetEnvironmentVariable("CVS_RSH");
                        if (null == cvsRsh || String.Empty == cvsRsh) {
                            Environment.GetEnvironmentVariables().Add("CVS_RSH", option.Value);
                        }
                        break;
                    default:
                        throw new ArgumentException("Unsupported option: " + option.Name);
                }
            }
            return globalArgs.ToString();
        }

        private String CreateCommandArgs (OptionCollection commandOptions) {
            StringBuilder commandArgs = new StringBuilder ();
            foreach (Option option in commandOptions) {
                if (!IfDefined || UnlessDefined) {
                    // skip option
                    continue;
                }

                Logger.Debug ("option.OptionName=[" + option.OptionName + "]");
                Logger.Debug ("option.Value=[" + option.Value + "]");
                switch (option.OptionName) {
                    case "sticky-tag":
                    case "-r":
                        commandArgs.Append("-r").Append(" ").Append(option.Value).Append(" ");
                        Logger.Debug ("setting sticky-tag=[" + option.Value + "]");
                        break;
                    case "override-directory":
                    case "-d":
                        commandArgs.Append("-d").Append(" ").Append(option.Value).Append(" ");;
                        Logger.Debug ("setting override-directory=[" + option.Value + "]");
                        break;
                    case "prune":
                    case "-P":
                        commandArgs.Append("-P").Append(" ").Append(" ");;
                        Logger.Debug("setting prune to true.");
                        break;
                    default:
                        StringBuilder msg = new StringBuilder ();
                        msg.Append(Environment.NewLine + "Unsupported argument.");
                        msg.Append(Environment.NewLine + "\tname=[").Append(option.OptionName).Append ("]");
                        msg.Append(Environment.NewLine + "\tvalue=[").Append(option.Value).Append ("]");
                        throw new NotSupportedException(msg.ToString());
                }
            }
            return commandArgs.ToString();
        }

	}
}
