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
//
// Gerry Shaw (gerry_shaw@yahoo.com)
// Ian MacLean (ian_maclean@another.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using Microsoft.Win32;

namespace SourceForge.NAnt {
    /// <summary>Central representation of a NAnt project.</summary>
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
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Protected Static Fields

        //xml element and attribute names that are not defined in metadata
        protected const string ROOT_XML = "project";
        protected const string PROJECT_NAME_ATTRIBUTE = "name";
        protected const string PROJECT_DEFAULT_ATTRIBUTE = "default";
        protected const string PROJECT_BASEDIR_ATTRIBUTE = "basedir";
        protected const string TARGET_XML = "target";
        protected const string TARGET_DEPENDS_ATTRIBUTE = "depends";

        #endregion Protected Static Fields

        #region Public Static Fields

        public const string NANT_PROPERTY_FILENAME = "nant.filename";
        public const string NANT_PROPERTY_VERSION = "nant.version";
        public const string NANT_PROPERTY_LOCATION = "nant.location";
        public const string NANT_PROPERTY_PROJECT_NAME = "nant.project.name";
        public const string NANT_PROPERTY_PROJECT_BUILDFILE = "nant.project.buildfile";
        public const string NANT_PROPERTY_PROJECT_BASEDIR = "nant.project.basedir";
        public const string NANT_PROPERTY_PROJECT_DEFAULT = "nant.project.default";

        public const string NANT_PROPERTY_ONSUCCESS = "nant.onsuccess";
        public const string NANT_PROPERTY_ONFAILURE = "nant.failure";

        #endregion Public Static Fields

        #region Public Instance Fields

        public event BuildEventHandler BuildStarted;
        public event BuildEventHandler BuildFinished;
        public event BuildEventHandler TargetStarted;
        public event BuildEventHandler TargetFinished;
        public event BuildEventHandler TaskStarted;
        public event BuildEventHandler TaskFinished;
        public event BuildEventHandler MessageLogged;

        #endregion Public Instance Fields

        #region Private Instance Fields

        string _projectName = "";
        string _defaultTargetName = null;

        int _indentationSize = 4;
        int _indentationLevel = 0;

        BuildListenerCollection _buildListeners = new BuildListenerCollection();
        StringCollection    _buildTargets = new StringCollection();
        TargetCollection    _targets = new TargetCollection();
        LocationMap         _locationMap = new LocationMap();
        PropertyDictionary  _properties = new PropertyDictionary();
        XmlDocument         _doc = null; // set in ctorHelper
        XmlNamespaceManager _nm = new XmlNamespaceManager(new NameTable()); //used to map "nant" to default namespace.
        
        // info about framework information
        FrameworkInfoDictionary _frameworkInfoDictionary = new FrameworkInfoDictionary();
        FrameworkInfo _defaultFramework;
        FrameworkInfo _currentFramework;

