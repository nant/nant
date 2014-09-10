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
// Ian MacLean (ian@maclean.ms)
// Gerry Shaw (gerry_shaw@yahoo.com)
// Scott Hernandez (ScottHernandez@_yeah_not_really_@hotmail.com)

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Xml;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks {
    /// <summary>
    /// Includes an external build file.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This task is used to break your build file into smaller chunks.  You 
    ///   can load a partial build file and have it included into the build file.
    ///   </para>
    ///   <note>
    ///   Any global (project level) tasks in the included build file are executed 
    ///   when this task is executed.  Tasks in target elements are only executed 
    ///   if that target is executed.
    ///   </note>
    ///   <note>
    ///   The project element attributes are ignored.
    ///   </note>
    ///   <note>
    ///   This task can only be in the global (project level) section of the 
    ///   build file.
    ///   </note>
    ///   <note>
    ///   This task can only include files from the file system.
    ///   </note>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Include a task that fetches the project version from the 
    ///   <c>GetProjectVersion.include</c> build file.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <include buildfile="GetProjectVersion.include" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("include")]
    public class IncludeTask : Task {
        #region Private Instance Fields

        private string _buildFileName;

        #endregion Private Instance Fields

        #region Private Static Fields

        /// <summary>
        /// Used to check for recursived includes.
        /// </summary>
        private static Stack _includedFileNames = new Stack();

        private static string _currentBasedir = "";
        private static int _nestinglevel = 0;

        #endregion Private Static Fields

        #region Public Instance Properties

        /// <summary>
        /// Build file to include.
        /// </summary>
        [TaskAttribute("buildfile", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string BuildFileName {
            get { return _buildFileName; }
            set { _buildFileName = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Verifies parameters.
        /// </summary>
        protected override void Initialize() {
            // Task can only be included as a global task.
            // This might not be a firm requirement but you could get some real 
            // funky errors if you start including targets wily-nily.
            if (Parent != null && !(Parent is Project)) {
                throw new BuildException(ResourceUtils.GetString("NA1180"), Location);
            }
            if (String.IsNullOrEmpty(_currentBasedir) || _nestinglevel == 0) {
                _currentBasedir = Project.BaseDirectory;
            }

            string buildFileName = null;

            try {
                // check if build file is valid file name
                buildFileName = Path.GetFullPath(Path.Combine(_currentBasedir, BuildFileName));
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1128"), BuildFileName),
                    Location, ex);
            }

            // check for recursive includes
            foreach (string currentFileName in _includedFileNames) {
                if (currentFileName == buildFileName) {
                    throw new BuildException(ResourceUtils.GetString("NA1179"), Location);
                }
            }
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <exception cref="BuildException">If the build file to include doesn't exist.
        /// </exception>
        protected override void ExecuteTask() {
            string includedFileName = Path.GetFullPath(Path.Combine(_currentBasedir, 
                BuildFileName));

            // check if build file exists
            if (!File.Exists(includedFileName)) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1127"), includedFileName), Location);
            }

            // check if file has already been mapped, if it has not yet been mapped,
            // add the include file to the map.  This addresses Bug#: 3016497
            if (Project.LocationMap.FileIsMapped(includedFileName)) {
                Log(Level.Verbose, ResourceUtils.GetString("String_DuplicateInclude"), includedFileName);
                return;
            } else {
                XmlDocument mapDoc = new XmlDocument();
                mapDoc.Load(includedFileName);
                Project.LocationMap.Add(mapDoc);
                mapDoc = null;
            }
            
            // push ourselves onto the stack (prevents recursive includes)
            _includedFileNames.Push(includedFileName);

            // increment the nesting level
            _nestinglevel ++;
            
            Log(Level.Verbose, "Including file {0}.", includedFileName);

            // store original base directory
            string oldBaseDir = _currentBasedir;
            
            // set basedir to be used by the nested calls (if any)
            _currentBasedir = Path.GetDirectoryName(includedFileName);
            
            try {
            
                // This section addresses SF Bug#:2824210..
                // 
                // Description of issue:
                // The root cause to this bug is mismatching NamespaceURIs between the main 
                // build file and the include file.  There is no place where the include
                // build file's Namespace is added to the main build files namespace collection.
                // So if the NamespaceURI of the include file doesn't match the main build file,
                // then the Project class would not import any of the include file contents.
                //
                // Resolution:
                // The key is to have the Namespaces match between project and include files.
                // Rather than remove the Namespace checks in the Project class, I decided to
                // copy the project's namespace into the include file prior to importing into the
                // main file.  Unfortunately, it is not an easy task.
                
                // Create two XmlDocument variables. One to sanitize the include file (doc) 
                // and one to pass to the project class (loadDoc).
                XmlDocument doc = new XmlDocument();
                XmlDocument loadDoc = new XmlDocument();
                
                // Gets the namespace from the main project.
                string projectNamespaceURI = Project.Document.DocumentElement.NamespaceURI;
                
                // String variable to hold the main build file's namespace.
                string projectURI = "";
                
                // Rather than loading the xml file directly into the XmlDocument, it is loaded
                // into an XmlTextReader so any NamespaceURI may be stripped out before proceeding
                // further.
                XmlTextReader includeXmlReader = new XmlTextReader(includedFileName);
                
                // Turn the namespaces off
                includeXmlReader.Namespaces = false;
                
                // Load the contents of the XmlTextReader into the doc XmlDocument
                doc.Load(includeXmlReader);
                
                // Strip the namespace attribute.
                doc.DocumentElement.Attributes.RemoveNamedItem("xmlns");
                
                // Kill the XmlTextReader
                ((IDisposable)includeXmlReader).Dispose();
                includeXmlReader = null;
                
                // Assigns the main build file's namespace to the
                // local string variable if it is not blank.
                if (!String.IsNullOrEmpty(projectNamespaceURI)) {
                    projectURI = projectNamespaceURI;
                }
                
                // If the projectURI is not empty at this point, add
                // the Namespace attribute to the doc XmlDocument.
                if (!String.IsNullOrEmpty(projectURI)) {
                    XmlAttribute projAttr = doc.CreateAttribute("xmlns");
                    projAttr.Value = projectURI;
                    doc.DocumentElement.Attributes.Append(projAttr);
                }
                
                // Set up a stringwriter and XmlTextWriter variable to pass the
                // contents of the doc XmlDocument variable to.
                using (StringWriter includeFileSW = new StringWriter()) {
                    XmlTextWriter includeFileXW = new XmlTextWriter(includeFileSW);
                    
                    // Loads the contents from doc to the XmlTextWriter.
                    doc.WriteTo(includeFileXW);
                    
                    // Then the contents of the XmlTextWriter to the loadDoc XmlDocument
                    // var.  This will ensure that the main build file's Namespace
                    // is loaded into the include file before it's passed to the Project
                    // class.
                    loadDoc.LoadXml(includeFileSW.ToString());
                    
                    // Kill the XmlTextWriter
                    ((IDisposable)includeFileXW).Dispose();
                    includeFileXW = null;
                }
                // Pass the loadDoc XmlDocument to the project.
                Project.InitializeProjectDocument(loadDoc);
                
            } catch (BuildException) {
                // rethrow exception
                throw;
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1128"), includedFileName),
                    Location, ex);
            } finally {
                // pop off the stack
                _includedFileNames.Pop();

                // decrease the nesting level
                _nestinglevel--;
                
                 // restore original base directory
               _currentBasedir = oldBaseDir;
           }
        }

        #endregion Override implementation of Task
    }
}
