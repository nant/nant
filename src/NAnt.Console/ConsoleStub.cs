// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Gerry Shaw
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
// Scott Hernandez(ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Configuration;
using System.Reflection;
using System.Globalization;
using System.Xml;

namespace NAnt.Console {
    /// <summary>
    /// Stub used to created <see cref="AppDomain" /> and launch real <c>ConsoleDriver</c> 
    /// class in Core assembly.
    /// </summary>
    public class ConsoleStub {
        #region Static Constructor

        static ConsoleStub() {
            // check a class in mscorlib to determine if we're running on Mono
            if (Type.GetType("Mono.Runtime", false) != null) {
                FrameworkFamily = "mono";
            } else {
                FrameworkFamily = "net";
            }

            if ((int) Environment.OSVersion.Platform == 128) {
                Platform = "unix";
            } else {
                Platform = "win32";
            }
        }

        #endregion Static Constructor

        #region Private Static Properties

        private static string FrameworkVersion {
            get {
                if (_frameworkVersion != null) {
                    return _frameworkVersion;
                }

                XmlNode nantNode = (XmlNode) ConfigurationSettings.GetConfig("nant");
                XmlElement frameworkNode = (XmlElement) nantNode.SelectSingleNode("frameworks/platform[@name='" + Platform + "']/framework[@family='" + FrameworkFamily + "' and @clrversion='" + Environment.Version.ToString(3) + "']");
                
                if (frameworkNode == null) {
                    System.Console.WriteLine("The NAnt configuration file ({0})"
                        + " does not have a <framework> node for the current"
                        + " runtime framework.", 
                        AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                    System.Console.WriteLine(string.Empty);
                    System.Console.WriteLine("Please add a <framework> node"
                        + " with family '{0}' and clrversion '{1}' under the"
                        + " '{2}' platform node.", FrameworkFamily, 
                        Environment.Version.ToString(3), Platform);
                    return null;
                } else {
                    _frameworkVersion = frameworkNode.GetAttribute("version");
                }

                return _frameworkVersion;
            }
        }

        #endregion Private Static Properties

        #region Public Static Methods

        /// <summary>
        /// Entry point for executable
        /// </summary>
        /// <param name="args">Command Line arguments</param>
        /// <returns>The result of the real execution</returns>
        [STAThread]
        public static int Main(string[] args) {
            AppDomain cd = AppDomain.CurrentDomain;
            AppDomain executionAD = cd;

            string nantShadowCopyFilesSetting = ConfigurationSettings.AppSettings.Get("nant.shadowfiles");
            string nantCleanupShadowCopyFilesSetting = ConfigurationSettings.AppSettings.Get("nant.shadowfiles.cleanup");

            if (FrameworkVersion == null) {
                // signal error
                return 1;
            }

            string frameworkFamilyLibDir = Path.Combine("lib", FrameworkFamily);
            string frameworkVersionLibDir = Path.Combine(frameworkFamilyLibDir, 
                FrameworkVersion);

            string privateBinPath = null;

            // add lib/<family>/<version> dir to privatebinpath if it exists
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, frameworkVersionLibDir))) {
                privateBinPath += (privateBinPath != null) ? ";" + frameworkVersionLibDir : frameworkVersionLibDir;
            }

