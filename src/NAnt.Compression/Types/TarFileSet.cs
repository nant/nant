// NAnt - A .NET build tool
// Copyright (C) 2003 Scott Hernandez
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

using System;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

using NAnt.Compression.Tasks;

namespace NAnt.Compression.Types {
    /// <summary>
    /// A <see cref="TarFileSet" /> is a <see cref="FileSet" /> with extra 
    /// attributes useful in the context of the <see cref="TarTask" />.
    /// </summary>
    [ElementName("tarfileset")]
    public class TarFileSet : FileSet {
        #region Private Instance Fields

        private int _fileMode = _fileFlag | 420; // = 644 octal
        private int _dirMode = _dirFlag | 493; // = 755 octal
        private string _userName;
        private int _uid;
        private string _groupName;
        private int _gid;
        private string _prefix;

        #endregion Private Instance Fields

        #region Private Static Fields

        private const int _dirFlag = 16384; // = 40000 octal
        private const int _fileFlag = 32768; // = 100000 octal

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// A 3 digit octal string, specify the user, group and other modes 
        /// in the standard Unix fashion. Only applies to plain files. The 
        /// default is <c>644</c>.
        /// </summary>
        [TaskAttribute("filemode")]
        public int FileMode {
            get { return _fileMode; }
            set { _fileMode = _fileFlag | Convert.ToInt32(Convert.ToString(value), 8); }
        }

        /// <summary>
        /// A 3 digit octal string, specify the user, group and other modes 
        /// in the standard Unix fashion. Only applies to directories. The 
        /// default is <c>755</c>.
        /// </summary>
        [TaskAttribute("dirmode")]
        public int DirMode {
            get { return _dirMode; }
            set { _dirMode = _dirFlag | Convert.ToInt32(Convert.ToString(value), 8); }
        }

        /// <summary>
        /// The username for the tar entry.
        /// </summary>
        [TaskAttribute("username")]
        public string UserName {
            get { return _userName; }
            set { _userName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The user identifier (UID) for the tar entry. 
        /// </summary>
        [TaskAttribute("uid")]
        public int Uid {
            get { return _uid; }
            set { _uid = value; }
        }

        /// <summary>
        /// The groupname for the tar entry.
        /// </summary>
        [TaskAttribute("groupname")]
        public string GroupName {
            get { return _groupName; }
            set { _groupName = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// The group identifier (GID) for the tar entry.
        /// </summary>
        [TaskAttribute("gid")]
        public int Gid {
            get { return _gid; }
            set { _gid = value; }
        }

        /// <summary>
        /// The top level directory prefix. If set, all file and directory paths 
        /// in the fileset will have this value prepended. Can either be a single 
        /// directory name or a "/" separated path.
        /// </summary>
        [TaskAttribute("prefix")]
        public string Prefix {
            get { return _prefix; }
            set { 
                _prefix = StringUtils.ConvertEmptyToNull(value);
                if (_prefix != null && !_prefix.EndsWith("/") && !_prefix.EndsWith("\\")) {
                    _prefix += "/";
                }
            }
        }

        #endregion Public Instance Properties
    }
}
