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
// Ian MacLean (ian_maclean@another.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Collections;
using System.Collections.Specialized;

namespace SourceForge.NAnt {

    /// <summary>Central representation of an NAnt project.</summary>
    /// <example>
    ///   <para>The <c>Run</c> method will initialize the project with the build file specified in the <c>BuildFile</c> property and execute the default target.</para>
    /// <code>
    /// <![CDATA[
    /// Project p = new Project("foo.build");
    /// p.Run();
    /// ]]>
    /// </code>
    ///   <para>If no target is given the default target will be executed if specified in the project.</para>
    /// <code>
    /// <![CDATA[
    /// Project p = new Project("foo.build");
    /// p.Execute("build"); /
    /// ]]>
    /// </code>
    /// </example>
    public class Project {

        //xml element and attribute names that are not defined in metadata
        protected const string ROOT_XML = "project";
        protected const string PROJECT_NAME_ATTRIBUTE = "name";
        protected const string PROJECT_DEFAULT_ATTRIBUTE = "default";
        protected const string PROJECT_BASEDIR_ATTRIBUTE = "basedir";
        protected const string TARGET_XML = "target";
        protected const string TARGET_DEPENDS_ATTRIBUTE = "depends";

        public const string NANT_PROPERTY_FILENAME = "nant.filename";
        public const string NANT_PROPERTY_VERSION = "nant.version";
        public const string NANT_PROPERTY_LOCATION = "nant.location";
        public const string NANT_PROPERTY_PROJECT_NAME = "nant.project.name";
        public const string NANT_PROPERTY_PROJECT_BUILDFILE = "nant.project.buildfile";
        public const string NANT_PROPERTY_PROJECT_BASEDIR = "nant.project.basedir";
        public const string NANT_PROPERTY_PROJECT_DEFAULT = "nant.project.default";

        public const string NANT_PROPERTY_ONSUCCESS = "nant.onsuccess";
        public const string NANT_PROPERTY_ONFAILURE = "nant.failure";


        string _projectName = "";
        string _defaultTargetName = null;
        bool   _verbose = false;

        StringCollection   _buildTargets = new StringCollection();
        TargetCollection   _targets = new TargetCollection();
        LocationMap        _locationMap = new LocationMap();
        PropertyDictionary _properties = new PropertyDictionary();
        XmlDocument    _doc = null; // set in ctorHelper

        public static event BuildEventHandler BuildStarted;
        public static event BuildEventHandler BuildFinished;
        public static event BuildEventHandler TargetStarted;
        public static event BuildEventHandler TargetFinished;
        public static event BuildEventHandler TaskStarted;
        public static event BuildEventHandler TaskFinished;

        public static void OnBuildStarted(object o, BuildEventArgs e) {
            if (BuildStarted != null) {
                BuildStarted(o, e);
            }
        }

        public static void OnBuildFinished(object o, BuildEventArgs e) {
            if (BuildFinished != null) {
                BuildFinished(o, e);
            }
        }

        public static void OnTargetStarted(object o, BuildEventArgs e) {
            if (TargetStarted != null) {
                TargetStarted(o, e);
            }
        }

        public static void OnTargetFinished(object o, BuildEventArgs e) {
            if (TargetFinished != null) {
                TargetFinished(o, e);
            }
        }

        public static void OnTaskStarted(object o, BuildEventArgs e) {
            if (TaskStarted != null) {
                TaskStarted(o, e);
            }
        }

        public static void OnTaskFinished(object o, BuildEventArgs e) {
            if (TaskFinished != null) {
                TaskFinished(o, e);
            }
        }

        /// <summary>
        /// Constructs a new Project with the given document.
        /// </summary>
        /// <param name="doc">Any valid build format will do.</param>
        public Project(XmlDocument  doc) {
            ctorHelper(doc);
        }

        /// <summary>
        /// Constructs a new Project with the given source.
        /// </summary>
        /// <param name="source">
        /// <para> The Source should be the full path to the build file.</para>
        /// <para> This can be of any form that XmlDocument.Load(string url) accepts.</para>
        /// </param>
        /// <remarks><para>If the source is a uri of form 'file:///path' then use the path part.</para></remarks>
        public Project(string source) {
            string path = source;
            //if the source is not a valid uri, pass it thru.
            //if the source is a file uri, pass the localpath of it thru.
            try {
                Uri testURI = new Uri(source);
                if(testURI.IsFile) {
                    path = testURI.LocalPath;
                }
            }
            catch(Exception e) {
                //do nothing.
                e.ToString();
            }
            finally{
                if(path == null)
                    path=source;
            }

            ctorHelper(LoadBuildFile(path));
        }

