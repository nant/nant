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

using System;

using NAnt.NUnit1.Types;

namespace NAnt.NUnit1.Tasks {
    public class RemoteNUnitTestRunner : MarshalByRefObject {
        #region Public Instance Constructors

        public RemoteNUnitTestRunner(NUnitTestData testData) {
            _runner = new NUnitTestRunner(testData);
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        public RunnerResult ResultCode {
            get { return _runner.ResultCode; }
        }

        public IResultFormatterCollection Formatters {
            get { return _runner.Formatters; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public void Run(string logPrefix, bool verbose) {
            _runner.Run(logPrefix, verbose);
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private NUnitTestRunner _runner;

        #endregion Private Instance Fields
    }
}
