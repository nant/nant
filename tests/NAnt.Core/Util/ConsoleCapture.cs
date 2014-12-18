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
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;

namespace Tests.NAnt.Core.Util {

    /// <summary>
    /// Captures console output to a string.
    /// </summary>
    /// <remarks>
    /// Used to capture the output so that it can be tested.
    /// </remarks>
    /// <example>
    ///     <code>
    /// using (ConsoleCapture c = new ConsoleCapture()) {
    ///     Console.WriteLine("Hello World");
    ///     string result = c.Close();
    ///     Console.WriteLine("cached results: '{0}'", result);
    /// }
    ///     </code>
    /// </example>
    public sealed class ConsoleCapture : IDisposable {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleCapture" />
        /// class.
        /// </summary>
        public ConsoleCapture() {
            _oldWriter = System.Console.Out;
            _oldErrorWriter = System.Console.Error;
            _writer = new ConsoleWriter();
            System.Console.SetOut(_writer);
            System.Console.SetError(_writer);
        }

        #endregion Public Instance Constructors

        #region Finalizer

        ~ConsoleCapture() {
            Dispose();
        }

        #endregion Finalizer

        #region Implementation of IDisposable

        public void Dispose() {
            if (!_disposed) {
                Close ();
                _writer.Close ();
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion Implementation of IDisposable

        #region Override implementation of Object

        /// <summary>
        /// Returns the contents of the capture buffer.  Can be called after 
        /// <see cref="Close()" /> is called.
        /// </summary>
        public override string ToString() {
            return _writer.ToString();
        }

        #endregion Override implementation of Object

        #region Public Instance Methods

        /// <summary>
        /// Restores console output and returns the contents of the captured 
        /// buffer.
        /// </summary>
        public string Close() {
            if (_disposed) {
                throw new ObjectDisposedException("ConsoleCapture", 
                    "Capture has already been closed/disposed.");
            }

            _writer.Flush();
            System.Console.SetOut(_oldWriter);
            System.Console.SetError(_oldErrorWriter);
            return ToString();
        }

        #endregion Public Instance Methods

        #region Private Instance Fields

        private bool _disposed;
        private ConsoleWriter _writer;
        private TextWriter   _oldWriter;
        private TextWriter   _oldErrorWriter;

        #endregion Private Instance Fields
    }
}