        /// <summary>
        /// Inits stuff:
        ///     <para>TaskFactory: Calls Initialize and AddProject </para>
        ///     <para>Log.IndentSize set to 12</para>
        ///     <para>Project properties are initialized ("nant.* stuff set")</para>
        ///     <list type="nant.items">
        ///         <listheader>NAnt Props:</listheader>
        ///         <item>nant.filename</item>
        ///         <item>nant.version</item>
        ///         <item>nant.location</item>
        ///         <item>nant.project.name</item>
        ///         <item>nant.project.buildfile (if doc has baseuri)</item>
        ///         <item>nant.project.basedir</item>
        ///         <item>nant.project.default = defaultTarget</item>
        ///         <item>nant.tasks.[name] = true</item>
        ///         <item>nant.tasks.[name].location = AssemblyFileName</item>
        ///     </list>
        /// </summary>
        /// <param name="doc">The Project Document.</param>
        protected virtual void ctorHelper(XmlDocument doc) {
            TaskFactory.AddProject(this);
            Log.IndentSize = 12;
            _doc = doc;

            string newBaseDir = null;

            //check to make sure that the root element in named correctly
            if(!doc.DocumentElement.Name.Equals(ROOT_XML))
                throw new ApplicationException("Root Element must be named " + ROOT_XML + " in " + doc.BaseURI);

            // get project attributes
            if(doc.DocumentElement.HasAttribute(PROJECT_NAME_ATTRIBUTE))    _projectName            = doc.DocumentElement.GetAttribute(PROJECT_NAME_ATTRIBUTE);
            if(doc.DocumentElement.HasAttribute(PROJECT_BASEDIR_ATTRIBUTE)) newBaseDir         = doc.DocumentElement.GetAttribute(PROJECT_BASEDIR_ATTRIBUTE);
            if(doc.DocumentElement.HasAttribute(PROJECT_DEFAULT_ATTRIBUTE)) _defaultTargetName  = doc.DocumentElement.GetAttribute(PROJECT_DEFAULT_ATTRIBUTE);

            // give the project a meaningful base directory
            if (newBaseDir == null) {
                if (BuildFileLocalName != null) {
                    newBaseDir = Path.GetDirectoryName(BuildFileLocalName);
                }
                else {
                    newBaseDir = Environment.CurrentDirectory;
                }
            }

            newBaseDir = Path.GetFullPath(newBaseDir);
            //BaseDirectory must be rooted.
            BaseDirectory = newBaseDir;

            //set here and in nant:Main
            Assembly ass = Assembly.GetExecutingAssembly();

            Properties.AddReadOnly(NANT_PROPERTY_FILENAME, ass.Location);
            Properties.AddReadOnly(NANT_PROPERTY_VERSION,  ass.GetName().Version.ToString());
            Properties.AddReadOnly(NANT_PROPERTY_LOCATION, Path.GetDirectoryName(ass.Location));

            Properties.AddReadOnly(NANT_PROPERTY_PROJECT_NAME, ProjectName);
            if(BuildFileURI != null) {
                Properties.AddReadOnly(NANT_PROPERTY_PROJECT_BUILDFILE, BuildFileURI.ToString());
            }

            Properties.AddReadOnly(NANT_PROPERTY_PROJECT_DEFAULT,   DefaultTargetName);
        }

        /// <summary>This method is only meant to be used by the <see cref="Project"/> class and <see cref="SourceForge.NAnt.Tasks.IncludeTask"/>.</summary>
        internal void InitializeProjectDocument(XmlDocument doc) {
            // load line and column number information into position map
            LocationMap.Add(doc);

            //TODO: Add support after lazy target init is done.
            //ArrayList globalTasks = new ArrayList();

            // initialize targets and global tasks
            foreach (XmlNode childNode in doc.DocumentElement.ChildNodes) {
                //add targets to list
                if(childNode.Name.Equals(TARGET_XML) && childNode.NamespaceURI.Equals(doc.DocumentElement.NamespaceURI)) {
                    Target target = new Target();
                    target.Project = this;
                    target.Parent = this;
                    target.Initialize(childNode);
                    Targets.Add(target);
                }
                //global tasks
                else if(!childNode.Name.StartsWith("#") && childNode.NamespaceURI.Equals(doc.DocumentElement.NamespaceURI)) {
                    Task task = CreateTask(childNode);

                    //see comments below.
                    //globalTasks.Add(task);
                    task.Parent = this;
                    task.Execute();

                }
            }

            /* Do not do this yet. We need to do lazy init of the Tasks first.
            foreach(Task task in globalTasks) {
                task.Execute();
            }
            */
        }

