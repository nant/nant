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
// Ian MacLean (ian_maclean@another.com)
// Scott Hernandez (ScottHernandez@hotmail.com)
// William E. Caputo (wecaputo@thoughtworks.com | logosity@yahoo.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using NAnt.Core.Util;

namespace NAnt.Core
{
	/// <summary>
	/// Summary description for ProjectSettingsLoader.
	/// </summary>
	internal class ProjectSettingsLoader
	{
		/// <summary>
		/// Holds a value indicating whether a scan for tasks has already been 
		/// performed on the configured task path.
		/// </summary>
		private static bool ScannedTaskPath = false;

		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected Project _proj;
		internal ProjectSettingsLoader(Project p)
		{
			_proj = p;
		}

		private PropertyDictionary Properties {
			get { return _proj.Properties;}	
		}
		
		#region Settings file Load routines
        
		/// <summary>
		/// Reads the list of global properties specified in the NAnt configuration
		/// file.
		/// </summary>
		/// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing global properties.</param>
		private void ProcessGlobalProperties(XmlNodeList propertyNodes) {
			//deals with xml info from the config file, not build document.
			foreach (XmlNode propertyNode in propertyNodes) {
				//skip special elements like comments, pis, text, etc.
				if (!(propertyNode.NodeType == XmlNodeType.Element)) {
					continue;	
				}

				string propertyName = GetXmlAttributeValue(propertyNode, "name");
				string propertyValue = GetXmlAttributeValue(propertyNode, "value");
				string propertyReadonly = GetXmlAttributeValue(propertyNode, "readonly");

				if (propertyReadonly != null && propertyReadonly == "true") {
					Properties.AddReadOnly(propertyName, propertyValue);
				} else {
					Properties[propertyName] = propertyValue;
				}
			}
		}

		/// <summary>
		/// Reads the list of framework-neutral properties defined in the 
		/// NAnt configuration file.
		/// </summary>
		/// <param name="propertyNodes">An <see cref="XmlNodeList" /> representing framework-neutral properties.</param>
		private void ProcessFrameworkNeutralProperties(XmlNodeList propertyNodes) {
			//deals with xml info from the config file, not build document.
			foreach (XmlNode propertyNode in propertyNodes) {
				//skip elements like comments, pis, text, etc.
				if (!(propertyNode.NodeType == XmlNodeType.Element)) {
					continue;	
				}

				string propertyName = GetXmlAttributeValue(propertyNode, "name");
				string propertyValue = GetXmlAttributeValue(propertyNode, "value");

				if (propertyName == null) {
					throw new ArgumentException("A framework-neutral property should at least have a name.");
				}

				if (propertyValue != null) {
					// expand properties in property value
					propertyValue = _proj.FrameworkNeutralProperties.ExpandProperties(propertyValue, Location.UnknownLocation);

					// add read-only property to collection of framework-neutral properties
					_proj.FrameworkNeutralProperties.AddReadOnly(propertyName, propertyValue);
				}
			}
		}

		/// <summary>
		/// Processes the framework nodes.
		/// </summary>
		/// <param name="frameworkNodes">An <see cref="XmlNodeList" /> representing supported frameworks.</param>
		private void ProcessFrameworks(XmlNodeList frameworkNodes) {
			//deals with xml info from the config file, not build document.
			foreach (XmlNode frameworkNode in frameworkNodes) {
				//skip special elements like comments, pis, text, etc.
				if (!(frameworkNode.NodeType == XmlNodeType.Element)) {
					continue;
				}

				PropertyDictionary frameworkProperties = null;
				string name = null;

				try {
					// initialize framework-specific properties
					frameworkProperties = new PropertyDictionary();

					// inject framework-neutral properties
					frameworkProperties.Inherit(_proj.FrameworkNeutralProperties, (StringCollection)null);

					// get framework attributes
					name = GetXmlAttributeValue(frameworkNode, "name");

					string description = GetXmlAttributeValue(frameworkNode, "description");
					string version = GetXmlAttributeValue(frameworkNode, "version");
					string runtimeEngine = GetXmlAttributeValue(frameworkNode, "runtimeengine");
					string frameworkDir = GetXmlAttributeValue(frameworkNode, "frameworkdirectory");
					string frameworkAssemblyDir = GetXmlAttributeValue(frameworkNode, "frameworkassemblydirectory");
					string sdkDir = GetXmlAttributeValue(frameworkNode, "sdkdirectory");

					// get framework-specific properties
					XmlNodeList propertyNodes = frameworkNode.SelectNodes("properties/property");

					foreach (XmlNode propertyNode in propertyNodes) {
						//skip non-nant namespace elements and special elements like comments, pis, text, etc.
						if (!(propertyNode.NodeType == XmlNodeType.Element)) {
							continue;	
						}

						string propertyName = GetXmlAttributeValue(propertyNode, "name");
						string propertyValue = null;

						if (propertyName == null) {
							throw new ArgumentException("A framework property should at least have a name.");
						}

						if (GetXmlAttributeValue(propertyNode, "useregistry") == "true") {
							string regKey = GetXmlAttributeValue(propertyNode, "regkey");

							if (regKey == null) {
								throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Framework property {0} is configured to be read from the registry but has no regkey attribute set.", propertyName));
							} else {
								// expand properties in regkey
								regKey = frameworkProperties.ExpandProperties(regKey, Location.UnknownLocation);
							}

							string regValue = GetXmlAttributeValue(propertyNode, "regvalue");

							if (regValue == null) {
								throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Framework property {0} is configured to be read from the registry but has no regvalue attribute set.", propertyName));
							} else {
								// expand properties in regvalue
								regValue = frameworkProperties.ExpandProperties(regValue, Location.UnknownLocation);
							}

							RegistryKey sdkKey = Registry.LocalMachine.OpenSubKey(regKey);

							if (sdkKey != null && sdkKey.GetValue(regValue) != null) {
								propertyValue = sdkKey.GetValue(regValue).ToString();
							}
						} else {
							propertyValue = GetXmlAttributeValue(propertyNode, "value");
						}

						if (propertyValue != null) {
							// expand properties in property value
							propertyValue = frameworkProperties.ExpandProperties(propertyValue, Location.UnknownLocation);

							// add read-only property to collection of framework properties
							frameworkProperties.AddReadOnly(propertyName, propertyValue);
						}
					}

					// create new FrameworkInfo instance, this will throw an
					// an exception if the framework is not valid
					FrameworkInfo info = new FrameworkInfo(name, 
						description, 
						version, 
						frameworkDir, 
						sdkDir, 
						frameworkAssemblyDir, 
						runtimeEngine, 
						frameworkProperties);

					// framework is valid, so add it for framework dictionary
					_proj.FrameworkInfoDictionary.Add(info.Name, info);
				} catch (Exception ex) {
					string msg = string.Format(CultureInfo.InvariantCulture, 
						"Framework {0} is invalid and has not been loaded : {1}", 
						name, ex.Message);

					_proj.Log(Level.Verbose, msg);
					logger.Info(msg, ex);
				}
			}
		}

