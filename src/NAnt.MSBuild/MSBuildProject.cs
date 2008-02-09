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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet;
using NAnt.VSNet.Tasks;
using NAnt.VSNet.Types;

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

        private Microsoft.Build.BuildEngine.Project _msproj;
        private Microsoft.Build.BuildEngine.Engine _msbuild;

        public MSBuildProject(SolutionBase solution, string projectPath, XmlElement xmlDefinition, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver, DirectoryInfo outputDir)
            : base(xmlDefinition, solutionTask, tfc, gacCache, refResolver, outputDir) {
            string cfgname = solutionTask.Configuration;
            string platform = solutionTask.Platform;

            _msbuild = MSBuildEngine.CreateMSEngine(solutionTask);
            _msproj = new Microsoft.Build.BuildEngine.Project(_msbuild);
            _msproj.FullFileName = projectPath;
            _msproj.LoadXml(xmlDefinition.OuterXml);
            _msproj.GlobalProperties.SetProperty("Configuration", cfgname);
            if (platform.Length > 0) _msproj.GlobalProperties.SetProperty("Platform", platform.Replace(" ", string.Empty));
            if (outputDir != null) _msproj.GlobalProperties.SetProperty("OutputPath", outputDir.FullName);

            //evaluating
            _guid = _msproj.GetEvaluatedProperty("ProjectGuid");
            _projectDirectory = new DirectoryInfo(_msproj.GetEvaluatedProperty("ProjectDir"));
            _projectPath = _msproj.GetEvaluatedProperty("ProjectPath");

            ProjectEntry projectEntry = solution.ProjectEntries [_guid];
            if (projectEntry != null && projectEntry.BuildConfigurations != null) {
                foreach (ConfigurationMapEntry ce in projectEntry.BuildConfigurations) {
                    Configuration solutionConfig = ce.Key;
                    Configuration projectConfig = ce.Value;

                    ProjectConfigurations[projectConfig] = new MSBuildConfiguration(this, _msproj, projectConfig);
                }
            } else {
                Configuration projectConfig = new Configuration (cfgname, platform);
                ProjectConfigurations[projectConfig] = new MSBuildConfiguration(this, _msproj, projectConfig);
            }

            //references
            _references = new ArrayList();
            Microsoft.Build.BuildEngine.BuildItemGroup refs = _msproj.GetEvaluatedItemsByName("Reference");
            foreach (Microsoft.Build.BuildEngine.BuildItem r in refs) {
                string rpath = r.FinalItemSpec;
                string priv = r.GetMetadata("Private");
                string hintpath = r.GetMetadata("HintPath");

                ReferenceBase reference = new MSBuildAssemblyReference(
                    xmlDefinition, ReferencesResolver, this, gacCache,
                    rpath, priv, hintpath);
                _references.Add(reference);
            }
            refs = _msproj.GetEvaluatedItemsByName("ProjectReference");
            foreach (Microsoft.Build.BuildEngine.BuildItem r in refs) {
                string pguid = r.GetMetadata("Project");
                string pname = r.GetMetadata("Name");
                string rpath = r.FinalItemSpec;
                string priv = r.GetMetadata("Private");
                ReferenceBase reference = new MSBuildProjectReference(
                    ReferencesResolver, this, solution, tfc, gacCache, outputDir,
                    pguid, pname, rpath, priv);
                _references.Add(reference);
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
                }
                else {
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
                }
                else {
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

        public override ArrayList References {
            get {
                return _references;
            }
        }

        public override ProjectReferenceBase CreateProjectReference(ProjectBase project, bool isPrivateSpecified, bool isPrivate) {
            return new MSBuildProjectReference(ReferencesResolver, this, project, isPrivateSpecified, isPrivate);
        }

        public override bool IsManaged(Configuration configuration) {
            return true;
        }

        protected override ProductVersion DetermineProductVersion(System.Xml.XmlElement docElement) {
            return ProductVersion.Whidbey;
        }       

        protected override void VerifyProjectXml(System.Xml.XmlElement docElement) {
            if(!IsMSBuildProject(docElement))
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    "Project '{0}' is not a valid MSBUILD project.", ProjectPath),
                    Location.UnknownLocation);
        }

        protected override BuildResult Build(Configuration solutionConfiguration) {
            // explicitly set the Configuration and Platform
            MSBuildConfiguration projectConfig = (MSBuildConfiguration) BuildConfigurations[solutionConfiguration];
            _msproj.GlobalProperties.SetProperty("Configuration", projectConfig.Name);
            _msproj.GlobalProperties.SetProperty("Platform", projectConfig.PlatformName.Replace(" ", string.Empty));

            // DONE: MSBuild'll resolve all references once again
            // is there any way how to disable it?
            // moreover, they could be resolved to something else!

            //We should set:
            //@(ReferencePath)
            //@(ReferenceDependencyPaths)
            //@(NativeReference)
            //@(NativeReferenceFile)
            //maybe @(WebReferenceUrl)
            //maybe @(_ReferenceRelatedPaths)
            //@(ReferenceSatellitePaths)

            // or maybe modify original references to contain full path to whatever we resolved?
            // that seems reasonable. Try it:
            _msproj.RemoveItemsByName("Reference");
            _msproj.RemoveItemsByName("ProjectReference");
            Microsoft.Build.BuildEngine.BuildItemGroup refs = _msproj.AddNewItemGroup();
            foreach (ReferenceBase r in _references) {
                string path = r.GetPrimaryOutputFile(solutionConfiguration);
                if (path == null || !File.Exists(path)) {
                    continue;
                }
                Microsoft.Build.BuildEngine.BuildItem i = refs.AddNewItem("Reference", r.Name);
                i.SetMetadata("HintPath", path);
                i.SetMetadata("CopyLocal", r.CopyLocal ? "True" : "False");
            }

            // this should disable assembly resolution and always use hintpath (which we supply)
            _msproj.GlobalProperties.SetProperty("AssemblySearchPaths", "{HintPathFromItem}");

            if(_msproj.Build())
                return BuildResult.Success;
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
