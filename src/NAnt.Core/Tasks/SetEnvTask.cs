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

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Sets an environment variable or a whole collection of them. Use an empty value attribute
    /// to clear a variable.
    /// </summary>
    ///   <remarks>    
    ///     <note>
    ///	Variables will be set for the current NAnt process and all child processes 
    ///	that NAnt spawns ( compilers, shell tools etc ). If the intention is to only 
    ///	set a variable for a single child process then using the <see cref="ExecTask" /> 
    ///	and its nested <see cref="ExecTask.Environment" /> element might be a better option. 
    ///     </note>
    ///     <note>
    ///Expansion of inline environment variables is performed using the syntax of the current 
    ///platform. So on Windows platforms using the string %PATH% in value attribute will result 
    ///in the value of the PATH variable being expanded in place before the variable is set.      
    ///     </note>
    ///   </remarks>
    /// <example>
    ///   <para>Set the MONO_PATH environment variable on a *nix platform.</para>
    ///   <code>
    ///     <![CDATA[
    ///     <setenv name=="MONO_PATH" value="/home/jimbob/dev/foo:$MONO_PATH"/>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Set a collection of environment variables. Note the nested variable used to set var3.</para>
    ///   <code>
    ///     <![CDATA[
    ///     <setenv >
    ///         <environment>
    ///             <option name="var1" value="value2" />
    ///             <option name="var2" value="value2" />
    ///             <option name="var3" value="value3:$var2" />
    ///         </environment>
    ///     </setenv>
    ///     ]]>
    ///   </code>
    /// </example>
    /// <para>
    /// </para>
    /// <example>
    /// </example>
    [TaskName("setenv")]
    public class SetEnvTask : Task {
        
        #region Protected Static Fields
        
        static string _matchEnvRegex = @"\$[a-zA-Z][a-zA-Z0-9_]+";
        static string _matchEnvRegexWin = @"%[a-zA-Z][a-zA-Z0-9_]+%"; 
        
        #endregion Protected Static Fields

        #region Private Instance Fields
        
        private string _name;
        private string _value;
        private OptionCollection _environment = new OptionCollection();

        #endregion Private Instance Fields

        #region Public Instance Properties
        
        /// <summary>
        /// The name of a single Environment variable to set
        /// </summary>
        [TaskAttribute("name", Required=false)]
        public string EnvName {
            get { return _name; }
            set { _name = StringUtils.ConvertEmptyToNull(value); }
        }
        
        /// <summary>
        /// The value of a single Environment variable to set
        /// </summary>
        [TaskAttribute("value", Required=false)]
        public string EnvValue {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// Collection of Environment variables to set.
        /// </summary>
        [BuildElementCollection("environment", "option")]
        public OptionCollection EnvironmentCollection {
            get { return _environment; }
        }

        #endregion Public Instance Properties
        
        #region DllImports
        /// <summary>
        /// Win32 DllImport for the SetEnvironmentVariable function.
        /// </summary>
        /// <param name="lpName"></param>
        /// <param name="lpValue"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError=true)]
        static extern bool SetEnvironmentVariable(string lpName, string lpValue);
                
        /// <summary>
        /// *nix dllimport for the setenv function.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="overwrite"></param>
        /// <returns></returns>
        [DllImport("libc")]
        static extern int setenv(string name, string value, int overwrite);
        
        #endregion DllImports
        
        #region Override implementation of Task
        
        /// <summary>
        /// Checks whether the task is initialized with valid attributes.
        /// </summary>
        /// <param name="taskNode"></param>
        protected override void InitializeTask(XmlNode taskNode) {
            if ( EnvName == null && EnvironmentCollection.Count == 0) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Either the name attribute or at least one nested <option> element is required."), Location );
            }
        }
        /// <summary>
        /// Set the environment variables
        /// </summary>
        protected override void ExecuteTask() {
            
            if (EnvName != null && EnvValue != null ) {
                // add the single env var 
                EnvironmentCollection.Add(new Option( EnvName, EnvValue ));
            }
            
            foreach( Option option in EnvironmentCollection ) {
                SetSingleEnvironmentVariable( option.OptionName, option.Value );
            }
        }
        
        #endregion Override implementation of Task

        #region Private Instance Methods
        
        /// <summary>
        /// Do the actual work here.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void SetSingleEnvironmentVariable(string name, string value) {
            
            Log(Level.Verbose, "Setting env var {0} to {1}", name, value);
            bool result;
            
            // expand any env vars in value
            string expandedValue = ExpandEnvironmentStrings ( value);
            if ( PlatformHelper.IsWin32 ) {
                result = SetEnvironmentVariable(name, expandedValue);
            } 
                else if (PlatformHelper.IsUnix){
                    result = setenv(name, expandedValue, 1) == 0 ? true : false; 
            } 
            else {
                throw new BuildException("Setenv not defined on this platform", Location );
            }
            if ( result != true  ) {
                throw new BuildException(string.Format("Error setting env var {0} to {1}", name, value ) , Location);
            }
        }
        /// <summary>
        /// callback to process environment variable matches.
        /// </summary>
        /// <param name="m"></param>
        /// <returns>The value of the matched env var if it exists otherwise the value of the match</returns>
        private string ExpandEnvVarMatchCallback(Match m) {
            string envVar = m.ToString();
            
            Match match = Regex.Match(m.ToString(), @"[a-zA-Z][a-zA-Z0-9_]+");
            string envName = match.Captures[0].ToString();
            string envValue = Environment.GetEnvironmentVariable(envName);
            if (envValue != null) {
                return envValue;
            } else {
                return envVar;
            }
        }
        /// <summary>
        /// Expand any inline environment variables in the passed string.
        /// The standard env var syntax for the current platform is used.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>The source string with any env vars expanded</returns>
        private string ExpandEnvironmentStrings(string text) {
            string matchEnvRegexWin = "";
            if ( PlatformHelper.IsWin32 ) {
                matchEnvRegexWin = _matchEnvRegexWin;
            } else {
                matchEnvRegexWin = _matchEnvRegex;
            }
            return Regex.Replace(text, matchEnvRegexWin, new MatchEvaluator(this.ExpandEnvVarMatchCallback));
        }
        #endregion Private Instance Methods
    }
}
