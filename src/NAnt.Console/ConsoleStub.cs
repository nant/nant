using System;
using System.IO;
using System.Configuration;
using System.Reflection;

namespace SourceForge.NAnt {
    /// <summary>
    /// Summary description for ConsoleStub.
    /// </summary>
    public class ConsoleStub {
        public static int Main(string[] args) {
            AppDomain cd = AppDomain.CurrentDomain;
            AppDomain executionAD = cd;

            string nantShadowCopyFilesSetting = ConfigurationSettings.AppSettings.Get("nant.shadowfiles");
            
            if(nantShadowCopyFilesSetting != null && bool.Parse(nantShadowCopyFilesSetting) == true) {

                System.AppDomainSetup myDomainSetup = new System.AppDomainSetup();

                myDomainSetup.PrivateBinPath = myDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
                myDomainSetup.ApplicationName = "NAnt";
                myDomainSetup.ShadowCopyFiles = "true";
                myDomainSetup.CachePath = Path.Combine(myDomainSetup.ApplicationBase, "cache");

                //myDomainSetup.ShadowCopyDirectories = Path.Combine(cd.BaseDirectory, "NAntShadowFiles");
                executionAD = AppDomain.CreateDomain( "Loading new Domain", null, myDomainSetup);
                
                //executionAD = AppDomain.CreateDomain("NAnt", cd.Evidence, cd.BaseDirectory, cd.RelativeSearchPath, true);
                Console.WriteLine("Using new AppDomain and shadowCopyFiles({0}) enabled={1}",executionAD.SetupInformation.CachePath, executionAD.ShadowCopyFiles);
            }

            object consoleDriver = executionAD.CreateInstanceFromAndUnwrap(Path.Combine(cd.BaseDirectory, "NAnt.Core.dll"), "SourceForge.NAnt.ConsoleDriver");
            Type consoleDriverType = consoleDriver.GetType();
            MethodInfo mainMethodInfo = null;
            foreach(MethodInfo meth in consoleDriverType.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                if(meth.Name.Equals("Main"))
                    mainMethodInfo = meth;
            }

            int ret = (int) mainMethodInfo.Invoke(consoleDriver, new Object[] {args});
            
            // didn't work, couldn't find method (missingmethodexception generated)
            //int ret = (int)consoleDriverType.InvokeMember("Main", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy | BindingFlags.Public, null, consoleDriver, args);
            
            //Failed with unknown com exception
            //int ret = executionAD.ExecuteAssembly(Path.Combine(cd.BaseDirectory, "NAnt.Core.dll"), null, args);
            
            if(!cd.Equals(executionAD))
                AppDomain.Unload(executionAD);

            return ret;
        }
    }
}
