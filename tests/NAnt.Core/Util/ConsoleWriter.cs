// NAnt - A .NET build tool
// Copyright (C) 2001-2007 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)

using System.IO;
using System.Text;

namespace Tests.NAnt.Core.Util {
    /// <summary>
    /// Specialized <see cref="StreamWriter" /> that uses the encoding the
    /// console uses to write output.
    /// </summary>
    public class ConsoleWriter : StreamWriter {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleWriter" />
        /// that uses a <see cref="MemoryStream" /> as underlying stream and
        /// the encoding that the console uses to write output.
        /// </summary>
        public ConsoleWriter() : base (new MemoryStream (), ConsoleEncoding) {
        }

        public override string ToString() {
            Flush ();
            BaseStream.Position = 0;
            StreamReader sr = new StreamReader (BaseStream, Encoding);
            return sr.ReadToEnd ();
        }

        static Encoding ConsoleEncoding {
            get {
#if NET_2_0
                return System.Console.OutputEncoding;
#else
                return Encoding.Default;
#endif
            }
        }
    }
}
