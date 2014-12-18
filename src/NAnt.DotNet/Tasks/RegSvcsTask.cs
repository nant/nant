// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//
// Arjen Poutsma (poutsma@yahoo.com)
// Giuseppe Greco (giuseppe.greco@agamura.com)

using System;
using System.Globalization;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Util;

namespace NAnt.DotNet.Tasks {
    /// <summary>
    /// Installs or removes .NET Services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This tasks provides the same functionality as the <c>regsvcs</c> tool 
    /// provided in the .NET SDK.
    /// </para>
    /// <para>
    /// It performs the following actions: 
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Loads and registers an assembly.</description>
    ///   </item>
    ///   <item>
    ///     <description>Generates, registers, and installs a type library into a specified COM+ application.</description>
    ///   </item>
    ///   <item>
    ///     <description>Configures services that are added programmatically to your class.</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Refer to the <see href="ms-help://MS.NETFrameworkSDK/cptools/html/cpgrfnetservicesinstallationutilityregsvcsexe.htm">.NET Services Installation Tool (Regsvcs.exe)</see> for more information.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Adds all public classes contained in <c>myTest.dll</c> to a COM+ 
    ///   application and produces the <c>myTest.tlb</c> type library. If the 
    ///   application already exists, it is overwritten.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regsvcs action="FindOrCreate" assembly="myTest.dll" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Adds all public classes contained in <c>myTest.dll</c> to <c>myTargetApp</c> 
    ///   and produces the <c>myTest.tlb</c> type library. If the application already 
    ///   exists, it is overwritten.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regsvcs action="FindOrCreate" assembly="myTest.dll" application="myTargetApp" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Adds all public classes contained in <c>myTest.dll</c> to a COM+ 
    ///   application and produces the <c>myTest.tlb</c> type library. A new 
    ///   application is always created.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regsvcs action="Create" assembly="myTest.dll" />
    ///     ]]>
    ///   </code>
    /// </example>
    /// <example>
    ///   <para>
    ///   Uninstalls the COM+ application contained in <c>myTest.dll</c>.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <regsvcs action="Uninstall" assembly="myTest.dll" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("regsvcs")]
    [ProgramLocation(LocationType.FrameworkDir)]
    public class RegsvcsTask : ExternalProgramBase {
        /// <summary>
        /// Defines the possible actions for a .NET Service.
        /// </summary>
        public enum ActionType {
            /// <summary>
            /// Finds or creates the target application.
            /// </summary>
            FindOrCreate,

            /// <summary>
            /// Creates the target application.
            /// </summary>
            Create,

            /// <summary>
            /// Uninstalls the target application.
            /// </summary>
            Uninstall
        }

        #region Private Instance Fields

        private StringBuilder _argumentBuilder = null;
        private ActionType _action = ActionType.FindOrCreate;
        private FileInfo _assemblyFile;
        private string _applicationName;
        private FileInfo _typeLibrary;
        private bool _existingTlb = false;
        private bool _existingApplication = false;
        private bool _noreconfig = false;
        private bool _componentsOnly = false;
        private string _partitionName = null;

        #endregion Private Instance Fields

        #region Public Instance Properties

