// NAnt - A .NET build tool
// Copyright (C) 2001-2002 Gerry Shaw
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

using System;
using System.IO;
using System.Collections;
using System.Xml;
using System.CodeDom.Compiler;

using NAnt.Core;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Summary description for ProjectSettings.
    /// </summary>
    public class ProjectSettings {
        public ProjectSettings( XmlElement elemRoot, XmlElement elemSettings ) {
            _elemSettings = elemSettings;
            _tfc = new TempFileCollection();
            Directory.CreateDirectory( _tfc.BasePath );

            _alSettings = new ArrayList();

            string strExtension = String.Empty;

            if ( elemRoot.FirstChild.Name == "VisualBasic" )
                _projectType = ProjectType.VBNet;
            else
                _projectType = ProjectType.CSharp;

            _strGUID = elemRoot.FirstChild.Attributes[ "ProjectGuid" ].Value.ToUpper();

            switch ( elemSettings.Attributes[ "OutputType" ].Value.ToLower() ) {
                case "library":
                    _alSettings.Add( "/target:library" );
                    _strOutputExtension = ".dll";
                    break;
                case "exe":
                    _alSettings.Add( "/target:exe" );
                    _strOutputExtension = ".exe";
                    break;
                case "winexe":
                    _alSettings.Add( "/target:winexe" );
                    _strOutputExtension = ".exe";
                    break;
                default:
                    throw new ApplicationException( String.Format( "Unknown output type: {0}", elemSettings.Attributes[ "OutputType" ].Value ) );
            }

            _strName = elemSettings.Attributes[ "AssemblyName" ].Value;
            _strOutputFile = String.Concat( elemSettings.Attributes[ "AssemblyName" ].Value, _strOutputExtension );
            _alSettings.Add( "/nologo" );

            if ( elemSettings.Attributes[ "RootNamespace" ] != null ) {
                _strRootNamespace = elemSettings.Attributes[ "RootNamespace" ].Value;
                if ( _projectType == ProjectType.VBNet )
                    _alSettings.Add( "/rootnamespace:" + _strRootNamespace );
            }

            Hashtable htStringSettings = new Hashtable();

            htStringSettings[ "ApplicationIcon" ] = @"/win32icon:""{0}""";

            foreach ( DictionaryEntry de in htStringSettings ) {
                string strValue = elemSettings.GetAttribute( de.Key.ToString() );
                if ( strValue != null && strValue.Length > 0 )
                    _alSettings.Add( String.Format( de.Value.ToString(), strValue ) );
            }
        }

        ~ProjectSettings() {
            _tfc.Delete();
        }

        public Task[] GetRequiredTasks() {
            return new Task[ 0 ];
        }

        public string[] Settings {
            get { return ( string[] )_alSettings.ToArray( typeof( string ) ); }
        }

        public string ProjectRootDirectory {
            get { return _strProjectDirectory; }
            set { _strProjectDirectory = value; }
        }

        public string GetTemporaryFilename( string strFilename ) {
            return Path.Combine( _tfc.BasePath, strFilename );
        }

        public string Name {
            get { return _strName; }
        }

        public TempFileCollection TemporaryFiles {
            get { return _tfc; }
        }

        public string OutputFile {
            get { return _strOutputFile; }
        }

        public string OutputExtension {
            get { return _strOutputExtension; }
        }

        public string RootNamespace {
            get { return _strRootNamespace; }
        }

        public string GUID {
            get { return _strGUID; }
        }

        public ProjectType Type {
            get { return _projectType; }
        }

        ArrayList            _alSettings;
        string                _strOutputFile;
        string                _strName;
        string                _strProjectDirectory;
        string                _strOutputExtension;
        string                _strRootNamespace;
        string                _strGUID;
        XmlElement            _elemSettings;
        TempFileCollection    _tfc;
        ProjectType            _projectType;
    }

    public enum ProjectType {
        VBNet = 0,
        CSharp = 1
    }
}