		/// <summary>
		/// Gets the value of the specified attribute from the specified node.
		/// </summary>
		/// <param name="xmlNode">The node of which the attribute value should be retrieved.</param>
		/// <param name="attributeName">The attribute of which the value should be returned.</param>
		/// <returns>
		/// The value of the attribute with the specified name or <c>null</c> if the attribute
		/// does not exist or has no value.
		/// </returns>
		private static string GetXmlAttributeValue(XmlNode xmlNode, string attributeName) {
			string attributeValue = null;

			if (xmlNode != null) {
				XmlAttribute xmlAttribute = (XmlAttribute)xmlNode.Attributes.GetNamedItem(attributeName);

				if (xmlAttribute != null) {
					attributeValue = StringUtils.ConvertEmptyToNull(xmlAttribute.Value);
				}
			}

			return attributeValue;
		}

		/// <summary>
		/// Loads and processes settings from the specified XmlNode on the configuration file. This is not coming from our build file.
		/// </summary>
		public void ProcessSettings(XmlNode nantNode) {
			logger.Debug(string.Format(CultureInfo.InvariantCulture, "[{0}].ConfigFile '{1}'",AppDomain.CurrentDomain.FriendlyName, AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));

			if (nantNode == null) { 
				// todo pull a settings file out of the assembly resource and copy to that location
				_proj.Log(Level.Warning, "NAnt settings not found. Defaulting to no known framework.");
				logger.Info("NAnt settings not found. Defaulting to no known framework.");
				return;
			}

			// process the framework-neutral properties
			ProcessFrameworkNeutralProperties(nantNode.SelectNodes("frameworks/properties/property"));

			// process the defined frameworks
			ProcessFrameworks(nantNode.SelectNodes("frameworks/framework"));

			// get taskpath setting to load external tasks and types from
			string taskPath = GetXmlAttributeValue(nantNode, "taskpath");

			if (taskPath != null && ScannedTaskPath == false) {
				string[] paths = taskPath.Split(';');

				foreach (string path in paths) {
					string fullpath = path;

					if (!Directory.Exists(path)) {
						// try relative path 
						fullpath = Path.GetFullPath(Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), path));
					}

					TypeFactory.ScanDir(fullpath);
				}

				ScannedTaskPath = true; // so we only load tasks once 
			}

			// determine default framework
			string defaultFramework = GetXmlAttributeValue(nantNode.SelectSingleNode("frameworks"), "default");

			if (defaultFramework != null && _proj.FrameworkInfoDictionary.ContainsKey(defaultFramework)) {
				Properties.AddReadOnly("nant.settings.defaultframework", defaultFramework);
				Properties.Add("nant.settings.currentframework", defaultFramework);
				_proj.DefaultFramework = _proj.FrameworkInfoDictionary[defaultFramework];
				_proj.CurrentFramework = _proj.DefaultFramework;
			} else {
				_proj.Log(Level.Warning, "Framework '{0}' does not exist or is not specified in the NAnt configuration file. Defaulting to no known framework.", defaultFramework);
			}

			// process global properties
			ProcessGlobalProperties(nantNode.SelectNodes("properties/property"));
		}

		#endregion Settings file Load routines
	}
}
