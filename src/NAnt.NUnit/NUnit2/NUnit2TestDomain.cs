// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

// Tomas Restrepo (tomasr@mvps.org)


using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Xml;
using NUnit.Framework;
using NUnit.Core;

namespace SourceForge.NAnt.Tasks.NUnit2 
{

   /// <summary>
   /// Custom TestDomain class, similar to the one
   /// included with NUnit 2.0, in order to workaround
   /// some limitations in it.
   /// </summary>
   internal class NUnit2TestDomain
   {

      private TextWriter _outStream;
      private TextWriter _errorStream;

      public NUnit2TestDomain(TextWriter outStream, TextWriter errorStream)
      {
         _outStream = outStream;
         _errorStream = errorStream;
      }


      /// <summary>
      /// Create a testsuite and run it from an assembly
      /// </summary>
      /// <param name="assemblyFile"></param>
      /// <param name="configFilePath"></param>
      /// <param name="listener"></param>
      /// <returns></returns>
      public TestResult Run ( 
                  string assemblyFile, 
                  string configFilePath, 
                  EventListener listener
               )
      {
         string assemblyDir = Path.GetDirectoryName(assemblyFile);
         AppDomain domain = CreateDomain(assemblyDir, configFilePath);

         string currentDir = Directory.GetCurrentDirectory();
         Directory.SetCurrentDirectory(assemblyDir);

         try 
         {
            RemoteTestRunner runner = CreateTestRunner(domain);

            runner.Initialize(assemblyFile);
            domain.DoCallBack(new CrossAppDomainDelegate(runner.BuildSuite));
            return runner.Run(listener, _outStream, _errorStream);

         } finally {
            Directory.SetCurrentDirectory(currentDir);
            AppDomain.Unload(domain);
         }
      }

      /// <summary>
      /// Run a single testcase
      /// </summary>
      /// <param name="testcase"></param>
      /// <param name="assemblyFile"></param>
      /// <param name="configFilePath"></param>
      /// <param name="listener"></param>
      /// <returns></returns>
      public TestResult RunTest ( 
                  string testcase, string assemblyFile,
                  string configFilePath, EventListener listener
               )
      {
         string assemblyDir = Path.GetDirectoryName(assemblyFile);
         AppDomain domain = CreateDomain(assemblyDir, configFilePath);

         string currentDir = Directory.GetCurrentDirectory();
         Directory.SetCurrentDirectory(assemblyDir);

         try 
         {
            RemoteTestRunner runner = CreateTestRunner(domain);
            runner.Initialize(testcase, assemblyFile);
            domain.DoCallBack(new CrossAppDomainDelegate(runner.BuildSuite));
            return runner.Run(listener, _outStream, _errorStream);

         } finally {
            Directory.SetCurrentDirectory(currentDir);
            AppDomain.Unload(domain);
         }
      }

      private AppDomain CreateDomain(string basedir, string configFilePath)
      {
         // spawn new domain in specified directory
         AppDomainSetup domSetup = new AppDomainSetup();
         domSetup.ApplicationBase = basedir;
         domSetup.ConfigurationFile = configFilePath;
         domSetup.ApplicationName = "NAnt NUnit2.0 Remote Domain";
         
         return AppDomain.CreateDomain(domSetup.ApplicationName, null, domSetup);
      }

      private RemoteTestRunner CreateTestRunner(AppDomain domain)
      {
          ObjectHandle oh = domain.CreateInstance ( 
                                 typeof(RemoteTestRunner).Assembly.FullName, 
                                 typeof(RemoteTestRunner).FullName, 
                                 false, 0, null, null, null, null, null
                              );
         return (RemoteTestRunner)oh.Unwrap();
      }


      
   } // class NUnit2TestDomain
   
} // namespace SourceForge.NAnt.Tasks.NUnit2 
