// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Text;

namespace SourceForge.NAnt {
    /// <summary>
    /// Commandline parser.
    /// </summary>
    public class CommandLineParser {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandLineParser" /> class
        /// using possible arguments deducted from the specific <see cref="Type" />.
        /// </summary>
        /// <param name="argumentSpecification">The <see cref="Type" /> from which the possible commandline arguments should be retrieved.</param>
        /// <exception cref="ArgumentNullException"><paramref name="argumentSpecification" /> is a null reference.</exception>
        public CommandLineParser(Type argumentSpecification) {
            if (argumentSpecification == null) {
                throw new ArgumentNullException("argumentSpecification");
            }

            this._argumentCollection = new CommandLineArgumentCollection();

            foreach (PropertyInfo propertyInfo in argumentSpecification.GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
                if (propertyInfo.CanWrite) {
                    CommandLineArgumentAttribute attribute = GetCommandLineAttribute(propertyInfo);
                    if (attribute is DefaultCommandLineArgumentAttribute) {
                        Debug.Assert(this._defaultArgument == null);
                        this._defaultArgument = new CommandLineArgument(attribute, propertyInfo);
                    } else if (attribute != null) {
                        this._argumentCollection.Add(new CommandLineArgument(attribute, propertyInfo));
                    }
                }
            }

            this._argumentSpecification = argumentSpecification;
        }
        
        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets a logo banner using version and copyright attributes defined on the 
        /// <see cref="Assembly.GetEntryAssembly()" /> or the 
        /// <see cref="Assembly.GetCallingAssembly()" />.
        /// </summary>
        /// <value>A logo banner.</value>
        public virtual string LogoBanner {
            get {
                StringBuilder logoBanner = new StringBuilder();
                Assembly assembly = Assembly.GetEntryAssembly();
                if (assembly == null) {
                    assembly = Assembly.GetCallingAssembly();
                }

                #region Add description to logo banner

                object[] productAttributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (productAttributes.Length > 0) {
                    AssemblyProductAttribute productAttribute = (AssemblyProductAttribute) productAttributes[0];
                    if (productAttribute.Product != null && productAttribute.Product.Length != 0) {
                        logoBanner.Append(productAttribute.Product);
                    }
                } else {
                    logoBanner.Append(assembly.GetName().Name);
                }

                #endregion

                #region Add version information to logo banner

                object[] informationalVersionAttributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                if (informationalVersionAttributes.Length > 0) {
                    AssemblyInformationalVersionAttribute versionAttribute = (AssemblyInformationalVersionAttribute) informationalVersionAttributes[0];
                    if (versionAttribute.InformationalVersion != null && versionAttribute.InformationalVersion.Length != 0) {
                        logoBanner.Append(" version " + versionAttribute.InformationalVersion);
                    }
                } else {
                    FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                    logoBanner.Append(" version " + info.FileMajorPart + "." + info.FileMinorPart + "." + info.FileBuildPart);
                }

                #endregion

                #region Add copyright information to logo banner

                object[] copyrightAttributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (copyrightAttributes.Length > 0) {
                    AssemblyCopyrightAttribute copyrightAttribute = (AssemblyCopyrightAttribute) copyrightAttributes[0];
                    if (copyrightAttribute.Copyright != null && copyrightAttribute.Copyright.Length != 0) {
                        logoBanner.Append(" " + copyrightAttribute.Copyright);
                    }
                }

                logoBanner.Append('\n');

                #endregion

                return logoBanner.ToString();
            }
        }

        /// <summary>
        /// Gets the usage instructions.
        /// </summary>
        /// <value>The usage instructions.</value>
        public virtual string Usage {
            get {
                StringBuilder helpText = new StringBuilder();
                Assembly assembly = Assembly.GetEntryAssembly();
                if (assembly == null) {
                    assembly = Assembly.GetCallingAssembly();
                }

                #region Add usage instructions to helptext

                if (helpText.Length > 0) {
                    helpText.Append('\n');
                }

                helpText.Append("Usage : " + assembly.GetName().Name + " [options]");

                if (this._defaultArgument != null) {
                    helpText.Append(" <" + this._defaultArgument.LongName + ">");
                    if (this._defaultArgument.AllowMultiple) {
                        helpText.Append(" <" + this._defaultArgument.LongName + ">");
                        helpText.Append(" ...");
                    }
                }

                helpText.Append('\n');

                #endregion

                #region Add options to helptext

                helpText.Append("Options : ");
                helpText.Append('\n');
                helpText.Append('\n');

                foreach (CommandLineArgument argument in this._argumentCollection) {
                    string valType = "";

                    if (argument.ValueType == typeof(string)) {
                        valType = ":<text>";
                    } else if (argument.ValueType == typeof(bool)) {
                        valType = "[+|-]";
                    } else if (argument.ValueType == typeof(FileInfo)) {
                        valType = ":<filename>";
                    } else if (argument.ValueType == typeof(int)) {
                        valType = ":<number>";
                    } else {
                        valType = ":" + argument.ValueType.FullName;
                    }

                    string optionName = argument.LongName;

                    if (argument.ShortName != null) {
                        if (argument.LongName.StartsWith(argument.ShortName)) {
                            optionName = optionName.Insert(argument.ShortName.Length, "[") + "]";
                        }
                        helpText.AppendFormat(CultureInfo.InvariantCulture, "  -{0,-30}{1}", optionName + valType, argument.Description);

                        if (!optionName.StartsWith(argument.ShortName)) {
                            helpText.AppendFormat(CultureInfo.InvariantCulture, " (Short format: /{0})", argument.ShortName);
                        }
                    } else {
                        helpText.AppendFormat(CultureInfo.InvariantCulture, "  -{0,-30}{1}", optionName + valType, argument.Description);
                    }
                    helpText.Append('\n');
                }

                #endregion

                return helpText.ToString();
            }
        }

