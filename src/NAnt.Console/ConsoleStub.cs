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
// Scott Hernandez (ScottHernandez@hotmail.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
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
            if (Type.GetType("System.MonoType", false) != null) {
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

        private static bool ShadowCopyFiles {
            get {
                try {
                    string shadowCopyFiles = ConfigurationSettings.AppSettings.Get("nant.shadowfiles");
                    if (shadowCopyFiles != null && bool.Parse(shadowCopyFiles)) {
                        return true;
                    }
                    return false;
                } catch {
                    // ignore errors, and consider shadow copy to be disabled
                    return false;
                }
            }
        }

        private static bool CleanShadowCopyCache {
            get {
                try {
                    string cleanShadowCopyCache = ConfigurationSettings.AppSettings.Get("nant.shadowfiles.cleanup");
                    if (cleanShadowCopyCache != null && bool.Parse(cleanShadowCopyCache)) {
                        return true;
                    }
                    return false;
                } catch {
                    // ignore errors, and consider shadow copy to be disabled
                    return false;
                }
            }
        }

        private static string CacheDirectory {
            get { 
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "cache");
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
            if (FrameworkVersion == null) {
                // signal error
                return 1;
            }

            // create domain setup
            AppDomainSetup domainSetup = CreateDomainSetup();
            // create the domain.
            AppDomain executionAD = AppDomain.CreateDomain(domainSetup.ApplicationName,
                AppDomain.CurrentDomain.Evidence, domainSetup);
            // instantiate helper object in newly constructed domain passing
            // in the command line arguments
            HelperArguments helper = (HelperArguments) executionAD.CreateInstanceAndUnwrap(
                typeof(ConsoleStub).Assembly.FullName, typeof(HelperArguments).FullName, false,
                BindingFlags.Public | BindingFlags.Instance, null, new object[] {args}, 
                CultureInfo.InvariantCulture, new object[0], AppDomain.CurrentDomain.Evidence);
            // perform the build    
            helper.CallConsoleRunner();
            // determine outcome of build
            int exitCode = helper.ExitCode;
            // unload domain in which NAnt was executed
            AppDomain.Unload(executionAD);
            // determine if we need to clean up cache folder, if shadow copying
            // was enabled
            if (ShadowCopyFiles && CleanShadowCopyCache) {
                if (Directory.Exists(CacheDirectory)) {
                    try {
                        Directory.Delete(CacheDirectory, true);
                    } catch (Exception ex) {
                        System.Console.WriteLine("Cache directory could not be"
                            + "cleaned: " + ex.Message);
                    }
                }
            }
            if (exitCode == -1) {
                throw new ApplicationException("No return code set!");
            } else {
                return exitCode;
            }
        }

        #endregion Public Static Methods

        #region Private Static Methods

        private static AppDomainSetup CreateDomainSetup() {
            string frameworkFamilyLibDir = Path.Combine("lib", FrameworkFamily);
            string frameworkVersionLibDir = Path.Combine(frameworkFamilyLibDir, 
                FrameworkVersion);

            string privateBinPath = null;

            // add lib/<family>/<version> dir to privatebinpath if it exists
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, frameworkVersionLibDir))) {
                privateBinPath += (privateBinPath != null) ? Path.PathSeparator 
                    + frameworkVersionLibDir : frameworkVersionLibDir;
            }

            // add lib/<family> dir to privatebinpath if it exists
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, frameworkFamilyLibDir))) {
                privateBinPath += (privateBinPath != null) ? Path.PathSeparator 
                    + frameworkFamilyLibDir : frameworkFamilyLibDir;
            }

            // add privatebinpath of current domain to privatebinpath 
            if (AppDomain.CurrentDomain.SetupInformation.PrivateBinPath != null) {
                privateBinPath += Path.PathSeparator + AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            }

            AppDomainSetup domainSetup = new System.AppDomainSetup();

            domainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            domainSetup.PrivateBinPath = privateBinPath;
            domainSetup.ApplicationName = "NAnt";
            domainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            // check if we need to enable shadow copying of files
            if (ShadowCopyFiles) {
                // turn shadow copying on
                domainSetup.ShadowCopyFiles = "true";

                // shadow copy everything in base directory of appdomain and
                // privatebinpath
                domainSetup.ShadowCopyDirectories = domainSetup.ApplicationBase 
                    + Path.PathSeparator + domainSetup.PrivateBinPath; 

                // try to cache in .\cache folder, if that fails, let the system 
                // figure it out.
                string cachePath = CacheDirectory;
                DirectoryInfo cachePathInfo = null;

                try {
                    cachePathInfo = Directory.CreateDirectory(cachePath);
                } catch (Exception e) {
                    System.Console.WriteLine("Failed to create: {0}. Using default CachePath." + e.ToString(), cachePath);
                } finally {
                    if (cachePathInfo != null) {
                        domainSetup.CachePath = cachePathInfo.FullName;
                    }
                }
            }

            return domainSetup;            
        }

        #endregion Private Static Methods

        #region Private Static Fields

        private static readonly string FrameworkFamily;
        private static readonly string Platform;
        private static string _frameworkVersion;

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            public HelperArguments(string[] args) {
                _args = args;
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

            #region Override implementation of MarshalByRefObject

            /// <summary>
            /// Obtains a lifetime service object to control the lifetime policy for 
            /// this instance.
            /// </summary>
            /// <returns>
            /// An object of type <see cref="ILease" /> used to control the lifetime 
            /// policy for this instance. This is the current lifetime service object 
            /// for this instance if one exists; otherwise, a new lifetime service 
            /// object initialized with a lease that will never time out.
            /// </returns>
            public override Object InitializeLifetimeService() {
                ILease lease = (ILease) base.InitializeLifetimeService();
                if (lease.CurrentState == LeaseState.Initial) {
                    lease.InitialLeaseTime = TimeSpan.Zero;
                }
                return lease;
            }

            #endregion Override implementation of MarshalByRefObject

            #region Public Instance Methods

            /// <summary>
            /// Invokes the application entry point in NAnt.Core.
            /// </summary>
            public void CallConsoleRunner() {
                MethodInfo mainMethodInfo = null;

                // load the core by name!
                Assembly nantCore = AppDomain.CurrentDomain.Load("NAnt.Core");

                logger.Info(string.Format(
                    CultureInfo.InvariantCulture,
                    "NAnt.Core Loaded: {0}", 
                    nantCore.FullName));

                // get the ConsoleDriver by name
                Type consoleDriverType = nantCore.GetType("NAnt.Core.ConsoleDriver", true, true);

                // find the Main Method, this method is less than optimal, but 
                // other methods failed.
                foreach (MethodInfo methodInfo in consoleDriverType.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                    if (methodInfo.Name.Equals("Main")) {
                        mainMethodInfo = methodInfo;
                        break;
                    }
                }

                // invoke the Main method and pass the command-line arguments 
                // as parameter.
                _exitCode = (int) mainMethodInfo.Invoke(null, new object[] {_args});

                logger.Debug(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' returned {1}", 
                    mainMethodInfo.ToString(), ExitCode));
            }

            #endregion Public Instance Methods

            #region Private Instance Fields

            private string[] _args;
            private int _exitCode = -1;

            #endregion Private Instance Fields

            #region Private Static Fields

            private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            #endregion Private Static Fields
        }
    }
}
