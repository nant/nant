// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System;
using System.Xml;
using System.CodeDom.Compiler;
using System.Collections;
using System.Globalization;
using System.IO;
using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet;
using NAnt.VSNet.Tasks;

namespace NAnt.MSBuild {
    internal class MSBuildProject : ProjectBase {
        public static bool IsMSBuildProject(XmlElement e) {
            if (e.LocalName == "Project" &&
                e.NamespaceURI.StartsWith("http://schemas.microsoft.com/developer/msbuild")
                )
                return true;
            return false;
        }

        public static bool IsMSBuildProject(XmlDocument doc) {
            return IsMSBuildProject(doc.DocumentElement);
        }

        private static string LoadGuid(XmlElement e, XmlNamespaceManager nm) {
            XmlNode node = e.SelectSingleNode("//x:ProjectGuid", nm);
            if (node == null) return "";
            return node.InnerText;
        }        

        public static string LoadGuid(XmlDocument doc) {
            XmlNamespaceManager nm = new XmlNamespaceManager(doc.NameTable);
            nm.AddNamespace("x", doc.DocumentElement.NamespaceURI);
            return LoadGuid(doc.DocumentElement,nm);
        }

        public static string LoadGuid(XmlElement e) {
            XmlNamespaceManager nm = new XmlNamespaceManager(e.OwnerDocument.NameTable);
            nm.AddNamespace("x", e.NamespaceURI);
            return LoadGuid(e,nm);
        }

        private readonly string _projectPath;
        private readonly ArrayList _references;
        private readonly string _guid;
        private readonly DirectoryInfo _projectDirectory;

        private NAnt.MSBuild.BuildEngine.Project _msproj;
        private NAnt.MSBuild.BuildEngine.Engine _msbuild;

        public MSBuildProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir)
            : base(xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            string cfgname = solutionTask.Configuration;
            string platform = solutionTask.Platform;

            _msbuild = MSBuildEngine.CreateMSEngine(solutionTask);
            _msproj = new NAnt.MSBuild.BuildEngine.Project(_msbuild);
            _msproj.FullFileName = projectPath;
            _msproj.LoadXml(xmlDefinition.OuterXml);

            _msproj.GlobalProperties.SetProperty("Configuration", cfgname);

            if (platform.Length > 0) {
                _msproj.GlobalProperties.SetProperty("Platform", platform.Replace(" ", string.Empty));
            }

            if (outputDir != null) {
                _msproj.GlobalProperties.SetProperty("OutputPath", outputDir.FullName);
            }

            bool generateDoc = false;
            //bool targwarnings = true;
            foreach (NAnt.Core.Tasks.PropertyTask property in solutionTask.CustomProperties) {
                string val;
                // expand properties in context of current project for non-dynamic properties
                if (!property.Dynamic) {
                    val = solutionTask.Project.ExpandProperties(property.Value, solutionTask.GetLocation()  );
                } else {
                    val = property.Value;
                }
                switch (property.PropertyName)
                {
                    //if (property.PropertyName == "TargetWarnings") targwarnings = Boolean.Parse(val);
                    case "GenerateDocumentation":
                        generateDoc = Boolean.Parse(val);
                        break;
                    default:
                        _msproj.GlobalProperties.SetProperty(property.PropertyName, val);
                        break;
                }

            }


            //set tools version to the msbuild version we got loaded
            _msproj.ToolsVersion = SolutionTask.Project.TargetFramework.Version.ToString();

            //TODO: honoring project's TargetFrameworkVersion is not working atm. System assemblies are resolved badly
            _msproj.GlobalProperties.SetProperty("TargetFrameworkVersion", "v" + SolutionTask.Project.TargetFramework.Version.ToString());

            //evaluating
            _guid = _msproj.GetEvaluatedProperty("ProjectGuid");
            _projectDirectory = new DirectoryInfo(_msproj.GetEvaluatedProperty("ProjectDir"));
            _projectPath = _msproj.GetEvaluatedProperty("ProjectPath");

            //TODO: honoring project's TargetFrameworkVersion is not working atm. System assemblies are resolved badly
            ////check if we targeting something else and throw a warning
            //if (targwarnings)
            //{
            //    string verString = _msproj.GetEvaluatedProperty("TargetFrameworkVersion");
            //    if (verString != null)
            //    {
            //        if (verString.StartsWith("v")) verString = verString.Substring(1);
            //        Version ver = new Version(verString);
            //        if (!ver.Equals(SolutionTask.Project.TargetFramework.Version))
            //        {
            //            Log(Level.Warning, "Project '{1}' targets framework {0}.", verString, Name);
            //        }
            //    }
            //}

            //project configuration
            ProjectEntry projectEntry = solution.ProjectEntries [_guid];
            if (projectEntry != null && projectEntry.BuildConfigurations != null) {
                foreach (ConfigurationMapEntry ce in projectEntry.BuildConfigurations) {
                    Configuration projectConfig = ce.Value;

                    ProjectConfigurations[projectConfig] = new MSBuildConfiguration(this, _msproj, projectConfig);
                }
            } else {
                Configuration projectConfig = new Configuration (cfgname, platform);
                ProjectConfigurations[projectConfig] = new MSBuildConfiguration(this, _msproj, projectConfig);
            }

            //references
            _references = new ArrayList();
            NAnt.MSBuild.BuildEngine.BuildItemGroup refs = _msproj.GetEvaluatedItemsByName("Reference");
            foreach (NAnt.MSBuild.BuildEngine.BuildItem r in refs) {
                string rpath = r.FinalItemSpec;
                string priv = r.GetMetadata("Private");
                string hintpath = r.GetMetadata("HintPath");
                string ext = r.GetMetadata("ExecutableExtension");

                ReferenceBase reference = new MSBuildAssemblyReference(
                    xmlDefinition, ReferencesResolver, this, gacCache,
                    rpath, priv, hintpath, ext);
                _references.Add(reference);
            }
            refs = _msproj.GetEvaluatedItemsByName("ProjectReference");
            foreach (NAnt.MSBuild.BuildEngine.BuildItem r in refs) {
                string pguid = r.GetMetadata("Project");
                string pname = r.GetMetadata("Name");
                string rpath = r.FinalItemSpec;
                string priv = r.GetMetadata("Private");
                ReferenceBase reference = new MSBuildProjectReference(
                    ReferencesResolver, this, solution, tfc, gacCache, outputDir,
                    pguid, pname, rpath, priv);
                _references.Add(reference);
            }

            if(generateDoc) {
                string xmlDocBuildFile = FileUtils.CombinePaths(OutputPath, this.Name + ".xml");

                //// make sure the output directory for the doc file exists
                //if (!Directory.Exists(Path.GetDirectoryName(xmlDocBuildFile))) {
                //    Directory.CreateDirectory(Path.GetDirectoryName(xmlDocBuildFile));
                //}

                // add built documentation file as extra output file
                ExtraOutputFiles[xmlDocBuildFile] = Path.GetFileName(xmlDocBuildFile);

                _msproj.GlobalProperties.SetProperty("DocumentationFile", xmlDocBuildFile);
            }
        }

