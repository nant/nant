// NAnt - A .NET build tool
// Copyright (C) 2002 Ryan Boggs
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
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Xml;
using System.Globalization;

using NAnt.Core;
using NAnt.Core.Util;
using NAnt.Core.Attributes;
using Microsoft.Win32;

namespace NAnt.Win32.Tasks {
    /// <summary>
    /// Reads the most recent Windows SDK InstallationFolder key into a NAnt property
    /// </summary>
    /// <example>
    ///   <code>
    ///     <![CDATA[
    /// <locatesdk property="dotNetFX" minsdk="v6.0" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("locatesdk")]
    internal class LocateSdkTask : Task {
        #region Private Instance Fields
        
        private string _propName;
        private string _minWinSdkVer = "v6.0";
        private string _maxWinSdkVer;
        private string _minNetFxVer = "2.0";
        private string _maxNetFxVer;
        private readonly string _registryBase = @"SOFTWARE\Microsoft\Microsoft SDKs\Windows";
        private readonly string _regexNetFxTools = @"^WinSDK.*NetFx.*Tools.*$";
        
        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        ///     <para>
        ///     The property to set to the value stored in the InstalledFolder key of the located WinSDK version.
        ///     </para>
        /// </summary>
        [TaskAttribute("property", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public virtual string PropertyName {
            get { return _propName; }
            set { _propName = value; }
        }

        /// <summary>
        ///     <para>
        ///     The minimum acceptable Windows SDK version.
        ///     </para>
        /// </summary>
        [TaskAttribute("minwinsdkver")]
        public string MinWinSdkVersion {
            get { return _minWinSdkVer; }
            set { _minWinSdkVer = value; }
        }

        /// <summary>
        ///     <para>
        ///     The maximum acceptable Windows SDK version.
        ///     </para>
        /// </summary>
        [TaskAttribute("maxwinsdkver")]
        public string MaxWinSdkVersion {
            get { return _maxWinSdkVer; }
            set { _maxWinSdkVer = value; }
        }
        
        /// <summary>
        ///     <para>
        ///     The minimum acceptable .NET sdk version.
        ///     </para>
        /// </summary>
        [TaskAttribute("minnetfxver")]
        public string MinNetFxVersion {
            get { return _minNetFxVer; }
            set { _minNetFxVer = value; }
        }
        
        /// <summary>
        ///     <para>
        ///     The maximum acceptable .NET sdk version.
        ///     </para>
        /// </summary>
        [TaskAttribute("maxnetfxver")]
        public string MaxNetFxVersion {
            get { return _maxNetFxVer; }
            set { _maxNetFxVer = value; }
        }
        
        #endregion Public Instance Properties

        #region Override implementation of Task
        
        /// <summary>
        /// locate the most recent WinSDK installed
        /// </summary>
        protected override void ExecuteTask() {
            // Initialize all necessary Version objects
            // These will hold the min, max, and loop WinSDK versions found
            Version minSdkVersion = StringToVersion(_minWinSdkVer);
            Version maxSdkVersion = StringToVersion(_maxWinSdkVer);
            Version loopSdkVersion = null;
            
            // These will hold the min, max, and loop .NET versions found
            Version minNetVersion = StringToVersion(_minNetFxVer);
            Version maxNetVersion = StringToVersion(_maxNetFxVer);
            Version loopNetVersion = null;
            
            // Bool variable used to indicate that a valid SDK was found
            bool sdkFound = false;
            
            // Get all of the WinSDK version keys from the user's registry and
            // load them into a string array.
            RegistryKey sdkRegSubKey = Registry.LocalMachine.OpenSubKey(_registryBase, false);
            string[] installedWinSdkVersions = sdkRegSubKey.GetSubKeyNames();
            
            // Sort and reverse the WinSDK version key array to make sure that
            // the latest version is reviewed first before reviewing earlier
            // versions.
            Array.Sort(installedWinSdkVersions);
            Array.Reverse(installedWinSdkVersions);
            
            // Loop through all of the WinSDK version keys.
            for(int i = 0; i < installedWinSdkVersions.Length; i++) {
                loopSdkVersion = StringToVersion(installedWinSdkVersions[i]);
                
                // If a maxVersion was indicated and the loopVersion is greater than
                // the maxVersion, skip to the next item in the installedVersion array.
                if (maxSdkVersion != null) {
                    if (loopSdkVersion > maxSdkVersion) {
                        continue;
                    }
                }
                
                // If the loopVersion is greater than or equal to the minVersion, loop through the subkeys
                // for a valid .NET sdk path
                if (minSdkVersion <= loopSdkVersion) {
                    // Gets all of the current WinSdk loop subkeys
                    string[] installedWinSdkSubKeys = sdkRegSubKey.OpenSubKey(installedWinSdkVersions[i]).GetSubKeyNames();
                    
                    // Sort and reverse the order of the subkeys to go from greatest to least
                    Array.Sort(installedWinSdkSubKeys);
                    Array.Reverse(installedWinSdkSubKeys);
                    
                    // Loop through all of the current WinSdk loop subkeys
                    for(int j = 0; j < installedWinSdkSubKeys.Length; j++) {
                        // Check to see if the current subkey matches the RegEx string
                        if (Regex.IsMatch(installedWinSdkSubKeys[j], _regexNetFxTools)) {
                            // Initialize the necessary string array to hold all 
                            // possible directory locations
                            string[] netFxDirs = new string[] {
                                sdkRegSubKey.OpenSubKey(installedWinSdkVersions[i]).OpenSubKey(installedWinSdkSubKeys[j]).GetValue("InstallationFolder").ToString(),
                                Path.Combine(sdkRegSubKey.OpenSubKey(installedWinSdkVersions[i]).OpenSubKey(installedWinSdkSubKeys[j]).GetValue("InstallationFolder").ToString(), "bin")
                            };
                            
                            // Loop through all of the directories in the possible directory
                            // locations array
                            foreach(string netFxDir in netFxDirs) {
                                // Set the full path to the gacutil.exe.config file based on the current
                                // directory in the directories array
                                string netFxXmlFile = Path.Combine(netFxDir, "gacutil.exe.config");
                            
                                // If the full file path exists, load the gacutil.exe.config xml file
                                if (File.Exists(netFxXmlFile)) {
                                    XmlDocument gacXmlDoc = new XmlDocument();
                                    gacXmlDoc.Load(netFxXmlFile);
                                    
                                    // Get the supported runtime version from the version attribute
                                    // and load it into the loopNetVersion Version object to use for
                                    // comparisons
                                    XmlNode gacVersion = gacXmlDoc.SelectSingleNode("/configuration/startup/requiredRuntime");
                                    XmlAttribute versionAttribute = gacVersion.Attributes["version"];
                                    loopNetVersion = StringToVersion(versionAttribute.Value.ToString());
                                    
                                    // If the maxNetVersion object is not null and is less than
                                    // the loopNetVersion, continue to the next iteration of the 
                                    // inner loop
                                    if (maxNetVersion != null) {
                                        if (loopNetVersion > maxNetVersion) {
                                            continue;
                                        }
                                    }
                                    
                                    // If loopNetVersion is greater than or equal to minNetVersion
                                    // assign the value of the InstallationFolder key of the current subfolder
                                    // to the property name and exit the inner loop
                                    if (minNetVersion <= loopNetVersion) {
                                        Properties[_propName] = netFxDir;
                                        sdkFound = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    // If a valid Sdk version was found within the current Sdk subkeys, break
                    // the outer loop.
                    if (sdkFound) {
                        break;
                    }
                }
            }
            
            // if the Properties dictionary does not contain the _propName as a key, throw an error.
            if (!sdkFound) {
                throw new BuildException(String.Format(CultureInfo.InvariantCulture,"System does not have minimum specified Windows SDK {0}!", _minWinSdkVer));
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods
        
        /// <summary>
        /// Converts a version expressed as a string into a Version object 
        /// </summary>
        /// <param name="sdkVersion">
        /// A <see cref="T:System.String"/> containing the version to convert.
        /// </param>
        /// <returns>
        /// A <see cref="Version"/> object representing the version string.
        /// </returns>
        private Version StringToVersion(string sdkVersion) {
            if (!String.IsNullOrEmpty(sdkVersion)) {
                // Make any non-numeric characters uppercase
                sdkVersion = sdkVersion.Trim().ToUpper();
                
                // Remove the leading v from the sdkVersion string
                if (sdkVersion.StartsWith("V")) {
                    sdkVersion = sdkVersion.Substring(1);
                }
                
                // Return a new Version object based on the sdkVersion string
                // If the sdkVersion string ends with an alphanumeric, it is
                // converted to a revision number for comparison purposes
                if (!char.IsNumber(sdkVersion, sdkVersion.Length - 1)) {
                    string sdkVerFormat = sdkVersion.Substring(0, sdkVersion.Length -1) + "." +
                        ((int)sdkVersion.ToCharArray()[sdkVersion.Length - 1]).ToString();
                    return new Version(sdkVerFormat);
                } else {
                    return new Version(sdkVersion);
                }
            // If the sdkVersion parameter is null or empty, return null
            } else {
                return null;
            }
        }

        #endregion Private Instance Methods
    }
}
