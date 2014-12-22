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
using System.Diagnostics;

using NAnt.Core;

namespace NAnt.MSBuild {
    internal sealed class MSBuildEngine {
        private static NAnt.MSBuild.BuildEngine.Engine _msbuild;

        public static NAnt.MSBuild.BuildEngine.Engine CreateMSEngine(NAnt.VSNet.Tasks.SolutionTask solutionTask) {
            if (_msbuild!=null) {
                return _msbuild;
            }

            try {
                _msbuild = NAnt.MSBuild.BuildEngine.Engine.LoadEngine(solutionTask.Project.TargetFramework);
            } catch (Exception e) {
                throw new BuildException(
                    String.Format(
                        "MSBuild v{0} can't be found. It is needed for building MSBuild projects. VS2005 and later is using MSBuild projects for C# and VB",
                        solutionTask.Project.TargetFramework.Version),
                    Location.UnknownLocation, e);
            }
            _msbuild.UnregisterAllLoggers();

            NAntLoggerVerbosity _verbosity = solutionTask.Verbose ? NAntLoggerVerbosity.Normal : NAntLoggerVerbosity.Minimal;
            NAntLogger _logger = NAntLogger.Create(solutionTask.Project.TargetFramework, solutionTask, _verbosity, _msbuild);
            if (_logger != null) {
                _msbuild.RegisterLogger(_logger);
            }
            
            solutionTask.Log(Level.Verbose, "Using MSBuild version {0}.", FileVersionInfo.GetVersionInfo(_msbuild.Assembly.Location).ProductVersion);

            return _msbuild;
        }

        //private static TargetDotNetFrameworkVersion GetTargetDotNetFrameworkVersion (FrameworkInfo framework) {
        //    switch (framework.ClrVersion.ToString (2)) {
        //        case "1.1":
        //            return TargetDotNetFrameworkVersion.Version11;
        //        case "2.0":
        //            return TargetDotNetFrameworkVersion.Version20;
        //        default:
        //            throw new BuildException ("Current target framework is not supported.",
        //                Location.UnknownLocation);
        //    }
        //}
    }
}
