// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>Central representation of a NAnt project.</summary>
    /// <example>
    ///   <para>
    ///   The <see cref="Run" /> method will initialize the project with the build
    ///   file specified in the constructor and execute the default target.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// Project p = new Project("foo.build", Level.Info);
    /// p.Run();
    ///     ]]>
    ///   </code>
    ///   <para>
    ///   If no target is given, the default target will be executed if specified 
    ///   in the project.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// Project p = new Project("foo.build", Level.Info);
    /// p.Execute("build");
    ///     ]]>
    ///   </code>
    /// </example>
    public class Project {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Holds a value indicating whether a scan for tasks has already been 
        /// performed on the configured task path.
        /// </summary>
        private static bool ScannedTaskPath = false;

        // XML element and attribute names that are not defined in metadata
        private const string RootXml = "project";
        private const string ProjectNameAttribute = "name";
        private const string ProjectDefaultAttribte = "default";
        private const string ProjectBaseDirAttribute = "basedir";
        private const string TargetXml = "target";

        #endregion Private Static Fields

        #region Internal Static Fields

        // named properties
        internal const string NAntPropertyFileName = "nant.filename";
        internal const string NAntPropertyVersion = "nant.version";
        internal const string NAntPropertyLocation = "nant.location";
        internal const string NAntPropertyProjectName = "nant.project.name";
        internal const string NAntPropertyProjectBuildFile = "nant.project.buildfile";
        internal const string NAntPropertyProjectBaseDir = "nant.project.basedir";
        internal const string NAntPropertyProjectDefault = "nant.project.default";
        internal const string NAntPropertyOnSuccess = "nant.onsuccess";
        internal const string NAntPropertyOnFailure = "nant.onfailure";

        #endregion Internal Static Fields

        #region Public Instance Events

        public event BuildEventHandler BuildStarted;
        public event BuildEventHandler BuildFinished;
        public event BuildEventHandler TargetStarted;
        public event BuildEventHandler TargetFinished;
        public event BuildEventHandler TaskStarted;
        public event BuildEventHandler TaskFinished;
        public event BuildEventHandler MessageLogged;

        #endregion Public Instance Events

        #region Private Instance Fields

        private string _projectName = "";
        private string _defaultTargetName = null;

        private int _indentationSize = 4;
        private int _indentationLevel = 0;

        private BuildListenerCollection _buildListeners = new BuildListenerCollection();
        private StringCollection _buildTargets = new StringCollection();
        private TargetCollection _targets = new TargetCollection();
        private LocationMap _locationMap = new LocationMap();
        private PropertyDictionary _properties = new PropertyDictionary();
        private PropertyDictionary _frameworkNeutralProperties = new PropertyDictionary();
        private XmlDocument _doc = null; // set in ctorHelper
        private XmlNamespaceManager _nm = new XmlNamespaceManager(new NameTable()); //used to map "nant" to default namespace.
        private DataTypeBaseDictionary _dataTypeReferences = new DataTypeBaseDictionary();
        
        // info about frameworks
        private FrameworkInfoDictionary _frameworkInfoDictionary = new FrameworkInfoDictionary();
        private FrameworkInfo _defaultFramework;
        private FrameworkInfo _currentFramework;

        /// <summary>
        /// Holds the default threshold for build loggers.
        /// </summary>
        private Level _threshold = Level.Info;

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
            CtorHelper(doc, threshold, indentLevel);
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// source, message threshold and default indentation level.
        /// </summary>
        /// <param name="uriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para> This can be of any form that <see cref="XmlDocument.Load(string)" /> accepts.</para>
        /// </param>
        /// <param name="threshold">The message threshold.</param>
        /// <remarks>
        /// If the source is a uri of form 'file:///path' then use the path part.
        /// </remarks>
        public Project(string uriOrFilePath, Level threshold) : this(uriOrFilePath, threshold, 0) {
        }

        /// <summary>
        /// Initializes a new <see cref="Project" /> class with the given 
        /// source, message threshold and indentation level.
        /// </summary>
        /// <param name="uriOrFilePath">
        /// <para>The full path to the build file.</para>
        /// <para>This can be of any form that <see cref="XmlDocument.Load(string)" /> accepts.</para>
        /// </param>
        /// <param name="threshold">The message threshold.</param>
        /// <param name="indentLevel">The project indentation level.</param>
        /// <remarks>
        /// If the source is a uri of form 'file:///path' then use the path part.
        /// </remarks>
        public Project(string uriOrFilePath, Level threshold, int indentLevel) {
            string path = uriOrFilePath;
            //if the source is not a valid uri, pass it thru.
            //if the source is a file uri, pass the localpath of it thru.
            try {
                Uri testURI = new Uri(uriOrFilePath);
                if(testURI.IsFile) {
                    path = testURI.LocalPath;
                }
            } catch(Exception ex) {
                logger.Debug("Error creating URI in project constructor. Moving on... ", ex);
            } finally {
                if (path == null) {
                    path = uriOrFilePath;
                }
            }

            CtorHelper(LoadBuildFile(path), threshold, indentLevel);
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the indendation level of the build output.
        /// </summary>
        /// <value>
        /// The indentation level of the build output.
        /// </value>
        /// <remarks>
        /// To change the <see cref="IndentationLevel" />, the <see cref="Indent()" /> 
        /// and <see cref="Unindent()" /> methods should be used.
        /// </remarks>
        public int IndentationLevel {
            get { return _indentationLevel; }
        }

        /// <summary>
        /// Gets or sets the indentation size of the build output.
        /// </summary>
        /// <value>
        /// The indendation size of the build output.
        /// </value>
        public int IndentationSize {
            get { return _indentationSize; }
        }

        /// <summary>
        /// Gets or sets the default threshold level for build loggers.
        /// </summary>
        /// <value>
        /// The default threshold level for build loggers.
        /// </value>
        public Level Threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        /// <summary>
        /// Gets the name of the <see cref="Project" />.
        /// </summary>
        /// <value>
        /// The name of the <see cref="Project" />.
        /// </value>
        public string ProjectName {
            get { return _projectName; }
        }

        /// <summary>
        /// Gets or sets the base directory used for relative references.
        /// </summary>
        /// <value>
        /// The base directory used for relative references.
        /// </value>
        /// <remarks>
        /// <para>
        /// The directory must be rooted. (must start with drive letter, unc, 
        /// etc.)
        /// </para>
        /// <para>
        /// The <see cref="BaseDirectory" /> sets and gets the special property 
        /// named "nant.project.basedir".
        /// </para>
        /// </remarks>
        public string BaseDirectory {
            get {
                string basedir = Properties[NAntPropertyProjectBaseDir];

                if (basedir == null) {
                    return null;
                }

                if (!Path.IsPathRooted(basedir)) {
                    throw new BuildException("BaseDirectory must be rooted! " + basedir);
                }

                return basedir; 
            }
            set {
                if (!Path.IsPathRooted(value)) {
                    throw new BuildException("BaseDirectory must be rooted! " + value);
                }

                Properties[NAntPropertyProjectBaseDir] = value;
            }
        }

        public XmlNamespaceManager NamespaceManager{
            get { return _nm; }
        }

        /// <summary>
        /// Gets the <see cref="Uri" /> form of the current project definition.
        /// </summary>
        /// <value>
        /// The <see cref="Uri" /> form of the current project definition.
        /// </value>
        public Uri BuildFileUri {
            get {
                //TODO: Need to remove this.
                if (Document == null || StringUtils.IsNullOrEmpty(Document.BaseURI)) {
                    return null; //new Uri("http://localhost");
                } else {
                    return new Uri(Document.BaseURI);
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
        /// This is the framework we will normally use unless the nant.settings.currentframework 
        /// has been set to somthing else.
        /// </summary>
        public FrameworkInfo DefaultFramework {
            get { return _defaultFramework; }
            set {
                _defaultFramework = value; 
                UpdateDefaultFrameworkProperties();
            }
        }

        /// <summary>
        /// Gets or sets the framework to use for compilation.
        /// </summary>
        /// <value>
        /// The framework to use for compilation.
        /// </value>
        /// <remarks>
        /// We will use compiler tools and system assemblies for this framework 
        /// in framework-related tasks.
        /// </remarks>
        public FrameworkInfo CurrentFramework {
            get { return _currentFramework; }
            set { 
                _currentFramework = value; 
                UpdateCurrentFrameworkProperties();
            }
        }
             
        /// <summary>
        /// Gets the path to the build file.
        /// </summary>
        /// <value>
        /// The path to the build file, or <see langword="null" /> if the build
        /// document is not file backed.
        /// </value>
        public string BuildFileLocalName {
            get {
                if (BuildFileUri != null && BuildFileUri.IsFile) {
                    return BuildFileUri.LocalPath;
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the active <see cref="Project" /> definition.
        /// </summary>
        /// <value>
        /// The active <see cref="Project" /> definition.
        /// </value>
        public XmlDocument Document {
            get { return _doc; }
        }

        /// <remarks>
        /// Gets the name of the target that will be executed when no other 
        /// build targets are specified.
        /// </remarks>
        public string DefaultTargetName {
            get { return _defaultTargetName; }
        }

        /// <summary>
        /// Gets a value indicating whether tasks should output more build log 
        /// messages.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if tasks should output more build log message; 
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool Verbose {
            get { return Level.Verbose >= Threshold; }
        }

        /// <summary>
        /// The list of targets to built.
        /// </summary>
        /// <remarks>
        /// Targets are built in the order they appear in the collection.  If 
        /// the collection is empty the default target will be built.
        /// </remarks>
        public StringCollection BuildTargets {
            get { return _buildTargets; }
        }

        /// <summary>
        /// Gets the properties defined in this project.
        /// </summary>
        /// <value>The properties defined in this project.</value>
        /// <remarks>
        /// <para>
        /// This is the collection of properties that are defined by the system 
        /// and property task statements.
        /// </para>
        /// <para>
        /// These properties can be used in expansion.
        /// </para>
        /// </remarks>
        public PropertyDictionary Properties {
            get { return _properties; }
        }

        /// <summary>
        /// Gets the framework-neutral properties defined in the NAnt 
        /// configuration file.
        /// </summary>
        /// <value>
        /// The framework-neutral properties defined in the NAnt configuration 
        /// file.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the collection of read-only properties that are defined in 
        /// the NAnt configuration file.
        /// </para>
        /// <para>
        /// These properties can only be used for expansion in framework-specific
        /// and framework-neutral configuration settings.  These properties are 
        /// not available for expansion in the build file.
        /// </para>
        /// </remarks>
        public PropertyDictionary FrameworkNeutralProperties {
            get { return _frameworkNeutralProperties; }
        }

        /// <summary>
        /// Gets the <see cref="DataTypeBase" /> instances defined in this project.
        /// </summary>
        /// <value>
        /// The <see cref="DataTypeBase" /> instances defined in this project.
        /// </value>
        /// <remarks>
        /// <para>
        /// This is the collection of <see cref="DataTypeBase" /> instances that
        /// are defined by <see cref="DataTypeBase" /> (eg fileset) declarations.
        /// </para>
        /// </remarks>
        public DataTypeBaseDictionary DataTypeReferences {
            get { return _dataTypeReferences; }
        }
        
        /// <summary>
        /// Gets the targets defined in this project.
        /// </summary>
        /// <value>
        /// The targets defined in this project.
        /// </value>
        public TargetCollection Targets {
            get { return _targets; }
        }

        /// <summary>
        /// Gets the build listeners for this project. 
        /// </summary>
        /// <value>
        /// The build listeners for this project.
        /// </value>
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
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnBuildStarted(object sender, BuildEventArgs e) {
            if (BuildStarted != null) {
                BuildStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="BuildFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnBuildFinished(object sender, BuildEventArgs e) {
            if (BuildFinished != null) {
                BuildFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTargetStarted(object sender, BuildEventArgs e) {
            if (TargetStarted != null) {
                TargetStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TargetFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTargetFinished(object sender, BuildEventArgs e) {
            if (TargetFinished != null) {
                TargetFinished(sender, e);
            }
        }

        /// <summary>
        /// Dispatches a <see cref="TaskStarted" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTaskStarted(object sender, BuildEventArgs e) {
            if (TaskStarted != null) {
                TaskStarted(sender, e);
            }
        }

        /// <summary>
        /// Dispatches the <see cref="TaskFinished" /> event to the build listeners 
        /// for this <see cref="Project" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BuildEventArgs" /> that contains the event data.</param>
        public void OnTaskFinished(object sender, BuildEventArgs e) {
            if (TaskFinished != null) {
                TaskFinished(sender, e);
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

        /// <summary>
        /// Executes the default target.
        /// </summary>
        /// <remarks>
        /// No top level error handling is done. Any <see cref="BuildException" /> 
        /// will be passed onto the caller.
        /// </remarks>
        public virtual void Execute() {
            // initialize the list of Targets, and execute any global tasks.
            InitializeProjectDocument(Document);

            if (BuildTargets.Count == 0 && !StringUtils.IsNullOrEmpty(DefaultTargetName)) {
                BuildTargets.Add(DefaultTargetName);
            }

            if (BuildTargets.Count == 0) {
                //throw new BuildException("No Target Specified");
            } else {
                foreach (string targetName in BuildTargets) {
                    Execute(targetName);
                }
            }
        }

        /// <summary>
        /// Executes a specific target, and only that target.
        /// </summary>
        /// <param name="targetName">target name to execute.</param>
        /// <remarks>
        /// Only the target is executed. Global tasks are not executed.
        /// </remarks>
        public void Execute(string targetName) {
            Target target = Targets.Find(targetName);
            if (target == null) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Unknown target '{0}'.", targetName));
            }
            target.Execute();
        }

        /// <summary>
        /// Executes the default target and wraps in error handling and time 
        /// stamping.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the build was successful; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        public bool Run() {
            Exception error = null;
            DateTime startTime = DateTime.Now;

            try {
                OnBuildStarted(this, new BuildEventArgs(this));

                Log(Level.Info, "Buildfile: {0}", BuildFileUri);

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
                string endTarget;
                if(error == null) {
                    endTarget = Properties[NAntPropertyOnSuccess];
                } else {
                    endTarget = Properties[NAntPropertyOnFailure];
                }

                // TO-DO : remove this after release of NAnt 0.8.4 or so
                string deprecatedFailureTarget = Properties["nant.failure"];
                if (!StringUtils.IsNullOrEmpty(deprecatedFailureTarget)) {
                    Log(Level.Warning, "The 'nant.failure' property has been deprecated." +
                        " You should use '{0}' to designate the target that should be" +
                        " executed when the build fails.\n", Project.NAntPropertyOnFailure);
                    if (error != null) {
                        Execute(deprecatedFailureTarget);
                    }
                }

                if (!StringUtils.IsNullOrEmpty(endTarget)) {
                    Execute(endTarget);
                }

                // fire BuildFinished event with details of build outcome
                BuildEventArgs buildFinishedArgs = new BuildEventArgs(this);
                buildFinishedArgs.Exception = error;
                OnBuildFinished(this, buildFinishedArgs);
            }
        }

        public DataTypeBase CreateDataTypeBase(XmlNode elementNode) {
            DataTypeBase type = TypeFactory.CreateDataType(elementNode, this);
            type.Project = this;
            type.Parent = this;
            type.Initialize(elementNode);
            return type;
        }

        /// <summary>
        /// Creates a new <see ref="Task" /> from the given <see cref="XmlNode" />.
        /// </summary>
        /// <param name="taskNode">The <see cref="Task" /> definition.</param>
        /// <returns>The new <see cref="Task" /> instance.</returns>
        public Task CreateTask(XmlNode taskNode) {
            return CreateTask(taskNode, null);
        }

        /// <summary>
        /// Creates a new <see cref="Task" /> from the given <see cref="XmlNode" /> 
        /// within a <see cref="Target" />.
        /// </summary>
        /// <param name="taskNode">The <see cref="Task" /> definition.</param>
        /// <param name="target">The owner <see cref="Target" />.</param>
        /// <returns>The new <see cref="Task" /> instance.</returns>
        public Task CreateTask(XmlNode taskNode, Target target) {
            Task task = TypeFactory.CreateTask(taskNode, this);
            task.Project = this;
            task.Parent = target;
            task.Initialize(taskNode);
            return task;
        }

        /// <summary>
        /// Expands a <see cref="string" /> from known properties.
        /// </summary>
        /// <param name="input">The <see cref="string" /> with replacement tokens.</param>
        /// <param name="location">The location in the build file. Used to throw more accurate exceptions.</param>
        /// <returns>The expanded and replaced <see cref="string" />.</returns>
        public string ExpandProperties(string input, Location location) {
            return Properties.ExpandProperties(input, location );
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
            if (StringUtils.IsNullOrEmpty(path)) {
                return BaseDirectory;
            }

            if (!Path.IsPathRooted(path)) {
                path = Path.GetFullPath(Path.Combine(BaseDirectory, path)); 
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
        public void Unindent() {
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
        ///     <para>TypeFactory: Calls Initialize and AddProject </para>
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
        protected void CtorHelper(XmlDocument doc, Level threshold, int indentLevel) {
            string newBaseDir = null;

            TypeFactory.AddProject(this);

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
            if (StringUtils.IsNullOrEmpty(doc.DocumentElement.NamespaceURI)) {
                string defURI;
                if (doc.DocumentElement.Attributes["xmlns", "nant"] == null) {
                    defURI = @"http://none";
                } else {
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
            if (!doc.DocumentElement.Name.Equals(RootXml)) {
                throw new ApplicationException(string.Format(CultureInfo.InvariantCulture,
                    "Root element in '{0}' must be named '{1}'.", doc.BaseURI, RootXml));
            }

            // get project attributes
            if (doc.DocumentElement.HasAttribute(ProjectNameAttribute)) {
                _projectName = doc.DocumentElement.GetAttribute(ProjectNameAttribute);
            }
            if (doc.DocumentElement.HasAttribute(ProjectBaseDirAttribute)) {
                newBaseDir = doc.DocumentElement.GetAttribute(ProjectBaseDirAttribute);
            }
            if (doc.DocumentElement.HasAttribute(ProjectDefaultAttribte)) {
                _defaultTargetName  = doc.DocumentElement.GetAttribute(ProjectDefaultAttribte);
            }

            // give the project a meaningful base directory
            if (StringUtils.IsNullOrEmpty(newBaseDir)) {
                if (!StringUtils.IsNullOrEmpty(BuildFileLocalName)) {
                    newBaseDir = Path.GetDirectoryName(BuildFileLocalName);
                } else {
                    newBaseDir = Environment.CurrentDirectory;
                }
            } else {
                // if basedir attribute is set to a relative path, then resolve 
                // it relative to the build file path
                if (!StringUtils.IsNullOrEmpty(BuildFileLocalName) && !Path.IsPathRooted(newBaseDir)) { 
                    newBaseDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(BuildFileLocalName), newBaseDir));
                }
            }

            newBaseDir = Path.GetFullPath(newBaseDir);

            // base directory must be rooted.
            BaseDirectory = newBaseDir;
            
            // load settings out of settings file
            ProcessSettings();

            // set here and in nant:Main
            Assembly ass = Assembly.GetExecutingAssembly();
            
            Properties.AddReadOnly(NAntPropertyFileName, ass.CodeBase);
            Properties.AddReadOnly(NAntPropertyVersion,  ass.GetName().Version.ToString());
            Properties.AddReadOnly(NAntPropertyLocation, AppDomain.CurrentDomain.BaseDirectory);
            Properties.AddReadOnly(NAntPropertyProjectName, ProjectName);

            if (BuildFileUri != null) {
                Properties.AddReadOnly(NAntPropertyProjectBuildFile, BuildFileUri.ToString());
            }

            Properties.AddReadOnly(NAntPropertyProjectDefault, DefaultTargetName);

            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NAntPropertyFileName, 
                Properties[NAntPropertyFileName]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NAntPropertyVersion, 
                Properties[NAntPropertyVersion]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NAntPropertyLocation, 
                Properties[NAntPropertyLocation]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NAntPropertyProjectName, 
                Properties[NAntPropertyProjectName]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NAntPropertyProjectBuildFile, 
                Properties[NAntPropertyProjectBuildFile]));
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "{0}={1}", 
                NAntPropertyProjectDefault, 
                Properties[NAntPropertyProjectDefault]));
        }

        #endregion Protected Instance Methods

        #region Internal Instance Methods

        /// <summary>
        /// This method is only meant to be used by the <see cref="Project"/> 
        /// class and <see cref="NAnt.Core.Tasks.IncludeTask"/>.
        /// </summary>
        internal void InitializeProjectDocument(XmlDocument doc) {
            // load line and column number information into position map
            LocationMap.Add(doc);

            // initialize targets 
            foreach (XmlNode childNode in doc.DocumentElement.ChildNodes) {
                if (childNode.Name.Equals(TargetXml) && childNode.NamespaceURI.Equals(doc.DocumentElement.NamespaceURI)) {
                    Target target = new Target();
                    target.Project = this;
                    target.Parent = this;
                    target.Initialize(childNode);
                    Targets.Add(target);
                }
            }

            // initialize datatypes and execute global tasks
            foreach (XmlNode childNode in doc.DocumentElement.ChildNodes) {
                if (!childNode.Name.StartsWith("#") && !childNode.Name.Equals(TargetXml) && childNode.NamespaceURI.Equals(doc.DocumentElement.NamespaceURI)) {
                    if (TypeFactory.TaskBuilders.Contains(childNode.Name)) {
                        // create task instance
                        Task task = CreateTask(childNode);
                        task.Parent = this;

                        // execute task
                        task.Execute();
                    } else if (TypeFactory.DataTypeBuilders.Contains(childNode.Name)) {
                        // we are an datatype declaration
                        DataTypeBase dataType = CreateDataTypeBase(childNode);
                        Log(Level.Verbose, "Adding a {0} reference with id '{1}'.", childNode.Name, dataType.ID);
                        DataTypeReferences.Add(dataType.ID, dataType);
                    } else {
                        throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                            "Invalid element <{0}>. Unknown task or datatype.", childNode.Name), 
                            LocationMap.GetLocation(childNode));
                    }
                }
            }
        }

        #endregion Internal Instance Methods

        #region Private Instance Methods

        /// <summary>
        /// Creates a new <see cref="XmlDocument" /> based on the project 
        /// definition.
        /// </summary>
        /// <param name="source">The source of the document.<para>Any form that is valid for <see cref="XmlDocument.Load(string)" /> can be used here.</para></param>
        /// <returns>
        /// An <see cref="XmlDocument" /> based on the specified project 
        /// definition.
        /// </returns>
        private XmlDocument LoadBuildFile(string source) {
            XmlDocument doc = new XmlDocument();
            //Uri srcURI = new Uri(source);
            try {
                doc.Load(source);
                // TODO: validate against xsd schema
            } catch (XmlException ex) {
                Location location = new Location(source, ex.LineNumber, ex.LinePosition);
                throw new BuildException("Error loading buildfile.", location, ex);
            } catch (Exception ex) {
                Location location = new Location(source);
                throw new BuildException("Error loading buildfile.", location, ex);
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
        private void UpdateCurrentFrameworkProperties() {
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
            if (CurrentFramework.RuntimeEngine != null) {
                Properties["nant.settings.currentframework.runtimeengine"] = CurrentFramework.RuntimeEngine.Name; 
            } else {
                Properties["nant.settings.currentframework.runtimeengine"] = null;
            }
        }

        #endregion Private Instance Methods

        #region Settings file Load routines
        
        /// <summary>
        /// Reads the list of global properties specified in the NAnt configuration
        /// file.
        /// </summary>
        /// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing global properties.</param>
        private void ProcessGlobalProperties(XmlNodeList propertyNodes) {
            foreach (XmlNode propertyNode in propertyNodes) {
                string propertyName = GetXmlAttributeValue(propertyNode, "name");
                string propertyValue = GetXmlAttributeValue(propertyNode, "value");

                string propertyReadonly = GetXmlAttributeValue(propertyNode, "readonly");
                if (propertyReadonly != null && propertyReadonly == "true") {
                    Properties.AddReadOnly(propertyName, propertyValue);
                } else {
                    Properties[propertyName] = propertyValue;
                }
            }
        }

        /// <summary>
        /// Reads the list of framework-neutral properties defined in the 
        /// NAnt configuration file.
        /// </summary>
        /// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing framework-neutral properties.</param>
        private void ProcessFrameworkNeutralProperties(XmlNodeList propertyNodes) {
            foreach (XmlNode propertyNode in propertyNodes){
                string propertyName = GetXmlAttributeValue(propertyNode, "name");
                string propertyValue = GetXmlAttributeValue(propertyNode, "value");
                
                if (propertyName == null) {
                    throw new ArgumentException("A framework-neutral property should at least have a name.");
                }

                if (propertyValue != null) {
                    // expand properties in property value
                    propertyValue = FrameworkNeutralProperties.ExpandProperties(propertyValue, Location.UnknownLocation);

                    // add read-only property to collection of framework-neutral properties
                    FrameworkNeutralProperties.AddReadOnly(propertyName, propertyValue);
                }
            }
        }

        /// <summary>
        /// Processes the framework nodes.
        /// </summary>
        /// <param name="frameworkNodes">An <see cref="XmlNodeList" /> representing supported frameworks.</param>
        private void ProcessFrameworks(XmlNodeList frameworkNodes) {
            foreach (XmlNode frameworkNode in frameworkNodes) {
                PropertyDictionary frameworkProperties = null;
                string name = null;

                try {
                    // initialize framework-specific properties
                    frameworkProperties = new PropertyDictionary();

                    // inject framework-neutral properties
                    frameworkProperties.Inherit(FrameworkNeutralProperties, (StringCollection) null);

                    // get framework attributes
                    name = GetXmlAttributeValue(frameworkNode, "name");
                    string description =  GetXmlAttributeValue(frameworkNode, "description");
                    string version = GetXmlAttributeValue(frameworkNode, "version");
                    string runtimeEngine = GetXmlAttributeValue(frameworkNode, "runtimeengine");
                    string frameworkDir = GetXmlAttributeValue(frameworkNode, "frameworkdirectory");
                    string frameworkAssemblyDir = GetXmlAttributeValue(frameworkNode, "frameworkassemblydirectory");
                    string sdkDir = GetXmlAttributeValue(frameworkNode, "sdkdirectory");

                    // get framework-specific properties
                    XmlNodeList propertyNodes = frameworkNode.SelectNodes("properties/property");
                    foreach (XmlNode propertyNode in propertyNodes) {
                        string propertyName = GetXmlAttributeValue(propertyNode, "name");
                        string propertyValue = null;
                
                        if (propertyName == null) {
                            throw new ArgumentException("A framework property should at least have a name.");
                        }

                        if (GetXmlAttributeValue(propertyNode, "useregistry") == "true") {
                            string regKey = GetXmlAttributeValue(propertyNode, "regkey");
                            if (regKey == null) {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Framework property {0} is configured to be read from the registry but has no regkey attribute set.", propertyName));
                            } else {
                                // expand properties in regkey
                                regKey = frameworkProperties.ExpandProperties(regKey, Location.UnknownLocation);
                            }

                            string regValue = GetXmlAttributeValue(propertyNode, "regvalue");
                            if (regValue == null) {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Framework property {0} is configured to be read from the registry but has no regvalue attribute set.", propertyName));
                            } else {
                                // expand properties in regvalue
                                regValue = frameworkProperties.ExpandProperties(regValue, Location.UnknownLocation);
                            }

                            RegistryKey sdkKey = Registry.LocalMachine.OpenSubKey(regKey);
                            if (sdkKey != null && sdkKey.GetValue(regValue) != null) {
                                propertyValue = sdkKey.GetValue(regValue).ToString();
                            }
                        } else {
                            propertyValue = GetXmlAttributeValue(propertyNode, "value");
                        }

                        if (propertyValue != null) {
                            // expand properties in property value
                            propertyValue = frameworkProperties.ExpandProperties(propertyValue, Location.UnknownLocation);

                            // add read-only property to collection of framework properties
                            frameworkProperties.AddReadOnly(propertyName, propertyValue);
                        }
                    }

                    // create new FrameworkInfo instance, this will throw an
                    // an exception if the framework is not valid
                    FrameworkInfo info = new FrameworkInfo(name, 
                        description,
                        version,
                        frameworkDir, 
                        sdkDir, 
                        frameworkAssemblyDir, 
                        runtimeEngine,
                        frameworkProperties);

                    // framework is valid, so add it for framework dictionary
                    FrameworkInfoDictionary.Add(info.Name, info);
                } catch (Exception ex) {
                    string msg = string.Format(CultureInfo.InvariantCulture, "Framework {0} is invalid and has not been loaded : {1}", name, ex.Message); 
                    Log(Level.Verbose, msg);
                    logger.Info(msg, ex);
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
                    attributeValue = StringUtils.ConvertEmptyToNull(xmlAttribute.Value);
                }
            }

            return attributeValue;
        }
                
        /// <summary>
        /// Loads and processes a settings file from the directory of the current 
        /// <see cref="Assembly" />.
        /// </summary>
        private void ProcessSettings(){
            XmlNode nantNode = ConfigurationSettings.GetConfig("nant") as XmlNode;

            logger.Debug(string.Format(CultureInfo.InvariantCulture, "[{0}].ConfigFile '{1}'",AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));

            if (nantNode == null) { 
                // todo pull a settings file out of the assembly resource and copy to that location
                Log(Level.Warning, "NAnt settings not found. Defaulting to no known framework.");
                logger.Info("NAnt settings not found. Defaulting to no known framework.");
                return;
            }

            // process the framework-neutral properties
            ProcessFrameworkNeutralProperties(nantNode.SelectNodes("frameworks/properties/property"));

            // process the defined frameworks
            ProcessFrameworks(nantNode.SelectNodes("frameworks/framework"));

            // get taskpath setting to load external tasks and types from
            string taskPath = GetXmlAttributeValue(nantNode, "taskpath"); 
            if (taskPath != null && ScannedTaskPath == false) { 
                string[] paths = taskPath.Split(';'); 
                foreach (string path in paths) { 
                    string fullpath = path; 
                    if (!Directory.Exists(path)) { 
                        // try relative path 
                        fullpath = Path.GetFullPath(Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), path)); 
                    } 
                    TypeFactory.ScanDir(fullpath); 
                } 
                ScannedTaskPath = true; // so we only load tasks once 
            } 

            // determine default framework
            string defaultFramework = GetXmlAttributeValue(nantNode.SelectSingleNode("frameworks"), "default");
            if (defaultFramework != null && _frameworkInfoDictionary.ContainsKey(defaultFramework)) {
                Properties.AddReadOnly("nant.settings.defaultframework", defaultFramework);
                Properties.Add("nant.settings.currentframework", defaultFramework);
                
                DefaultFramework = _frameworkInfoDictionary[defaultFramework];
                CurrentFramework = _defaultFramework;
            } else {
                Log(Level.Warning, "Framework '{0}' does not exist or is not specified in the NAnt configuration file. Defaulting to no known framework.", defaultFramework);
            }

            // process global properties
            ProcessGlobalProperties(nantNode.SelectNodes("properties/property"));
        }

        #endregion Settings file Load routines
    }
}
