using System;
using System.IO;
using NAnt.Core.Attributes;

namespace NAnt.Core.Configuration {
    /// <summary>
    /// Represents an explicitly named list of directories.
    /// </summary>
    /// <remarks>
    /// A <see cref="DirList" /> is useful when you want to capture a list of
    /// directories regardless whether they currently exist.
    /// </remarks>
    [Serializable]
    internal class DirList : Element {
        private DirectoryInfo _baseDirectory;
        private DirectoryName[] _directoryNames;

        /// <summary>
        /// The base of the directory of this dirlist. The default is the project
        /// base directory.
        /// </summary>
        [TaskAttribute("dir")]
        public DirectoryInfo Directory {
            get { 
                if (_baseDirectory == null) {
                    return new DirectoryInfo(Project.BaseDirectory);
                }
                return _baseDirectory; 
            }
            set { _baseDirectory = value; }
        }

        [BuildElementArray("directory")]
        public DirectoryName[] DirectoryNames {
            get { return _directoryNames; }
            set { _directoryNames = value; }
        }

        public string[] GetDirectories() {
            string baseDir = Directory.FullName;
            return GetDirectories(baseDir);
        }

        internal string[] GetDirectories(string baseDir) {
            if (baseDir == null)
                throw new ArgumentNullException("baseDir");

            if (_directoryNames == null) {
                return new string[0];
            }

            string[] directories = new string[_directoryNames.Length];
            for (int i = 0; i < _directoryNames.Length; i++) {
                DirectoryName dirName = _directoryNames [i];
                directories [i] = Path.Combine (baseDir, dirName.DirName);
            }
            return directories;
        }
    }
}
