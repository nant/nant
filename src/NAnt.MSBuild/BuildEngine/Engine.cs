// NAnt - A .NET build tool
// Copyright (C) 2001-2011 Gerry Shaw
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
using System.Reflection;
using System.IO;

namespace NAnt.MSBuild.BuildEngine {
    internal class Engine : MarshalByRefObject {
        object _obj;
        Type _t; 
        Assembly _a;

        private Engine() {
        }

        public static Engine LoadEngine(NAnt.Core.FrameworkInfo framework) {
            //System.AppDomainSetup myDomainSetup = new System.AppDomainSetup();
            //myDomainSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            //myDomainSetup.ApplicationName = "MSBuild";

            //string tempFile = Path.GetTempFileName();
            //using (StreamWriter sw = File.CreateText(tempFile))
            //{
            //    sw.WriteLine(String.Format(
            //        "<?xml version='1.0'?><configuration><runtime>"
            //        + "<assemblyBinding xmlns='urn:schemas-microsoft-com:asm.v1'>"
            //        + "<dependentAssembly><assemblyIdentity name='Microsoft.Build.Framework' publicKeyToken='b03f5f7f11d50a3a' culture='neutral'/><bindingRedirect oldVersion='0.0.0.0-99.9.9.9' newVersion='{0}'/></dependentAssembly>"
            //        + "<dependentAssembly><assemblyIdentity name='Microsoft.Build.Engine' publicKeyToken='b03f5f7f11d50a3a' culture='neutral'/><bindingRedirect oldVersion='0.0.0.0-99.9.9.9' newVersion='{0}'/></dependentAssembly>"
            //        + "</assemblyBinding></runtime></configuration>",
            //        new Version(framework.Version.Major, framework.Version.Minor, 0, 0)));
            //}

            //myDomainSetup.ConfigurationFile = tempFile;// AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            //var executionAD = AppDomain.CreateDomain(myDomainSetup.ApplicationName,
            //    AppDomain.CurrentDomain.Evidence, myDomainSetup);
            //AppDomain.CurrentDomain.AssemblyLoad += new AssemblyLoadEventHandler(CurrentDomain_AssemblyLoad);
            //executionAD.AssemblyLoad += new AssemblyLoadEventHandler(executionAD_AssemblyLoad);

            //File.Delete(tempFile);

            //Loader l = (Loader)executionAD.CreateInstanceAndUnwrap(typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
            Loader l = new Loader();
            l.framework = framework;
            //executionAD.DoCallBack(new CrossAppDomainDelegate(l.DoLoad));
            l.DoLoad();
            return l.engine;            
        }

        //[Serializable]
        public class Loader : MarshalByRefObject {
            public NAnt.Core.FrameworkInfo framework;
            public Engine engine;

            public void DoLoad() {
                engine = new Engine();
                string pth = Path.Combine(framework.FrameworkDirectory.FullName, "Microsoft.Build.Engine.dll");
                if (File.Exists(pth)) {
                    engine._a = Assembly.LoadFile(pth);
                } else {
                    //frameworks 3.0 and 3.5 do not copy its assemblies into filesystem. They reside just in assembly cache (GAC)

                    //Microsoft.Build.Engine, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
                    AssemblyName an = new AssemblyName("Microsoft.Build.Engine");
                    an.Version = new Version(framework.Version.Major, framework.Version.Minor, 0, 0);
                    an.CultureInfo = System.Globalization.CultureInfo.InvariantCulture;
                    an.SetPublicKeyToken(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a });

                    engine._a = Assembly.Load(an); //load from GAC
                }
                engine._t = engine._a.GetType("Microsoft.Build.BuildEngine.Engine");
                engine._obj = Activator.CreateInstance(engine._t);

                //2.0
                if (engine._a.GetName().Version.Major == 2) {
                    engine._t.GetProperty("BinPath").SetValue(engine._obj, framework.FrameworkDirectory.FullName, null);
                }
            }
        }

        static void executionAD_AssemblyLoad(object sender, AssemblyLoadEventArgs args) {
        }

        static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args) {
        }

        internal Assembly Assembly {
            get { return _a; }
        }

        internal object Object {
            get { return _obj; }
        }

        public void UnregisterAllLoggers() {
            _t.GetMethod("UnregisterAllLoggers").Invoke(_obj, null);
        }

        public void RegisterLogger(/*ILogger*/object logger) {
            _t.GetMethod("RegisterLogger").Invoke(_obj, new object[] {logger});
        }
    }
}
