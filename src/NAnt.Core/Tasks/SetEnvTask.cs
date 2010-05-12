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
// Gert Driesen (driesen@users.sourceforge.net)

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

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

#if !NET_2_0
        private const int ERROR_ENVVAR_NOT_FOUND = 203;
#endif

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
                
        [BuildElementArray("variable", ElementType=typeof(EnvironmentVariable))]
        public EnvironmentVariableCollection EnvironmentVariables {
            get { return _environmentVariables; }
            set { _environmentVariables = value; }
        }

        #endregion Public Instance Properties
        
        #region DllImports

#if !NET_2_0
        /// <summary>
        /// Win32 DllImport for the SetEnvironmentVariable function.
        /// </summary>
        /// <param name="lpName"></param>
        /// <param name="lpValue"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError=true)]
        private static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        /// <summary>
        /// *nix dllimport for the setenv function.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="overwrite"></param>
        /// <returns>
        /// <c>0</c> if the execution is successful; otherwise, <c>-1</c>.
        /// </returns>
        [DllImport("libc")]
        private static extern int setenv(string name, string value, int overwrite);

        /// <summary>
        /// Deletes all instances of the variable name.
        /// </summary>
        /// <param name="name">The variable to unset.</param>
        /// <returns>
        /// <c>0</c> if the execution is successful; otherwise, <c>-1</c>.
        /// </returns>
        [DllImport("libc")]
        private static extern int unsetenv(string name);
#endif

        #endregion DllImports

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
            // If value is null or empty (""), keep the expanded variable null.
            // This will prevent the SetEnvironmentVariable method (both from 
            // kernel.dll (.NET 1.0) and System.Environment (.NET 2.0+)) from
            // assigning a env var an empty value.  Seems to be an issue that
            // was introduced with .NET 4.0
            string expandedValue = null;
            if (!StringUtils.IsNullOrEmpty(value)) {
                expandedValue = Environment.ExpandEnvironmentVariables(value);
            }

#if NET_2_0
            try {
                Environment.SetEnvironmentVariable (name, expandedValue);
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Error setting environment variable \"{0}\" to \"{1}\".", 
                    name, value), Location, ex);
            }
#else
            bool result;

            // set the environment variable
            if (PlatformHelper.IsUnix) {
                if (expandedValue == null || expandedValue.Length == 0 || expandedValue [0] == '\0') {
                    result = unsetenv(name) == 0;
                } else {
                    result = setenv(name, expandedValue, 1) == 0;
                }

                if (!result) {
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Error setting environment variable \"{0}\" to \"{1}\".", 
                        name, value), Location);
                }
            } else {
                result = SetEnvironmentVariable(name, expandedValue);
                if (!result) {
                    int error = Marshal.GetLastWin32Error ();
                    if (error == ERROR_ENVVAR_NOT_FOUND) {
                        // attempt to delete environment variable that does not
                        // exist
                        return;
                    }
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                        "Error setting environment variable \"{0}\" to \"{1}\".", 
                        name, value), Location, new Win32Exception (error));
                }
            }
#endif
        }
        
        #endregion Private Instance Methods
    }
}
