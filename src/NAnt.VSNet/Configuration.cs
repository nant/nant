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

using System;
using System.Globalization;

namespace NAnt.VSNet {
    public class Configuration : IComparable {
        public Configuration() {
        }

        public Configuration(string name, string platform) {
            if (name == null)
                throw new ArgumentNullException ("name");

            _name = name;
            _platform = platform;
        }

        public string Name {
            get { return _name; }
            set {
                if (value == null)
                    throw new ArgumentNullException ("value");
                _name = value;
            }
        }

        public string Platform {
            get {
                if (_platform == null)
                    return string.Empty;
                return _platform;
            }
            set { _platform = value; }
        }

        #region Implementation of IComparable

        public int CompareTo(object obj) {
            if (this.Equals(obj))
                return 0;
            return 1;
        }

        #endregion Implementation of IComparable

        #region Override implementation of Object

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Configuration config = obj as Configuration;
            if (config == null)
                return false;

            return (string.Compare (Name, config.Name, true, CultureInfo.InvariantCulture) == 0)
                && (string.Compare (Platform, config.Platform, true, CultureInfo.InvariantCulture) == 0);
        }

        public override int GetHashCode() {
            return Name.ToLower (CultureInfo.InvariantCulture).GetHashCode ()
                ^ Platform.ToLower (CultureInfo.InvariantCulture).GetHashCode ();
        }

        public override string ToString() {
            if (Platform.Length == 0) {
                return Name;
            }
            return Name + "|" + Platform;
        }

        #endregion Override implementation of Object

        public static bool operator == (Configuration c1, Configuration c2) {
            if ((object) c1 == null) {
                return ((object) c2 == null);
            }
            return c1.Equals (c2);
        }

        public static bool operator != (Configuration c1, Configuration c2) {
            return !(c1 == c2);
        }

        public static Configuration Parse (string config) {
            if (config == null) {
                throw new ArgumentNullException ("config");
            }

            int index = config.IndexOf("|");
            if (index > 0 && index < config.Length) {
                return new Configuration (config.Substring(0, index),
                    config.Substring (index + 1));
            } else {
                return new Configuration (config, null);
            }
        }

        private string _name;
        private string _platform;
    }
}
