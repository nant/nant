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

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

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
    [TaskName("license")]
    public class LicenseTask : Task {
        #region Private Instance Fields

        private FileSet _assemblies;
        private string _input;
        private string _output;
        private string _strTarget;

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
            get { return _input; }
            set { _input = value; }
        }

        /// <summary>
        /// Name of the resource file to output.
        /// </summary>
        [TaskAttribute("output", Required=false)]
        public string Output {
            get { return _output; }
            set { _output = value; }
        }

        /// <summary>
        /// Names of the references to scan for the licensed component.
        /// </summary>
        [FileSet("assemblies")]
        public FileSet Assemblies {
            get { return _assemblies; }
            set { _assemblies = value; }
        }

        /// <summary>
        /// The output executable file for which the license will be generated.
        /// </summary>
        [TaskAttribute("licensetarget", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string Target{
            get { return _strTarget; }
            set { _strTarget = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Generates the license file.
        /// </summary>
        protected override void ExecuteTask(){
            string strLicxFilename = null;
            string strResourceFilename = null;

            try {
                // Get the input .licx file
                strLicxFilename = Project.GetFullPath(_input);
            } catch (Exception e) {
                string msg = String.Format(CultureInfo.InvariantCulture,  "Could not determine path from {0}", _input);
                throw new BuildException(msg, Location, e);
            }

            try {
                // Get the output .licenses file
                if (_output == null) {
                    strResourceFilename = Project.GetFullPath(_strTarget + ".licenses");
                } else {
                    strResourceFilename = Project.GetFullPath(_output);
                }
            } catch (Exception e) {
                string msg = String.Format(CultureInfo.InvariantCulture,  "Could not determine path from output file {0} and target {1}", _output, _strTarget);
                throw new BuildException(msg, Location, e);
            }

            Log(Level.Verbose, LogPrefix + "Compiling license file {0} to {1} using target {2}.", Path.GetFileName(_input), Path.GetFileName(strResourceFilename), _strTarget);

            StringCollection alAssemblies = new StringCollection();
            AppDomain newDomain = AppDomain.CreateDomain("LicenseGatheringDomain", AppDomain.CurrentDomain.Evidence, new AppDomainSetup());

            Log(Level.Verbose, LogPrefix + "Loading assemblies:");

            // First, load all the assemblies so that we can search for the licensed component
            foreach (string strAssembly in _assemblies.FileNames) {

                try {
                    string strRealAssemblyName = Project.GetFullPath(strAssembly);

                    // See if we've got an absolute path to the assembly
                    if (File.Exists(strRealAssemblyName)) {
                        // Don't load an assembly that has already been loaded (including assemblies loaded before this task)
                        if (!alAssemblies.Contains(Path.GetFullPath(strRealAssemblyName).ToLower(CultureInfo.InvariantCulture))) {
                            Log(Level.Verbose, LogPrefix + strAssembly + " (added to loade)");
                            alAssemblies.Add(strRealAssemblyName);
                        } else {
                            Log(Level.Verbose, LogPrefix + strAssembly + " (not added to load)");
                        }
                    } else {
                        if (!alAssemblies.Contains(Path.GetFullPath(strRealAssemblyName).ToLower(CultureInfo.InvariantCulture))) {
                            // No absolute path, ask .NET to load it for us (use the original assembly name)
                            FileInfo fiAssembly = new FileInfo(strAssembly);
                            alAssemblies.Add(Path.GetFileNameWithoutExtension(fiAssembly.Name));
                            Log(Level.Verbose, LogPrefix + strAssembly + " (added to load)");
                        } else {
                            Log(Level.Verbose, LogPrefix + strAssembly + " (not added to load)");
                        }
                    }
                } catch (Exception e) {
                    throw new BuildException(String.Format(CultureInfo.InvariantCulture,  "Unable to load specified assembly: {0}", strAssembly), e);
                }
            }

            LicenseGatherer licenseGatherer = (LicenseGatherer)
                newDomain.CreateInstanceAndUnwrap(typeof(LicenseGatherer).Assembly.FullName,
                typeof(LicenseGatherer).FullName, false, BindingFlags.Public | BindingFlags.Instance,
                null, new object[0], CultureInfo.InvariantCulture, new object[0],
                AppDomain.CurrentDomain.Evidence);
            licenseGatherer.CreateLicenseFile(alAssemblies, strLicxFilename, strResourceFilename, _strTarget, Verbose, LogPrefix, Location);
            AppDomain.Unload(newDomain);

            return;
        }

        #endregion Override implementation of Task

        #region private class used for writing the license in a seperate AppDomain
        /// <summary>Responsible to read the license and write them to a license file</summary>
        private class LicenseGatherer : MarshalByRefObject {
            /// <summary>Creates the whole license file</summary>
            /// <param name="assemblies">the assemblies to load</param>
            /// <param name="licx">The input file</param>
            /// <param name="licenseFile">The license file</param>
            /// <param name="targetFile">The assembly to license for</param>
            /// <param name="isVerbose">Are we in verbose state?</param>
            /// <param name="logPrefix">The LogPrefix</param>
            /// <param name="location">Where we're in the build script?</param>
            public void CreateLicenseFile(StringCollection assemblies, string licx, string licenseFile, string targetFile, bool isVerbose, string logPrefix, NAnt.Core.Location location) {
                ArrayList alAssemblies = new ArrayList();

                // load each assembly and add it to hashtable
                foreach (string assemblyFileName in assemblies) {
                    if (isVerbose) {
                        Console.WriteLine(logPrefix + assemblyFileName);
                    }
                    Assembly assembly = Assembly.LoadFrom(assemblyFileName, AppDomain.CurrentDomain.Evidence);
                    if (assembly == null) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture,  "Failed to load assembly: {0}", assemblyFileName), location);
                    }
                    alAssemblies.Add(assembly);
                }

                DesigntimeLicenseContext dlc = new DesigntimeLicenseContext();
                LicenseManager.CurrentContext = dlc;
                // Read in the input file
                using (StreamReader sr = new StreamReader(licx)) {
                    Hashtable htLicenses = new Hashtable();

                    while(true) {
                        string strLine = sr.ReadLine();

                        if (strLine == null) {
                            break;
                        }

                        strLine = strLine.Trim();
                        // Skip comments and empty lines and already processed assemblies
                        if (strLine.StartsWith("#") || strLine.Length == 0 || htLicenses.Contains(strLine)) {
                            continue;
                        }

                        if (isVerbose) {
                            Console.WriteLine(logPrefix + strLine + ": ");
                        }

                        // Strip off the assembly name, if it exists
                        string strTypeName;

                        if (strLine.IndexOf(',') == -1) {
                            strTypeName = strLine.Trim();
                        } else {
                            strTypeName = strLine.Split(',')[0];
                        }

                        Type tp = null;

                        // Try to locate the type in each assembly
                        foreach (Assembly asm in alAssemblies) {
                            tp = asm.GetType(strTypeName, false, true);
                            if (tp == null) {
                                continue;
                            } // if
                            htLicenses[ strLine ] = tp;
                            break;
                        }

                        if (tp == null) {
                            throw new BuildException(String.Format(CultureInfo.InvariantCulture,  "Failed to locate type: {0}", strTypeName), location);
                        }

                        if (isVerbose && tp != null) {
                            if (isVerbose) {
                                Console.WriteLine(logPrefix + logPrefix + ((Type) htLicenses[strLine]).Assembly.CodeBase);
                            }
                        }

                        // Ensure that we've got a licensed component
                        if (tp.GetCustomAttributes(typeof(LicenseProviderAttribute), true).Length == 0) {
                            throw new BuildException(String.Format(CultureInfo.InvariantCulture,  "Type is not a licensed component: {0}", tp), location);
                        }

                        try {
                            LicenseManager.CreateWithContext(tp, dlc);
                        } catch (Exception e) {
                            throw new BuildException(String.Format(CultureInfo.InvariantCulture,  "Failed to create license for type {0}", tp), location, e);
                        }
                    }
                }

                // Overwrite the existing file, if it exists - is there a better way?
                if (File.Exists(licenseFile)) {
                    File.SetAttributes(licenseFile, FileAttributes.Normal);
                    File.Delete(licenseFile);
                    if (isVerbose) {
                        Console.WriteLine(logPrefix + "Deleted old " + licenseFile);
                    }
                }

                // Now write out the license file, keyed to the appropriate output target filename
                // This .license file will only be valid for this exe/dll
                using (FileStream fs = new FileStream(licenseFile, FileMode.Create)) {
                    // Note the ToUpper() - this is the behaviour of VisualStudio
                    DesigntimeLicenseContextSerializer.Serialize(fs, targetFile.ToUpper(CultureInfo.InvariantCulture), dlc);
                    if (isVerbose) {
                        Console.WriteLine(logPrefix + "Created new " + licenseFile);
                    }
                }

                dlc = null;
                return;
            }
        }
        #endregion
    }
}
