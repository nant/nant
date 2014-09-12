// NAnt - A .NET build tool
// Copyright (C) 2001-2008 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net.be)

namespace NAnt.Core.Extensibility {
    internal class FunctionArgument {
        public FunctionArgument(string name, int index, object value, ExpressionTokenizer.Position beforeArgument, ExpressionTokenizer.Position afterArgument) {
            this._name = name;
            this._index = index;
            this._value = value;
            this._beforeArgument = beforeArgument;
            this._afterArgument = afterArgument;
        }

        public int Index {
            get { return _index; }
        }

        public string Name {
            get { return _name; }
        }

        public object Value {
            get { return _value; }
        }

        public ExpressionTokenizer.Position BeforeArgument {
            get { return _beforeArgument; }
        }

        public ExpressionTokenizer.Position AfterArgument {
            get { return _afterArgument; }
        }

        private readonly int _index;
        private readonly string _name;
        private readonly object _value;
        private readonly ExpressionTokenizer.Position _beforeArgument;
        private readonly ExpressionTokenizer.Position _afterArgument;
    }
}