        /// <summary>
        /// Defines the action to take with the assembly. The default is 
        /// <see cref="ActionType.FindOrCreate" />.
        /// </summary>
        [TaskAttribute("action")]
        public ActionType Action {
            get { return _action; }
            set { 
                if (!Enum.IsDefined(typeof(ActionType), value)) {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, ResourceUtils.GetString("NA2002"), 
                        value)); 
                } else {
                    this._action = value;
                }
            }
        }

        /// <summary>
        /// The source assembly file.
        /// </summary>
        /// <remarks>
        /// The assembly must be signed with a strong name.
        /// </remarks>
        [TaskAttribute("assembly", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public FileInfo AssemblyFile {
            get { return _assemblyFile; }
            set { _assemblyFile = value; }
        }

        /// <summary>
        /// Specifies the type library file to install.
        /// </summary>
        [TaskAttribute("tlb")]
        public FileInfo TypeLibrary {
            get { return _typeLibrary; }
            set { _typeLibrary = value; }
        }

        /// <summary>
        /// Uses an existing type library. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("existingtlb")]
        [BooleanValidator()]
        public bool ExistingTypeLibrary {
            get { return _existingTlb; }
            set { _existingTlb = value; }
        }

        /// <summary>
        /// Do not reconfigure an existing target application. 
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("noreconfig")]
        [BooleanValidator()]
        public bool NoReconfig {
            get { return _noreconfig; }
            set {_noreconfig = value; }
        }

        /// <summary>
        /// Configures components only; ignores methods and interfaces.
        /// The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("componentsonly")]
        [BooleanValidator()]
        public bool ComponentsOnly {
            get { return _componentsOnly; }
            set { _componentsOnly = value; }
        }

        /// <summary>
        /// Expect an existing application. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("existingapp")]
        [BooleanValidator()]
        public bool ExistingApplication {
            get { return _existingApplication; }
            set { _existingApplication = value; }
        }

        /// <summary>
        /// Specifies the name of the COM+ application to either find or create.
        /// </summary>
        [TaskAttribute("application")]
        public string ApplicationName {
            get { return _applicationName; }
            set { _applicationName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Specifies the name or id of the COM+ application to either find or 
        /// create.
        /// </summary>
        [TaskAttribute("partition")]
        public string PartitionName {
            get { return _partitionName; }
            set { _partitionName = StringUtils.ConvertEmptyToNull(value); }
        }

        #endregion Public Instance Properties

        #region Override implementation of ExternalProgramBase

        /// <summary>
        /// Gets the command-line arguments for the external program.
        /// </summary>
        /// <value>
        /// The command-line arguments for the external program.
        /// </value>
        public override string ProgramArguments {
            get {
                if (_argumentBuilder != null) {
                    return _argumentBuilder.ToString();
                } else {
                    return null;
                }
            }
        }

        /// <summary>
        /// Performs the specified action.
        /// </summary>
        protected override void ExecuteTask() {
            _argumentBuilder = new StringBuilder();

            switch (Action) {
                case ActionType.Create:
                    _argumentBuilder.Append("/c ");
                    Log(Level.Info, ResourceUtils.GetString("String_CreatingCOM+Application"),
                        AssemblyFile.FullName);
                    break;
                case ActionType.FindOrCreate:
                    _argumentBuilder.Append("/fc ");
                    Log(Level.Info, ResourceUtils.GetString("String_FindingCOM+Application"),
                        AssemblyFile.FullName);
                    break;
                case ActionType.Uninstall:
                    _argumentBuilder.Append("/u ");
                    Log(Level.Info, ResourceUtils.GetString("String_UninstallingCOM+Application"),
                        AssemblyFile.FullName);
                    break;
            }

            if (TypeLibrary != null) {
                _argumentBuilder.AppendFormat("/tlb:\"{0}\" ", TypeLibrary.FullName);
            }

            if (ExistingTypeLibrary) {
                _argumentBuilder.Append("/extlb ");
            }

            if (NoReconfig) {
                _argumentBuilder.Append("/noreconfig ");
            }

            if (ComponentsOnly) {
                _argumentBuilder.Append("/componly ");
            }

            if (ApplicationName != null) {
                _argumentBuilder.AppendFormat("/appname:\"{0}\" ", ApplicationName);
            }

            if (ExistingApplication) {
                _argumentBuilder.Append("/exapp ");
            }

            if (PartitionName != null) {
                _argumentBuilder.AppendFormat("/parname:\"{0}\" ", PartitionName);
            }

            if (!Verbose) {
                _argumentBuilder.Append("/quiet ");
            }

            // suppresses display of the sign-on banner 
            _argumentBuilder.Append("/nologo ");

            // output the assembly name enclosed with quotes
            _argumentBuilder.Append("\"" + AssemblyFile.FullName + "\"");

            // call base class to do perform the actual call
            base.ExecuteTask();
        }

        #endregion Override implementation of ExternalProgramBase
    }
}
