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
// Ian MacLean (ian_maclean@another.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.Reflection;
using System.Xml;

using NAnt.Core.Attributes;

namespace NAnt.Core {
    /// <summary>
    /// Provides the abstract base class for tasks.
    /// </summary>
    /// <remarks>A task is a piece of code that can be executed.</remarks>
    public abstract class Task : Element {
        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Private Static Fields

        #region Private Instance Fields

        bool _failOnError = true;
        bool _verbose = false;
        bool _ifDefined = true;
        bool _unlessDefined = false;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Determines if task failure stops the build, or is just reported. Default is "true".
        /// </summary>
        [TaskAttribute("failonerror")]
        [BooleanValidator()]
        public bool FailOnError {
            get { return _failOnError; }
            set { _failOnError = value; }
        }

        /// <summary>
        /// Task reports detailed build log messages.  Default is "false".
        /// </summary>
        [TaskAttribute("verbose")]
        [BooleanValidator()]
        public bool Verbose {
            get { return (_verbose || Project.Verbose); }
            set { _verbose = value; }
        }

        /// <summary>
        /// If true then the task will be executed; otherwise skipped. Default is "true".
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Opposite of if.  If false then the task will be executed; otherwise skipped. Default is "false".
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
        public string LogPrefix {
            get {
                string prefix = "[" + Name + "] ";
                return prefix.PadLeft(Project.IndentationSize);
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Executes the task unless it is skipped.
        /// </summary>
        public void Execute() {
            logger.Debug(string.Format(
                CultureInfo.InvariantCulture,
                "Task.Execute() for '{0}'", 
                Name));
                
            if (IfDefined && !UnlessDefined) {
                try {
                    Project.OnTaskStarted(this, new BuildEventArgs(this));
                    ExecuteTask();
                } catch (Exception e) {
                    logger.Error(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} Generated Exception", 
                        Name), e);

                    if (FailOnError) {
                        throw;
                    } else {
                        if (this.Verbose) {
                            Log(Level.Error, LogPrefix + e.ToString());
                        } else {
                            Log(Level.Error, LogPrefix + e.Message);
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
        /// <param name="message">The message to be logged.</param>
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
        public override void Log(Level messageLevel, string message) {
            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
                Project.Log(this, Level.Info, message);
            } else {
                Project.Log(this, messageLevel, message);
            }
        }

        /// <summary>
        /// Logs a formatted message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
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
        public override void Log(Level messageLevel, string message, params object[] args) {
            string logMessage = string.Format(CultureInfo.InvariantCulture, message, args);

            if (_verbose && messageLevel == Level.Verbose && Project.Threshold == Level.Info) {
                Project.Log(this, Level.Info, logMessage);
            } else {
                Project.Log(this, messageLevel, logMessage);
            }
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
                    attributeNode = GetAttributeConfigurationNode(frameworkAttribute.Name);

                    if (attributeNode != null) {
                        // get the configured value
                        attributeValue = attributeNode.InnerText;

                        if (frameworkAttribute.ExpandProperties && Project.CurrentFramework != null) {
                            // expand attribute properites
                            try {
                                attributeValue = Project.CurrentFramework.Properties.ExpandProperties(attributeValue, Location);
                            } catch (Exception ex) {
                                // throw BuildException if required
                                if (frameworkAttribute.Required) {
                                    throw new BuildException(String.Format(CultureInfo.InvariantCulture, "'{0}' is a required framework configuration setting for the '{1}' build element that should be set in the NAnt configuration file.", frameworkAttribute.Name, this.Name), Location, ex);
                                }

                                // set value to null
                                attributeValue = null;
                            }
                        }
                    } else {
                        // check if its required
                        if (frameworkAttribute.Required) {
                            throw new BuildException(String.Format(CultureInfo.InvariantCulture, "'{0}' is a required framework configuration setting for the '{1}' build element that should be set in the NAnt configuration file.", frameworkAttribute.Name, this.Name), Location);
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
                                    logger.Info(string.Format(
                                        CultureInfo.InvariantCulture,
                                        "Configuration value {0} for task {1} was not considered valid by {2}.", 
                                        attributeValue, Name, validator.GetType().Name));

                                    validator.Validate(attributeValue);
                                }
                            } catch (ValidationException ve) {
                                logger.Error("Validation Exception", ve);
                                throw new ValidationException("Validation failed on" + propertyInfo.DeclaringType.FullName, Location, ve);
                            }

                            // holds the attribute value converted to the property type
                            object propertyValue = null;

                            // If the object is an emum
                            if (propertyType.IsEnum) {
                                try {
                                    propertyValue = Enum.Parse(propertyType, attributeValue);
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

        /// <summary>
        /// Sets a string value ensuring that it will be null if an empty string 
        /// is passed.
        /// </summary>
        protected string SetStringValue(string value) {
            if (value != null && value.Trim().Length != 0) {
                return value;
            } else {
                return null;
            }
        }
        
        /// <summary><note>Deprecated (to be deleted).</note></summary>
        [Obsolete("Deprecated- Use InitializeTask instead")]
        protected override void InitializeElement(XmlNode elementNode) {
            // Just defer for now so that everything just works
            InitializeTask(elementNode);
        }

        /// <summary>Initializes the task.</summary>
        protected virtual void InitializeTask(XmlNode taskNode) {
        }

        /// <summary>Executes the task.</summary>
        protected abstract void ExecuteTask();

        #endregion Protected Instance Methods
    }
}