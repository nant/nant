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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.Runtime.Serialization;

namespace NAnt.Core {
    [Serializable]
    public class ExpressionParseException : Exception {

        private int _startPos = -1;
        private int _endPos = -1;

        /// <summary>
        /// Gets the start position.
        /// </summary>
        /// <value>
        /// The start position.
        /// </value>
        public int StartPos {
            get { return _startPos; }
        }

        /// <summary>
        /// Gets the end position.
        /// </summary>
        /// <value>
        /// The end position.
        /// </value>
        public int EndPos {
            get { return _endPos; }
        }

        public ExpressionParseException() : base () {}
        public ExpressionParseException(string message) : base(message, null) {}
        public ExpressionParseException(string message, Exception inner) : base(message, inner) {}
        protected ExpressionParseException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _startPos = (int)info.GetValue("startPos", typeof(int));
            _endPos = (int)info.GetValue("endPos", typeof(int));
        }
        
        public ExpressionParseException(string message, int pos) : base(message, null) {
            _startPos = pos;
            _endPos = -1;
        }
        
        public ExpressionParseException(string message, int startPos, int endPos) : base(message, null) {
            _startPos = startPos;
            _endPos = endPos;
        }
        
        public ExpressionParseException(string message, int startPos, int endPos, Exception inner) : base(message, inner) {
            _startPos = startPos;
            _endPos = endPos;
        }
        
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("startPos", _startPos);
            info.AddValue("endPos", _endPos);

            base.GetObjectData(info, context);
        }
    }
}
