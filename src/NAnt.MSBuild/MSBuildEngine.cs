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
using System.Text;

using NAnt.Core;
using NAnt.Core.Tasks;

namespace NAnt.MSBuild {
    internal sealed class MSBuildEngine {
        private static Microsoft.Build.BuildEngine.Engine _msbuild;

        public static Microsoft.Build.BuildEngine.Engine CreateMSEngine(NAnt.VSNet.Tasks.SolutionTask solutionTask) {
            if (_msbuild!=null) {
                return _msbuild;
            }
            _msbuild = new Microsoft.Build.BuildEngine.Engine(
                solutionTask.Project.TargetFramework.FrameworkDirectory.FullName
                );
            _msbuild.UnregisterAllLoggers();

            _msbuild.RegisterLogger(
                new NAntLogger(solutionTask,
                solutionTask.Verbose ? Microsoft.Build.Framework.LoggerVerbosity.Normal : Microsoft.Build.Framework.LoggerVerbosity.Minimal
                )
                );

            /*
            foreach (PropertyTask property in solutionTask.CustomProperties) {
                string val;
                // expand properties in context of current project for non-dynamic properties
                if (!property.Dynamic) {
                    val = solutionTask.Project.ExpandProperties(property.Value, solutionTask.Location);
                }
                else
                    val = property.Value;
                _msbuild.GlobalProperties.SetProperty(property.PropertyName, val);
            }
            */

            return _msbuild;
        }
    }
}