        /// <summary>
        /// Creates a new XmlDocument based on the project definition.
        /// </summary>
        /// <param name="source">The source of the document. <para>Any form that is valid for XmlDocument.Load(string url) can be used here.</para></param>
        /// <returns>The project document.</returns>
        private XmlDocument LoadBuildFile(string source) {
            XmlDocument doc = new XmlDocument();
            //Uri srcURI = new Uri(source);
            try {
                doc.Load(source);
                // TODO: validate against xsd schema
            } catch (XmlException e) {
                string message = "Error loading buildfile";
                Location location = new Location(source, e.LineNumber, e.LinePosition);
                throw new BuildException(message, location, e);
            } catch (Exception e) {
                string message = "Error loading buildfile";
                Location location = new Location(source);
                throw new BuildException(message, location, e);
            }
            return doc;
        }
        /// <summary>The name of the project.</summary>
        public string ProjectName {
            get { return _projectName; }
        }

        /// <summary>
        /// The Base Directory used for relative references.
        /// </summary>
        /// <remarks>
        ///     <para>The directory must be rooted. (must start with drive letter, unc, etc.)</para>
        ///     <para>The BaseDirectory sets and gets the special property named 'nant.project.basedir'.</para>
        /// </remarks>
        public string BaseDirectory {
            get {
                string basedir = Properties[NANT_PROPERTY_PROJECT_BASEDIR];

                if (basedir == null) return null;

                if (!Path.IsPathRooted(basedir))
                    throw new BuildException("BaseDirectory must be rooted! " + basedir);

                return basedir; }
            set {
                if (!Path.IsPathRooted(value))
                    throw new BuildException("BaseDirectory must be rooted! " + value);

                Properties[NANT_PROPERTY_PROJECT_BASEDIR] = value;
            }
        }

        /// <summary>
        /// The URI form of the current Document
        /// </summary>
        public Uri BuildFileURI {
            get {
                //TODO: Need to remove this.
                if(Doc == null || Doc.BaseURI == "") {
                    return null;//new Uri("http://localhost");
                }
                else {
                    return new Uri(Doc.BaseURI);
                }
            }
        }

        /// <summary>
        /// If the build document is not file backed then null will be returned.
        /// </summary>
        public string BuildFileLocalName {
            get {
                if (BuildFileURI != null && BuildFileURI.IsFile) {
                    return BuildFileURI.LocalPath;
                }
                else {
                    return null;
                }
            }
        }

        /// <summary>Returns the active build file</summary>
        public virtual XmlDocument Doc {
            get { return _doc; }
        }

        /// <remarks>
        ///   <para>Used only if BuildTargets collection is empty.</para>
        /// </remarks>
        public string DefaultTargetName {
            get { return _defaultTargetName; }
        }

        /// <summary>When true tasks should output more build log messages.</summary>
        public bool Verbose {
            get { return _verbose; }
            set { _verbose = value; }
        }

        /// <summary>The list of targets to built.</summary>
        /// <remarks>
        ///   <para>Targets are built in the order they appear in the collection.  If the collection is empty the default target will be built.</para>
        /// </remarks>
        public StringCollection BuildTargets {
            get { return _buildTargets; }
        }
        /// <summary> The NAnt Properties.</summary>
        ///
        /// <remarks>
        ///   <para>This is the collection of Properties that are defined by the system and property task statements.</para>
        ///   <para>These properties can be used in expansion.</para>
        /// </remarks>
        public PropertyDictionary Properties {
            get { return _properties; }
        }

        internal LocationMap LocationMap {
            get { return _locationMap; }
        }

        /// <summary>
        /// The targets defined in the this project. (RO Collection)
        /// </summary>
        public TargetCollection Targets {
            get { return _targets; }
        }

        /// <summary>Executes the default target.</summary>
        /// <remarks>
        ///     <para>No top level error handling is done. Any BuildExceptions will make it out of this method.</para>
        /// </remarks>
        public virtual void Execute() {

            //will initialize the list of Targets, and execute any global tasks.
            InitializeProjectDocument(Doc);

            if (BuildTargets.Count == 0 && DefaultTargetName != null) {
                BuildTargets.Add(DefaultTargetName);
            }

            if (BuildTargets.Count == 0) {
                //TODO: Display Project Help...
                //throw new BuildException("No Target Specified");
            }
            else {
                foreach(string targetName in BuildTargets) {
                    Execute(targetName);
                }
            }
        }

