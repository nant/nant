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
// Dmitry Jemerov <yole@yole.ru>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace NAnt.VSNet {
    /// <summary>
    /// A mapping from properties in the .vcproj file to command line arguments.
    /// </summary>
    public class VcArgumentMap {
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VcArgumentMap" />
        /// class.
        /// </summary>
        public VcArgumentMap() {
            _htArgs = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        public void AddString(string propName, string argName) {
            _htArgs [propName] = new VcStringArgument(argName);
        }

        public void AddBool(string propName, string argName) {
            _htArgs [propName] = new VcBoolArgument(argName);
        }

        public void AddEnum(string propName, string argName, params string[] values) {
            _htArgs [propName] = new VcEnumArgument(argName, values);
        }

        public string GetArgument(string propName, string propValue) {
            VcArgument arg = (VcArgument) _htArgs [propName];
            if (arg == null)
                return null;
            return arg.MapValue(propValue);
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        public static VcArgumentMap CreateCLArgumentMap() {
            VcArgumentMap map = new VcArgumentMap();
            map.AddEnum("AssemblerOutput", null, null, "FA", "FAcs", "FAc", "FAs");
            map.AddBool("BufferSecurityCheck", "GS");
            map.AddEnum("CompileAs", null, null, "TC", "TP");
            map.AddEnum("DebugInformationFormat", null, null, "Z7", "Zd", "Zi", "ZI");
            map.AddBool("EnableFunctionLevelLinking", "Gy");
            map.AddBool("EnableIntrinsicFunctions", "Oi");
            map.AddBool("ExceptionHandling", "EHsc");
            map.AddBool("RuntimeTypeInfo", "GR");
            map.AddEnum("FavorSizeOrSpeed", null, null, "Ot", "Os");
            map.AddBool("GlobalOptimizations", "Og");
            map.AddEnum("InlineFunctionExpansion", null, "Ob0", "Ob1", "Ob2");
            map.AddBool("OmitFramePointers", "Oy");
            map.AddEnum("Optimization", null, "Od", "O1", "O2", "Ox");
            map.AddEnum("RuntimeLibrary", null, "MT", "MTd", "MD", "MDd", "ML", "MLd");
            map.AddBool("StringPooling", "GF");
            map.AddEnum("StructMemberAlignment", null, null, "Zp1", "Zp2", "Zp4", "Zp8", "Zp16");
            map.AddEnum("UsePrecompiledHeader", null, null, "Yc", "YX", "Yu");
            map.AddEnum("WarningLevel", null, "W0", "W1", "W2", "W3", "W4");
            return map;
        }

        public static VcArgumentMap CreateLinkerArgumentMap() {
            VcArgumentMap map = new VcArgumentMap();
            map.AddBool("GenerateDebugInformation", "DEBUG");
            map.AddEnum("LinkIncremental", null, null, "INCREMENTAL:NO", "INCREMENTAL");
            map.AddString("ModuleDefinitionFile", "DEF:");
            map.AddEnum("OptimizeForWindows98", "OPT:", null, "NOWIN98", "WIN98");
            map.AddEnum("SubSystem", "SUBSYSTEM:", null, "CONSOLE", "WINDOWS");
            return map;
        }

        #endregion Public Static Methods

        #region Private Instance Fields

        private Hashtable _htArgs;

        #endregion Private Instance Fields

        private abstract class VcArgument {
            private string _name;
            
            protected VcArgument(string name) {
                _name = name;
            }

            internal abstract string MapValue(string propValue);

            protected string FormatOption(string value) {
                if (_name == null) {
                    return "/" + value;
                }
                return "/" + _name + value;
            }
        }

        private class VcStringArgument: VcArgument {
            internal VcStringArgument(string name): base(name) {
            }

            internal override string MapValue(string propValue) {
                return FormatOption(propValue);
            }
        }

        private class VcBoolArgument: VcArgument {
            internal VcBoolArgument(string name): base(name) {
            }

            internal override string MapValue(string propValue) {
                if (String.Compare(propValue, "TRUE", true, CultureInfo.InvariantCulture) == 0) {
                    return FormatOption("");
                }
                return null;
            }
        }

        private class VcEnumArgument: VcArgument {
            private string[] _values;
            
            internal VcEnumArgument(string name, string[] values): base(name) {
                _values = values;
            }

            internal override string MapValue(string propValue) {
                int iValue = -1;
                try {
                    iValue = Int32.Parse(propValue);
                }
                catch(FormatException) {
                    return null;
                }
                
                if (iValue < 0 || iValue >= _values.Length || _values [iValue] == null) {
                    return null;
                }
                return FormatOption(_values [iValue]);
            }
        }
    }
}
