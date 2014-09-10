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
// Ian MacLean (imaclean@gmail.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

 namespace NAnt.Core.Tasks {
    /// <summary>
    /// Sets an environment variable or a whole collection of them. Use an empty 
    /// <see cref="LiteralValue" /> attribute to clear a variable.
    /// </summary>
    /// <remarks>
    ///   <note>
    ///   Variables will be set for the current NAnt process and all child 
    ///   processes that NAnt spawns (compilers, shell tools, etc). If the 
    ///   intention is to only set a variable for a single child process, then
    ///   using the <see cref="ExecTask" /> and its nested <see cref="ExecTask.EnvironmentSet" /> 
    ///   element might be a better option. 
    ///   </note>
    ///   <note>
    ///   Expansion of inline environment variables is performed using the syntax 
    ///   of the current platform. So on Windows platforms using the string %PATH% 
    ///   in the <see cref="LiteralValue" /> attribute will result in the value of 
    ///   the PATH variable being expanded in place before the variable is set.
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>Set the MONO_PATH environment variable on a *nix platform.</para>
    ///   <code>
    ///     <![CDATA[
    ///     <setenv name=="MONO_PATH" value="/home/jimbob/dev/foo:%MONO_PATH%"/>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Set a collection of environment variables. Note the nested variable used to set var3.</para>
    ///   <code>
    ///     <![CDATA[
    ///     <setenv>
    ///             <variable name="var1" value="value2" />
    ///             <variable name="var2" value="value2" />
    ///             <variable name="var3" value="value3:%var2%" />
    ///     </setenv>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Set environment variables using nested path elements.</para>
    ///   <code>
    ///     <![CDATA[
    ///     <path id="build.path">
    ///            <pathelement dir="c:/windows" />
    ///            <pathelement dir="c:/cygwin/usr/local/bin" />
    ///        </path>
    ///     <setenv>         
    ///             <variable name="build_path" >
    ///                    <path refid="build.path" />
    ///             </variable>
    ///             <variable name="path2">
    ///                <path>
    ///                    <pathelement dir="c:/windows" />
    ///                    <pathelement dir="c:/cygwin/usr/local/bin" />
    ///                </path>
    ///             </variable>
    ///     </setenv>    
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("setenv")]
    public class SetEnvTask : Task {
        #region Private Instance Fields

        private string _name;
        private string _value;
        private string _literalValue;
        private FileInfo _file;
        private DirectoryInfo _directory;
        private PathSet _path;
        private EnvironmentVariableCollection _environmentVariables = new EnvironmentVariableCollection();

        #endregion Private Instance Fields

        #region Private Static Fields

        #endregion Private Static Fields

        #region Public Instance Properties
        
        /// <summary>
        /// The name of a single Environment variable to set
        /// </summary>
        [TaskAttribute("name")]
        public string EnvName {
            get { return _name; }
            set { _name = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The literal value for the environment variable.
        /// </summary>
        [TaskAttribute("value")]
        public string LiteralValue {
            get { return _literalValue; }
            set { 
                _value = value;
                _literalValue = value;
            }
        }

        /// <summary>
        /// The value for a file-based environment variable. NAnt will convert 
        /// it to an absolute filename.
        /// </summary>
        [TaskAttribute("file")]
        public FileInfo File {
            get { return _file; }
            set { 
                _value = value.ToString();
                _file = value;
            }
        }

        /// <summary>
        /// The value for a directory-based environment variable. NAnt will 
        /// convert it to an absolute path.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Directory {
            get { return _directory; }
            set { 
                _value = value.ToString();
                _directory = value;
            }
        }

        /// <summary>
        /// The value for a PATH like environment variable. You can use 
        /// <c>:</c> or <c>;</c> as path separators and NAnt will convert it to 
        /// the platform's local conventions.
        /// </summary>
        [TaskAttribute("path")]
        public PathSet Path {
            get { return _path; }
            set { 
                _value = value.ToString(); 
                _path = value;
            }
        }

        /// <summary>
        /// Gets or sets the environment variables.
        /// </summary>
        /// <value>
        /// The environment variables.
        /// </value>
        [BuildElementArray("variable", ElementType=typeof(EnvironmentVariable))]
        public EnvironmentVariableCollection EnvironmentVariables {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }

        #endregion Public Instance Properties
        
        #region Override implementation of Task

        /// <summary>
        /// Checks whether the task is initialized with valid attributes.
        /// </summary>
        protected override void Initialize() {
            if (EnvName == null && EnvironmentVariables.Count == 0) {
                throw new BuildException("Either the \"name\" attribute or at"
                    + " least one nested <variable> element is required.", 
                    Location);
            }
        }

        /// <summary>
        /// Set the environment variables
        /// </summary>
        protected override void ExecuteTask() {
            if (EnvName != null) {
                // add single environment variable
                EnvironmentVariables.Add(new EnvironmentVariable(EnvName, _value));
            }

            foreach (EnvironmentVariable env in EnvironmentVariables) {
                if (env.IfDefined && !env.UnlessDefined) {
                    SetSingleEnvironmentVariable(env.VariableName, env.Value);
                }
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods
        
        /// <summary>
        /// Do the actual work here.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        private void SetSingleEnvironmentVariable(string name, string value) {
            Log(Level.Verbose, "Setting environment variable \"{0}\" to \"{1}\".", 
                name, value);

            // expand any env vars in value
            string expandedValue = null;
            if (value != null) {
                expandedValue = Environment.ExpandEnvironmentVariables(value);
            }

            try {
                Environment.SetEnvironmentVariable (name, expandedValue);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error setting environment variable \"{0}\" to \"{1}\".", 
                    name, value), Location, ex);
            }
        }
        
        #endregion Private Instance Methods
    }
}