            // add lib/<family> dir to privatebinpath if it exists
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, frameworkFamilyLibDir))) {
                privateBinPath += (privateBinPath != null) ? ";" + frameworkFamilyLibDir : frameworkFamilyLibDir;
            }

            // add privatebinpath of current domain to privatebinpath 
            if (AppDomain.CurrentDomain.SetupInformation.PrivateBinPath != null) {
                privateBinPath += ";" + AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            }

            if (nantShadowCopyFilesSetting != null && bool.Parse(nantShadowCopyFilesSetting) == true) {
                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "Shadowing files({0}) -- cleanup={1}", 
                    nantShadowCopyFilesSetting, 
                    nantCleanupShadowCopyFilesSetting));
                    */

                System.AppDomainSetup myDomainSetup = new System.AppDomainSetup();

                myDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "NAntDomain.PrivateBinPath={0}", 
                    myDomainSetup.PrivateBinPath));
                    */

                myDomainSetup.PrivateBinPath = privateBinPath;

                myDomainSetup.ApplicationName = "NAnt";

                // copy the config file location
                myDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            
                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "NAntDomain.ConfigurationFile={0}", 
                    myDomainSetup.ConfigurationFile));
                    */

                // yes, cache the files
                myDomainSetup.ShadowCopyFiles = "true";

                // shadowcopy everything in base directory of appdomain and
                // privatebinpath
                myDomainSetup.ShadowCopyDirectories = myDomainSetup.ApplicationBase 
                    + ";" +myDomainSetup.PrivateBinPath; 
                
                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "NAntDomain.ShadowCopyDirectories={0}", 
                    myDomainSetup.ShadowCopyDirectories));
                    */

                // try to cache in .\cache folder, if that fails, let the system 
                // figure it out.
                string cachePath = Path.Combine(myDomainSetup.ApplicationBase, "cache");
                DirectoryInfo cachePathInfo = null;

                try {
                    cachePathInfo = Directory.CreateDirectory(cachePath);
                } catch (Exception e) {
                    System.Console.WriteLine("Failed to create: {0}. Using default CachePath." + e.ToString(), cachePath);
                } finally {
                    if(cachePathInfo != null) {
                        myDomainSetup.CachePath = cachePathInfo.FullName;
                    }

                    /*
                    logger.Debug(string.Format(
                        CultureInfo.InvariantCulture,
                        "NAntDomain.CachePath={0}", 
                        myDomainSetup.CachePath));
                        */
                }

                // create the domain.
                executionAD = AppDomain.CreateDomain(myDomainSetup.ApplicationName,
                    AppDomain.CurrentDomain.Evidence, myDomainSetup);

                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "NAntDomain.SetupInfo:\n{0}", 
                    executionAD.SetupInformation));
                    */
            }

            // use helper object to hold (and serialize) args for callback.
            /*
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "Creating HelperArgs({0})", 
                args.ToString()));
                */
            
            HelperArguments helper = new HelperArguments(args, 
                privateBinPath);

            executionAD.DoCallBack(new CrossAppDomainDelegate(helper.CallConsoleRunner));

            // unload if remote/new appdomain
            if (!cd.Equals(executionAD)) {
                string cachePath = executionAD.SetupInformation.CachePath;

                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "Unloading '{0}' AppDomain", 
                    executionAD.FriendlyName));
                    */

                AppDomain.Unload(executionAD);

                if (nantCleanupShadowCopyFilesSetting != null && bool.Parse(nantCleanupShadowCopyFilesSetting) == true) {
                    /*
                    logger.Debug(string.Format(
                        CultureInfo.InvariantCulture,
                        "Unloading '{0}' AppDomain", 
                        executionAD.FriendlyName));
                        */
                    try {
                        /*
                        logger.Debug(string.Format(
                            CultureInfo.InvariantCulture,
                            "Cleaning up CacheFiles in '{0}'", 
                            cachePath));
                            */
                        Directory.Delete(cachePath, true);
                    } catch (FileNotFoundException ex) {
                        ex = ex;

                        /*
                        logger.Error("Files not found.", ex);
                        */
                    } catch (Exception ex) {
                        System.Console.WriteLine("Unable to delete cache path '{1}'.\n\n{0}.", ex.ToString(), cachePath);
                    }
                }
            }

            if (helper == null || helper.ExitCode == -1) {
                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "Return Code null or -1"));
                    */
                throw new ApplicationException("No return code set!");
            } else {
                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "Return Code = {0}", 
                    helper.ExitCode));
                    */
                return helper.ExitCode;
            }
        }

        #endregion Public Static Methods

        #region Private Static Fields

        private static readonly string FrameworkFamily;
        private static readonly string Platform;
        private static string _frameworkVersion;

        /*
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        */

        #endregion Private Static Fields

        /// <summary>
        /// Helper class for invoking the application entry point in NAnt.Core
        /// and passing the command-line arguments.
        /// </summary>
        [Serializable()]
        private class HelperArguments : MarshalByRefObject {
            #region Public Instance Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="HelperArguments" />
            /// class with the specified command-line arguments.
            /// </summary>
            /// <param name="args">The commandline arguments passed to NAnt.exe.</param>
            /// <param name="probePaths">Directories relative to the base directory of the AppDomain to probe for missing assembly references.</param>
            public HelperArguments(string[] args, string probePaths) {
                _args = args;
                _probePaths = probePaths;
            }

            #endregion Public Instance Constructors

            #region Public Instance Properties

            /// <summary>
            /// Gets the status that the build process returned when it exited.
            /// </summary>
            /// <value>
            /// The code that the build process specified when it terminated.
            /// </value>
            public int ExitCode {
                get { return _exitCode; }
            }

            #endregion Public Instance Properties

            #region Public Instance Methods

            /// <summary>
            /// Invokes the application entry point in NAnt.Core.
            /// </summary>
            public void CallConsoleRunner() {
                if (_probePaths != null) {
                    foreach (string probePath in _probePaths.Split(';')) {
                        AppDomain.CurrentDomain.AppendPrivatePath(probePath);
                    }
                }

                MethodInfo mainMethodInfo = null;

                //load the core by name!
                Assembly nantCore = AppDomain.CurrentDomain.Load("NAnt.Core");

                /*
                logger.Info(string.Format(
                    CultureInfo.InvariantCulture,
                    "NAnt.Core Loaded: {0}", 
                    nantCore.FullName));
                    */

                //get the ConsoleDriver by name
                Type consoleDriverType = nantCore.GetType("NAnt.Core.ConsoleDriver", true, true);

                //find the Main Method, this method is less than optimal, but other methods failed.
                foreach (MethodInfo methodInfo in consoleDriverType.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                    if (methodInfo.Name.Equals("Main")) {
                        mainMethodInfo = methodInfo;
                        break;
                    }
                }

                // invoke the Main method and pass the command-line arguments as parameter.
                _exitCode = (int) mainMethodInfo.Invoke(null, new object[] {_args});

                /*
                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' returned {1}", 
                    mainMethodInfo.ToString(), ExitCode));
                    */
            }

            #endregion Public Instance Methods

            #region Private Instance Fields

            private string[] _args;
            private string _probePaths;
            private int _exitCode = -1;

            #endregion Private Instance Fields
        }
    }
}
