// NAnt - A .NET build tool
// Copyright (C) 2001-2005 Gerry Shaw
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
// Owen Rogers (exortech@gmail.com)


using System;
using System.Collections;

using NAnt.Core.Util;

namespace Tests.NAnt.Core.Util {
    internal class MockDateTimeProvider : DateTimeProvider {
        private Queue _expectations = new Queue();

        public override DateTime Now {
            get { return (DateTime) _expectations.Dequeue(); }
        }

        public void SetExpectedNow(DateTime value) {
            _expectations.Enqueue(value);
        }
    }
}