        internal string OutputPath {
            get {
                if (OutputDir != null) {
                    return this.OutputDir.FullName;
                }
                return _msproj.GetEvaluatedProperty("OutputPath");
            }
        }

        public override string Name {
            get {
                string projectPath;

                if (IsUrl(_projectPath)) {
                    // construct uri for project path
                    Uri projectUri = new Uri(_projectPath);

                    // get last segment of the uri (which should be the 
                    // project file itself)
                    projectPath = projectUri.LocalPath;
                } else {
                    projectPath = ProjectPath;
                }

                // return file part without extension
                return Path.GetFileNameWithoutExtension(projectPath);
            }
        }

        public override ProjectType Type {
            get { return ProjectType.MSBuild; }
        }

        public override string ProjectPath {
            get {
                if (IsUrl(_projectPath)) {
                    return _projectPath;
                } else {
                    return FileUtils.GetFullPath(_projectPath);
                }
            }
        }

        public override System.IO.DirectoryInfo ProjectDirectory {
            get { return _projectDirectory; }
        }

        public override ProjectLocation ProjectLocation {
            get { return ProjectLocation.Local; }
        }

        public override string Guid {
            get { return _guid; }
            set { throw new InvalidOperationException("It is not allowed to change the GUID of a MSBuild project"); }
        }

        public override System.Collections.ArrayList References {
            get { return _references; }
        }

        public override ProjectReferenceBase CreateProjectReference(ProjectBase project, bool isPrivateSpecified, bool isPrivate) {
            return new MSBuildProjectReference(ReferencesResolver, this, project, isPrivateSpecified, isPrivate);
        }

        public override bool IsManaged(Configuration configuration) {
            return true;
        }

