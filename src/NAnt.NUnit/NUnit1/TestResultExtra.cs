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

// Ian MacLean (ian_maclean@another.com)

using System;
using System.Text;
using NUnit.Framework;

namespace SourceForge.NAnt.Tasks.NUnit {

	/// <summary>
	/// This is purely to decorate NUnits TestResult with extra information such as run-time etc ...
	///  TODO come up with a better name
	///</summary>
	public class TestResultExtra : TestResult {
		
		long _runTime; // in milliseconds ?
		 
		// Attribute properties
        public long RunTime        { get { return _runTime; } set { _runTime = value; } }
		
		public TestResultExtra() {
			
		}
	}
}
