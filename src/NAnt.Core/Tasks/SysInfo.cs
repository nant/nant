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
using System.IO;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Set properties with system information.</summary>
	/// <remarks>
	///   <para>Sets a number of properties with information about the system environment.  The intent of this task is for nightly build logs to have a record of system information so that the build was performed on.</para>
	///   <list type="table">
	///     <listheader><term>Property</term>      <description>Value</description></listheader>
	///     <item><term>sys.clr.version</term>     <description>Common Language Runtime version number.</description></item>
	///     <item><term>sys.env.*</term>           <description>Environment variables (e.g., sys.env.PATH).</description></item>
	///     <item><term>sys.os.folder.system</term><description>The System directory.</description></item>
	///     <item><term>sys.os.folder.temp</term>  <description>The temporary directory.</description></item>
	///     <item><term>sys.os.platform</term>     <description>Operating system platform ID.</description></item>
	///     <item><term>sys.os.version</term>      <description>Operating system version.</description></item>
   ///     <item><term>sys.os</term>              <description>Operating system version string.</description></item>
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
            Properties.Add(Prefix + "clr.version", Environment.Version.ToString());
            Properties.Add(Prefix + "os.platform", Environment.OSVersion.Platform.ToString());
            Properties.Add(Prefix + "os.version", Environment.OSVersion.Version.ToString());
            Properties.Add(Prefix + "os.folder.system", Environment.GetFolderPath(Environment.SpecialFolder.System));
            Properties.Add(Prefix + "os.folder.temp", Path.GetTempPath());
            Properties.Add(Prefix + "os", Environment.OSVersion.ToString());

            // set environment variables
            IDictionary variables = Environment.GetEnvironmentVariables();
            foreach (string name in variables.Keys) {
                string value = (string) variables[name];
                Properties.Add(Prefix + "env." + name, value);
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
