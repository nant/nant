// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Scott Hernandez
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

// Scott Hernandez (ScottHernandez@hotmail.com)

using System;
using System.Collections.Specialized;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Configuration;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks {

    /// <summary>Provides the abstract base class for a Microsoft .Net Framework SDK external program task.</summary>
    public abstract class MsftFXSDKExternalProgramBase : ExternalProgramBase {
        protected static string FXBin = null;
        public override string ProgramFileName  {
            get { 
                return ProgramFilepath(this);
            } 
        }
        /// <summary>
        /// Instead of relying on the .NET external program to be in the user's path, point
        /// to the compiler directly since it lives in the .NET Framework's bin directory.
        /// <note>This method also checks for the existance of the end filepath against the filesystem!</note>
        /// </summary>
        /// <param name="epb">The External Program to lookup info for.</param>
        /// <returns>A fully qualifies pathname including the program name. Null is returned if there are any errors or the combined filepath is not found!</returns>
        public static string ProgramFilepath(ExternalProgramBase epb) {
            
            string enableLookup = epb.Project.Properties["doNotFind.dotnet.exes"];
            if(enableLookup != null && bool.Parse(enableLookup) == true)
                return epb.Name;

            string sdkInstallPath = null;
            try{
                if(FXBin == null) {
                    RegistryKey dotNetFXKey = Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Microsoft").OpenSubKey(".NETFramework");
                    sdkInstallPath = dotNetFXKey.GetValue("sdkInstallRoot").ToString();
                }
            }
            catch(Exception e){ /*no-op*/ }

            string pfn = null;
            if(sdkInstallPath != null){
                try {
                    pfn = Path.Combine(Path.Combine(sdkInstallPath,"bin"), epb.Name + ".exe");
                }
                catch {
                    //no-op, ignore the error
                }
            }
            if(pfn != null && File.Exists(pfn))
                return pfn;
            else 
                return epb.Name;
        }
    }
}
