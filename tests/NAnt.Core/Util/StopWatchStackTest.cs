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

using NUnit.Framework;

using NAnt.Core.Util;

namespace Tests.NAnt.Core.Util {
    [TestFixture]
    public class StopWatchStackTest {
        private StopWatchStack stack;
        private MockDateTimeProvider mockDateTimeProvider;
        private DateTime startTime;

        [SetUp]
        protected void CreateStopWatchStackAndSetUpMocks() {
            startTime = new DateTime(2004, 12, 1, 12, 0, 0);
            mockDateTimeProvider = new MockDateTimeProvider();
            stack = new StopWatchStack(mockDateTimeProvider);
        }

        [Test]
        public void ShouldPushAndPopSingleStopWatch() {
            mockDateTimeProvider.SetExpectedNow(startTime); // start stop watch
            mockDateTimeProvider.SetExpectedNow(startTime.AddMilliseconds(2)); // stop stop watch

            stack.PushStart();
            TimeSpan elapsed = stack.PopStop();
            Assert.IsNotNull(elapsed);
            Assert.AreEqual(2, elapsed.Milliseconds, "two milliseconds should have elapsed");
        }

        [Test]
        public void ShouldPushAndPopMultipleStopWatches() {
            mockDateTimeProvider.SetExpectedNow(startTime); // start first stop watch
            mockDateTimeProvider.SetExpectedNow(startTime.AddMilliseconds(2)); // start second stop watch
            mockDateTimeProvider.SetExpectedNow(startTime.AddMilliseconds(4)); // stop second stop watch
            mockDateTimeProvider.SetExpectedNow(startTime.AddMilliseconds(4)); // stop first stop watch

            stack.PushStart();
            stack.PushStart();
            Assert.AreEqual(2, stack.PopStop().Milliseconds, "two milliseconds should have elapsed for the second stopwatch");
            Assert.AreEqual(4, stack.PopStop().Milliseconds, "four milliseconds should have elapsed for the first stopwatch");
        }
    }
}