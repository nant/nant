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
// Ian MacLean (ian@maclean.ms)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Types {
    /// <summary>
    /// The FileSet element.
    /// </summary>
    /// <remarks>
    /// Used as a child element in various file-related tasks, including delete, copy, touch, get, atrrib, move...
    /// </remarks>
    /// <history>
    /// <change date="20030224" author="Brian Deacon (bdeacon at vidya dot com">Added support for the failonempty attribute</change>
    /// </history>
    public class FileSet : Element {
        #region Private Instance Fields

        bool _hasScanned = false;
        bool _defaultExcludes = true;
        bool _failOnEmpty = false;
        DirectoryScanner _scanner = new DirectoryScanner();
        StringCollection _asis = new StringCollection();
        PathScanner _pathFiles = new PathScanner();

        #endregion Private Instance Fields

        #region Private Static Fields

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion Private Static Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSet" /> class.
        /// </summary>
        public FileSet() {
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSet" /> class from
        /// the specified <see cref="FileSet" />.
        /// </summary>
        /// <param name="source">The <see cref="FileSet" /> that should be used to create a new instance of the <see cref="FileSet" /> class.</param>
        protected FileSet(FileSet source) {
            _defaultExcludes = source._defaultExcludes;
            Location = source.Location;
            Parent = source.Parent;
            Project = source.Project;
            if ( XmlNode != null ) {
                XmlNode = source.XmlNode.Clone();
            }
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// When set to true, causes the fileset element to throw a ValidationException 
        /// when no files match the includes and excludes criteria. Default is "false".
        /// </summary>
        [TaskAttribute("failonempty")]
        [BooleanValidator()]
        public bool FailOnEmpty {
            get { return _failOnEmpty; }
            set { _failOnEmpty = value; }
        }

        /// <summary>
        /// Indicates whether default excludes should be used or not. Default is "true".
        /// </summary>
        [TaskAttribute("defaultexcludes")]
        [BooleanValidator()]
        public bool DefaultExcludes {
            get { return _defaultExcludes; }
            set { _defaultExcludes = value; }
        }

        /// <summary>
        /// The base of the directory of this fileset. Default is project base directory.
        /// </summary>
        [TaskAttribute("basedir")]
        public string BaseDirectory {
            get { return _scanner.BaseDirectory; }
            set { _scanner.BaseDirectory = value; }
        }

        public StringCollection Includes {
            get { return _scanner.Includes; }
        }

        public StringCollection Excludes {
            get { return _scanner.Excludes; }
        }

        public StringCollection AsIs {
            get { return _asis; }
        }

        public PathScanner PathFiles {
            get { return _pathFiles; }
        }

        /// <summary>
        /// Gets the collection of file names that match the fileset.
        /// </summary>
        /// <value>
        /// A collection that contains the file names that match the 
        /// <see cref="FileSet" />.
        /// </value>
        public StringCollection FileNames {
            get {
                if (!_hasScanned) {
                    Scan();
                }
                return _scanner.FileNames;
            }
        }

        /// <summary>
        /// Gets the collection of directory names that match the fileset.
        /// </summary>
        /// <value>
        /// A collection that contains the directory names that match the 
        /// <see cref="FileSet" />.
        /// </value>
        public StringCollection DirectoryNames {
            get { 
                if (!_hasScanned) {
                    Scan();
                }
                return _scanner.DirectoryNames;
            }
        }

        [BuildElementArray("includes")]
        public IncludesElement[] SetIncludes {
            set {                foreach(IncludesElement include in value) {                    if (include.IfDefined && !include.UnlessDefined) {
                        if (include.AsIs) {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including AsIs=", include.Pattern));
                            AsIs.Add(include.Pattern);
                        } else if (include.FromPath) {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including FromPath=", include.Pattern));
                            PathFiles.Add(include.Pattern);
                        } else {
                            logger.Debug(string.Format(CultureInfo.InvariantCulture, "Including pattern", include.Pattern));
                            Includes.Add(include.Pattern);
                        }
                    }                }            }        }

        [BuildElementArray("excludes")]
        public ExcludesElement[] SetExcludes {
            set {
                foreach(ExcludesElement exclude in value) {
                    if (exclude.IfDefined && !exclude.UnlessDefined) {
                        logger.Debug(string.Format(CultureInfo.InvariantCulture, "Excluding pattern", exclude.Pattern));
                        Excludes.Add(exclude.Pattern);
                    }
                }
            }        }

        [BuildElementArray("includesList")]
        public IncludesListElement[] SetIncludesList {
            set {
                foreach (IncludesListElement include in value){
                    if (include.IfDefined && !include.UnlessDefined) {
                        foreach (string s in include.Files) {
                            AsIs.Add(s);
                        }
                    }
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Element

        protected override void InitializeElement(XmlNode elementNode) {
            if (BaseDirectory == null) {
                BaseDirectory = Project.BaseDirectory;
            } else {
                BaseDirectory = Project.GetFullPath(BaseDirectory);
            }

            if (DefaultExcludes) {
                // add default exclude patterns
                Excludes.Add("**/*~");
                Excludes.Add("**/#*#");
                Excludes.Add("**/.#*");
                Excludes.Add("**/%*%");
                Excludes.Add("**/CVS");
                Excludes.Add("**/CVS/**");
                Excludes.Add("**/.cvsignore");
                Excludes.Add("**/SCCS");
                Excludes.Add("**/SCCS/**");
                Excludes.Add("**/vssver.scc");
                Excludes.Add("**/_vti_cnf/**");
            }
        }

        #endregion Override implementation of Element

        #region Public Instance Methods

        public void Scan() {
            try {
                _scanner.Scan();

                // Add all the as-is patterns to the scanned files.
                foreach (string name in AsIs) {
                    if (Directory.Exists(name))
                        _scanner.DirectoryNames.Add(name);
                    else
                        _scanner.FileNames.Add(name);
                }

                // Add all the path-searched patterns to the scanned files.
                foreach (string name in PathFiles.Scan()) {
                    _scanner.FileNames.Add(name);
                }
            } catch (Exception e) {
                throw new BuildException("Error creating file set.", Location, e);
            }
            _hasScanned = true;

            if (FailOnEmpty && _scanner.FileNames.Count == 0) {
                throw new ValidationException(string.Format(CultureInfo.InvariantCulture, "The fileset specified is empty after scanning '{0}' for: {1}", _scanner.BaseDirectory, _scanner.Includes.ToString()), Location);
            }
        }

        #endregion Public Instance Methods

        #region Public Static Methods

        /// <summary>
        /// Determines if a file has a more recent last write time than the 
        /// given time.
        /// </summary>
        /// <param name="fileNames">A collection of filenames to check last write times against.</param>
        /// <param name="targetLastWriteTime">The datetime to compare against.</param>
        /// <returns>
        /// The name of the first file that has a last write time greater than 
        /// <paramref name="targetLastWriteTime" />; otherwise, null.
        /// </returns>
        public static string FindMoreRecentLastWriteTime(StringCollection fileNames, DateTime targetLastWriteTime) {
            foreach (string fileName in fileNames) {
                // only check fully file names that have a full path
                if (Path.IsPathRooted(fileName)) {
                    FileInfo fileInfo = new FileInfo(fileName);
                    if (!fileInfo.Exists) {
                        logger.Info(string.Format(CultureInfo.InvariantCulture, "File '{0}' does not exist (and is not newer than {1})", fileName, targetLastWriteTime));
                        return fileName;
                    }
                    if (fileInfo.LastWriteTime > targetLastWriteTime) {
                        logger.Info(string.Format(CultureInfo.InvariantCulture, "'{0}' was newer than {1}", fileName, targetLastWriteTime));
                        return fileName;
                    }
                }
            }
            return null;
        }

        #endregion Public Static Methods

        // These classes provide a way of getting the Element task to initialize
        // the values from the build file.

        public class ExcludesElement : Element {
            #region Private Instance Fields

            string _pattern;
            bool _ifDefined = true;
            bool _unlessDefined = false;

            #endregion Private Instance Fields

            #region Public Instance Properties

            /// <summary>
            /// The pattern or file name to include.
            /// </summary>
            [TaskAttribute("name", Required=true)]
            public string Pattern {
                get { return _pattern; }
                set { _pattern= value; }
            }

            /// <summary>
            /// If true then the pattern will be included; otherwise skipped. 
            /// Default is "true".
            /// </summary>
            [TaskAttribute("if")]
            [BooleanValidator()]
            public bool IfDefined {
                get { return _ifDefined; }
                set { _ifDefined = value; }
            }

            /// <summary>
            /// Opposite of if. If false then the pattern will be included; 
            /// otherwise skipped. Default is "false".
            /// </summary>
            [TaskAttribute("unless")]
            [BooleanValidator()]
            public bool UnlessDefined {
                get { return _unlessDefined; }
                set { _unlessDefined = value; }
            }

            #endregion Public Instance Properties
        }

        public class IncludesElement : ExcludesElement {
            #region Private Instance Fields

            bool _asIs = false;
            bool _fromPath = false;

            #endregion Private Instance Fields

            #region Public Instance Properties

            /// <summary>
            /// If true then the file name will be added to the fileset without 
            /// pattern matching or checking if the file exists.
            /// </summary>
            [TaskAttribute("asis")]
            [BooleanValidator()]
            public bool AsIs {
                get { return _asIs; }
                set { _asIs = value; }
            }

            /// <summary>
            /// If true then the file will be searched for on the path.
            /// </summary>
            [TaskAttribute("frompath")]
            [BooleanValidator()]
            public bool FromPath {
                get { return _fromPath; }
                set { _fromPath = value; }
            }

            #endregion Public Instance Properties
        }
        
        public class IncludesListElement : ExcludesElement {
            #region Private Instance Fields

            string[] files;

            #endregion Private Instance Fields

            #region Public Instance Properties

            public string[] Files {
                get { return files; }
            }

            #endregion Public Instance Properties

            #region Override implementation of Element

            protected override void InitializeElement(XmlNode elementNode)
            {
                StringCollection f=new StringCollection();

                using (System.IO.Stream fil=File.OpenRead(Pattern)) {
                    if(fil==null) {
                        throw new BuildException(String.Format(CultureInfo.InvariantCulture, "'{0}' list could not be opened",Pattern));
                    }
                    System.IO.StreamReader rd = new System.IO.StreamReader(fil);
                    while(rd.Peek()>-1) {
                        f.Add(rd.ReadLine());
                    }
                }

                files=new string[f.Count];
                f.CopyTo(files,0);
            }

            #endregion Override implementation of Element
        }
    }
}
