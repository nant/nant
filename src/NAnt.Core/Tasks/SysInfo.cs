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

using System;
using System.Collections;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core.Functions;
using System.Text.RegularExpressions;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Sets properties with system information.
    /// </summary>
    /// <remarks>
    ///   <para>Sets a number of properties with information about the system environment.  The intent of this task is for nightly build logs to have a record of system information so that the build was performed on.</para>
    ///   <list type="table">
    ///     <listheader>
    ///       <term>Property</term>
    ///       <description>Value</description>
    ///     </listheader>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.clr.version</term>
    ///       <description>Common Language Runtime version number.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.env.*</term>
    ///       <description>Environment variables (e.g., &lt;<see cref="Prefix" />&gt;.env.PATH). Note that on x64 machines, variable's names containing "(x86)" will contain ".x86" instead (e.g., &lt;<see cref="Prefix" />&gt;.env.ProgramFiles.x86).</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.platform</term>
    ///       <description>Operating system platform ID.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.version</term>
    ///       <description>Operating system version.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os</term>
    ///       <description>Operating system version string.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.applicationdata</term>
    ///       <description>The directory that serves as a common repository for application-specific data for the current roaming user.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.commonapplicationdata</term>
    ///       <description>The directory that serves as a common repository for application-specific data that is used by all users.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.commonprogramfiles</term>
    ///       <description>The directory for components that are shared across applications.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.desktopdirectory</term>
    ///       <description>The directory used to physically store file objects on the desktop. Do not confuse this directory with the desktop folder itself, which is a virtual folder.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.programfiles</term>
    ///       <description>The Program Files directory.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.system</term>
    ///       <description>The System directory.</description>
    ///     </item>
    ///     <item>
    ///       <term>&lt;<see cref="Prefix" />&gt;.os.folder.temp</term>
    ///       <description>The temporary directory.</description>
    ///     </item>
    ///   </list>
    ///   <para>
    ///   When the name of an environment variable contains characters that are not allowed 
    ///   in a property name, the task will use a property name where each of such characters
    ///   is replaced with an underscore (_).
    ///   </para>
    ///   <para>
    ///   Moreover when the name of an environment variable ends with the string "(x86)" the name
    ///   of the property that is defined by this task will end with ".x86" instead.
    ///   </para>
    ///   <para>
    ///   For example the environment variable "ProgramFiles(x86)" will become "sys.env.ProgramFiles.x86"
    ///   but an environment variable named "Program(x86)Files" would become "sys.env.Program_x86_Files".
    ///   </para>
    ///   <note>
    ///   we advise you to use the following functions instead:
    ///   </note>
    ///   <list type="table">
    ///     <listheader>
    ///         <term>Function</term>
    ///         <description>Description</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="EnvironmentFunctions.GetOperatingSystem()" /></term>
    ///         <description>Gets a <see cref="OperatingSystem" /> object that identifies this operating system.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="EnvironmentFunctions.GetFolderPath(Environment.SpecialFolder)" /></term>
    ///         <description>Gets the path to a system special folder.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="EnvironmentFunctions.GetVariable(string)" /></term>
    ///         <description>Returns the value of a environment variable.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="PathFunctions.GetTempPath()" /></term>
    ///         <description>Gets the path to the temporary directory.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="EnvironmentFunctions.GetVersion()" /></term>
    ///         <description>Gets the Common Language Runtime version.</description>
    ///     </item>
    ///   </list>  
    /// </remarks>
    /// <example>
    ///   <para>Register the properties with the default property prefix.</para>
    ///   <code>
    ///     <![CDATA[
    /// <sysinfo />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Register the properties without a prefix.</para>
    ///   <code>
    ///     <![CDATA[
    /// <sysinfo prefix="" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>Register properties and display a summary.</para>
    ///   <code>
    ///     <![CDATA[
    /// <sysinfo verbose="true" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("sysinfo")]
    public class SysInfoTask : Task {
        #region Private Instance Fields

        private string _prefix = "sys.";

        #endregion Private Instance Fields

        #region Public Instance Properties
       
        /// <summary>
        /// The string to prefix the property names with. The default is "sys.".
        /// </summary>
        [TaskAttribute("prefix", Required=false)]
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Executes the task.
        /// </summary>
        protected override void ExecuteTask() {
            Log(Level.Info, "Setting system information properties under {0}*", Prefix);

            // set properties
            Properties[Prefix + "clr.version"] = Environment.Version.ToString();
            Properties[Prefix + "os.platform"] = Environment.OSVersion.Platform.ToString(CultureInfo.InvariantCulture);
            Properties[Prefix + "os.version"]  = Environment.OSVersion.Version.ToString();
            Properties[Prefix + "os.folder.applicationdata"] = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);  
            Properties[Prefix + "os.folder.commonapplicationData"] = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);  
            Properties[Prefix + "os.folder.commonprogramFiles"] = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);  
            Properties[Prefix + "os.folder.desktopdirectory"] = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);                          
            Properties[Prefix + "os.folder.programfiles"] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);                        
            Properties[Prefix + "os.folder.system"] = Environment.GetFolderPath(Environment.SpecialFolder.System);                                   
            Properties[Prefix + "os.folder.temp"] = Path.GetTempPath();
            Properties[Prefix + "os"] = Environment.OSVersion.ToString();

            // set environment variables
            IDictionary variables = Environment.GetEnvironmentVariables();
            foreach (string name in variables.Keys) {
                try {
                    string safeName = name.EndsWith("(x86)") ? name.Replace("(x86)", ".x86") : name;    // since on 64bit Windows provide such variable names, let's make them nice
                    safeName = Regex.Replace(safeName, "[^_A-Za-z0-9\\-.]", "_");      // see PropertyDictionary.ValidatePropertyName
                    Properties[Prefix + "env." + safeName] = (string)variables[name];
                } catch (Exception ex) {
                    if (!FailOnError) {
                        Log(Level.Warning, "Property could not be created for"
                            + " environment variable '{0}' : {1}", name, 
                            ex.Message);
                    } else {
                        throw;
                    }
                }
            }

            // display the properties
            if (Verbose) {
                foreach (DictionaryEntry entry in Properties) {
                    string name = (string) entry.Key;
                    if (name.StartsWith(Prefix)) {
                        Log(Level.Info, "{0} = {1}", name, entry.Value.ToString());
                    }
                }
            }
        }

        #endregion Override implementation of Task
    }
}
