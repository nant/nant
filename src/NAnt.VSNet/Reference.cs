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
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;

using Microsoft.Win32;

using NAnt.Core;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Summary description for Reference.
    /// </summary>
    public class Reference {
        public Reference( Solution sln, ProjectSettings ps, XmlElement elemReference, Task nanttask ) {
            _ps = ps;
            _nanttask = nanttask;
            _dtReferenceTimeStamp = DateTime.MinValue;
            _bIsSystem = false;

            _bIsCreated = false;
            DirectoryInfo diGAC = new DirectoryInfo( nanttask.Project.CurrentFramework.FrameworkDirectory.FullName );
            _strName = ( string )elemReference.Attributes[ "Name" ].Value;

            if ( elemReference.Attributes[ "Project" ] != null ) {
                if ( sln == null )
                    throw new Exception( "External reference found, but no solution specified: " + _strName );

                Project p = new Project( _nanttask, ps.TemporaryFiles );
                string strFile = sln.GetProjectFileFromGUID( elemReference.GetAttribute( "Project" ) );
                if ( strFile == null )
                    throw new Exception( "External reference found, but project was not loaded: " + _strName );

                p.Load( sln, strFile );
                // We don't know what the timestamp of the project is going to be, because we don't know what configuration
                // we will be building
                _dtReferenceTimeStamp = DateTime.MinValue;

                _p = p;
                _bCopyLocal = _bPrivateSpecified ? _bIsPrivate : true;
                return;
            }

            if ( elemReference.Attributes[ "WrapperTool" ] != null )
                _strImportTool = elemReference.Attributes[ "WrapperTool" ].Value;
                
            // Read the private flag
            _bPrivateSpecified = ( elemReference.Attributes[ "Private" ] != null );
            if ( _bPrivateSpecified )
                _bIsPrivate = ( elemReference.Attributes[ "Private" ].Value == "True" );
            else
                _bIsPrivate = false;

            if ( _strImportTool == "tlbimp" || _strImportTool == "primary" || _strImportTool == "aximp" ) {
                HandleWrapperImport( elemReference );
            }
            else {
                _strReferenceFile = elemReference.Attributes[ "AssemblyName" ].Value + ".dll";
                
                string strGACFile = Path.Combine( diGAC.FullName, _strReferenceFile );
                if ( File.Exists( strGACFile ) ) {
                    // This file is in the GAC
                    _strBaseDirectory = diGAC.FullName;
                    _bCopyLocal = _bPrivateSpecified ? _bIsPrivate : false;
                    _strReferenceFile = strGACFile;
                    _bIsSystem = true;
                }
                else {
                    FileInfo fiRef = new FileInfo( Path.Combine( ps.ProjectRootDirectory, elemReference.Attributes[ "HintPath" ].Value ) );
                    // We may be loading a project whose references are not compiled yet
                    //if ( !fiRef.Exists )
                    //    throw new Exception( "Couldn't find referenced assembly: " + _strReferenceFile );

                    _strBaseDirectory = fiRef.DirectoryName;
                    _strReferenceFile = fiRef.FullName;
                    _bCopyLocal = _bPrivateSpecified ? _bIsPrivate : true;
                }

                _dtReferenceTimeStamp = GetTimestamp( _strReferenceFile );
            }
        }

        private void HandleWrapperImport( XmlElement elemReference ) {
            string strVersionKey = String.Format( @"TYPELIB\{0}\{1}.{2}", 
                elemReference.Attributes[ "Guid" ].Value,
                elemReference.Attributes[ "VersionMajor" ].Value,
                elemReference.Attributes[ "VersionMinor" ].Value
                );

            string strRegistryKey = String.Format( @"TYPELIB\{0}\{1}.{2}\{3}\win32", 
                elemReference.Attributes[ "Guid" ].Value,
                elemReference.Attributes[ "VersionMajor" ].Value,
                elemReference.Attributes[ "VersionMinor" ].Value,
                elemReference.Attributes[ "Lcid" ].Value
                );

            // First, look for a primary interop assembly
            using ( RegistryKey rk = Registry.ClassesRoot.OpenSubKey( strVersionKey ) ) {
                if ( rk.GetValue( "PrimaryInteropAssemblyName" ) != null ) {
                    _strReferenceFile = ( string )rk.GetValue( "PrimaryInteropAssemblyName" );
                    // Assembly.Load does its own checking
                    //if ( !File.Exists( _strReferenceFile ) )
                    //    throw new Exception( "Couldn't find referenced primary interop assembly: " + _strReferenceFile );
                    Assembly asmRef = Assembly.Load( _strReferenceFile );
                    _strReferenceFile = new Uri( asmRef.CodeBase ).LocalPath;
                    _strBaseDirectory = Path.GetDirectoryName( _strReferenceFile );
                    _bCopyLocal = _bPrivateSpecified ? _bIsPrivate : false;
                    _dtReferenceTimeStamp = GetTimestamp( _strReferenceFile );

                    return;
                }
            }

            using ( RegistryKey rk = Registry.ClassesRoot.OpenSubKey( strRegistryKey ) ) {
                if ( rk == null )
                    throw new ApplicationException( String.Format( "Couldn't find reference to type library {0} ({1})", elemReference.Attributes[ "Name" ].Value, strRegistryKey ) );

                _strTypeLib = ( string )rk.GetValue( null );
                if ( !File.Exists( _strTypeLib ) )
                    throw new Exception( "Couldn't find referenced type library: " + _strTypeLib );

                _dtReferenceTimeStamp = GetTimestamp( _strTypeLib );
                _strInteropFile = "Interop." + elemReference.Attributes[ "Name" ].Value + ".dll";
                _strReferenceFile = _strInteropFile;
                _strNamespace = elemReference.Attributes[ "Name" ].Value;
                _bCopyLocal = true;
                _bIsCreated = true;
            }

        }

        public bool CopyLocal {
            get { return _bCopyLocal; }
        }

        public bool IsCreated {
            get { return _bIsCreated; }
        }

        public void GetCreationCommand( ConfigurationSettings cs, out string strProgram, out string strCommandLine ) {
            _strReferenceFile = new FileInfo( Path.Combine( cs.OutputPath, _strInteropFile ) ).FullName;

            strCommandLine = @"""" + _strTypeLib + @""" /silent /out:""" + _strReferenceFile + @"""";
            if ( _strImportTool == "tlbimp" )
                strCommandLine += " /namespace:" + _strNamespace;
            strProgram = _strImportTool + ".exe";
        }

        public string GetBaseDirectory( ConfigurationSettings cs ) {
            if ( _p != null )
                return _p.GetConfigurationSettings( cs.Name ).OutputPath;

            return _strBaseDirectory;
        }

        public string[] GetReferenceFiles( ConfigurationSettings cs ) {
            if ( _p != null ) {
                _strReferenceFile = _p.GetConfigurationSettings( cs.Name ).FullOutputFile; 
            }


            FileInfo fi = new FileInfo( _strReferenceFile );
            if ( !fi.Exists ) {
                if ( _p == null )
                    throw new Exception( "Couldn't find referenced assembly: " + _strReferenceFile );
                else
                    throw new Exception( "Couldn't find referenced project's output: " + _strReferenceFile );
            }

            string strReferenceFiles = "*.dll";
            
            StringCollection sc = new StringCollection();

            // Get a list of the references in the output directory
            foreach ( string strReferenceFile in Directory.GetFiles( fi.DirectoryName, strReferenceFiles ) ) {
                // Now for each reference, get the related files (.xml, .pdf, etc...)
                string strRelatedFiles = Path.GetFileName( Path.ChangeExtension( strReferenceFile, ".*" ) );

                foreach ( string strRelatedFile in Directory.GetFiles( fi.DirectoryName, strRelatedFiles ) ) {
                    // Ignore any other the garbage files created
                    string strExtension = Path.GetExtension( strRelatedFile ).ToLower();
                    if ( strExtension != ".dll" && strExtension != ".xml" && strExtension != ".pdb" )
                        continue;

                    sc.Add( new FileInfo( strRelatedFile ).Name );
                }
            }

            return ( String[] )new ArrayList( sc ).ToArray( typeof( string ) );
        }

        public string Setting {
            get { return String.Format( @"/r:""{0}""", _strReferenceFile ); }
        }

        public string Filename {
            get { return _strReferenceFile; }
            set { 
                _strReferenceFile = value; 
                _strBaseDirectory = new FileInfo( _strReferenceFile ).DirectoryName;
                _dtReferenceTimeStamp = GetTimestamp( _strReferenceFile );
            }
        }

        public ConfigurationSettings ConfigurationSettings {
            set { _cs = value; }
        }

        public string Name {
            get { return _strName; }
        }

        public bool IsSystem {
            get { return _bIsSystem; }
        }
        
        public bool IsProjectReference {
            get { return _p != null; }
        }
        
        public string ProjectReferenceGUID {
            get { return _p.GUID; }
        }

        public DateTime Timestamp {
            get { 
                if ( _p != null )
                    return GetTimestamp( _p.GetConfigurationSettings( _cs.Name ).FullOutputFile );

                return _dtReferenceTimeStamp; 
            }
        }

        private DateTime GetTimestamp( string strFile ) {
            if ( !File.Exists( strFile ) )
                return DateTime.MaxValue;

            return File.GetLastWriteTime( strFile );
        }

        string        _strName;
        string        _strReferenceFile;
        string        _strInteropFile;
        string        _strTypeLib;
        string        _strNamespace;
        string        _strBaseDirectory;
        bool        _bCopyLocal;
        bool        _bIsCreated;
        bool        _bIsSystem;
        string        _strImportTool;
        DateTime    _dtReferenceTimeStamp;    
        bool        _bPrivateSpecified;
        bool        _bIsPrivate;

        private ProjectSettings            _ps;
        private ConfigurationSettings    _cs;

        private Project _p;
        private Task _nanttask;
    }
}
