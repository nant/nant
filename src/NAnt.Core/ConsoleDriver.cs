// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Main entry point to NAnt that is called by the ConsoleStub.
    /// </summary>
    public class ConsoleDriver {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
        #endregion Private Static Fields

        #region Public Static Methods
                
        /// <summary>
        /// Starts NAnt. This is the Main entry point.
        /// </summary>
        /// <param name="args">Command Line args, or whatever you want to pass it. They will treated as Command Line args.</param>
        /// <returns>
        /// The exit code.
        /// </returns>
        public static int Main(string[] args) {
            CommandLineParser commandLineParser = null;
            Project project = null;
            Level projectThreshold = Level.Info;

            try {
                PropertyDictionary buildOptionProps = new PropertyDictionary();

                CommandLineOptions cmdlineOptions = new CommandLineOptions();
                commandLineParser = new CommandLineParser(typeof(CommandLineOptions));
                commandLineParser.Parse(args, cmdlineOptions);

                if (!cmdlineOptions.NoLogo) {
                    Console.WriteLine(commandLineParser.LogoBanner);
                }

                if (cmdlineOptions.ShowHelp) {
                    ConsoleDriver.ShowHelp(commandLineParser);
                    return 0;
                }

                // determine the project message threshold
                if (cmdlineOptions.Debug) {
                    projectThreshold = Level.Debug;
                } else if (cmdlineOptions.Verbose) {
                    projectThreshold = Level.Verbose;
                } else if (cmdlineOptions.Quiet) {
                    projectThreshold = Level.Warning;
                }

                if (cmdlineOptions.BuildFile != null) {
                    if(project != null) {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Buildfile has already been loaded! Using new value '{0}'; discarding old project file '{1}'", cmdlineOptions.BuildFile, project.BuildFileUri));
                    }

                    project = new Project(cmdlineOptions.BuildFile, projectThreshold, cmdlineOptions.IndentationLevel);
                }

                // Get build file name if the project has not been created.
                // If a build file was not specified on the command line.
                if(project == null) {
                    project = new Project(GetBuildFileName(Environment.CurrentDirectory, null, cmdlineOptions.FindInParent), projectThreshold, cmdlineOptions.IndentationLevel);
                }

                // add build logger and build listeners to project
                ConsoleDriver.AddBuildListeners(cmdlineOptions, project);

                // copy cmd line targets
                foreach (string target in cmdlineOptions.Targets) {
                    project.BuildTargets.Add(target);
                }

                // build collection of valid properties that were specified on 
                // the command line.
                foreach (string property in cmdlineOptions.Properties) {
                    Match match = Regex.Match(property, @"(\w+[^=]*)=(\w*.*)");
                    if (match.Success) {
                        string name = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        buildOptionProps.AddReadOnly(name, value);
                    }
                }

                // add valid properties to the project.
                foreach (System.Collections.DictionaryEntry de in buildOptionProps) {
                    project.Properties.AddReadOnly((string) de.Key, (string) de.Value);
                }

                //add these here and in the project .ctor
                Assembly ass = Assembly.GetExecutingAssembly();

                project.Properties.AddReadOnly(Project.NAntPropertyFileName, ass.Location);
                project.Properties.AddReadOnly(Project.NAntPropertyVersion,  ass.GetName().Version.ToString());
                project.Properties.AddReadOnly(Project.NAntPropertyLocation, Path.GetDirectoryName(ass.Location));

                if (cmdlineOptions.DefaultFramework != null) {
                    FrameworkInfo frameworkInfo = project.FrameworkInfoDictionary[cmdlineOptions.DefaultFramework];

                    if (frameworkInfo != null) {
                        project.CurrentFramework = project.DefaultFramework = frameworkInfo; 
                    } else {
                        logger.Fatal("Invalid framework name specified: '" + cmdlineOptions.DefaultFramework + "'");
                        Console.WriteLine(string.Format(
                            CultureInfo.InvariantCulture, 
                            "Invalid framework '{0}' specified.", 
                            cmdlineOptions.DefaultFramework));
                        Console.WriteLine();

                        if (project.FrameworkInfoDictionary.Count == 0) {
                            Console.WriteLine("There are no supported frameworks available on your system.");
                        } else {
                            Console.WriteLine("Possible values include:");
                            Console.WriteLine();

                            foreach (string s in project.FrameworkInfoDictionary.Keys) {
                                Console.WriteLine(" {0} ({1})", s, project.FrameworkInfoDictionary[s].Description);
                            }
                        }
                        // signal error
                        return 1;
                    }
                }

                if (cmdlineOptions.ShowProjectHelp) {
                    ConsoleDriver.ShowProjectHelp(project.Document);
                } else {
                    if (!project.Run()) {
                        return 1;
                    }
                }
                // signal success
                return 0;
            } catch (CommandLineArgumentException ex) {
                // Log exception to internal log
                logger.Warn("Invalid command line specified.", ex);
                // Write logo banner to conole if parser was created successfully
                if (commandLineParser != null) {
                    Console.WriteLine(commandLineParser.LogoBanner);
                }
                // Write message of exception to console
                Console.WriteLine(ex.Message);
                // insert empty line
                Console.WriteLine();
                // instruct users to check the usage instructions
                Console.WriteLine("Try 'nant -help' for more information");
                // signal error
                return 1;
            } catch (ApplicationException ex) {
                Console.WriteLine("BUILD FAILED");
                // insert empty line
                Console.WriteLine();
                // output message of exception
                Console.WriteLine(ex.Message);
                // output message of nested exception
                Exception nestedException = ex.InnerException;
                while (nestedException != null && !StringUtils.IsNullOrEmpty(nestedException.Message)) {
                    Console.WriteLine(" " + nestedException.Message);
                    nestedException = nestedException.InnerException;
                }
                // insert empty line
                Console.WriteLine();
                // check if warning messages will be logged to the internal log
                if (logger.IsWarnEnabled) {
                    logger.Warn("NAnt Build Failure", ex);
                    Console.WriteLine("Consult the log4net output for more information.");
                } else {
                    Console.WriteLine("For more information regarding the cause of the " +
                        "build failure, enable log4net using the instructions in NAnt.exe.config and " +
                        "run the build again.");
                }
                // insert empty line
                Console.WriteLine();
                // instruct users to check the usage instructions
                Console.WriteLine("Try 'nant -help' for more information");
                // signal error
                return 1;
            } catch (Exception ex) {
                // all other exceptions should have been caught
                Console.WriteLine("INTERNAL ERROR");
                // insert empty line
                Console.WriteLine();
                // output message of exception
                Console.WriteLine(ex.Message);
                // insert empty line
                Console.WriteLine();
                // check if fatal messages will be logged to the internal log
                if (logger.IsFatalEnabled) {
                    logger.Fatal("Internal Nant Error", ex);
                    Console.WriteLine("Consult the log4net output for more information.");
                } else {
                    Console.WriteLine("For more information regarding the cause of the " +
                        "build failure, enable log4net using the instructions in NAnt.exe.config and " +
                        "run the build again.");
                }
                // insert empty line
                Console.WriteLine();
                // instruct users to report this problem
                Console.WriteLine("Please send bug report to nant-developers@lists.sourceforge.net");
                // signal fatal error
                return 2;
            } finally {
                if (project != null) {
                    project.DetachBuildListeners();
                }
            }
        }

        /// <summary>
        /// Prints the projecthelp to the console.
        /// </summary>
        /// <param name="buildDoc">The build file to show help for.</param>
        /// <remarks>
        /// <paramref name="buildDoc" /> is loaded and transformed with 
        /// <c>ProjectHelp.xslt</c>, which is an embedded resource.
        /// </remarks>
        public static void ShowProjectHelp(XmlDocument buildDoc) {
            // load our transform file out of the embedded resources
            Stream xsltStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjectHelp.xslt");
            if(xsltStream == null) {
                throw new Exception("Missing 'ProjectHelp.xslt' Resource Stream");
            }

            XslTransform transform = new XslTransform();
            XmlTextReader reader = new XmlTextReader(xsltStream, XmlNodeType.Document, null);
            transform.Load(reader);

            StringBuilder sb = new StringBuilder();
            StringWriter writer = new StringWriter(sb, CultureInfo.InvariantCulture);
            XsltArgumentList arguments = new XsltArgumentList();

            // Do transformation
            transform.Transform(buildDoc, arguments, writer);

            // Write projecthelp to console
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Gets the file name for the build file in the specified directory.
        /// </summary>
        /// <param name="directory">The directory to look for a build file.  When in doubt use Environment.CurrentDirectory for directory.</param>
        /// <param name="searchPattern">Look for a build file with this pattern or name.  If null look for a file that matches the default build pattern (*.build).</param>
        /// <param name="findInParent">Whether or not to search the parent directories for a build file.</param>
        /// <returns>The path to the build file or <c>null</c> if no build file could be found.</returns>
        public static string GetBuildFileName(string directory, string searchPattern, bool findInParent) {
            string buildFileName = null;
            if (Path.IsPathRooted(searchPattern)) {
                buildFileName = searchPattern;
            } else {
                if (searchPattern == null) {
                    searchPattern = "*.build";
                }

                // look for a default.build
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                FileInfo[] files = directoryInfo.GetFiles("default.build");
                if (files.Length == 1) {
                    buildFileName = files[0].FullName;
                    return buildFileName;
                }
                
                // now find any file ending in .build
                files = directoryInfo.GetFiles(searchPattern);
                if (files.Length == 1) { // got a single .build
                    buildFileName = files[0].FullName;
                } else if (files.Length > 1) {
                    throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "More than one '{0}' file found in '{1}' and no default.build.  Use -buildfile:<file> to specify or create a default.build file.", searchPattern, directory));
                } else if (files.Length == 0 && findInParent) { // recurse up the tree
                    DirectoryInfo parentDirectoryInfo = directoryInfo.Parent;
                    if (findInParent && parentDirectoryInfo != null) {
                        buildFileName = GetBuildFileName(parentDirectoryInfo.FullName, searchPattern, findInParent);
                    } else {
                        throw new ApplicationException((String.Format(CultureInfo.InvariantCulture, "Could not find a '{0}' file in directory tree.", searchPattern)));
                    }
                } else {
                    throw new ApplicationException((String.Format(CultureInfo.InvariantCulture, "Could not find a '{0}' file in '{1}'", searchPattern, directory)));
                }
            }
            return buildFileName;
        }

        /// <summary>
        /// Dynamically constructs an <see cref="IBuildLogger" /> instance of 
        /// the class specified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// At this point, only looks in the assembly where <see cref="IBuildLogger" /> 
        /// is defined.
        /// </para>
        /// </remarks>
        /// <param name="className">The fully qualified name of the logger that should be instantiated.</param>
        /// <exception cref="ArgumentException"><paramref name="className" /> does not implement <see cref="IBuildLogger" />.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static IBuildLogger CreateLogger(string className) {
            Assembly assembly = Assembly.GetAssembly(typeof(IBuildLogger));

            object buildLogger = Activator.CreateInstance(assembly.GetType(className, true));

            if (!typeof(IBuildLogger).IsAssignableFrom(buildLogger.GetType())) {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "{0} does not implement {1}.",
                    buildLogger.GetType().FullName, typeof(IBuildLogger).FullName));
            }

            return (IBuildLogger) buildLogger;
        }

        /// <summary>
        /// Dynamically constructs an <see cref="IBuildListener" /> instance of 
        /// the class specified.
        /// </summary>
        /// <remarks>
        /// <para>
        /// At this point, only looks in the assembly where <see cref="IBuildListener" /> 
        /// is defined.
        /// </para>
        /// </remarks>
        /// <param name="className">The fully qualified name of the listener that should be instantiated.</param>
        /// <exception cref="ArgumentException"><paramref name="className" /> does not implement <see cref="IBuildListener" />.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static IBuildListener CreateListener(string className) {
            Assembly assembly = Assembly.GetAssembly(typeof(IBuildListener));

            object buildListener = Activator.CreateInstance(assembly.GetType(className, true));

            if (!typeof(IBuildListener).IsAssignableFrom(buildListener.GetType())) {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "{0} does not implement {1}.",
                    buildListener.GetType().FullName, typeof(IBuildListener).FullName));
            }

            return (IBuildListener) buildListener;
        }

        #endregion Public Static Methods

        #region Private Static Methods

        /// <summary>
        /// Add the listeners specified in the command line arguments,
        /// along with the default listener, to the specified project.
        /// </summary>
        /// <param name="cmdlineOptions">The command-line options.</param>
        /// <param name="project">The <see cref="Project" /> to add listeners to.</param>
        private static void AddBuildListeners(CommandLineOptions cmdlineOptions, Project project) {
            BuildListenerCollection listeners = new BuildListenerCollection();
            IBuildLogger buildLogger = null;
            TextWriter outputWriter = Console.Out;

            if (cmdlineOptions.LogFile != null) {
                try {
                    outputWriter = new StreamWriter(new FileStream(cmdlineOptions.LogFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read));
                } catch (Exception ex) {
                    logger.Warn(string.Format(CultureInfo.InvariantCulture, "Error creating output log file {0}.", cmdlineOptions.LogFile.FullName), ex);
                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error creating output log file {0}: {1}", cmdlineOptions.LogFile.FullName, ex.Message));
                }
            }

            if (cmdlineOptions.LoggerType != null) {
                try {
                    buildLogger = ConsoleDriver.CreateLogger(cmdlineOptions.LoggerType);
                } catch (Exception ex) {
                    logger.Warn(string.Format(CultureInfo.InvariantCulture, "Error creating logger of type {0}.", cmdlineOptions.LoggerType), ex);
                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error creating logger of type {0}: {1}", cmdlineOptions.LoggerType, ex.Message));
                }
            }

            // if no logger was specified on the commandline or an error occurred 
            // while creating an instance of the specified logger, use the default 
            // logger.
            if (buildLogger == null) {
                buildLogger = new DefaultLogger();
            }

            // only set OutputWriter if build logger does not derive from 
            // DefaultLogger, or if logfile was specified on command-line. 
            // Setting the OutputWriter of the DefaultLogger to Console.Out 
            // would cause issues with unit tests.
            if (!typeof(DefaultLogger).IsAssignableFrom(buildLogger.GetType()) || outputWriter is StreamWriter) {
                buildLogger.OutputWriter = outputWriter;
            }

            // set threshold of build logger equal to threshold of project
            buildLogger.Threshold = project.Threshold;

            // add build logger to listeners collection
            listeners.Add(buildLogger);

            // add listeners to listener collection
            foreach (string listenerTypeName in cmdlineOptions.Listeners) {
                try {
                    IBuildListener listener = ConsoleDriver.CreateListener(listenerTypeName);
                    listeners.Add(listener);
                } catch (Exception ex) {
                    logger.Warn(string.Format(CultureInfo.InvariantCulture, "Error creating listener of type {0}.", listenerTypeName), ex);
                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error creating listener of type {0}: {1}", listenerTypeName, ex.Message));
                }
            }

            // attach listeners to project
            project.AttachBuildListeners(listeners);
        }
        
        /// <summary>
        /// Spits out generic help info to the console.
        /// </summary>
        private static void ShowHelp(CommandLineParser parser) {
            Console.WriteLine("NAnt comes with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("This is free software, and you are welcome to redistribute it under certain");
            Console.WriteLine("conditions set out by the GNU General Public License.  A copy of the license");
            Console.WriteLine("is available in the distribution package and from the NAnt web site.");
            Console.WriteLine();
            Console.WriteLine(parser.Usage);
            Console.WriteLine("A file ending in .build will be used if no buildfile is specified.");
        }

        #endregion Private Static Methods
    }
}
