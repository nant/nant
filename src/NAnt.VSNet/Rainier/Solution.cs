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
// Matthew Mastracci (matt@aclaro.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Util;

using NAnt.VSNet;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet.Rainier {
    /// <summary>
    /// Analyses Microsoft Visual Studio .NET 2002 (Rainier) solution files.
    /// </summary>
    public class Solution : SolutionBase {
        public Solution(string solutionContent, SolutionTask solutionTask, TempFileCollection tfc, GacCache gacCache, ReferencesResolver refResolver) : base(solutionTask, tfc, gacCache, refResolver) {
            Regex reProjects = new Regex(@"Project\(\""(?<package>\{.*?\})\"".*?\""(?<name>.*?)\"".*?\""(?<project>.*?)\"".*?\""(?<guid>.*?)\""(?<all>[\s\S]*?)EndProject", RegexOptions.Multiline);
            MatchCollection projectMatches = reProjects.Matches(solutionContent);

            foreach (Match projectMatch in projectMatches) {
                string project = projectMatch.Groups["project"].Value;
                string guid = projectMatch.Groups["guid"].Value;

                // translate partial project path or URL to absolute path
                string fullProjectPath = TranslateProjectPath(solutionTask.SolutionFile.DirectoryName,
                    project);

                if (Project.IsEnterpriseTemplateProject(fullProjectPath)) {
                    RecursiveLoadTemplateProject(fullProjectPath);
                } else {
                    // only C#, VB.NET and C++ projects are supported at this moment
                    if (!ProjectFactory.IsSupportedProjectType(project)) {
                        // output a warning message in the build log
                        Log(Level.Warning, "Only C#, VB.NET and C++ projects" +
                            " are supported.  Skipping project '{0}'.", project);
                        // skip the project
                        continue;
                    }

                    // add project path to collection
                    ProjectFiles[guid] = fullProjectPath;
                }

                // set-up project dependencies
                Regex reDependencies = new Regex(@"^\s+" + guid + @"\.[0-9]+ = (?<dep>\{\S*\}?)\s*$", RegexOptions.Multiline);
                MatchCollection dependencyMatches = reDependencies.Matches(solutionContent);

                foreach (Match dependencyMatch in dependencyMatches) {
                    string dependency = dependencyMatch.Groups["dep"].Value;
                    AddProjectDependency(guid, dependency);
                }

                // set-up project configuration
                Regex reProjectBuildConfig = new Regex(@"^\s+" + guid + @"\.(?<configuration>[a-zA-Z]+)\.Build\.0\s+\S+", RegexOptions.Multiline);
                MatchCollection projectBuildMatches = reProjectBuildConfig.Matches(solutionContent);

                // initialize hashtable that will hold the project build configurations
                Hashtable projectBuildConfiguration = CollectionsUtil.CreateCaseInsensitiveHashtable();

                if (projectBuildMatches.Count > 0) {
                    foreach (Match projectBuildMatch in projectBuildMatches) {
                        string configuration = projectBuildMatch.Groups["configuration"].Value;
                        projectBuildConfiguration[configuration] = null;
                    }
                }

                // add project build configuration, this signals that project was 
                // loaded in context of solution file
                ProjectBuildConfigurations[guid] = projectBuildConfiguration;
            }

            LoadProjectGuids(new ArrayList(solutionTask.Projects.FileNames), false);
            LoadProjectGuids(new ArrayList(solutionTask.ReferenceProjects.FileNames), true);
            LoadProjects(gacCache, refResolver);
        }
    }
}
