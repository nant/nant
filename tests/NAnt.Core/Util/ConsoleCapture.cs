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

// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Text;

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

    /// <summary>Captures console output to a string</summary>
    /// <remarks>
    ///     <para>Used to capture the output so that it can be tested.</para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///         using (ConsoleCapture c = new ConsoleCapture()) {
    ///             Console.WriteLine("Hello World");
    ///             string result = c.Close();
    ///             Console.WriteLine("cached results: '{0}'", result);
    ///         }
    ///     </code>
    /// </example
    public sealed class ConsoleCapture : IDisposable {
        bool _disposed = false;

        StringWriter _writer;
        TextWriter   _oldWriter;

        public ConsoleCapture() {
            _oldWriter = Console.Out;
            _writer = new StringWriter();
            Console.SetOut(_writer);
        }

        ~ConsoleCapture() {
            Dispose();
        }

        /// <summary>Restores Console output and returns the contents of the captured buffer.</summary>
        public string Close() {
            if (_disposed) {
                throw new ObjectDisposedException("ConsoleCapture", "Capture has already been closed/disposed.");
            }
            Dispose();
            return ToString();
        }

        public void Dispose() {
            if (!_disposed) {
                _writer.Flush();
                _writer.Close();
                Console.SetOut(_oldWriter);
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>Returns the contents of the capture buffer.  Can be called after Close() is called.</summary>
        public override string ToString() {
            return _writer.ToString();
        }
    }
}
