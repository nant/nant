// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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
// Tomas Restrepo (tomasr@mvps.org)

using NUnit.Framework;
using NUnit.Runner;
using SourceForge.NAnt.Tasks.NUnit.Formatters;

namespace SourceForge.NAnt.Tasks.NUnit 
{

   using System;
   using System.Xml;
   
   using SourceForge.NAnt.Attributes;
    
   public class RemoteNUnitTestRunner : MarshalByRefObject 
   {
      private NUnitTestRunner _runner;

      public RunnerResult ResultCode {
         get { return _runner.ResultCode; }
      }
      public FormatterCollection Formatters {
         get { return _runner.Formatters; }
      }

      public RemoteNUnitTestRunner(NUnitTestData testData)
      {
         _runner = new NUnitTestRunner(testData);

      }

      public void Run(string logPrefix, bool verbose)
      {
         _runner.Run(logPrefix, verbose);
      }
                  
   }    
}