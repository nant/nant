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

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;

namespace SourceForge.NAnt {
    /// <summary>
    /// ConsoleDriver is used as the main entry point to NAnt. It is called by the ConsoleStub.
    /// </summary>
    public class ConsoleDriver {
        private static readonly log4net.ILog debuglogger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
        const string buildfileOption    = "-buildfile:";
        const string buildfileOption2   = "-file:";
        const string buildfileOption3   = "-f:";
        const string setOption          = "-D:";
        const string helpOption         = "-help";
        const string helpOption2        = "-?";
        const string projectHelpOption  = "-projecthelp";
        const string verboseOption      = "-verbose";
        const string verboseOption2     = "-v";
        const string findOption         = "-find";
        const string loggerOption       = "-logger:";
        const string logFileOption      = "-logfile:";
        const string logFileOption2     = "-l:";
        
        //not documented. Used for testing.
        const string indentOption       = "-indent";
                
        private enum CommandLineOption {
            OPTION_BUILDFILE,
            OPTION_SET,
            OPTION_HELP,
            OPTION_PROJECTHELP,
            OPTION_VERBOSE,
            OPTION_FIND,
            OPTION_INDENT,
            OPTION_LOGGER,
            OPTION_LOGFILE,
            OPTION_TARGET
        }
        
