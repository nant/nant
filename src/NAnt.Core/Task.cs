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
// Mike Krueger (mike@icsharpcode.net)
// Ian MacLean (imaclean@gmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Xml;

using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core {
    /// <summary>
    /// Provides the abstract base class for tasks.
    /// </summary>
    /// <remarks>
    /// A task is a piece of code that can be executed.
    /// </remarks>
    [Serializable()]
    public abstract class Task : Element, IConditional {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Private Instance Fields

        private bool _failOnError = true;
        private bool _verbose;
        private bool _ifDefined = true;
        private bool _unlessDefined;
        private Level _threshold = Level.Debug;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Determines if task failure stops the build, or is just reported. 
        /// The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("failonerror")]
        [BooleanValidator()]
        public bool FailOnError {
            get { return _failOnError; }
            set { _failOnError = value; }
        }

        /// <summary>
        /// Determines whether the task should report detailed build log messages. 
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("verbose")]
        [BooleanValidator()]
        public virtual bool Verbose {
            get { return (_verbose || Project.Verbose); }
            set { _verbose = value; }
        }

        /// <summary>
        /// If <see langword="true" /> then the task will be executed; otherwise, 
        /// skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Opposite of <see cref="IfDefined" />. If <see langword="false" /> 
        /// then the task will be executed; otherwise, skipped. The default is 
        /// <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        /// <summary>
        /// The name of the task.
        /// </summary>
        public override string Name {
            get {
                string name = null;
                TaskNameAttribute taskName = (TaskNameAttribute) Attribute.GetCustomAttribute(GetType(), typeof(TaskNameAttribute));
                if (taskName != null) {
                    name = taskName.Name;
                }
                return name;
            }
        }

        /// <summary>
        /// The prefix used when sending messages to the log.
        /// </summary>
        [Obsolete("Will be removed soon", false)]
        public string LogPrefix {
            get {
                string prefix = "[" + Name + "] ";
                return prefix.PadLeft(Project.IndentationSize);
            }
        }

        /// <summary>
        /// Gets or sets the log threshold for this <see cref="Task" />. By
        /// default the threshold of a task is <see cref="Level.Debug" />,
        /// causing no messages to be filtered in the task itself.
        /// </summary>
        /// <value>
        /// The log threshold level for this <see cref="Task" />.
        /// </value>
        /// <remarks>
        /// When the threshold of a <see cref="Task" /> is higher than the
        /// threshold of the <see cref="Project" />, then all messages will
        /// still be delivered to the build listeners.
        /// </remarks>
        public Level Threshold {
            get { return _threshold; }
            set { _threshold = value; }
        }

        #endregion Public Instance Properties

        #region Internal Instance Properties

        /// <summary>
        /// Returns the TaskBuilder used to construct an instance of this
        /// <see cref="Task" />.
        /// </summary>
        internal TaskBuilder TaskBuilder {
            get {
                return TypeFactory.TaskBuilders [Name];
            }
        }

        #endregion Internal Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Executes the task unless it is skipped.
        /// </summary>
        public void Execute() {
            logger.DebugFormat(CultureInfo.InvariantCulture,
                ResourceUtils.GetString("String_TaskExecute"), 
                Name);

            if (IfDefined && !UnlessDefined) {
                try {
                    Project.OnTaskStarted(this, new BuildEventArgs(this));
                    ExecuteTask();
                } catch (Exception ex) {
                    logger.ErrorFormat(
                        CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1077"), 
                        Name, ex);

                    if (FailOnError) {
                        throw;
                    } else {
                        if (this.Verbose) {
                            // output exception (with stacktrace) to build log
                            Log(Level.Error, ex.ToString());
                        } else {
                            string msg = ex.Message;
                            // get first nested exception
                            Exception nestedException = ex.InnerException;
                            // set initial indentation level for the nested exceptions
                            int exceptionIndentationLevel = 0;
                            // output message of nested exceptions
                            while (nestedException != null && !String.IsNullOrEmpty(nestedException.Message)) {
                                // indent exception message with 4 extra spaces 
                                // (for each nesting level)
                                exceptionIndentationLevel += 4;
                                // start new line for each exception level
                                msg = (msg != null) ? msg + Environment.NewLine : string.Empty;
                                // output exception message
                                msg += new string(' ', exceptionIndentationLevel) 
                                    + nestedException.Message;
                                // move on to next inner exception
                                nestedException = nestedException.InnerException;
                            }

                            // output message of exception(s) to build log
                            Log(Level.Error, msg);
                        }
                    }
                } finally {
                    Project.OnTaskFinished(this, new BuildEventArgs(this));
                }
            }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="format">The message to be logged.</param>
        /// <remarks>
        /// <para>
        /// The actual logging is delegated to the project.
        /// </para>
        /// <para>
        /// If the <see cref="Verbose" /> attribute is set on the task and a
        /// message is logged with level <see cref="Level.Verbose" />, the
        /// priority of the message will be increased to <see cref="Level.Info" />
        /// when the threshold of the build log is <see cref="Level.Info" />.
        /// </para>
        /// <para>
        /// This will allow individual tasks to run in verbose mode while
        /// the build log itself is still configured with threshold 
        /// <see cref="Level.Info" />.
        /// </para>
        /// <para>
        /// The threshold of the project is not taken into account to determine
        /// whether a message should be passed to the logging infrastructure, 
        /// as build listeners might be interested in receiving all messages.
        /// </para>
        /// </remarks>
        public override void Log(Level messageLevel, string format) {
            if (!IsLogEnabledFor(messageLevel)) {
                return;
            }

            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
                Project.Log(this, Level.Info, format);
            } else {
                Project.Log(this, messageLevel, format);
            }
        }

        /// <summary>
        /// Logs a formatted message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="format">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// <para>
        /// The actual logging is delegated to the project.
        /// </para>
        /// <para>
        /// If the <see cref="Verbose" /> attribute is set on the task and a 
        /// message is logged with level <see cref="Level.Verbose" />, the 
        /// priority of the message will be increased to <see cref="Level.Info" />.
        /// when the threshold of the build log is <see cref="Level.Info" />.
        /// </para>
        /// <para>
        /// This will allow individual tasks to run in verbose mode while
        /// the build log itself is still configured with threshold 
        /// <see cref="Level.Info" />.
        /// </para>
        /// </remarks>
        public override void Log(Level messageLevel, string format, params object[] args) {
            if (!IsLogEnabledFor(messageLevel))
            {
                return;
            }

            string logMessage = string.Format(CultureInfo.InvariantCulture, format, args);
            Log(messageLevel, logMessage);
        }

        /// <summary>
        /// Determines whether build output is enabled for the given 
        /// <see cref="Level" />.
        /// </summary>
        /// <param name="messageLevel">The <see cref="Level" /> to check.</param>
        /// <returns>
        /// <see langword="true" /> if messages with the given <see cref="Level" />
        /// should be passed on to the logging infrastructure; otherwise, 
        /// <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// The threshold of the project is not taken into account to determine
        /// whether a message should be passed to the logging infrastructure, 
        /// as build listeners might be interested in receiving all messages.
        /// </remarks>
        public bool IsLogEnabledFor(Level messageLevel) {
            if (_verbose && messageLevel == Level.Verbose) {
                return Level.Info >= Threshold;
            }

            return (messageLevel >= Threshold);
        }

        /// <summary>
        /// Initializes the configuration of the task using configuration 
        /// settings retrieved from the NAnt configuration file.
        /// </summary>
        /// <remarks>
        /// TO-DO : Remove this temporary hack when a permanent solution is 
        /// available for loading the default values from the configuration
        /// file if a build element is constructed from code.
        /// </remarks>
        public void InitializeTaskConfiguration() {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo propertyInfo in properties) {
                XmlNode attributeNode = null;
                string attributeValue = null;

                FrameworkConfigurableAttribute frameworkAttribute = (FrameworkConfigurableAttribute) 
                    Attribute.GetCustomAttribute(propertyInfo, typeof(FrameworkConfigurableAttribute));

                if (frameworkAttribute != null) {
                    // locate XML configuration node for current attribute
                    attributeNode = GetAttributeConfigurationNode(
                        Project.TargetFramework, frameworkAttribute.Name);

                    if (attributeNode != null) {
                        // get the configured value
                        attributeValue = attributeNode.InnerText;

                        if (frameworkAttribute.ExpandProperties && Project.TargetFramework != null) {
                            try {
                                // expand attribute properites
                                attributeValue = Project.TargetFramework.Project.Properties.ExpandProperties(
                                    attributeValue, Location);
                            } catch (Exception ex) {
                                // throw BuildException if required
                                if (frameworkAttribute.Required) {
                                    throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                        ResourceUtils.GetString("NA1075"), frameworkAttribute.Name, Name), Location, ex);
                                }

                                // set value to null
                                attributeValue = null;
                            }
                        }
                    } else {
                        // check if its required
                        if (frameworkAttribute.Required) {
                            throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                "'{0}' is a required framework configuration setting for the '{1}'" 
                                + " build element that should be set in the NAnt configuration file.", 
                                frameworkAttribute.Name, Name), Location);
                        }
                    }

                    if (attributeValue != null) {
                        if (propertyInfo.CanWrite) {
                            Type propertyType = propertyInfo.PropertyType;

                            //validate attribute value with custom ValidatorAttribute(ors)
                            object[] validateAttributes = (ValidatorAttribute[]) 
                                Attribute.GetCustomAttributes(propertyInfo, typeof(ValidatorAttribute));
                            try {
                                foreach (ValidatorAttribute validator in validateAttributes) {
                                    logger.InfoFormat(CultureInfo.InvariantCulture,
                                        ResourceUtils.GetString("NA1074"), 
                                        attributeValue, Name, validator.GetType().Name);

                                    validator.Validate(attributeValue);
                                }
                            } catch (ValidationException ve) {
                                logger.Error("Validation Exception", ve);
                                throw new ValidationException("Validation failed on" + propertyInfo.DeclaringType.FullName, Location, ve);
                            }

                            // holds the attribute value converted to the property type
                            object propertyValue = null;

                            // If the object is an enum
                            if (propertyType.IsEnum) {
                                try {
                                    TypeConverter tc = TypeDescriptor.GetConverter(propertyType);
                                    if (!(tc.GetType() == typeof(EnumConverter))) {
                                        propertyValue = tc.ConvertFrom(attributeValue);
                                    } else {
                                        propertyValue = Enum.Parse(propertyType, attributeValue);
                                    }
                                } catch (Exception) {
                                    // catch type conversion exceptions here
                                    string message = "Invalid configuration value \"" + attributeValue + "\". Valid values for this attribute are: ";
                                    foreach (object value in Enum.GetValues(propertyType)) {
                                        message += value.ToString() + ", ";
                                    }
                                    // strip last ,
                                    message = message.Substring(0, message.Length - 2);
                                    throw new BuildException(message, Location);
                                }
                            } else {
                                propertyValue = Convert.ChangeType(attributeValue, propertyInfo.PropertyType, CultureInfo.InvariantCulture);
                            }

                            //set property value
                            propertyInfo.SetValue(this, propertyValue, BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.InvariantCulture);
                        }
                    }
                }
            }
        }

        #endregion Public Instance Methods

        #region Protected Instance Methods

        /// <summary>Initializes the task.</summary>
        protected override void Initialize() {
            // Just defer for now so that everything just works
            InitializeTask(XmlNode);
        }

        /// <summary>Initializes the task.</summary>
        [Obsolete("Deprecated. Use Initialize() instead")]
        protected virtual void InitializeTask(XmlNode taskNode) {
        }

        /// <summary>Executes the task.</summary>
        protected abstract void ExecuteTask();

        /// <summary>
        /// Locates the XML node for the specified attribute in either the
        /// configuration section of the extension assembly or the.project.
        /// </summary>
        /// <param name="attributeName">The name of attribute for which the XML configuration node should be located.</param>
        /// <param name="framework">The framework to use to obtain framework specific information, or <see langword="null" /> if no framework specific information should be used.</param>
        /// <returns>
        /// The XML configuration node for the specified attribute, or 
        /// <see langword="null" /> if no corresponding XML node could be 
        /// located.
        /// </returns>
        /// <remarks>
        /// If there's a valid current framework, the configuration section for
        /// that framework will first be searched.  If no corresponding 
        /// configuration node can be located in that section, the framework-neutral
        /// section of the project configuration node will be searched.
        /// </remarks>
        protected override XmlNode GetAttributeConfigurationNode(FrameworkInfo framework, string attributeName) {
            XmlNode extensionConfig = TaskBuilder.ExtensionAssembly.ConfigurationSection;
            if (extensionConfig != null) {
                return base.GetAttributeConfigurationNode(extensionConfig,
                    framework, attributeName);
            }
            return base.GetAttributeConfigurationNode(framework, attributeName);
        }

        #endregion Protected Instance Methods
    }
}