        /// <summary>
        /// Gets a value indicating whether no arguments were specified on the
        /// commandline.
        /// </summary>
        public bool NoArgs {
            get {
                foreach(CommandLineArgument argument in this._argumentCollection) {
                    if (argument.SeenValue) {
                        return true;
                    }
                }

                if (this._defaultArgument != null) {
                    return this._defaultArgument.SeenValue;
                }

                return false;
            }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Parses an argument list.
        /// </summary>
        /// <param name="args">The arguments to parse.</param>
        /// <param name="destination">The destination object on which properties will be set corresponding to the specified arguments.</param>
        /// <exception cref="ArgumentNullException"><paramref name="destination" /> is a null reference.</exception>
        /// <exception cref="ArgumentException">The <see cref="Type" /> of <paramref name="destination" /> does not match the argument specification that was used to initialize the parser.</exception>
        public void Parse(string[] args, object destination) {
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }

            if (!this._argumentSpecification.IsAssignableFrom(destination.GetType())) {
                throw new ArgumentException("Type of destination does not match type of argument specification.");
            }

            ParseArgumentList(args);

            // check for missing required arguments
            foreach (CommandLineArgument arg in this._argumentCollection) {
                arg.Finish(destination);
            }

            if (this._defaultArgument != null) {
                this._defaultArgument.Finish(destination);
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private void ParseArgumentList(string[] args) {
            if (args != null) {
                foreach (string argument in args) {
                    if (argument.Length > 0) {
                        switch (argument[0]) {
                            case '-':
                            case '/':
                                int endIndex = argument.IndexOfAny(new char[] {':', '+', '-'}, 1);
                                string option = argument.Substring(1, endIndex == -1 ? argument.Length - 1 : endIndex - 1);
                                string optionArgument;

                                if (option.Length + 1 == argument.Length) {
                                    optionArgument = null;
                                } else if (argument.Length > 1 + option.Length && argument[1 + option.Length] == ':') {
                                    optionArgument = argument.Substring(option.Length + 2);
                                } else {
                                    optionArgument = argument.Substring(option.Length + 1);
                                }
                                
                                CommandLineArgument arg = this._argumentCollection[option];
                                if (arg == null) {
                                    throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown argument '{0}'", argument));
                                } else {
                                    if (arg.IsExclusive && args.Length > 1) {
                                        throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Commandline argument '-{0}' cannot be combined with other arguments.", arg.LongName));
                                    } else {
                                        arg.SetValue(optionArgument);
                                    }
                                }
                                break;
                            default:
                                if (this._defaultArgument != null) {
                                    this._defaultArgument.SetValue(argument);
                                } else {
                                    throw new CommandLineArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown argument '{0}'", argument));
                                }
                                break;
                        }
                    }
                }
            }
        }

        #endregion Private Instance Methods

        #region Private Static Methods

        /// <summary>
        /// Returns the <see cref="CommandLineArgumentAttribute" /> that's applied 
        /// on the specified property.
        /// </summary>
        /// <param name="propertyInfo">The property of which applied <see cref="CommandLineArgumentAttribute" /> should be returned.</param>
        /// <returns>
        /// The <see cref="CommandLineArgumentAttribute" /> that's applied to the 
        /// <paramref name="propertyInfo" />, or a null reference if none was applied.
        /// </returns>
        private static CommandLineArgumentAttribute GetCommandLineAttribute(PropertyInfo propertyInfo) {
            object[] attributes = propertyInfo.GetCustomAttributes(typeof(CommandLineArgumentAttribute), false);
            if (attributes.Length == 1)
                return (CommandLineArgumentAttribute) attributes[0];

            Debug.Assert(attributes.Length == 0);
            return null;
        }

        #endregion Private Static Methods

        #region Private Instance Fields

        private CommandLineArgumentCollection _argumentCollection; 
        private CommandLineArgument _defaultArgument;
        private Type _argumentSpecification;

        #endregion Private Instance Fields
    }
}