        /// <summary>Executes a specific target, and only that target.</summary>
        /// <param name="targetName">target name to execute.</param>
        /// <remarks>
        ///   <para>Only the target is executed. No global tasks are executed.</para>
        /// </remarks>
        public void Execute(string targetName) {
            Target target = Targets.Find(targetName);
            if (target == null) {
                throw new BuildException(String.Format("unknown target '{0}'", targetName));
            }
            target.Execute();
        }


        /// <summary>
        /// Does Execute() and wraps in error handling and time stamping.
        /// </summary>
        /// <returns>Indication of success</returns>
        public bool Run() {
            bool success = true;
            try {

                Project.OnBuildStarted(this, new BuildEventArgs(_projectName));
                DateTime startTime = DateTime.Now;

                Log.WriteLine("Buildfile: {0}", BuildFileURI);

                // Write verbose project information after Initialize to make sure
                // properties are correctly initialized.
                Log.WriteLineIf(Verbose, "Base Directory: {0}", BaseDirectory);

                Execute();

                Log.WriteLine();
                Log.WriteLine("BUILD SUCCEEDED");

                TimeSpan buildTime = DateTime.Now - startTime;
                Log.WriteLine();
                Log.WriteLine("Total time: {0} seconds", (int) buildTime.TotalSeconds);

                success = true;
                return true;

            } catch (BuildException e) {
                Log.WriteLine();
                Log.WriteLine("BUILD FAILED");
                Log.WriteLine(e.Message);
                if (e.InnerException != null) {
                    Log.WriteLine(e.InnerException.Message);
                }
                success = false;
                return false;

            } catch (Exception e) {
                // all other exceptions should have been caught
                Log.WriteLine();
                Log.WriteLine("INTERNAL ERROR");
                Log.WriteLine(e.ToString());
                Log.WriteLine();
                Log.WriteLine("Please send bug report to nant-developers@lists.sourceforge.net");
                success = false;
                return false;
            }
            finally {
                string endTask;
                if(success)
                    endTask = _properties[NANT_PROPERTY_ONSUCCESS];
                else
                    endTask = _properties[NANT_PROPERTY_ONFAILURE];

                if(endTask != null && endTask != string.Empty)
                    Execute(endTask);

                Project.OnBuildFinished(this, new BuildEventArgs(_projectName));
            }
        }

        /// <summary>
        /// Creates a new Task from the given XmlNode
        /// </summary>
        /// <param name="taskNode">The task definition.</param>
        /// <returns>The new Task instance</returns>
        public Task CreateTask(XmlNode taskNode) {
            return CreateTask(taskNode, null);
        }

        /// <summary>
        /// Creates a new Task from the given XmlNode within a Target
        /// </summary>
        /// <param name="taskNode">The task definition.</param>
        /// <param name="target">The owner Target</param>
        /// <returns>The new Task instance</returns>
        public Task CreateTask(XmlNode taskNode, Target target) {
            Task task = TaskFactory.CreateTask(taskNode, this);
            task.Project = this;
            task.Parent = target;
            task.Initialize(taskNode);
            return task;
        }

        /// <summary>
        /// Expands a string from known properties
        /// </summary>
        /// <param name="input">The string with replacement tokens</param>
        /// <returns>The expanded and replaced string</returns>
        public string ExpandProperties(string input) {
            string output = input;
            if (input != null) {
                const string pattern = @"\$\{([^\}]*)\}";
                foreach (Match m in Regex.Matches(input, pattern)) {
                    if (m.Length > 0) {

                        string token         = m.ToString();
                        string propertyName  = m.Groups[1].Captures[0].Value;
                        string propertyValue = Properties[propertyName];

                        if (propertyValue != null) {
                            output = output.Replace(token, propertyValue);
                        }
                        else {
                            throw new BuildException(String.Format("Property '{0}' has not been set!", propertyName));
                        }
                    }
                }
            }
            return output;
        }

        /// <summary>Combine with project's <see cref="BaseDirectory"/> to form a full path to file or directory.</summary>
        /// <remarks>
        ///   <para>If it is possible for the <c>path</c> to contain property macros the <c>path</c> call <see cref="ExpandProperties"/> first.</para>
        /// </remarks>
        /// <returns>
        ///   <para>A rooted path.</para>
        /// </returns>
        /// <param name="path">The relative or absolute path.</param>
        public string GetFullPath(string path) {
            if (path == null) {
                return BaseDirectory;
            }

            //Docs above read we should do this. But it should be done before it gets here.
            //path = this.ExpandProperties(path);

            if (!Path.IsPathRooted(path)) {
                path = Path.Combine(BaseDirectory, path);
            }
            return path;
        }
    }
}
