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
using System.IO;
using System.Xml;
using System.Text;
using System.Xml.Xsl;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SourceForge.NAnt {
    /// <summary>
    /// ConsoleDriver is used as the main entry point to NAnt. It is called by the ConsoleStub.
    /// </summary>
    public class ConsoleDriver {
        /// <summary>
        /// Starts NAnt. This is the Main entry point
        /// </summary>
        /// <param name="args">Command Line args, or whatever you want to pass it. They will treated as Command Line args.</param>
        /// <returns>The exit code.</returns>
        public static int Main(string[] args) {
            StreamWriter logFileStream = null;

            try {
                Project project = null;

                const string buildfileOption   = "-buildfile:";
                const string setOption         = "-D:";
                const string helpOption        = "-help";
                const string projectHelpOption = "-projecthelp";
                const string verboseOption     = "-verbose";
                const string findOption        = "-find";
                //not documented. Used for testing.
                const string indentOption        = "-indent";
                const string loggerOption      = "-logger:";
                const string logFileOption      = "-logfile:";

                bool showHelp = false;
                bool showProjectHelp = false;
                bool findInParent = false;
                bool verbose = false;
                System.Collections.Specialized.StringCollection targets = new System.Collections.Specialized.StringCollection();
                PropertyDictionary buildOptionProps = new PropertyDictionary();

                bool changeLogger = false;
                string loggerType = "";
                string logFile = null;

                foreach (string arg in args) {
                    if (arg.StartsWith(indentOption)){
                        Log.IndentLevel = Int32.Parse(arg.Substring(indentOption.Length + 1));
                    } else if (arg.StartsWith(buildfileOption)) {
                        if(project != null)
                            Log.WriteLine("Buildfile has already been loaded! Using new value '{0}'; discarding old project file '{1}'",arg.Substring(buildfileOption.Length), project.BuildFileURI);
                        project = new Project(arg.Substring(buildfileOption.Length));
                    } else if (arg.StartsWith(setOption)) {
                        // Properties from command line cannot be overwritten by
                        // the build file.  Once set they are set for the rest of the build.
                        Match match = Regex.Match(arg, @"-D:(\w+.*)=(\w*.*)");
                        if (match.Success) {
                            string name = match.Groups[1].Value;
                            string value = match.Groups[2].Value;
                            buildOptionProps.AddReadOnly(name, value);
                        }
                    } else if (arg.StartsWith(helpOption)) {
                        showHelp = true;
                    } else if (arg.StartsWith(projectHelpOption)) {
                        showProjectHelp = true;
                    } else if (arg.StartsWith(verboseOption)) {
                        verbose = true;
                    } else if (arg.StartsWith(findOption)) {
                        findInParent = true;
                    } else if (arg.StartsWith(loggerOption)) {
                        changeLogger = true;
                        loggerType = arg.Substring(loggerOption.Length);
                    } else if (arg.StartsWith(logFileOption)) {
                        logFile = arg.Substring(logFileOption.Length);
                    } else if (arg.Length > 0) {
                        if (arg.StartsWith("-")) {
                            throw new ApplicationException(String.Format("Unknown argument '{0}'", arg));
                        }
                        // must be a target if not an option
                        targets.Add(arg);
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
                        Console.WriteLine(String.Format("Error creating logger of type: {0}", loggerType));
                        throw new ApplicationException(e.Message);
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
                    Console.WriteLine("  {0} search parent directories for buildfile", findOption.PadRight(optionPadding));
                    Console.WriteLine("  {0} use value for given property", (setOption + "<property>=<value>").PadRight(optionPadding));
                    Console.WriteLine("  {0} displays more information during build process", verboseOption.PadRight(optionPadding));
                    Console.WriteLine("  {0} use given class name as logger", loggerOption.PadRight(optionPadding));
                    Console.WriteLine("  {0} use value as name of log output file", logFileOption.PadRight(optionPadding));
                    Console.WriteLine();
                    Console.WriteLine("A file ending in .build will be used if no buildfile is specified.");

                } else {
                    // Get build file name if the project has not been created.
                    // If a build file was not specified on the command line.
                    if(project == null) {
                        project = new Project(GetBuildFileName(Environment.CurrentDirectory, null, findInParent));
                    }
                    
                    project.Verbose = verbose;
                   
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
                        if(arg.StartsWith(buildfileOption)) continue;
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
                if (e.Message.Length > 0) {
                    Console.WriteLine(e.Message);
                }
                Console.WriteLine("Try 'nant -help' for more information");
                return 1;

            } catch (Exception e) {
                // all other exceptions should have been caught
                Console.WriteLine("INTERNAL ERROR");
                Console.WriteLine(e.ToString());
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
                        throw new ApplicationException((String.Format("Could not find a '{0}' file in '{1}'", searchPattern, directory)));
                    }
                } else { // files.Length > 1
                    throw new ApplicationException(String.Format("More than one '{0}' file found in '{1}'.  Use -buildfile:<file> to specify.", searchPattern, directory));
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

    }
}