        /// <summary>
        /// Holds the default threshold for build loggers.
        /// </summary>
        Level _threshold = Level.Info;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// document and message threshold and with default indentation 
        /// level (0).
        /// </summary>
        /// <param name="doc">Any valid build format will do.</param>
        /// <param name="threshold">The message threshold.</param>
        public Project(XmlDocument doc, Level threshold) : this(doc, threshold, 0) {
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// document, message threshold and indentation level.
        /// </summary>
        /// <param name="doc">Any valid build format will do.</param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        public Project(XmlDocument doc, Level threshold, int indentLevel) {
            ctorHelper(doc, threshold, indentLevel);
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// source, message threshold and default indentation level.
        /// </summary>
        /// <param name="UriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para> This can be of any form that XmlDocument.Load(string url) accepts.</para>
        /// </param>
        /// <param name="threshold">The message threshold.</param>
        /// <remarks><para>If the source is a uri of form 'file:///path' then use the path part.</para></remarks>
        public Project(string UriOrFilePath, Level threshold) : this(UriOrFilePath, threshold, 0) {
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// source, message threshold and indentation level.
        /// </summary>
        /// <param name="UriOrFilePath">
        /// <para> The full path to the build file.</para>
        /// <para> This can be of any form that XmlDocument.Load(string url) accepts.</para>
        /// </param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        /// <remarks><para>If the source is a uri of form 'file:///path' then use the path part.</para></remarks>
        public Project(string UriOrFilePath, Level threshold, int indentLevel) {
            string path = UriOrFilePath;
            //if the source is not a valid uri, pass it thru.
            //if the source is a file uri, pass the localpath of it thru.
            try {
                Uri testURI = new Uri(UriOrFilePath);
                if(testURI.IsFile) {
                    path = testURI.LocalPath;
                }
            }
            catch(Exception e) {
                logger.Debug("Error creating URI in project constructor. Moving on... ", e);
            }
            finally{
                if(path == null)
                    path = UriOrFilePath;
            }

            ctorHelper(LoadBuildFile(path), threshold, indentLevel);
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the indendation level of the build output.
        /// </summary>
        /// <value>The indentation level of the build output.</value>
        /// <remarks>
        /// To change the <see cref="IndentationLevel" />, the <see cref="Indent()" /> 
        /// and <see cref="UnIndent()" /> methods should be used.
        /// </remarks>
        public int IndentationLevel {
            get { return _indentationLevel; }
        }

        /// <summary>
        /// Gets or sets the indentation size of the build output.
        /// </summary>
        /// <value>The indendation size of the build output.</value>
        public int IndentationSize {
            get { return _indentationSize; }
        }

        /// <summary>
        /// Gets or sets the default threshold level for build loggers.
        /// </summary>
        /// <value>The default threshold level for build loggers.</value>
        public Level Threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        /// <summary>
        /// Gets the name of the <see cref="Project" />.
        /// </summary>
        /// <value>The name of the <see cref="Project" />.</value>
        public string ProjectName {
            get { return _projectName; }
        }

        /// <summary>
        /// Gets or sets the base directory used for relative references.
        /// </summary>
        /// <value>The base directory used for relative references.</value>
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

        public XmlNamespaceManager NamespaceManager{
            get { return _nm; }
        }

        /// <summary>
        /// Gets the <see cref="Uri" /> form of the current project definition.
        /// </summary>
        /// <value>The <see cref="Uri" /> form of the current project definition.</value>
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
        /// Table of framework info - accessible by tasks and others
        /// </summary>
        public FrameworkInfoDictionary FrameworkInfoDictionary {
            get { return _frameworkInfoDictionary; }
        }
        
        /// <summary>
        /// This is the framework we will normally use unless the nant.settings.currentframework has been set to somthing else  
        /// </summary>
        public FrameworkInfo DefaultFramework {
            get { return _defaultFramework; }  
            set{ _defaultFramework = value; 
                UpdateDefaultFrameworkProperties();
            }
        }
        /// <summary>
        /// Current Framework to use for compilation. ie if its set to NET-1.0 then will will use compiler tools for that framework version
        /// </summary>
        public FrameworkInfo CurrentFramework {
            get { return _currentFramework; }
            set{ 
                _currentFramework = value; 
                UpdateCurrentFrameworkProperties();
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

        /// <summary>
        /// Gets the active <see cref="Project" /> definition.
        /// </summary>
        /// <value>The active <see cref="Project" /> definition.</value>
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
            get { return Level.Verbose >= Threshold; }
        }

        /// <summary>The list of targets to built.</summary>
        /// <remarks>
        ///   <para>Targets are built in the order they appear in the collection.  If the collection is empty the default target will be built.</para>
        /// </remarks>
        public StringCollection BuildTargets {
            get { return _buildTargets; }
        }
        /// <summary>Gets the properties defined in this project.</summary>
        /// <value>The properties deined in this project.</value>
        /// <remarks>
        ///   <para>This is the collection of Properties that are defined by the system and property task statements.</para>
        ///   <para>These properties can be used in expansion.</para>
        /// </remarks>
        public PropertyDictionary Properties {
            get { return _properties; }
        }

        /// <summary>
        /// Gets the targets defined in this project.
        /// </summary>
        /// <value>The targets defined in this project.</value>
        public TargetCollection Targets {
            get { return _targets; }
        }

        /// <summary>
        /// Gets a the build listeners for this project. 
        /// </summary>
        /// <value>The build listeners for this project.</value>
        public BuildListenerCollection BuildListeners {
            get { return _buildListeners; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        internal LocationMap LocationMap {
            get { return _locationMap; }
        }

        #endregion Internal Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Dispatches a <see cref="BuildStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnBuildStarted(object source, BuildEventArgs e) {
            if (BuildStarted != null) {
                BuildStarted(source, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="BuildFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnBuildFinished(object source, BuildEventArgs e) {
            if (BuildFinished != null) {
                BuildFinished(source, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTargetStarted(object source, BuildEventArgs e) {
            if (TargetStarted != null) {
                TargetStarted(source, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTargetFinished(object source, BuildEventArgs e) {
            if (TargetFinished != null) {
                TargetFinished(source, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TaskStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTaskStarted(object source, BuildEventArgs e) {
            if (TaskStarted != null) {
                TaskStarted(source, e);
            }
        }

        /// <summary>
        /// Dispatches the <see cref="TaskFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTaskFinished(object source, BuildEventArgs e) {
            if (TaskFinished != null) {
                TaskFinished(source, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="MessageLogged" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnMessageLogged(BuildEventArgs e) {
            if (MessageLogged != null) {
                MessageLogged(this , e);
            }
        }

        /// <summary>
        /// Writes a <see cref="Project" /> level message to the build log with
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        public void Log(Level messageLevel, string message) {
            BuildEventArgs eventArgs = new BuildEventArgs(this);
            eventArgs.Message = message;
            eventArgs.MessageLevel = messageLevel;

            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Writes a <see cref="Project" /> level formatted message to the build 
        /// log with the given <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        public void Log(Level messageLevel, string message, params object[] args) {
            BuildEventArgs eventArgs = new BuildEventArgs(this);
            eventArgs.Message = string.Format(CultureInfo.InvariantCulture, message, args);
            eventArgs.MessageLevel = messageLevel;

            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Writes a <see cref="Task" /> task level message to the build log 
        /// with the given <see cref="Level" />.
        /// </summary>
        /// <param name="task">The <see cref="Task" /> from which the message originated.</param>
        /// <param name="messageLevel">The <see cref="Level" /> to log at.</param>
        /// <param name="message">The message to log.</param>
        public void Log(Task task, Level messageLevel, string message) {
            BuildEventArgs eventArgs = new BuildEventArgs(task);
            eventArgs.Message = message;
            eventArgs.MessageLevel = messageLevel;

            OnMessageLogged(eventArgs);
        }

        /// <summary>
        /// Writes a <see cref="Target" /> level message to the build log with 
        /// the given <see cref="Level" />.
        /// </summary>
        /// <param name="target">The <see cref="Target" /> from which the message orignated.</param>
        /// <param name="messageLevel">The level to log at.</param>
        /// <param name="message">The message to log.</param>
        public void Log(Target target, Level messageLevel, string message) {
            BuildEventArgs eventArgs = new BuildEventArgs(target);
            eventArgs.Message = message;
            eventArgs.MessageLevel = messageLevel;

            OnMessageLogged(eventArgs);
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
                throw new BuildException(String.Format(CultureInfo.InvariantCulture, "unknown target '{0}'", targetName));
            }
            target.Execute();
        }

        /// <summary>
        /// Does Execute() and wraps in error handling and time stamping.
        /// </summary>
        /// <returns>Indication of success</returns>
        public bool Run() {
            Exception error = null;
            DateTime startTime = DateTime.Now;

            try {
                OnBuildStarted(this, new BuildEventArgs(this));

                Log(Level.Info, "Buildfile: {0}", BuildFileURI);

                // Write verbose project information after Initialize to make sure
                // properties are correctly initialized.
                Log(Level.Verbose, "Base Directory: {0}.", BaseDirectory);

                // execute the project
                Execute();

                // signal build success
                return true;
            } catch (BuildException e) {
                // store exception in error variable in order to include it 
                // in the BuildFinished event.
                error = e;

                // log exception details to log4net
                logger.Error("Build failed.", e);

                // signal build failure
                return false;
            } catch (Exception e) {
                // store exception in error variable in order to include it 
                // in the BuildFinished event.
                error = e;

                // log exception details to log4net
                logger.Fatal("Build failed.", e);

                // signal build failure
                return false;
            } finally {
                string endTask;
                if(error != null) {
                    endTask = _properties[NANT_PROPERTY_ONSUCCESS];
                } else {
                    endTask = _properties[NANT_PROPERTY_ONFAILURE];
                }

                if (endTask != null && endTask.Length != 0) {
                    Execute(endTask);
                }

                // output total build time to build log
                TimeSpan buildTime = DateTime.Now - startTime;
                Log(Level.Info, "Total time: {0} seconds.", (int) buildTime.TotalSeconds);

                // fire BuildFinished event with details of build outcome
                BuildEventArgs buildFinishedArgs = new BuildEventArgs(this);
                buildFinishedArgs.Exception = error;
                OnBuildFinished(this, buildFinishedArgs);
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
        /// <param name="location">The location in the build file. Used to throw more accurate exceptions</param>
        /// <returns>The expanded and replaced string</returns>
        public string ExpandProperties(string input, Location location) {
            return _properties.ExpandProperties(input, location );
        }

        /// <summary>
        /// Combines the specified path with the <see cref="BaseDirectory"/> of 
        /// the <see cref="Project" /> to form a full path to file or directory.
        /// </summary>
        /// <param name="path">The relative or absolute path.</param>
        /// <returns>
        /// A rooted path, or the <see cref="BaseDirectory" /> of the <see cref="Project" /> 
        /// if the <paramref name="path" /> parameter is a null reference.
        /// </returns>
        public string GetFullPath(string path) {
            if (path == null) {
                return BaseDirectory;
            }

            if (!Path.IsPathRooted(path)) {
                path = Path.GetFullPath( Path.Combine(BaseDirectory, path) ); 
            }
            return path;
        }

        /// <summary>
        /// Creates the default <see cref="IBuildLogger" /> and attaches it to
        /// the <see cref="Project" />.
        /// </summary>
        public void CreateDefaultLogger() {
            IBuildLogger buildLogger = new DefaultLogger();

            // hook up to build events
            BuildStarted += new BuildEventHandler(buildLogger.BuildStarted);
            BuildFinished += new BuildEventHandler(buildLogger.BuildFinished);
            TargetStarted += new BuildEventHandler(buildLogger.TargetStarted);
            TargetFinished += new BuildEventHandler(buildLogger.TargetFinished);
            TaskStarted += new BuildEventHandler(buildLogger.TaskStarted);
            TaskFinished += new BuildEventHandler(buildLogger.TaskFinished);
            MessageLogged += new BuildEventHandler(buildLogger.MessageLogged);

            // set threshold of logger equal to threshold of the project
            buildLogger.Threshold = Threshold;

            // add default logger to list of build listeners
            BuildListeners.Add(buildLogger);
        }

        /// <summary>
        /// Increases the <see cref="IndentationLevel" /> of the <see cref="Project" />.
        /// </summary>
        public void Indent() {
            _indentationLevel++;
        }

        /// <summary>
        /// Decreases the <see cref="IndentationLevel" /> of the <see cref="Project" />.
        /// </summary>
        public void UnIndent() {
            _indentationLevel--;
        }

        /// <summary>
        /// Detaches the currently attached <see cref="IBuildListener" /> instances
        /// from the <see cref="Project" />.
        /// </summary>
        public void DetachBuildListeners() {
            foreach (IBuildListener listener in BuildListeners) {
                BuildStarted -= new BuildEventHandler(listener.BuildStarted);
                BuildFinished -= new BuildEventHandler(listener.BuildFinished);
                TargetStarted -= new BuildEventHandler(listener.TargetStarted);
                TargetFinished -= new BuildEventHandler(listener.TargetFinished);
                TaskStarted -= new BuildEventHandler(listener.TaskStarted);
                TaskFinished -= new BuildEventHandler(listener.TaskFinished);
                MessageLogged -= new BuildEventHandler(listener.MessageLogged);

                if (typeof(IBuildLogger).IsAssignableFrom(listener.GetType())) {
                    ((IBuildLogger) listener).Flush();
                }
            }
            BuildListeners.Clear();
        }

        /// <summary>
        /// Attaches the specified build listeners to the <see cref="Project" />.
        /// </summary>
        /// <param name="listeners">The <see cref="IBuildListener" /> instances to attach to the <see cref="Project" />.</param>
        /// <remarks>
        /// The currently attached <see cref="IBuildListener" /> instances will 
        /// be detached before the new <see cref="IBuildListener" /> instances 
        /// are attached.
        /// </remarks>
        public void AttachBuildListeners(BuildListenerCollection listeners) {
            // detach currently attached build listeners
            DetachBuildListeners();

            foreach (IBuildListener listener in listeners) {
                // hook up listener to project build events
                BuildStarted += new BuildEventHandler(listener.BuildStarted);
                BuildFinished += new BuildEventHandler(listener.BuildFinished);
                TargetStarted += new BuildEventHandler(listener.TargetStarted);
                TargetFinished += new BuildEventHandler(listener.TargetFinished);
                TaskStarted += new BuildEventHandler(listener.TaskStarted);
                TaskFinished += new BuildEventHandler(listener.TaskFinished);
                MessageLogged += new BuildEventHandler(listener.MessageLogged);

                // add listener to project listener list
                BuildListeners.Add(listener);
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

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
        /// <param name="doc">An <see cref="XmlDocument" /> representing the project definition.</param>
        /// <param name="threshold">The project message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        protected virtual void ctorHelper(XmlDocument doc, Level threshold, int indentLevel) {
            string newBaseDir = null;

            TaskFactory.AddProject(this);

            // set the project definition
            _doc = doc;

            // set the indentation size of the build output
            _indentationSize = 12;

            // set the indentation level of the build output
            _indentationLevel = indentLevel;

            // set the project message threshold
            Threshold = threshold;

            // add default logger
            CreateDefaultLogger();

            //fill the namespace manager up. So we can make qualified xpath expressions.
            if(doc.DocumentElement.NamespaceURI == null || doc.DocumentElement.NamespaceURI.Equals(string.Empty)){
                string defURI;
                if(doc.DocumentElement.Attributes["xmlns", "nant"] == null){
                    defURI = @"http://none";
                }else {
                    defURI = doc.DocumentElement.Attributes["xmlns", "nant"].Value;
                }
                XmlAttribute attr = doc.CreateAttribute("xmlns");
                attr.Value= defURI;
                doc.DocumentElement.Attributes.Append(attr);
                
                //if(!defURI.Equals(doc.DocumentElement.NamespaceURI))
                //    throw new BuildException(string.Format("Default namespace is bad! {0}!={1}", defURI, doc.DocumentElement.NamespaceURI));
            }
            
            _nm.AddNamespace("nant", doc.DocumentElement.NamespaceURI);

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
            } else {
            
                // if basedir attribute is set to a relative path the resolve it relative to the build file path
                if ( BuildFileLocalName != null && ! Path.IsPathRooted(newBaseDir) ) { 
                    newBaseDir = Path.GetDirectoryName(Path.Combine( Path.GetDirectoryName(BuildFileLocalName), newBaseDir ) );
                }
            }

            newBaseDir = Path.GetFullPath(newBaseDir);
            //BaseDirectory must be rooted.
            BaseDirectory = newBaseDir;
            
            // Load settings out of settings file
            ProcessSettings();

            //set here and in nant:Main
            Assembly ass = Assembly.GetExecutingAssembly();
            
            Properties.AddReadOnly(NANT_PROPERTY_FILENAME, ass.CodeBase);
            Properties.AddReadOnly(NANT_PROPERTY_VERSION,  ass.GetName().Version.ToString());
            Properties.AddReadOnly(NANT_PROPERTY_LOCATION, AppDomain.CurrentDomain.BaseDirectory);

            Properties.AddReadOnly(NANT_PROPERTY_PROJECT_NAME, ProjectName);
            if(BuildFileURI != null) {
                Properties.AddReadOnly(NANT_PROPERTY_PROJECT_BUILDFILE, BuildFileURI.ToString());
            }

            Properties.AddReadOnly(NANT_PROPERTY_PROJECT_DEFAULT,   DefaultTargetName);

            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NANT_PROPERTY_FILENAME, 
                Properties[NANT_PROPERTY_FILENAME]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NANT_PROPERTY_VERSION, 
                Properties[NANT_PROPERTY_VERSION]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NANT_PROPERTY_LOCATION, 
                Properties[NANT_PROPERTY_LOCATION]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NANT_PROPERTY_PROJECT_NAME, 
                Properties[NANT_PROPERTY_PROJECT_NAME]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NANT_PROPERTY_PROJECT_BUILDFILE, 
                Properties[NANT_PROPERTY_PROJECT_BUILDFILE]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NANT_PROPERTY_PROJECT_DEFAULT, 
                Properties[NANT_PROPERTY_PROJECT_DEFAULT]));
        }

        #endregion Protected Instance Methods

        #region Internal Instance Methods

        /// <summary>
        /// This method is only meant to be used by the <see cref="Project"/> 
        /// class and <see cref="SourceForge.NAnt.Tasks.IncludeTask"/>.
        /// </summary>
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
                } else if(!childNode.Name.StartsWith("#") && childNode.NamespaceURI.Equals(doc.DocumentElement.NamespaceURI)) {
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

        #endregion Internal Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Creates a new <see cref="XmlDocument" /> based on the project definition.
        /// </summary>
        /// <param name="source">The source of the document.<para>Any form that is valid for <see cref="XmlDocument.Load(string)" /> can be used here.</para></param>
        /// <returns>An <see cref="XmlDocument" /> based on the specified project definition.</returns>
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

        /// <summary>
        /// Updates dependent properties when the <see cref="DefaultFramework" /> 
        /// is set.
        /// </summary>
        private void UpdateDefaultFrameworkProperties() {      
            Properties["nant.settings.defaultframework"] = DefaultFramework.Name;    
            Properties["nant.settings.defaultframework.version"] = DefaultFramework.Version;
            Properties["nant.settings.defaultframework.description"] = DefaultFramework.Description;             
            Properties["nant.settings.defaultframework.frameworkdirectory"] = DefaultFramework.FrameworkDirectory.FullName; 
            if (DefaultFramework.SdkDirectory != null) {
                Properties["nant.settings.defaultframework.sdkdirectory"] = DefaultFramework.SdkDirectory.FullName; 
            } else {
                Properties["nant.settings.defaultframework.sdkdirectory"] = null;
            }
            Properties["nant.settings.defaultframework.frameworkassemblydirectory"] = DefaultFramework.FrameworkAssemblyDirectory.FullName; 
            Properties["nant.settings.defaultframework.basiccompiler"] = DefaultFramework.BasicCompilerName; 
            Properties["nant.settings.defaultframework.jsharpcompiler"] = DefaultFramework.JSharpCompilerName; 
            Properties["nant.settings.defaultframework.jscriptcompiler"] = DefaultFramework.JScriptCompilerName; 
            Properties["nant.settings.defaultframework.csharpcompiler"] = DefaultFramework.CSharpCompilerName; 
            Properties["nant.settings.defaultframework.resgentool"] = DefaultFramework.ResGenToolName;         
            Properties["nant.settings.defaultframework.description"] = DefaultFramework.Description;     
            if (DefaultFramework.RuntimeEngine != null) {
                Properties["nant.settings.defaultframework.runtimeengine"] = DefaultFramework.RuntimeEngine.Name; 
            } else {
                Properties["nant.settings.defaultframework.runtimeengine"] = null;
            }
        }
        
        /// <summary>
        /// Updates dependent properties when the <see cref="CurrentFramework" /> 
        /// is set.
        /// </summary>
        private void UpdateCurrentFrameworkProperties(){
            Properties["nant.settings.currentframework"] = CurrentFramework.Name;
            Properties["nant.settings.currentframework.version"] = CurrentFramework.Version;
            Properties["nant.settings.currentframework.description"] = CurrentFramework.Description; 
            Properties["nant.settings.currentframework.frameworkdirectory"] = CurrentFramework.FrameworkDirectory.FullName; 
            if (CurrentFramework.SdkDirectory != null) {
                Properties["nant.settings.currentframework.sdkdirectory"] = CurrentFramework.SdkDirectory.FullName; 
            } else {
                Properties["nant.settings.currentframework.sdkdirectory"] = null; 
            }
            Properties["nant.settings.currentframework.frameworkassemblydirectory"] = CurrentFramework.FrameworkAssemblyDirectory.FullName; 
            Properties["nant.settings.currentframework.csharpcompiler"] = CurrentFramework.CSharpCompilerName; 
            Properties["nant.settings.currentframework.basiccompiler"] = CurrentFramework.BasicCompilerName; 
            Properties["nant.settings.currentframework.jsharpcompiler"] = CurrentFramework.JSharpCompilerName; 
            Properties["nant.settings.currentframework.jscriptcompiler"] = CurrentFramework.JScriptCompilerName; 
            Properties["nant.settings.currentframework.resgentool"] = CurrentFramework.ResGenToolName;             
            Properties["nant.settings.currentframework.description"] = CurrentFramework.Description; 
            if (CurrentFramework.RuntimeEngine != null) {
                Properties["nant.settings.currentframework.runtimeengine"] = CurrentFramework.RuntimeEngine.Name; 
            } else {
                Properties["nant.settings.currentframework.runtimeengine"] = null;
            }
        }

        #endregion Private Instance Methods

        #region Settings file Load routines
        
        /// <summary>
        /// Reads the list of global properties specified in the settings file.
        /// </summary>
        /// <param name="propertyNodes">An <see cref="XmlNode" /> containing childnodes representing global properties.</param>
        private void ProcessGlobalProperties(XmlNodeList propertyNodes) {
            foreach( XmlNode propertyNode in propertyNodes ){
                string propName = propertyNode.Attributes["name"].Value;
                string propValue= propertyNode.Attributes["value"].Value;

                XmlNode readonlyNode = propertyNode.Attributes["readonly"];
                if (readonlyNode != null && readonlyNode.Value == "true" ) {
                    Properties.AddReadOnly(propName, propValue);
                }     
                else {
                    Properties[propName] =  propValue;
                }
            }
        }

        /// <summary>
        /// Processes the framework info.
        /// </summary>
        /// <param name="frameworkInfoNodes">An <see cref="XmlNode" /> containing childnodes representing supported frameworks.</param>
        private void ProcessFrameworkInfo(XmlNodeList frameworkInfoNodes) {
            foreach (XmlNode frameworkNode in frameworkInfoNodes) {
                // load the runtimInfo stuff
                XmlNode sdkDirectoryNode = frameworkNode.SelectSingleNode("sdkdirectory");
                XmlNode frameworkDirectoryNode = frameworkNode.SelectSingleNode("frameworkdirectory");
                XmlNode frameworkAssemDirectoryNode = frameworkNode.SelectSingleNode("frameworkassemblydirectory");

                string name = GetXmlAttributeValue(frameworkNode, "name");
                string description =  GetXmlAttributeValue(frameworkNode, "description");
                string version = GetXmlAttributeValue(frameworkNode, "version");
                string csharpCompilerName = GetXmlAttributeValue(frameworkNode, "csharpcompilername");
                string basicCompilerName = GetXmlAttributeValue(frameworkNode, "basiccompilername");
                string jsharpCompilerName = GetXmlAttributeValue(frameworkNode, "jsharpcompilername");
                string jscriptCompilerName = GetXmlAttributeValue(frameworkNode, "jscriptcompilername");
                string resgenToolName = GetXmlAttributeValue(frameworkNode, "resgenname");
                string runtimeEngine = GetXmlAttributeValue(frameworkNode, "runtimeengine");

                string sdkDirectory = null;
                string frameworkDirectory = null;
                string frameworkAssemblyDirectory = null;

                // Do some validation here on null or not null fields
                if (GetXmlAttributeValue(sdkDirectoryNode, "useregistry") == "true") {
                    string regKey = GetXmlAttributeValue(sdkDirectoryNode, "regkey");
                    string regValue = GetXmlAttributeValue(sdkDirectoryNode, "regvalue");
                    RegistryKey sdkKey = Registry.LocalMachine.OpenSubKey(regKey);

                    if (sdkKey != null && sdkKey.GetValue(regValue) != null) {
                        sdkDirectory = sdkKey.GetValue(regValue).ToString() + Path.DirectorySeparatorChar + "bin";
                    }
                } else {
                    sdkDirectory = GetXmlAttributeValue(sdkDirectoryNode, "dir");
                }

                if (GetXmlAttributeValue(frameworkDirectoryNode, "useregistry") == "true" ) {
                    string regKey = GetXmlAttributeValue(frameworkDirectoryNode, "regkey");
                    string regValue = GetXmlAttributeValue(frameworkDirectoryNode, "regvalue");
                    RegistryKey frameworkKey = Registry.LocalMachine.OpenSubKey(regKey);
                    
                    if (frameworkKey != null && frameworkKey.GetValue(regValue) != null) {
                        frameworkDirectory = frameworkKey.GetValue(regValue).ToString() + "v" + version + Path.DirectorySeparatorChar;
                    }
                } else {
                    frameworkDirectory = GetXmlAttributeValue(frameworkDirectoryNode, "dir");
                }
                
                if (GetXmlAttributeValue(frameworkAssemDirectoryNode, "useregistry") == "true") {
                    string regKey = GetXmlAttributeValue(frameworkAssemDirectoryNode, "regkey");
                    string regValue = GetXmlAttributeValue(frameworkAssemDirectoryNode, "regvalue");
                    RegistryKey frameworkAssemKey = Registry.LocalMachine.OpenSubKey(regKey);
                    
                    if (frameworkAssemKey != null && frameworkAssemKey.GetValue(regValue) != null) {
                        frameworkAssemblyDirectory = frameworkAssemKey.GetValue(regValue).ToString() + "v" + version + Path.DirectorySeparatorChar;
                    }
                } else {
                    frameworkAssemblyDirectory = GetXmlAttributeValue(frameworkAssemDirectoryNode, "dir");
                }

                FrameworkInfo info = null;
                try {
                    info = new FrameworkInfo( name, 
                        description, 
                        version, 
                        frameworkDirectory, 
                        sdkDirectory, 
                        frameworkAssemblyDirectory, 
                        csharpCompilerName, 
                        basicCompilerName,
                        jsharpCompilerName,
                        jscriptCompilerName,
                        resgenToolName,
                        runtimeEngine );
                } catch (Exception e ) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "settings warning: frameworkinfo {0} is invalid and has not been loaded: ", name ); 
                    Log(Level.Verbose, msg + e.Message);
                    logger.Info(msg, e);
                } 
                // just ignore frameworks that don't validate
                if (info != null ) {
                    _frameworkInfoDictionary.Add(info.Name, info);
                }
            }
        }

        /// <summary>
        /// Gets the value of the specified attribute from the specified node.
        /// </summary>
        /// <param name="xmlNode">The node of which the attribute value should be retrieved.</param>
        /// <param name="attributeName">The attribute of which the value should be returned.</param>
        /// <returns>
        /// The value of the attribute with the specified name or <c>null</c> if the attribute
        /// does not exist or has no value.
        /// </returns>
        private static string GetXmlAttributeValue(XmlNode xmlNode, string attributeName) {
            string attributeValue = null;

            if (xmlNode != null) {
                XmlAttribute xmlAttribute = (XmlAttribute) xmlNode.Attributes.GetNamedItem(attributeName);
                if (xmlAttribute != null) {
                    attributeValue = xmlAttribute.Value.Trim();
                    if (attributeValue.Length == 0) {
                        attributeValue = null;
                    }
                }
            }

            return attributeValue;
        }
                
        /// <summary>
        /// Loads and processes a settings file from the directory of the current 
        /// <see cref="Assembly" />.
        /// </summary>
        private void ProcessSettings(){
            XmlDocument confdoc = new XmlDocument();
            _frameworkInfoDictionary = new FrameworkInfoDictionary();
            
            object testobj = ConfigurationSettings.GetConfig("nantsettings");
            XmlNode node = testobj as XmlNode;
            logger.Debug(string.Format(CultureInfo.InvariantCulture, "[{0}].ConfigFile '{1}'",AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));

            if (node == null) { 
                // todo pull a settings file out of the assembly resource and copy to that location
                Log(Level.Warning, "Framework settings not found. Defaulting to no known framework.");
                logger.Info("Framework settings not found. Defaulting to no known framework.");
                return;
            }

            logger.Debug("Current Config:\n" + node.OuterXml);
            //TODO: Replace XPath Expressions. (Or use namespace/prefix'd element names)
            //If a default namespace is specified this will fail.
            XmlNodeList frameworkInfoNodes = node.SelectNodes("frameworks/frameworkinfo");
            ProcessFrameworkInfo(frameworkInfoNodes);

            string defaultFramework = GetXmlAttributeValue(node, "defaultframework");
            if (defaultFramework != null && _frameworkInfoDictionary.ContainsKey( defaultFramework ) ) {
                Properties.AddReadOnly("nant.settings.defaultframework", defaultFramework );
                Properties.Add("nant.settings.currentframework", defaultFramework );
                
                DefaultFramework = _frameworkInfoDictionary[defaultFramework];
                CurrentFramework = _defaultFramework;
            } else {
                Log(Level.Warning, "Framework {0} does not exist or is not specified in the config. Defaulting to no known framework.", defaultFramework);
            }

            //TODO: Replace XPath Expressions. (Or use namespace/prefix'd element names)
            // now load the default property set
            XmlNodeList propertyNodes = node.SelectNodes("properties/property");
            ProcessGlobalProperties(propertyNodes);
        }

        #endregion Settings file Load routines
    }
}
