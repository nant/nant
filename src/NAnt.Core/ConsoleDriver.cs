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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

namespace SourceForge.NAnt {
    /// <summary>
    /// Main entry point to NAnt that is called by the ConsoleStub.
    /// </summary>
    public class ConsoleDriver {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
        #endregion Private Static Fields

        #region Public Static Methods
                
        /// <summary>
        /// Starts NAnt. This is the Main entry point
        /// </summary>
        /// <param name="args">Command Line args, or whatever you want to pass it. They will treated as Command Line args.</param>
        /// <returns>The exit code.</returns>
        public static int Main(string[] args) {
            CommandLineParser commandLineParser = null;
            StreamWriter logFileStream = null;
            Project project = null;

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

                if (cmdlineOptions.BuildFile != null) {
                    if(project != null ) {
                        Log.WriteLine("Buildfile has already been loaded! Using new value '{0}'; discarding old project file '{1}'", cmdlineOptions.BuildFile.FullName, project.BuildFileURI);
                    }
                    project = new Project(cmdlineOptions.BuildFile.FullName, cmdlineOptions.Verbose);
                }

                if (cmdlineOptions.Indent != 0) {
                    Log.IndentLevel = cmdlineOptions.Indent;
                }

                foreach (string property in cmdlineOptions.Properties) {
                    Match match = Regex.Match(property, @"(\w+.*)=(\w*.*)");
                    if (match.Success) {
                        string name = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        buildOptionProps.AddReadOnly(name, value);
                    }
                }

                if (cmdlineOptions.LoggerType != null) {
                    Log.Listeners.Clear();
                    try {
                        LogListener logListener;
                        if(cmdlineOptions.LogFile != null) {
                            logFileStream = new StreamWriter(new FileStream(cmdlineOptions.LogFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None));
                            logListener = CreateLogger(cmdlineOptions.LoggerType, logFileStream);
                        } else {
                            logListener = CreateLogger(cmdlineOptions.LoggerType);
                        }
                        Log.Listeners.Add(logListener);
                    } catch(Exception e) {
                        logger.Warn(string.Format(CultureInfo.InvariantCulture, "Error creating logger of type: {0}.", cmdlineOptions.LoggerType), e);
                        Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error creating logger of type: {0}", cmdlineOptions.LoggerType));
                    }
                }

                foreach (LogListener listener in Log.Listeners) {
                    if (listener is IBuildEventConsumer) {
                        IBuildEventConsumer i = (IBuildEventConsumer) listener;
                        Project.BuildStarted += new BuildEventHandler(i.BuildStarted);
                        Project.BuildFinished += new BuildEventHandler(i.BuildFinished);
                        Project.TargetStarted += new BuildEventHandler(i.TargetStarted);
                        Project.TargetFinished += new BuildEventHandler(i.TargetFinished);
                        Project.TaskStarted += new BuildEventHandler(i.TaskStarted);
                        Project.TaskFinished += new BuildEventHandler(i.TaskFinished);
                    }
                }

                // Get build file name if the project has not been created.
                // If a build file was not specified on the command line.
                if(project == null) {
                    project = new Project(GetBuildFileName(Environment.CurrentDirectory, null, cmdlineOptions.FindInParent), cmdlineOptions.Verbose);
                }                    

                // copy cmd line targets
                foreach (string target in cmdlineOptions.Targets) {
                    project.BuildTargets.Add(target);
                }

                foreach (System.Collections.DictionaryEntry de in buildOptionProps) {
                    project.Properties.AddReadOnly((string) de.Key, (string) de.Value);
                }

                //add these here and in the project .ctor
                Assembly ass = Assembly.GetExecutingAssembly();

                project.Properties.AddReadOnly(Project.NANT_PROPERTY_FILENAME, ass.Location);
                project.Properties.AddReadOnly(Project.NANT_PROPERTY_VERSION,  ass.GetName().Version.ToString());
                project.Properties.AddReadOnly(Project.NANT_PROPERTY_LOCATION, Path.GetDirectoryName(ass.Location));

                if (cmdlineOptions.ShowProjectHelp) {
                    ConsoleDriver.ShowProjectHelp(project.Doc);
                } else {
                    if (!project.Run()) {
                        return 1;
                    }
                }
                return 0;
            } catch (CommandLineArgumentException e) {
                // Log exception to internal log
                logger.Warn("Invalid command line specified.", e);

                // Write logo banner to conole if parser was created successfully
                if (commandLineParser != null) {
                    Console.WriteLine(commandLineParser.LogoBanner);
                }

                // Write message of exception to console
                Console.WriteLine(e.Message);
                return 1;
            } catch (ApplicationException e) {
                if (e.InnerException != null && e.InnerException.Message != null) {
                    Console.WriteLine(e.Message + "\n\t" + e.InnerException.Message);
                } else {
                    Console.WriteLine(e.Message);
                }

                Console.WriteLine();
                if (logger.IsWarnEnabled) {
                    logger.Warn("Internal Nant Error", e);
                    Console.WriteLine("Consult the log4net output for more information.");
                } else {
                    Console.WriteLine("For more information regarding the cause of the " +
                        "build failure, enable log4net using the instructions in NAnt.exe.config and " +
                        "run the build again.");
                }

                Console.WriteLine();
                Console.WriteLine("Try 'nant -help' for more information");
                return 1;
            } catch (Exception e) {
                // all other exceptions should have been caught
                Console.WriteLine("INTERNAL ERROR");
                Console.WriteLine(e.Message);

                Console.WriteLine();
                if (logger.IsFatalEnabled) {
                    logger.Fatal("Internal Nant Error", e);
                    Console.WriteLine("Consult the log4net output for more information.");
                } else {
                    Console.WriteLine("For more information regarding the cause of the " +
                        "build failure, enable log4net using the instructions in NAnt.exe.config and " +
                        "run the build again.");
                }

                Console.WriteLine();
                Console.WriteLine("Please send bug report to nant-developers@lists.sourceforge.net");
                return 2;
            } finally {
                if(logFileStream != null) {
                    logFileStream.Close();
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

                //Log.WriteLine("Searching for '{0}' file in '{1}'", searchPattern, directory);

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
                } 
                else if (files.Length > 1 ) {              
                    throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "More than one '{0}' file found in '{1}' and no default.build.  Use -buildfile:<file> to specify or create a default.build file.", searchPattern, directory));                    
                }
                else if (files.Length == 0 && findInParent ) { // recurse up the tree
                    DirectoryInfo parentDirectoryInfo = directoryInfo.Parent;
                    if ( findInParent && parentDirectoryInfo != null) {
                        buildFileName = GetBuildFileName(parentDirectoryInfo.FullName, searchPattern, findInParent );
                    } 
                }
                else {
                     throw new ApplicationException((String.Format(CultureInfo.InvariantCulture, "Could not find a '{0}' file in '{1}'", searchPattern, directory)));
                }
            }
            return buildFileName;
        }

        /// <summary>
        /// Dynamically constructs an instance of the class specified.
        /// </summary>
        /// <remarks>
        /// At this point, only looks in the assembly where <see cref="LogListener" /> 
        /// is defined.
        /// </remarks>
        public static LogListener CreateLogger(string className) {
            Assembly assembly = Assembly.GetAssembly(typeof(LogListener));

            return (LogListener) Activator.CreateInstance(assembly.GetType(className, true));
        }

        /// <summary>
        /// Dynamically constructs an instance of the class specified using the 
        /// passed <see cref="TextWriter" />.
        /// </summary>
        /// <remarks>
        /// At this point, only looks in the assembly where <see cref="LogListener" /> 
        /// is defined.
        /// </remarks>
        public static LogListener CreateLogger(string className, TextWriter writer) {
            Assembly assembly = Assembly.GetAssembly(typeof(LogListener));

            object[] args = new object[1];
            args[0] = writer;
            return (LogListener) Activator.CreateInstance(assembly.GetType(className, true), BindingFlags.Public | BindingFlags.Instance, null, args, CultureInfo.InvariantCulture);
        }

        #endregion Public Static Methods

        #region Private Static Methods

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