        /// <summary>
        /// Determines the version of the target msbuild file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method identifies the msbuild version by reviewing the following tags in order:
        /// </para>
        /// <list type="number">
        /// <item>
        /// <description>&lt;ProductVersion&gt;</description>
        /// </item>
        /// <item>
        /// <description>&lt;TargetFrameworkVersion&gt;</description>
        /// </item>
        /// <item>
        /// <description>ToolsVersion attribute</description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="docElement">
        /// A <see cref="System.Xml.XmlElement"/> representing the msbuild project file.
        /// </param>
        /// <returns>
        /// A <see cref="NAnt.VSNet.ProductVersion"/> enum value indicating the msbuild project
        /// file version.
        /// </returns>
        /// <exception cref="NAnt.Core.BuildException">
        /// version string found in the tags listed above is not recognized.
        /// </exception>
        protected override ProductVersion DetermineProductVersion(System.Xml.XmlElement docElement) {
            XmlNamespaceManager _nsMgr = new XmlNamespaceManager(new NameTable());
            _nsMgr.AddNamespace("ms", docElement.NamespaceURI);

            // <ProductVersion> element node
            XmlNode _productVerNode = docElement.SelectSingleNode("ms:PropertyGroup/ms:ProductVersion", _nsMgr);
            // <TargetFrameworkVersion> element node
            XmlNode _targetNetVerNode = docElement.SelectSingleNode("ms:PropertyGroup/ms:TargetFrameworkVersion", _nsMgr);

            // If the <ProductVersion> element exists and it is not empty, get the
            // product version from it.
            if (_productVerNode != null && !String.IsNullOrEmpty(_productVerNode.InnerText)) {
                Version _ver = new Version(_productVerNode.InnerText);

                switch (_ver.Major) {
                    case 8:
                        //    <ProductVersion>8.0.50727</ProductVersion>
                        return ProductVersion.Whidbey;
                    case 9:
                        //    <ProductVersion>9.0.21022</ProductVersion>
                        if (_ver.Build <= 21022) {
                            return ProductVersion.Orcas;
                        }
                        return ProductVersion.Rosario;
                }

            // If the <TargetFrameworkVersion> element exists, get the product version from it.
            } else if (_targetNetVerNode != null) {
                string targetFrameworkVer = _targetNetVerNode.InnerText;

                switch (targetFrameworkVer.ToUpper().Trim()) {
                    case "V4.0":
                        return ProductVersion.Rosario;
                    case "V3.5":
                        return ProductVersion.Orcas;
                    case "V2.0":
                        return ProductVersion.Whidbey;
                }

            // If neither of the above mentioned tags exist, look for the "ToolsVersion"
            // attribute in the <Project> tag.
            } else {
                XmlAttribute toolsVersionAttribute = docElement.Attributes["ToolsVersion"];

                // If the ToolsVersion attribute does not exist at this point,
                // assume that the project is 2.0.
                if (toolsVersionAttribute == null) {
                    return ProductVersion.Whidbey;
                }

                switch (toolsVersionAttribute.Value) {
                    case "4.0":
                        return ProductVersion.Rosario;
                    case "3.5":
                        return ProductVersion.Orcas;
                    case "2.0":
                        return ProductVersion.Whidbey;
                }
            }

            // Throw a buildexception if none of the version numbers above are found.
            throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                "Unknown Project version '{0}'.", ProjectPath), Location.UnknownLocation);
        }       

        protected override void VerifyProjectXml(System.Xml.XmlElement docElement) {
            if(!IsMSBuildProject(docElement)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project '{0}' is not a valid MSBUILD project.", ProjectPath),
                    Location.UnknownLocation);
            }
        }

        protected override BuildResult Build(Configuration solutionConfiguration) {
            // explicitly set the Configuration and Platform
            MSBuildConfiguration projectConfig = (MSBuildConfiguration) BuildConfigurations[solutionConfiguration];
            _msproj.GlobalProperties.SetProperty("Configuration", projectConfig.Name);

            if (!String.IsNullOrEmpty(projectConfig.PlatformName)) {
                _msproj.GlobalProperties.SetProperty("PlatformTarget", projectConfig.PlatformName.Replace(" ", string.Empty));
            }
            
            //modify original references to contain full path to whatever we resolved
            _msproj.RemoveItemsByName("Reference");
            _msproj.RemoveItemsByName("ProjectReference");
            NAnt.MSBuild.BuildEngine.BuildItemGroup refs = _msproj.AddNewItemGroup();
            foreach (ReferenceBase r in _references) {
                string path = r.GetPrimaryOutputFile(solutionConfiguration);
                if (path == null || !File.Exists(path)) {
                    if (path == null) {
                        Log(Level.Warning, "Reference \"{0}\" of project {1} failed to be found.", r.Name, this.Name);
                    } else {
                        Log(Level.Warning, "Reference \"{0}\" of project {1} failed to be found (resolved to {2})", r.Name, this.Name, path);
                    }
                    continue;
                }
                NAnt.MSBuild.BuildEngine.BuildItem i = refs.AddNewItem("Reference", r.Name);
                i.SetMetadata("HintPath", path);
                i.SetMetadata("CopyLocal", r.CopyLocal ? "True" : "False");
            }

            //this should disable assembly resolution and always use hintpath (which we supply)
            if(_msbuild.Assembly.GetName().Version.Major >= 4) {
                //MSBuild 4 adds some system references automatically, so adding TargetFrameworkDirectory for those
                _msproj.GlobalProperties.SetProperty("AssemblySearchPaths", "{HintPathFromItem};{TargetFrameworkDirectory}");
            } else {
                _msproj.GlobalProperties.SetProperty("AssemblySearchPaths", "{HintPathFromItem}");
            }

            if(_msproj.Build()) {
                return BuildResult.Success;
            }
            return BuildResult.Failed;
        }

        private static bool IsUrl(string fileName) {
            if (fileName.StartsWith(Uri.UriSchemeFile) || fileName.StartsWith(Uri.UriSchemeHttp) || fileName.StartsWith(Uri.UriSchemeHttps)) {
                return true;
            }

            return false;
        }
    }
}

