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

using System;
using System.IO;

namespace NAnt.Core.Filters {
    /// <summary>
    /// Functions as a chainable TextReader
    /// </summary>
    /// <remarks>
    /// Implements a abstraction over a TextReader that allows the class to represent
    /// either a TextReader or another ChainableReader to which it is chained.
    ///
    /// By passing a ChainableReader as a constructor paramater it is possiable to
    /// chain many ChainableReaders together.  The last ChainableReader in the chain must
    /// be based on a TextReader.
    /// </remarks>
    public abstract class ChainableReader : Element, IDisposable {
        //Delegates for supported common functionality in
        //encapsulated TextReader and ChainableReader
        private delegate int internalRead();
        private delegate int internalPeek();
        private delegate void internalClose();

        //Point to the appropriate methods
        //in a TextReader or a ChainableReader.
        private internalRead InternalRead;
        private internalPeek InternalPeek;
        private internalClose InternalClose;

        private bool _baseReader;

        #region Public Instance Properties

        /// <summary>
        /// Gets a value indicating if the reader is backed by a stream in the 
        /// chain.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the reader is backed by a stream;
        /// otherwise, <see langword="false" />.
        /// </value>
        public bool Base{
            get { return _baseReader; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Makes it so all calls to Read and Peek are passed  the ChainableReader
        /// passed as a parameter.
        /// </summary>
        /// <param name="parentChainedReader">ChainableReader to forward calls to</param>
        public virtual void Chain(ChainableReader parentChainedReader) {
            if (parentChainedReader == null) {
                throw new ArgumentNullException("parentChainedReader", "Argument can not be null");
            }

            //Assign delegates
            InternalRead = new internalRead(parentChainedReader.Read);
            InternalPeek = new internalPeek(parentChainedReader.Peek);
            InternalClose = new internalClose(parentChainedReader.Close);

            //This is just a reader in the chain
            _baseReader = false;
        }

        /// <summary>
        /// Makes it so all calls to Read and Peek are passed the TextReader
        /// passed as a parameter.
        /// </summary>
        /// <param name="baseReader">TextReader to forward calls to</param>
        public virtual void Chain(TextReader baseReader) {
            if (baseReader == null) {
                throw new ArgumentNullException("baseReader", "Argument can not be null");
            }

            // Assign delegates
            InternalRead = new internalRead(baseReader.Read);
            InternalPeek = new internalPeek(baseReader.Peek);
            InternalClose = new internalClose(baseReader.Close);

            // This is the base reader
            _baseReader = true;
        }

        /// <summary>
        /// Forwards Peek calls to the TextReader or ChainableReader passed in the corresponding constructor.
        /// </summary>
        /// <returns>Character or -1 if end of stream</returns>
        public virtual int Peek() {
            return InternalPeek();
        }

        /// <summary>
        /// Forwards Read calls to the TextReader or ChainableReader passed in the corresponding constructor.
        /// </summary>
        /// <returns>
        /// Character or -1 if end of stream.
        /// </returns>
        public virtual int Read() {
            return InternalRead();
        }

        /// <summary>
        /// Closes the reader.
        /// </summary>
        public virtual void Close() {
            InternalClose();
        }

        #endregion Public Instance Methods

        #region Implementation of IDisposable

        /// <summary>
        /// Calls close and supresses the finalizer for the object.
        /// </summary>
        public void Dispose() {
            Close();
            GC.SuppressFinalize(this);
        }

        #endregion Implementation of IDisposable
    }
}
