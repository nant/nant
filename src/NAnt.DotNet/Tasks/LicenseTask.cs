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
// Matthew Mastracci (mmastrac@canada.com)
// Sascha Andres (sa@programmers-world.com)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Generates a <c>.licence</c> file from a <c>.licx</c> file.
    /// </summary>
    /// <remarks>
    /// If no output file is specified, the default filename is the name of the
    /// target file with the extension <c>.licenses</c> appended.
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Generate the file <c>component.exe.licenses</c> file from <c>component.licx</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <license input="component.licx" licensetarget="component.exe" />
    ///     ]]>
    ///   </code>
    /// </example>
    [Serializable()]
    [TaskName("license")]
    public class LicenseTask : Task {
        #region Private Instance Fields

        private FileSet _assemblies = new FileSet();
        private string _input;
        private string _output;
        private string _target;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseTask" /> class.
        /// </summary>
        public LicenseTask(){
            _assemblies = new FileSet();
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Input file to process.
        /// </summary>
        [TaskAttribute("input", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Input {
            get { return (_input != null) ? Project.GetFullPath(_input) : null; }
            set { _input = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Name of the license file to output.
        /// </summary>
        [TaskAttribute("output", Required=false)]
        public string Output {
            get { return (_output != null) ? Project.GetFullPath(_output) : null; }
            set { _output = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Names of the references to scan for the licensed component.
        /// </summary>
        [BuildElement("assemblies")]
        public FileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// The executable file for which the license will be generated.
        /// </summary>
        [TaskAttribute("licensetarget", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Target {
            get { return _target; }
            set { _target = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Initializes the <see cref="LicenseTask" /> class.
        /// </summary>
        /// <param name="taskNode">The <see cref="XmlNode" /> used to initialize the task.</param>
        protected override void InitializeTask(XmlNode taskNode) {
            if (!File.Exists(Input)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Input file '{0}' does not exist.", Input), Location);
            }
        }


        /// <summary>
        /// Generates the license file.
        /// </summary>
        protected override void ExecuteTask(){
            string resourceFilename = null;

            // fix references to system assemblies
            if (Project.CurrentFramework != null) {
                foreach (string pattern in Assemblies.Includes) {
                    if (Path.GetFileName(pattern) == pattern) {
                        string frameworkDir = Project.CurrentFramework.FrameworkAssemblyDirectory.FullName;
                        string localPath = Path.Combine(Assemblies.BaseDirectory, pattern);
                        string fullPath = Path.Combine(frameworkDir, pattern);

                        if (!File.Exists(localPath) && File.Exists(fullPath)) {
                            // found a system reference
                            Assemblies.FileNames.Add(fullPath);
                        }
                    }
                }
            }

            try {
                // get the output .licenses file
                if (Output == null) {
                    resourceFilename = Project.GetFullPath(Target + ".licenses");
                } else {
                    resourceFilename = Project.GetFullPath(Output);
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                    "Could not determine path from output file {0} and target {1}.", 
                    Output, Target), Location, ex);
            }

            // make sure the directory for the .licenses file exists
            Directory.CreateDirectory(Path.GetDirectoryName(resourceFilename));

            Log(Level.Verbose, LogPrefix + "Compiling license file {0} to {1}" 
                + " using target {2}.", Input, resourceFilename, Target);

            // create new domain
            AppDomain newDomain = AppDomain.CreateDomain("LicenseGatheringDomain", 
                AppDomain.CurrentDomain.Evidence);

            LicenseGatherer licenseGatherer = (LicenseGatherer)
                newDomain.CreateInstanceAndUnwrap(typeof(LicenseGatherer).Assembly.FullName,
                typeof(LicenseGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance,
                null, new object[0], CultureInfo.InvariantCulture, new object[0],
                AppDomain.CurrentDomain.Evidence);
            licenseGatherer.CreateLicenseFile(this, resourceFilename);

            // unload newly created domain
            AppDomain.Unload(newDomain);
        }

        #endregion Override implementation of Task

        /// <summary>
        /// Responsible for reading the license and writing them to a license 
        /// file.
        /// </summary>
        private class LicenseGatherer : MarshalByRefObject {
            /// <summary>
            /// Creates the whole license file.
            /// </summary>
            /// <param name="licenseTask">The <see cref="LicenseTask" /> instance for which the license file should be created.</param>
            /// <param name="licenseFile">The license file to create.</param>
            public void CreateLicenseFile(LicenseTask licenseTask, string licenseFile) {
                ArrayList assemblies = new ArrayList();

                // create assembly resolver
                AssemblyResolver assemblyResolver = new AssemblyResolver();

                // attach it the current domain
                assemblyResolver.Attach();

                licenseTask.Log(Level.Verbose, licenseTask.LogPrefix 
                    + "Loading assemblies ...");

                try {
                    // first, load all the assemblies so that we can search for the 
                    // licensed component
                    foreach (string assemblyFileName in licenseTask.Assemblies.FileNames) {
                        // holds a valid idicating whether the assembly should
                        // be loaded
                        bool loadAssembly = true;

                        // check if there's a valid current framework
                        if (licenseTask.Project.CurrentFramework != null) {
                            // get framework assembly directory
                            DirectoryInfo frameworkAssemblyDir = licenseTask.Project.
                                CurrentFramework.FrameworkAssemblyDirectory;

                            // get path to reference assembly
                            DirectoryInfo referenceAssemblyDir = new DirectoryInfo(
                                Path.GetDirectoryName(assemblyFileName));

                            // check if the assembly is a system assembly for the 
                            // currently active framework
                            if (referenceAssemblyDir.FullName == frameworkAssemblyDir.FullName) {
                                // don't load any assemblies from currently 
                                // activate framework, as this will eventually
                                // cause InvalidCastExceptions when 
                                // LicenseManager.CreateWithContext is called
                                loadAssembly = false;
                            }
                        }

                        if (loadAssembly) {
                            Assembly assembly = Assembly.LoadFrom(assemblyFileName, 
                                AppDomain.CurrentDomain.Evidence);

                            if (assembly != null) {
                                // output assembly filename to build log
                                licenseTask.Log(Level.Verbose, licenseTask.LogPrefix 
                                    + "{0} (loaded)", assemblyFileName);

                                assemblies.Add(assembly);
                            }
                        } else {
                            // output assembly filename to build log
                            licenseTask.Log(Level.Verbose, licenseTask.LogPrefix 
                                + "{0} (skipped)", assemblyFileName);
                        }
                    }

                    DesigntimeLicenseContext dlc = new DesigntimeLicenseContext();
                    LicenseManager.CurrentContext = dlc;

                    // read the input file
                    using (StreamReader sr = new StreamReader(licenseTask.Input)) {
                        Hashtable licenseTypes = new Hashtable();

                        licenseTask.Log(Level.Verbose, licenseTask.LogPrefix + 
                            "Creating licenses ...");

                        while (true) {
                            string line = sr.ReadLine();

                            if (line == null) {
                                break;
                            }

                            line = line.Trim();
                            // Skip comments, empty lines and already processed assemblies
                            if (line.StartsWith("#") || line.Length == 0 || licenseTypes.Contains(line)) {
                                continue;
                            }

                            licenseTask.Log(Level.Verbose, licenseTask.LogPrefix 
                                + line + ": ");

                            // Strip off the assembly name, if it exists
                            string typeName;

                            if (line.IndexOf(',') != -1) {
                                typeName = line.Split(',')[0];
                            } else {
                                typeName = line;
                            }

                            Type tp = null;

                            // try to locate the type in each assembly
                            foreach (Assembly assembly in assemblies) {
                                if (tp == null) {
                                    tp = assembly.GetType(typeName, false, true);
                                }

                                if (tp != null) {
                                    break;
                                }
                            }

                            if (tp == null) {
                                try {
                                    // final attempt, assuming line contains
                                    // assembly qualfied name
                                    tp = Type.GetType(line, false, false);
                                } catch {
                                    // ignore error, we'll report the load
                                    // failure later
                                }
                            }

                            if (tp == null) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,  
                                    "Failed to locate type {0}.", typeName), licenseTask.Location);
                            } else {
                                // add license type to list of processed license types
                                licenseTypes[line] = tp;
                                // output assembly from which license type was loaded
                                licenseTask.Log(Level.Verbose, licenseTask.LogPrefix 
                                    + ((Type) licenseTypes[line]).Assembly.CodeBase);
                            }

                            // ensure that we've got a licensed component
                            if (tp.GetCustomAttributes(typeof(LicenseProviderAttribute), true).Length == 0) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture,  
                                    "Type {0} is not a licensed component.", tp.FullName), 
                                    licenseTask.Location);
                            }

                            try {
                                LicenseManager.CreateWithContext(tp, dlc);
                            } catch (Exception ex) {
                                throw new BuildException(string.Format(CultureInfo.InvariantCulture, 
                                    "Failed to create license for type {0}.", tp), 
                                    licenseTask.Location, ex);
                            }
                        }
                    }

                    // overwrite the existing file, if it exists - is there a better way?
                    if (File.Exists(licenseFile)) {
                        File.SetAttributes(licenseFile, FileAttributes.Normal);
                        File.Delete(licenseFile);
                    }

                    // write out the license file, keyed to the appropriate output 
                    // target filename
                    // this .license file will only be valid for this exe/dll
                    using (FileStream fs = new FileStream(licenseFile, FileMode.Create)) {
                        // note the ToUpper() - this is the behaviour of VisualStudio
                        DesigntimeLicenseContextSerializer.Serialize(fs, Path.GetFileName(licenseTask.Target.ToUpper(CultureInfo.InvariantCulture)), dlc);
                        licenseTask.Log(Level.Verbose, licenseTask.LogPrefix + "Created new license file {0}.", licenseFile);
                    }

                    dlc = null;
                } finally {
                    assemblyResolver.Detach();
                }
            }
        }
    }
}
