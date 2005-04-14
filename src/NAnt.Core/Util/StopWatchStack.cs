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

namespace NAnt.Core.Util {
    public class StopWatchStack {
        private readonly DateTimeProvider _dtProvider;
        private readonly Stack _stack = new Stack();

        public StopWatchStack(DateTimeProvider dtProvider) {
            _dtProvider = dtProvider;
        }

        public void PushStart() {
            _stack.Push(new StopWatch(_dtProvider));
        }

        public TimeSpan PopStop() {
            return ((StopWatch) _stack.Pop()).Elapsed();
        }

        private class StopWatch {
            private readonly DateTimeProvider _dtProvider;
            private readonly DateTime _start;

            public StopWatch(DateTimeProvider dtProvider) {
                _dtProvider = dtProvider;
                _start = dtProvider.Now;
            }

            public TimeSpan Elapsed() {
                return _dtProvider.Now - _start;
            }
        }
    }
}