        /// <summary>
        /// Given one parsed command-line argument, identify the supported flag being provided,
        /// or throw an ApplicationException if invalid.
        /// </summary>
        /// <param name="arg">A single command-line argument.</param>
        /// <returns>The CommandLineOption enum value indicating the flag represented by arg.</returns>
        private static CommandLineOption IdentifyArgument(string arg) {
            if (    arg.StartsWith(buildfileOption) || 
                arg.StartsWith(buildfileOption2) ||
                arg.StartsWith(buildfileOption3)) {
                return CommandLineOption.OPTION_BUILDFILE;
            } else if (arg.StartsWith(setOption)) {
                return CommandLineOption.OPTION_SET;
            } else if (arg.Equals(helpOption) || arg.Equals(helpOption2)) {
                return CommandLineOption.OPTION_HELP;
            } else if (arg.Equals(projectHelpOption))   {
                return CommandLineOption.OPTION_PROJECTHELP;
            } else if (arg.Equals(verboseOption) || arg.Equals(verboseOption2)) {
                return CommandLineOption.OPTION_VERBOSE;
            } else if (arg.Equals(findOption)) {
                return CommandLineOption.OPTION_FIND;
            } else if (arg.StartsWith(indentOption)) {
                return CommandLineOption.OPTION_INDENT;
            } else if (arg.StartsWith(loggerOption)) {
                return CommandLineOption.OPTION_LOGGER;
            } else if (arg.StartsWith(logFileOption) || arg.StartsWith(logFileOption2)) {
                return CommandLineOption.OPTION_LOGFILE;
            }
                //I kept this logic about arg.Length > 0, but isn't it redundant?  
                //"".StartsWith("-") == false
            else if (arg.Length > 0 && arg.StartsWith("-")) {
                throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "Unknown argument '{0}'", arg));
            } else {
                // must be a target if not an option
                return CommandLineOption.OPTION_TARGET;
            }


        }

        /// <summary>
        /// Starts NAnt. This is the Main entry point
        /// </summary>
        /// <param name="args">Command Line args, or whatever you want to pass it. They will treated as Command Line args.</param>
        /// <returns>The exit code.</returns>
        public static int Main(string[] args) {
            StreamWriter logFileStream = null;
            bool verbose = false;

            try {
                Project project = null;
                bool showHelp = false;
                bool showProjectHelp = false;
                bool findInParent = false;
                System.Collections.Specialized.StringCollection targets = new System.Collections.Specialized.StringCollection();
                PropertyDictionary buildOptionProps = new PropertyDictionary();

                bool changeLogger = false;
                string loggerType = "";
                string logFile = null;

                foreach (string arg in args) {
                    CommandLineOption currentOption = IdentifyArgument(arg);
                    switch (currentOption) {
                        
                        case CommandLineOption.OPTION_BUILDFILE:
                            if(project != null ) {
                                Log.WriteLine("Buildfile has already been loaded! Using new value '{0}'; discarding old project file '{1}'",arg.Substring(arg.IndexOf(":") + 1), project.BuildFileURI);                         
                            }
                            project = new Project(arg.Substring(arg.IndexOf(":") + 1), verbose );
                            break;
                        case CommandLineOption.OPTION_FIND:
                            findInParent = true;
                            break;
                        case CommandLineOption.OPTION_HELP:
                            showHelp = true;
                            break;
                        case CommandLineOption.OPTION_INDENT:
                            Log.IndentLevel = Int32.Parse(arg.Substring(indentOption.Length + 1));
                            break;
                        case CommandLineOption.OPTION_LOGFILE:
                            logFile = arg.Substring(arg.IndexOf(":") + 1);
                            break;
                        case CommandLineOption.OPTION_LOGGER:
                            changeLogger = true;
                            loggerType = arg.Substring(arg.IndexOf(":") + 1);
                            break;
                        case CommandLineOption.OPTION_PROJECTHELP:
                            showProjectHelp = true;
                            break;
                        case CommandLineOption.OPTION_SET:
                            // Properties from command line cannot be overwritten by
                            // the build file.  Once set they are set for the rest of the build.
                            Match match = Regex.Match(arg, @"-D:(\w+.*)=(\w*.*)");
                            if (match.Success) {
                                string name = match.Groups[1].Value;
                                string value = match.Groups[2].Value;
                                buildOptionProps.AddReadOnly(name, value);
                            }
                            break;
                        case CommandLineOption.OPTION_TARGET:
                            targets.Add(arg);
                            break;
                        case CommandLineOption.OPTION_VERBOSE:
                            verbose = true;
                            break;
                    }
                }

                if (changeLogger) {
                    LogListener logger;
                    try {
                        if(logFile != null) {
                            logFileStream = new StreamWriter(new FileStream(logFile, FileMode.Create, FileAccess.Write, FileShare.None));
                            logger = CreateLogger(loggerType, logFileStream);
                        } else {
                            logger = CreateLogger(loggerType);
                        }
                    } catch(Exception e) {
                        Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Error creating logger of type: {0}", loggerType));
                        throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "Error creating logger of type: {0}",loggerType), e);
                    }
                    Log.Listeners.Clear();
                    Log.Listeners.Add(logger);
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

                if (showHelp) {
                    ShowCommandLineHelp();

                } else {
                    // Get build file name if the project has not been created.
                    // If a build file was not specified on the command line.
                    if(project == null) {
                        project = new Project(GetBuildFileName(Environment.CurrentDirectory, null, findInParent), verbose);
                    }                    
                                       
                    // copy cmd line targets
                    foreach( string target in targets ){
                        project.BuildTargets.Add( target );
                    }
                    
                    foreach(System.Collections.DictionaryEntry de in buildOptionProps) {
                        project.Properties.AddReadOnly((string)de.Key, (string)de.Value);
                    }

                    //add these here and in the project .ctor
                    Assembly ass = Assembly.GetExecutingAssembly();

                    project.Properties.AddReadOnly(Project.NANT_PROPERTY_FILENAME, ass.Location);
                    project.Properties.AddReadOnly(Project.NANT_PROPERTY_VERSION,  ass.GetName().Version.ToString());
                    project.Properties.AddReadOnly(Project.NANT_PROPERTY_LOCATION, Path.GetDirectoryName(ass.Location));

                    //constructs a copy of ths args and removes the buildfile arg.
                    StringBuilder argsString = new StringBuilder(args.Length * 12, 600);
                    foreach (string arg in args) {
                        if (CommandLineOption.OPTION_BUILDFILE == IdentifyArgument(arg)) {
                            continue;
                        }
                        argsString.Append(arg);
                        argsString.Append(" ");
                    }
                    project.Properties.AddReadOnly("nant.cl-opts", argsString.ToString());

                    if (showProjectHelp) {
                        ShowProjectHelp(project.Doc);
                    } else {
                        if (!project.Run()) {
                            throw new ApplicationException("");
                        }
                    }
                }
                return 0;
            } catch (ApplicationException e) {
                debuglogger.Debug("Internal Nant Error", e);
               
                if (e.InnerException != null && e.InnerException.Message != null) {
                    Console.WriteLine(e.Message + "\n\t" + e.InnerException.Message);
                } else {
                    Console.WriteLine(e.Message);
                }
                Console.WriteLine("More information was logged via log4net at level debug");
                Console.WriteLine();
                Console.WriteLine("Try 'nant -help' for more information");
                return 1;
            } catch (Exception e) {
                debuglogger.Debug("Internal Nant Error", e);

                // all other exceptions should have been caught
                Console.WriteLine("INTERNAL ERROR");
                Console.WriteLine(e.Message);
                Console.WriteLine();
                Console.WriteLine("More information was logged via log4net at level debug");
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
        /// Prints help to Console. The <code>buildDoc</code> is loaded and transformed with 'ProjectHelp.xslt'
        /// </summary>
        /// <param name="buildDoc">The build file to show help for.</param>
        public static void ShowProjectHelp(XmlDocument buildDoc) {

            //string resourceDirectory =
            //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\NAnt";

            // load our transform file out of the embedded resources
            Stream xsltStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProjectHelp.xslt");

            if(xsltStream == null) {
                throw new ApplicationException("Missing 'ProjectHelp.xslt' Resource Stream");
            }

            XslTransform transform = new XslTransform();
            XmlTextReader reader = new XmlTextReader( xsltStream, XmlNodeType.Document, null );
            transform.Load(reader);

            StringBuilder sb = new StringBuilder();
            StringWriter writer = new StringWriter(sb);
            XsltArgumentList arguments = new XsltArgumentList();

            // Do transform
            transform.Transform(buildDoc, arguments, writer );
            string outstr = sb.ToString();
            System.Console.WriteLine( sb.ToString() );
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

                // find first file ending in .build
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                FileInfo[] files = directoryInfo.GetFiles(searchPattern);
                if (files.Length == 1) {
                    buildFileName = Path.Combine(directory, files[0].Name);
                } else if (files.Length == 0) {
                    DirectoryInfo parentDirectoryInfo = directoryInfo.Parent;
                    if (findInParent && parentDirectoryInfo != null) {
                        buildFileName = GetBuildFileName(parentDirectoryInfo.FullName, searchPattern, findInParent);
                    } else {
                        throw new ApplicationException((String.Format(CultureInfo.InvariantCulture, "Could not find a '{0}' file in '{1}'", searchPattern, directory)));
                    }
                } else { // files.Length > 1
                    throw new ApplicationException(String.Format(CultureInfo.InvariantCulture, "More than one '{0}' file found in '{1}'.  Use -buildfile:<file> to specify.", searchPattern, directory));
                }
            }
            return buildFileName;
        }

        ///<summary>dynamically constructs an instance of the class specified. At this point, only looks in the assembly where LogListener is defined</summary>
        public static LogListener CreateLogger(string className) {
            Assembly assembly = Assembly.GetAssembly(typeof(LogListener));

            return (LogListener) Activator.CreateInstance(assembly.GetType(className, true));
        }

        ///<summary>dynamically constructs an instance of the class specified using the passed TextWriter. At this point, only looks in the assembly where LogListener is defined</summary>
        public static LogListener CreateLogger(string className, TextWriter writer) {
            Assembly assembly = Assembly.GetAssembly(typeof(LogListener));

            object[] args = new object[1];
            args[0] = writer;
            return (LogListener) Activator.CreateInstance(assembly.GetType(className, true), args);
        }

        /// <summary>
        /// Spits out generic help info to the console.
        /// </summary>
        private static void ShowCommandLineHelp() {
            // Get version information directly from assembly.  This takes more
            // work but keeps the version numbers being displayed in sync with
            // what the assembly is marked with.
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            const int optionPadding = 23;

            Console.WriteLine("NAnt version {0} Copyright (C) 2001-{1} Gerry Shaw",
                info.FileMajorPart + "." + info.FileMinorPart + "." + info.FileBuildPart,
                DateTime.Now.Year);
            Console.WriteLine(Assembly.GetExecutingAssembly().CodeBase);
            Console.WriteLine("http://nant.sf.net");
            Console.WriteLine();
            Console.WriteLine("NAnt comes with ABSOLUTELY NO WARRANTY.");
            Console.WriteLine("This is free software, and you are welcome to redistribute it under certain");
            Console.WriteLine("conditions set out by the GNU General Public License.  A copy of the license");
            Console.WriteLine("is available in the distribution package and from the NAnt web site.");
            Console.WriteLine();
            Console.WriteLine("usage: nant [options] [target [target2 [target3] ... ]]");
            Console.WriteLine();
            Console.WriteLine("options:");
            Console.WriteLine("  {0} print this message", helpOption.PadRight(optionPadding));
            Console.WriteLine("  {0} print project help information", projectHelpOption.PadRight(optionPadding));
            Console.WriteLine("  {0} use given buildfile", (buildfileOption + "<file>").PadRight(optionPadding));
            Console.WriteLine("     {0} ''        ", (buildfileOption2 + "<file>").PadRight(optionPadding));
            Console.WriteLine("     {0} ''        ", (buildfileOption3 + "<file>").PadRight(optionPadding));
            Console.WriteLine("  {0} search parent directories for buildfile", findOption.PadRight(optionPadding));
            Console.WriteLine("  {0} use value for given property", (setOption + "<property>=<value>").PadRight(optionPadding));
            Console.WriteLine("  {0} displays more information during build process", (verboseOption + ", " + verboseOption2).PadRight(optionPadding));
            Console.WriteLine("  {0} use given class name as logger", loggerOption.PadRight(optionPadding));
            Console.WriteLine("  {0} use value as name of log output file", (logFileOption + ", " + logFileOption2).PadRight(optionPadding));
            Console.WriteLine();
            Console.WriteLine("A file ending in .build will be used if no buildfile is specified.");
        }
    }
}
