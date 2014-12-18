// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
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
// Matthew Mastracci (matt@aclaro.com)
// Gert Driesen (drieseng@users.sourceforge.net)

using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using NAnt.Core.Util;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Analyses Microsoft Visual Studio .NET 2005 (Whidbey) solution files.
    /// </summary>
    internal class WhidbeySolution : SolutionBase {
        private const string SolutionFolder_GUID = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        public WhidbeySolution(string solutionContent, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver)
            : base(solutionTask, tfc, gacCache, refResolver) {
            Regex reProjects = new Regex(@"Project\(\""(?<package>\{.*?\})\"".*?\""(?<name>.*?)\"".*?\""(?<project>.*?)\"".*?\""(?<guid>.*?)\""(?<all>[\s\S]*?)EndProject", RegexOptions.Multiline);
            MatchCollection projectMatches = reProjects.Matches(solutionContent);

            Hashtable explicitProjectDependencies = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach (Match projectMatch in projectMatches) {
                string project = projectMatch.Groups["project"].Value;
                string guid = projectMatch.Groups["guid"].Value;
                string package = projectMatch.Groups["package"].Value;

                // bug #1732361: skip solution folders
                if (package == SolutionFolder_GUID) {
                    continue;
                }

                // translate partial project path or URL to absolute path
                string fullProjectPath = TranslateProjectPath(solutionTask.SolutionFile.DirectoryName,
                    project);

                if (ManagedProjectBase.IsEnterpriseTemplateProject(fullProjectPath)) {
                    RecursiveLoadTemplateProject(fullProjectPath);
                } else {
                    // add project entry to collection
                    ProjectEntries.Add(new ProjectEntry(guid, fullProjectPath));
                }

                // set-up project dependencies
                Regex reDependencies = new Regex(@"^\s+(?<guid>\{[0-9a-zA-Z]{8}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{12}\})\s+=\s+(?<dep>\{[0-9a-zA-Z]{8}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{12}\})", RegexOptions.Multiline);
                MatchCollection dependencyMatches = reDependencies.Matches(projectMatch.Value);

                foreach (Match dependencyMatch in dependencyMatches) {
                    string dependency = dependencyMatch.Groups["dep"].Value;

                    if (!explicitProjectDependencies.ContainsKey(guid)) {
                        explicitProjectDependencies[guid] = CollectionsUtil.CreateCaseInsensitiveHashtable();
                    }
                    ((Hashtable)explicitProjectDependencies[guid])[dependency] = null;
                }

                // set-up project configuration 
                Regex reProjectBuildConfig = new Regex(@"^\s+" + Regex.Escape(guid) + @"\.(?<solutionConfiguration>[^|]+)\|(?<solutionPlatform>[^.]+)\.Build\.0\s*=\s*(?<projectConfiguration>[^|]+)\|(?<projectPlatform>[\.\w ]+)\s*", RegexOptions.Multiline);
                MatchCollection projectBuildMatches = reProjectBuildConfig.Matches(solutionContent);

                ProjectEntry projectEntry = ProjectEntries [guid];
                if (projectEntry == null) {
                    // TODO: determine if we should report an error if a build
                    // configuration is defined for a project that does not
                    // exist in the solution
                    continue;
                }

                // holds mapping between project configuration(s) and solution(s)
                ConfigurationMap buildConfigurations = new ConfigurationMap(
                    projectBuildMatches.Count);

                for (int i = 0; i < projectBuildMatches.Count; i++) {
                    Match projectBuildMatch = projectBuildMatches [i];
                    string solutionConfigName = projectBuildMatch.Groups["solutionConfiguration"].Value;
                    string solutionPlatform = projectBuildMatch.Groups["solutionPlatform"].Value;
                    string projectConfigName = projectBuildMatch.Groups["projectConfiguration"].Value;
                    string projectPlatform = projectBuildMatch.Groups["projectPlatform"].Value;
                    Configuration solutionConfig = new Configuration(
                        solutionConfigName, solutionPlatform);
                    Configuration projectConfig = new Configuration(
                        projectConfigName, projectPlatform);
                    buildConfigurations [solutionConfig] = projectConfig;
                }

                // add map to corresponding project entry
                projectEntry.BuildConfigurations = buildConfigurations;
            }

            LoadProjectGuids(new ArrayList(solutionTask.Projects.FileNames), false);
            LoadProjectGuids(new ArrayList(solutionTask.ReferenceProjects.FileNames), true);
            LoadProjects(gacCache, refResolver, explicitProjectDependencies);
        }
    }
}
