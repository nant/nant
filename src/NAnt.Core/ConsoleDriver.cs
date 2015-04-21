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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

using NAnt.Core.Tasks;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Main entry point to NAnt that is called by the ConsoleStub.
    /// </summary>
    public class ConsoleDriver {
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
            
            // create assembly resolver
            AssemblyResolver assemblyResolver = new AssemblyResolver();
            
            // attach assembly resolver to the current domain
            assemblyResolver.Attach();

            CommandLineOptions cmdlineOptions = new CommandLineOptions();
            try {                
                commandLineParser = new CommandLineParser(typeof(CommandLineOptions), true);
                commandLineParser.Parse(args, cmdlineOptions);

                if (!cmdlineOptions.NoLogo) {
                    Console.WriteLine(commandLineParser.LogoBanner);
                    // insert empty line
                    Console.WriteLine();
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
                    if (project != null) {
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Buildfile has already been loaded! Using new value '{0}'; discarding old project file '{1}'", cmdlineOptions.BuildFile, project.BuildFileUri));
                        // insert empty line
                        Console.WriteLine();
                    }

                    project = new Project(cmdlineOptions.BuildFile, projectThreshold, cmdlineOptions.IndentationLevel);
                }

                // get build file name if the project has not been created.
                // If a build file was not specified on the command line.
                if (project == null) {
                    project = new Project(GetBuildFileName(Environment.CurrentDirectory, null, cmdlineOptions.FindInParent), projectThreshold, cmdlineOptions.IndentationLevel);
                }

                // load extension asseemblies
                LoadExtensionAssemblies(cmdlineOptions.ExtensionAssemblies, project);

                PropertyDictionary buildOptionProps = new PropertyDictionary(project);

                // add build logger and build listeners to project
                ConsoleDriver.AddBuildListeners(cmdlineOptions, project);
    
                // copy cmd line targets
                foreach (string target in cmdlineOptions.Targets) {
                    project.BuildTargets.Add(target);
                }

                // build collection of valid properties that were specified on 
                // the command line.
                foreach (string key in cmdlineOptions.Properties) {
                    buildOptionProps.AddReadOnly(key, 
                        cmdlineOptions.Properties.Get(key));
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

                if (cmdlineOptions.TargetFramework != null) {
                    FrameworkInfo framework = project.Frameworks[cmdlineOptions.TargetFramework];

                    if (framework != null) {
                        try {
                            framework.Validate();
                            project.TargetFramework = framework;
                        } catch (Exception ex) {
                            // write message of exception to console
                            WriteException(ex);
                            // output full stacktrace when NAnt is started in debug mode
                            if (Level.Debug >= projectThreshold) {
                                // insert empty line
                                Console.Error.WriteLine();
                                // output header
                                Console.Error.WriteLine("Stacktrace:");
                                // insert empty line
                                Console.Error.WriteLine();
                                // output full stacktrace
                                Console.Error.WriteLine(ex.ToString());
                            }
                            // signal error
                            return 1;
                        }
                    } else {
                        Console.Error.WriteLine("Invalid framework '{0}' specified.", 
                            cmdlineOptions.TargetFramework);

                        // insert empty line
                        Console.Error.WriteLine();

                        FrameworkInfo[] installedFrameworks = project.GetFrameworks(
                            FrameworkTypes.Installed);

                        if (installedFrameworks.Length == 0) {
                            Console.Error.WriteLine("There are no supported frameworks available on your system.");
                        } else {
                            Console.Error.WriteLine("Possible values include:");
                            // insert empty line
                            Console.Error.WriteLine();

                            foreach (FrameworkInfo fi in installedFrameworks) {
                                Console.Error.WriteLine("{0} ({1})",
                                    fi.Name, fi.Description);
                            }
                        }
                        // signal error
                        return 1;
                    }
                }

                // Enable parallel execution of targets
                project.RunTargetsInParallel = cmdlineOptions.UseJobs;

                if (cmdlineOptions.ShowProjectHelp) {
                    Console.WriteLine();
                    ConsoleDriver.ShowProjectHelp(project.Document);
                } else {
                    if (!project.Run()) {
                        return 1;
                    }
                }
                // signal success
                return 0;
            } catch (CommandLineArgumentException ex) {
                // Write logo banner to console if parser was created successfully
                if (commandLineParser != null) {
                    Console.WriteLine(commandLineParser.LogoBanner);
                    // insert empty line
                    Console.Error.WriteLine();
                }
                // write message of exception to console
                WriteException(ex);
                // output full stacktrace when NAnt is started in debug mode
                if (Level.Debug >= projectThreshold) {
                    // insert empty line
                    Console.Error.WriteLine();
                    // output header
                    Console.Error.WriteLine("Stacktrace:");
                    // insert empty line
                    Console.Error.WriteLine();
                    // output full stacktrace
                    Console.Error.WriteLine(ex.ToString());
                }
                // insert empty line
                Console.WriteLine();
                // instruct users to check the usage instructions
                Console.WriteLine("Try 'nant -help' for more information");
                // signal error
                return 1;
            } catch (ApplicationException ex) {
                // insert empty line
                Console.Error.WriteLine();
                // output build result
                Console.Error.WriteLine("BUILD FAILED");
                // insert empty line
                Console.Error.WriteLine();
                // write message of exception to console
                WriteException(ex);
                // output full stacktrace when NAnt is started in debug mode
                if (Level.Debug >= projectThreshold) {
                    // insert empty line
                    Console.Error.WriteLine();
                    // output header
                    Console.Error.WriteLine("Stacktrace:");
                    // insert empty line
                    Console.Error.WriteLine();
                    // output full stacktrace
                    Console.Error.WriteLine(ex.ToString());
                } else {
                    // insert empty line
                    Console.WriteLine(string.Empty);
                    // output help text
                    Console.WriteLine("For more information regarding the cause of the " +
                        "build failure, run the build again in debug mode.");
                }
                // insert empty line
                Console.WriteLine();
                // instruct users to check the usage instructions
                Console.WriteLine("Try 'nant -help' for more information");
                // signal error
                return 1;
            } catch (Exception ex) {
                // insert empty line
                Console.Error.WriteLine();
                // all other exceptions should have been caught
                Console.Error.WriteLine("INTERNAL ERROR");
                // insert empty line
                Console.Error.WriteLine();
                // write message of exception to console
                WriteException(ex);
                // output full stacktrace when NAnt is started in verbose mode
                if (Level.Verbose >= projectThreshold) {
                    // insert empty line
                    Console.Error.WriteLine();
                    // output header
                    Console.Error.WriteLine("Stacktrace:");
                    // insert empty line
                    Console.Error.WriteLine();
                    // output full stacktrace
                    Console.Error.WriteLine(ex.ToString());
                } else {
                    // insert xempty line
                    Console.WriteLine();
                    // output help text
                    Console.WriteLine("For more information regarding the cause of the " +
                        "build failure, run the build again in verbose mode.");
                }
                // insert empty line
                Console.WriteLine();
                // instruct users to report this problem
                Console.WriteLine("Please send a bug report (including the version of NAnt you're using) to nant-developers@lists.sourceforge.net");
                // signal fatal error
                return 2;
            } finally {
                if (project != null) {
                    project.DetachBuildListeners();
                }
                // detach assembly resolver from the current domain
                assemblyResolver.Detach();
                if (cmdlineOptions.Pause)
                    Console.ReadKey();
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
            Stream xsltStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "NAnt.Core.Resources.ProjectHelp.xslt");
            if (xsltStream == null) {
                throw new Exception("Missing 'ProjectHelp.xslt' Resource Stream");
            }

            XmlTextReader reader = new XmlTextReader(xsltStream, XmlNodeType.Document,null);

            //first load in an XmlDocument so we can set the appropriate nant-namespace
            XmlDocument xsltDoc = new XmlDocument();
            xsltDoc.Load(reader);
            xsltDoc.DocumentElement.SetAttribute("xmlns:nant",buildDoc.DocumentElement.NamespaceURI);

            XslCompiledTransform transform = new XslCompiledTransform();
            XsltSettings settings = new XsltSettings(false, true);
            transform.Load(xsltDoc, settings, new XmlUrlResolver());

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
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1001")
                        + "  Use -buildfile:<file> to specify the build file to execute or "
                        + " create a default.build file.", searchPattern, directory));
                } else if (files.Length == 0 && findInParent) { // recurse up the tree
                    DirectoryInfo parentDirectoryInfo = directoryInfo.Parent;
                    if (findInParent && parentDirectoryInfo != null) {
                        buildFileName = GetBuildFileName(parentDirectoryInfo.FullName, searchPattern, findInParent);
                    } else {
                        throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, 
                            ResourceUtils.GetString("NA1007"), searchPattern));
                    }
                } else {
                    throw new ApplicationException(string.Format(CultureInfo.InvariantCulture, 
                        ResourceUtils.GetString("NA1004"), searchPattern, directory ));
                }
            }
            return buildFileName;
        }

        /// <summary>
        /// Loads the extension assemblies in the current <see cref="AppDomain" />
        /// and scans them for extensions.
        /// </summary>
        /// <param name="extensionAssemblies">The extension assemblies to load.</param>
        /// <param name="project">The <see cref="Project" /> which will be used to output messages to the build log.</param>
        private static void LoadExtensionAssemblies(StringCollection extensionAssemblies, Project project) {
            LoadTasksTask loadTasks = new LoadTasksTask();
            loadTasks.Project = project;
            loadTasks.NamespaceManager = project.NamespaceManager;
            loadTasks.Parent = project;
            loadTasks.Threshold = (project.Threshold == Level.Debug) ? 
                Level.Debug : Level.Warning;

            foreach (string extensionAssembly in extensionAssemblies) {
                loadTasks.TaskFileSet.Includes.Add(extensionAssembly);
            }

            loadTasks.Execute();
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
        /// <param name="typeName">The fully qualified name of the logger that should be instantiated.</param>
        /// <exception cref="TypeLoadException">Type <paramref name="typeName" /> could not be loaded.</exception>
        /// <exception cref="ArgumentException"><paramref name="typeName" /> does not implement <see cref="IBuildLogger" />.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static IBuildLogger CreateLogger(string typeName) {
            Type loggerType = ReflectionUtils.GetTypeFromString(typeName, false);
            if (loggerType == null) {
                throw new TypeLoadException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1006"), typeName));
            }

            object logger = Activator.CreateInstance(loggerType);

            IBuildLogger buildLogger = logger as IBuildLogger;

            if (buildLogger != null)
                return buildLogger;

            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "{0} does not implement {1}.",
                    logger.GetType().FullName, typeof(IBuildLogger).FullName));
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
        /// <param name="typeName">The fully qualified name of the listener that should be instantiated.</param>
        /// <exception cref="TypeLoadException">Type <paramref name="typeName" /> could not be loaded.</exception>
        /// <exception cref="ArgumentException"><paramref name="typeName" /> does not implement <see cref="IBuildListener" />.</exception>
        [ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.NoFlags)]
        public static IBuildListener CreateListener(string typeName) {
            Type listenerType = ReflectionUtils.GetTypeFromString(typeName, false);
            if (listenerType == null) {
                throw new TypeLoadException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1006"), typeName));
            }

            object listener = Activator.CreateInstance(listenerType);

            IBuildListener buildListener = listener as IBuildListener;
            if (buildListener != null) 
                return buildListener;
            
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "{0} does not implement {1}.",
                    listener.GetType().FullName, typeof(IBuildListener).FullName));
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
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1005"), cmdlineOptions.LogFile.FullName),
                        Location.UnknownLocation, ex);
                }
            }

            if (cmdlineOptions.LoggerType != null) {
                try {
                    buildLogger = ConsoleDriver.CreateLogger(cmdlineOptions.LoggerType);
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1003"), cmdlineOptions.LoggerType),
                        Location.UnknownLocation, ex);
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
            if (!(buildLogger is DefaultLogger) || cmdlineOptions.LogFile != null) {
                buildLogger.OutputWriter = outputWriter;
            }

            // set threshold of build logger equal to threshold of project
            buildLogger.Threshold = project.Threshold;

            // set emacs mode
            buildLogger.EmacsMode = cmdlineOptions.EmacsMode;

            // add build logger to listeners collection
            listeners.Add(buildLogger);

            // add listeners to listener collection
            foreach (string listenerTypeName in cmdlineOptions.Listeners) {
                try {
                    IBuildListener listener = ConsoleDriver.CreateListener(listenerTypeName);
                    listeners.Add(listener);
                } catch (Exception ex) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1002"), listenerTypeName),
                        Location.UnknownLocation, ex);
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

        /// <summary>
        /// Write the message of the specified <see cref="Exception" /> and
        /// the inner exceptions to <see cref="Console.Error" />.
        /// </summary>
        /// <param name="cause">The <see cref="Exception" /> to write to <see cref="Console.Error" />.</param>
        private static void WriteException(Exception cause) {
            int indentLevel = 0;
            while (cause != null && !String.IsNullOrEmpty(cause.Message)) {
                if (!String.IsNullOrEmpty(cause.Message)) {
                    if (indentLevel > 0) {
                        // insert empty line
                        Console.Error.WriteLine();
                    }

                    // indent exception message with extra spaces (for each
                    // nesting level)
                    Console.Error.WriteLine(new string(' ', indentLevel * INDENTATION_SIZE) 
                        + cause.Message);
                    // increase indentation level
                    indentLevel++;
                }
                // move on to next inner exception
                cause = cause.InnerException;
            }
        }

        #endregion Private Static Methods

        #region Private Static Fields

        private const int INDENTATION_SIZE = 4;

        #endregion Private Static Fields
    }
}
