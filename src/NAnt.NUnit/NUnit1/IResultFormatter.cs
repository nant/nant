// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Ian MacLean (ian_maclean@another.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System.IO;

using NUnit.Framework;

namespace NAnt.NUnit1.Types {
    public interface IResultFormatter : ITestListener {
        /// <summary>
        /// The whole test suite started.
        /// </summary>
        void StartTestSuite(NUnitTestData suite); 

        /// <summary>
        /// The whole test suite ended.
        /// </summary>
        void EndTestSuite(TestResultExtra result);

        /// <summary>
        /// Sets the <see cref="TextWriter" /> the formatter is supposed to write 
        /// its results to.
        /// </summary>
        void SetOutput(TextWriter writer);
    }
}
