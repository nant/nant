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

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {
    /// <summary>
    /// Sets properties with system information.
    /// </summary>
    /// <remarks>
    ///   <para>Sets a number of properties with information about the system environment.  The intent of this task is for nightly build logs to have a record of system information so that the build was performed on.</para>
    ///   <list type="table">
    ///     <listheader><term>Property</term>      <description>Value</description></listheader>
    ///     <item><term>sys.clr.version</term>     <description>Common Language Runtime version number.</description></item>
    ///     <item><term>sys.env.*</term>           <description>Environment variables (e.g., sys.env.PATH).</description></item>
    ///     <item><term>sys.os.platform</term>              <description>Operating system platform ID.</description></item>
    ///     <item><term>sys.os.version</term>               <description>Operating system version.</description></item>
    ///     <item><term>sys.os</term>                       <description>Operating system version string.</description></item>
    ///     <item><term><b>Special folders</b></term><description><a href="ms-help://MS.VSCC/MS.MSDNQTR.2003FEB.1033/cpref/html/frlrfSystemEnvironmentSpecialFolderClassTopic.htm">See the Microsoft.NET Framework SDK documentation for more details on the following.</a></description></item>
    ///     <item><term>sys.os.folder.applicationdata</term><description>The directory that serves as a common repository for application-specific data for the current roaming user.</description></item>
    ///     <item><term>sys.os.folder.commonapplicationdata</term><description>The directory that serves as a common repository for application-specific data that is used by all users.</description></item>
    ///     <item><term>sys.os.folder.commonprogramfiles</term><description>The directory for components that are shared across applications.</description></item>
    ///     <item><term>sys.os.folder.desktopdirectory</term><description>The directory used to physically store file objects on the desktop. Do not confuse this directory with the desktop folder itself, which is a virtual folder.</description></item>            
    ///     <item><term>sys.os.folder.programfiles</term>   <description>The Program Files directory.</description></item>         
    ///     <item><term>sys.os.folder.system</term>         <description>The System directory.</description></item>
    ///     <item><term>sys.os.folder.temp</term>           <description>The temporary directory.</description></item>    
    ///   </list>
    /// </remarks>
    /// <example>
    ///   <para>Register the properties with the default property prefix.</para>
    ///   <code>&lt;sysinfo/&gt;</code>
    ///   <para>Register the properties without a prefix.</para>
    ///   <code>&lt;sysinfo prefix=""/&gt;</code>
    ///   <para>Register properties and display a summary</para>
    ///   <code>&lt;sysinfo verbose="true"/&gt;</code>
    /// </example>
    [TaskName("sysinfo")]
    public class SysInfoTask : Task {
        string _prefix = "sys.";
       
        /// <summary>The string to prefix the property names with.  Default is "sys."</summary>
        [TaskAttribute("prefix", Required=false)]
        public string Prefix {
            get { return _prefix; }
            set { _prefix = value; }
        }

        protected override void ExecuteTask() {
            Log.WriteLine(LogPrefix + "Setting system information properties under " + Prefix + "*");

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
                Properties[Prefix + "env." + name] = (string)variables[name];
            }

            // display the properties
            if (Verbose) {
                Log.WriteLine(LogPrefix + "nant.version = " + Properties["nant.version"]);
                foreach (DictionaryEntry entry in Properties) {
                    string name = (string) entry.Key;
                    if (name.StartsWith(Prefix) && !name.StartsWith(Prefix + "env.")) {
                        Log.WriteLine(LogPrefix + name + " = " + entry.Value.ToString());
                    }
                }
            }
        }
    }
}
