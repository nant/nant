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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Configuration;
using System.Reflection;
using System.Globalization;
using System.Text;
using System.Xml;

using System.Security;
using System.Security.Permissions;

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

            // check for non-Unix platforms - see FAQ for more details
            // http://www.mono-project.com/FAQ:_Technical#How_to_detect_the_execution_platform_.3F
            int platform = (int) Environment.OSVersion.Platform;
            if (platform != 4 && platform != 128) {
                Platform = "win32";
            } else {
                Platform = "unix";
            }
        }

        #endregion Static Constructor

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

            string nantShadowCopyFilesSetting = ConfigurationManager.AppSettings.Get("nant.shadowfiles");
            string nantCleanupShadowCopyFilesSetting = ConfigurationManager.AppSettings.Get("nant.shadowfiles.cleanup");


            Framework runtimeFramework = Framework.GetRuntimeFramework();
            if (runtimeFramework == null) {
                // signal error
                return 1;
            }

            string privateBinPath = ConstructPrivateBinPath(runtimeFramework,
                AppDomain.CurrentDomain.BaseDirectory);

            if (nantShadowCopyFilesSetting != null && bool.Parse(nantShadowCopyFilesSetting) == true) {
                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "Shadowing files({0}) -- cleanup={1}", 
                    nantShadowCopyFilesSetting, nantCleanupShadowCopyFilesSetting);

                System.AppDomainSetup myDomainSetup = new System.AppDomainSetup();

                myDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "NAntDomain.PrivateBinPath={0}", 
                    myDomainSetup.PrivateBinPath);

                myDomainSetup.PrivateBinPath = privateBinPath;

                myDomainSetup.ApplicationName = "NAnt";

                // copy the config file location
                myDomainSetup.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "NAntDomain.ConfigurationFile={0}", 
                    myDomainSetup.ConfigurationFile);

                // yes, cache the files
                myDomainSetup.ShadowCopyFiles = "true";

                // shadowcopy everything in base directory of appdomain and
                // privatebinpath
                myDomainSetup.ShadowCopyDirectories = myDomainSetup.ApplicationBase 
                    + Path.PathSeparator + myDomainSetup.PrivateBinPath;

                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "NAntDomain.ShadowCopyDirectories={0}", 
                    myDomainSetup.ShadowCopyDirectories);

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

                    logger.DebugFormat(CultureInfo.InvariantCulture,
                        "NAntDomain.CachePath={0}", 
                        myDomainSetup.CachePath);
                }

                // create the domain.
                PermissionSet myDomainPermSet = new PermissionSet(PermissionState.Unrestricted);
                executionAD = AppDomain.CreateDomain(myDomainSetup.ApplicationName, AppDomain.CurrentDomain.Evidence, 
                    myDomainSetup, myDomainPermSet);

                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "NAntDomain.SetupInfo:\n{0}", 
                    executionAD.SetupInformation);
            }

            // use helper object to hold (and serialize) args for callback.
            logger.DebugFormat(CultureInfo.InvariantCulture,
                "Creating HelperArgs({0})", 
                args.ToString());

            HelperArguments helper = new HelperArguments(args, 
                privateBinPath);

            executionAD.DoCallBack(new CrossAppDomainDelegate(helper.CallConsoleRunner));

            // unload if remote/new appdomain
            if (!cd.Equals(executionAD)) {
                string cachePath = executionAD.SetupInformation.CachePath;

                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "Unloading '{0}' AppDomain", 
                    executionAD.FriendlyName);

                AppDomain.Unload(executionAD);

                if (nantCleanupShadowCopyFilesSetting != null && bool.Parse(nantCleanupShadowCopyFilesSetting) == true) {
                    logger.DebugFormat(CultureInfo.InvariantCulture,
                        "Unloading '{0}' AppDomain", 
                        executionAD.FriendlyName);
                    try {
                        logger.DebugFormat(CultureInfo.InvariantCulture,
                            "Cleaning up CacheFiles in '{0}'", 
                            cachePath);

                        Directory.Delete(cachePath, true);
                    } catch (FileNotFoundException ex) {
                        logger.Error("Files not found.", ex);
                    } catch (Exception ex) {
                        System.Console.WriteLine("Unable to delete cache path '{1}'.\n\n{0}.", ex.ToString(), cachePath);
                    }
                }
            }

            if (helper == null || helper.ExitCode == -1) {
                logger.DebugFormat(
                    CultureInfo.InvariantCulture,
                    "Return Code null or -1");

                throw new ApplicationException("No return code set!");
            } else {
                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "Return Code = {0}", 
                    helper.ExitCode);

                return helper.ExitCode;
            }
        }

        #endregion Public Static Methods

        #region Private Static Methods

        /// <summary>
        /// Constructs the privatebinpath.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   For the common version dir, we do not use the framework version
        ///   as defined in the NAnt configuration file but the CLR version
        ///   since the assemblies in that directory are not specific to a 
        ///   certain family and the framwork version might differ between
        ///   families (eg. mono 1.0 == .NET 1.1).
        ///   </para>
        /// </remarks>
        /// <param name="runtimeFramework">The runtime framework.</param>
        /// <param name="baseDir">The base directory of the domain.</param>
        /// <returns>
        /// The privatebinpath.
        /// </returns>
        private static string ConstructPrivateBinPath (Framework runtimeFramework, string baseDir) {
            StringBuilder sb = new StringBuilder ();

            foreach (string probePath in runtimeFramework.ProbePaths) {
                string fullDir = Path.Combine (baseDir, probePath);
                AppendPrivateBinDir(baseDir, fullDir, sb);
            }

            // add privatebinpath of current domain to privatebinpath 
            if (AppDomain.CurrentDomain.SetupInformation.PrivateBinPath != null) {
                if (sb.Length > 0) {
                    sb.Append(Path.PathSeparator);
                }
                sb.Append(AppDomain.CurrentDomain.SetupInformation.PrivateBinPath);
            }

            return sb.ToString();
        }

        private static void AppendPrivateBinDir(string baseDir, string dir, StringBuilder sb) {
            if (!Directory.Exists (dir)) {
                return;
            }

            if (sb.Length != 0) {
                sb.Append(Path.PathSeparator);
            }
            sb.Append(GetRelativePath(baseDir, dir));

            string[] subDirs = Directory.GetDirectories(dir);
            for (int i = 0; i < subDirs.Length; i++) {
                AppendPrivateBinDir(baseDir, subDirs[i], sb);
            }
        }

        /// <summary>
        /// Given an absolute directory and an absolute file name, returns a 
        /// relative file name.
        /// </summary>
        /// <param name="basePath">An absolute directory.</param>
        /// <param name="absolutePath">An absolute file name.</param>
        /// <returns>
        /// A relative file name for the given absolute file name.
        /// </returns>
        private static string GetRelativePath(string basePath, string absolutePath) {
            string fullBasePath = Path.GetFullPath(basePath);
            string fullAbsolutePath = Path.GetFullPath(absolutePath);

            bool caseInsensitive = false;

            // check if we're not on unix
            if ((int) Environment.OSVersion.Platform != 128) {
                // for simplicity, we'll consider all filesystems on windows
                // to be case-insensitive
                caseInsensitive = true;

                // on windows, paths with different roots are located on different
                // drives, so only absolute names will do
                if (string.Compare(Path.GetPathRoot(fullBasePath), Path.GetPathRoot(fullAbsolutePath), caseInsensitive) != 0) {
                    return fullAbsolutePath;
                }
            }

            int baseLen = fullBasePath.Length;
            int absoluteLen = fullAbsolutePath.Length;

            // they are on the same "volume", find out how much of the base path
            // is in the absolute path
            int i = 0;
            while (i < absoluteLen && i < baseLen && string.Compare(fullBasePath[i].ToString(), fullAbsolutePath[i].ToString(), caseInsensitive) == 0) {
                i++;
            }
            
            if (i == baseLen && (fullAbsolutePath[i] == Path.DirectorySeparatorChar || fullAbsolutePath[i-1] == Path.DirectorySeparatorChar)) {
                // the whole current directory name is in the file name,
                // so we just trim off the current directory name to get the
                // current file name.
                if (fullAbsolutePath[i] == Path.DirectorySeparatorChar) {
                    // a directory name might have a trailing slash but a relative
                    // file name should not have a leading one...
                    i++;
                }

                return fullAbsolutePath.Substring(i);
            }

            // The file is not in a child directory of the current directory, so we
            // need to step back the appropriate number of parent directories by
            // using ".."s.  First find out how many levels deeper we are than the
            // common directory

            string commonPath = fullBasePath.Substring(0, i);

            int levels = 0;
            string parentPath = fullBasePath;

            // remove trailing directory separator character
            if (parentPath[parentPath.Length - 1] == Path.DirectorySeparatorChar) {
                parentPath = parentPath.Substring(0, parentPath.Length - 1);
            }

            while (string.Compare(parentPath,commonPath, caseInsensitive) != 0) {
                levels++;
                DirectoryInfo parentDir = Directory.GetParent(parentPath);
                if (parentDir != null) {
                    parentPath = parentDir.FullName;
                } else {
                    parentPath = null;
                }
            }
                
            string relativePath = "";
            
            for (i = 0; i < levels; i++) {
                relativePath += ".." + Path.DirectorySeparatorChar;
            }

            relativePath += fullAbsolutePath.Substring(commonPath.Length);
            return relativePath;
        }

        #endregion Private Static Methods

        #region Private Static Fields

        private static readonly string FrameworkFamily;
        private static readonly string Platform;

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
                // explicitly add the lib directory to privatebinpath although 
                // its added to privatebinpath in the config file, as entries 
                // in the config file are not reflected in SetupInformation
                if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib"))) {
                    AppDomain.CurrentDomain.AppendPrivatePath("lib");
                }

                // add framework specific entries to privatebinpath
                if (_probePaths != null) {
                    foreach (string probePath in _probePaths.Split(Path.PathSeparator)) {
                        logger.DebugFormat(CultureInfo.InvariantCulture,
                            "Adding '{0}' to private bin path.", 
                            probePath);
                        AppDomain.CurrentDomain.AppendPrivatePath(probePath);
                    }
                }

                MethodInfo mainMethodInfo = null;

                //load the core by name!
                Assembly nantCore = AppDomain.CurrentDomain.Load("NAnt.Core");

                logger.InfoFormat(CultureInfo.InvariantCulture,
                    "NAnt.Core Loaded: {0}", 
                    nantCore.FullName);

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

                logger.DebugFormat(CultureInfo.InvariantCulture,
                    "'{0}' returned {1}", 
                    mainMethodInfo.ToString(), ExitCode);
            }

            #endregion Public Instance Methods

            #region Private Instance Fields

            private string[] _args;
            private string _probePaths;
            private int _exitCode = -1;

            #endregion Private Instance Fields

            #region Private Static Fields

            private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            #endregion Private Static Fields
        }

        private class Framework {
            private readonly string _version;
            private readonly string[] _probePaths;

            private Framework (string version, string [] probePaths) {
                _version = version;
                _probePaths = probePaths;
            }

            public string Version {
                get { return _version; }
            }

            public string [] ProbePaths {
                get { return _probePaths; }
            }

            public static Framework GetRuntimeFramework () {

                XmlNode nantNode = (XmlNode) ConfigurationManager.GetSection("nant");
                if (nantNode == null) { 
                    System.Console.WriteLine("The \"nant\" section in the NAnt"
                        + " configuration file ({0}) is not available.",
                        AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                    return null;
                }

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
                }

                string frameworkVersion = frameworkNode.GetAttribute("version");

                XmlNodeList includeNodes = frameworkNode.SelectNodes("runtime/probing-paths/directory");
                ArrayList includes = new ArrayList (includeNodes.Count);
                foreach (XmlNode node in includeNodes) {
                    XmlElement includeNode = (XmlElement) node;
                    string name = includeNode.GetAttribute("name");
                    includes.Add (name);
                }

                string[] probePaths = new string[includes.Count];
                includes.CopyTo (probePaths, 0);
                return new Framework(frameworkVersion, probePaths);
            }
        }
    }
}
