// NAnt - A .NET build tool
// Copyright (C) 2002-2003 Gerry Shaw
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

// Scott Hernandez(ScottHernandez@hotmail.com)

using System;
using System.IO;
using System.Configuration;
using System.Reflection;

namespace SourceForge.NAnt {
    /// <summary>
    /// Stub used to created AppDamain and launch real ConsoleDriver class in Core assembly.
    /// </summary>
    public class ConsoleStub {
        /// <summary>
        /// Entry point for executable
        /// </summary>
        /// <param name="args">Command Line arguments</param>
        /// <returns>The result of the real execution</returns>
        public static int Main(string[] args) {
            AppDomain cd = AppDomain.CurrentDomain;
            AppDomain executionAD = cd;

            string nantShadowCopyFilesSetting = ConfigurationSettings.AppSettings.Get("nant.shadowfiles");
            string nantCleanupShadowCopyFilesSetting = ConfigurationSettings.AppSettings.Get("nant.shadowfiles.cleanup");
            
            if(nantShadowCopyFilesSetting != null && bool.Parse(nantShadowCopyFilesSetting) == true) {

                System.AppDomainSetup myDomainSetup = new System.AppDomainSetup();

                myDomainSetup.PrivateBinPath = myDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                myDomainSetup.ApplicationName = "NAnt";
                //yes, cache the files
                myDomainSetup.ShadowCopyFiles = "true";
                //but what files you say... everything in ".", "./bin", "./tasks" .
                myDomainSetup.ShadowCopyDirectories=myDomainSetup.PrivateBinPath + ";" + Path.Combine(myDomainSetup.PrivateBinPath,"bin") + ";" + Path.Combine(myDomainSetup.PrivateBinPath,"tasks") + ";";

                //try to cache in .\cache folder, if that fails, let the system figure it out.
                string cachePath = Path.Combine(myDomainSetup.ApplicationBase, "cache");
                DirectoryInfo cachePathInfo = null;
                try{
                   cachePathInfo = Directory.CreateDirectory(cachePath);
                }
                catch(Exception e){
                    Console.WriteLine("Failed to create: {0}. Using default CachePath.", cachePath);
                }
                finally{
                    if(cachePathInfo != null)
                        myDomainSetup.CachePath = cachePathInfo.FullName;
                }

                //create the domain.
                //Console.WriteLine("Caching to {0}", myDomainSetup.CachePath);
                executionAD = AppDomain.CreateDomain(myDomainSetup.ApplicationName, null, myDomainSetup);

                //Console.WriteLine("Using new AppDomain and shadowCopyFiles({0}) enabled={1}",executionAD.SetupInformation.CachePath, executionAD.ShadowCopyFiles);
            }

            //use helper object to hold (and serialize) args for callback.
            helperArgs helper = new helperArgs(args);
            executionAD.DoCallBack(new CrossAppDomainDelegate(helper.CallConsoleRunner));

            //unload if remote/new appdomain
            if(!cd.Equals(executionAD)) {
                
                /*
                Console.WriteLine();
                Console.WriteLine("> Unloading AppDomain and Assemblies");
                foreach(Assembly ass in executionAD.GetAssemblies()){
                    Console.WriteLine("> {0}", ass.Location, ass.CodeBase);
                }
                */

                AppDomain.Unload(executionAD);
                if(nantCleanupShadowCopyFilesSetting != null && bool.Parse(nantCleanupShadowCopyFilesSetting) == true) {
                    Directory.Delete(executionAD.SetupInformation.CachePath);
                }
            }

            return helper.Return;
        }

        [Serializable]
        public class helperArgs {
            private string[] args = null;
            private int ret = 0;

            private helperArgs(){}
            public helperArgs(string[] args0) {this.args = args0;}

            public void CallConsoleRunner() {
                //load the core by name!
                Assembly nantCore = AppDomain.CurrentDomain.Load("NAnt.Core");
                //get the ConsoleDriver by name
                Type consoleDriverType = nantCore.GetType("SourceForge.NAnt.ConsoleDriver", true, true);
                MethodInfo mainMethodInfo = null;
                //find the Main Method, this method is less than optimal, but other methods failed.
                foreach(MethodInfo meth in consoleDriverType.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                    if(meth.Name.Equals("Main"))
                        mainMethodInfo = meth;
                }

                ret = (int) mainMethodInfo.Invoke(null, new Object[] {args});
            }
            public int Return { get { return ret;}}
        }
    }
}
