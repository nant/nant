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
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Xml;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Tasks;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Summary description for Project.
    /// </summary>
    public class Project {
        private const string COMMAND_FILE = "compile-commands.txt";

        public Project(Task nanttask) {
            _htConfigurations = new Hashtable();
            _htReferences = new Hashtable();
            _htFiles = new Hashtable();
            _htResources = new Hashtable();
            _htAssemblies = new Hashtable();
            _nanttask = nanttask;
        }

        private static bool IsURL( string strFilename ) {
            XmlDocument doc = new XmlDocument();
            if ( strFilename.StartsWith( Uri.UriSchemeFile ) || strFilename.StartsWith( Uri.UriSchemeHttp ) || strFilename.StartsWith( Uri.UriSchemeHttps ) ) {
                return true;
            }

            return false;
        }

        private static XmlDocument LoadXmlDocument( string strFilename ) {
            XmlDocument doc = new XmlDocument();
            if ( !IsURL( strFilename ) ) {
                doc.Load( strFilename );
            }
            else {
                Uri uri = new Uri( strFilename );
                if ( uri.Scheme == Uri.UriSchemeFile ) {
                    doc.Load( uri.LocalPath );
                }
                else {
                    doc.LoadXml( WebDavClient.GetFileContentsStatic( strFilename ) );
                }
            }

            return doc;
        }

        public static bool IsEnterpriseTemplateProject( string strFilename ) {
            XmlDocument doc = LoadXmlDocument( strFilename );
            return ( doc.DocumentElement.Name.ToString() == "EFPROJECT" );
        }

        public static string LoadGUID( string strFilename ) {
            XmlDocument doc = LoadXmlDocument( strFilename );

            ProjectSettings ps = new ProjectSettings( doc.DocumentElement, ( XmlElement )doc.SelectSingleNode( "//Build/Settings" ) );
            return ps.GUID;
        }

        public void Load( Solution sln, string strFilename ) {
            XmlDocument doc = LoadXmlDocument( strFilename );

            _ps = new ProjectSettings( doc.DocumentElement, ( XmlElement )doc.SelectSingleNode( "//Build/Settings" ) );

            _bWebProject = IsURL( strFilename );
            _strWebProjectBaseUrl = String.Empty;
            string strWebCacheDirectory = String.Empty;

            if ( !_bWebProject ) {
                _strProjectDirectory = new FileInfo( strFilename ).DirectoryName;
            }
            else {
                string strProjectDirectory = strFilename.Replace( ":", "_" );
                Console.WriteLine( strProjectDirectory );
                strProjectDirectory = strProjectDirectory.Replace( "/", "_" );
                Console.WriteLine( strProjectDirectory );
                strProjectDirectory = strProjectDirectory.Replace( "\\", "_" );
                Console.WriteLine( strProjectDirectory );
                strProjectDirectory = Path.Combine( _ps.TemporaryFiles.BasePath, strProjectDirectory );
                Console.WriteLine( strProjectDirectory );
                Directory.CreateDirectory( strProjectDirectory );

                strWebCacheDirectory = strProjectDirectory;
                _strWebProjectBaseUrl = strFilename.Substring( 0, strFilename.LastIndexOf( "/" ) );

                _strProjectDirectory = Path.GetDirectoryName( sln.Filename );
            }

            _ps.ProjectRootDirectory = _strProjectDirectory;

            XmlNodeList nlConfigurations, nlReferences, nlFiles, nlImports;

            nlConfigurations = doc.SelectNodes( "//Config" );
            foreach ( XmlElement elemConfig in nlConfigurations ) {
                ConfigurationSettings cs = new ConfigurationSettings( _ps, elemConfig );
                _htConfigurations[ elemConfig.Attributes[ "Name" ].Value.ToLower() ] = cs;
            }

            nlReferences = doc.SelectNodes( "//References/Reference" );
            foreach ( XmlElement elemReference in nlReferences ) {
                Reference reference = new Reference( sln, _ps, elemReference, _nanttask );
                _htReferences[ elemReference.Attributes[ "Name" ].Value ] = reference;
            }

            if ( _ps.Type == ProjectType.VBNet ) {
                nlImports = doc.SelectNodes( "//Imports/Import" );
                foreach ( XmlElement elemReference in nlImports ) {
                    _strImports += elemReference.Attributes[ "Namespace" ].Value.ToString() + ",";
                }
                if ( _strImports.Length > 0 ) {
                    _strImports = "/Imports:" + _strImports;
                }
            }

            nlFiles = doc.SelectNodes( "//Files/Include/File" );
            foreach ( XmlElement elemFile in nlFiles ) {
                string strBuildAction = elemFile.Attributes[ "BuildAction" ].Value;

                if ( _bWebProject ) {
                    WebDavClient wdc = new WebDavClient( new Uri( _strWebProjectBaseUrl ) );
                    string strOutputFile = Path.Combine( strWebCacheDirectory, elemFile.Attributes[ "RelPath" ].Value );
                    wdc.DownloadFile( strOutputFile, elemFile.Attributes[ "RelPath" ].Value );

                    FileInfo fi = new FileInfo( strOutputFile );
                    if ( strBuildAction == "Compile" ) {
                        _htFiles[ fi.FullName ] = null;
                    }
                    else if ( strBuildAction == "EmbeddedResource" ) {
                        Resource r = new Resource( this, fi.FullName, elemFile.Attributes[ "RelPath" ].Value, fi.DirectoryName + @"\" + elemFile.Attributes[ "DependentUpon" ].Value, _nanttask );
                        _htResources[ r.InputFile ] = r;
                    }
                }
                else {
                    if ( strBuildAction == "Compile" ) {
                        _htFiles[ elemFile.Attributes[ "RelPath" ].Value ] = null;
                    }
                    else if ( strBuildAction == "EmbeddedResource" ) {
                        string strResourceFilename = Path.Combine( _ps.ProjectRootDirectory, elemFile.GetAttribute( "RelPath" ) );
                        string strDependentOn = ( elemFile.Attributes[ "DependentUpon" ] != null ) ? Path.Combine( new FileInfo( strResourceFilename ).DirectoryName, elemFile.Attributes[ "DependentUpon" ].Value ) : null;
                        Resource r = new Resource( this, strResourceFilename, elemFile.Attributes[ "RelPath" ].Value, strDependentOn, _nanttask );
                        _htResources[ r.InputFile ] = r;
                    }
                }
            }
        }

        public string Name {
            get { return _ps.Name; }
        }

        public string[] Configurations {
            get { return ( String[] )new ArrayList( _htConfigurations.Keys ).ToArray( typeof( string ) ); }
        }

        public ConfigurationSettings GetConfigurationSettings( string strConfiguration ) {
            return ( ConfigurationSettings )_htConfigurations[ strConfiguration ];
        }

        public Reference[] References {
            get { return ( Reference[] )new ArrayList( _htReferences.Values ).ToArray( typeof( Reference ) ); }
        }

        public Resource[] Resources {
            get { return ( Resource[] )new ArrayList( _htResources.Values ).ToArray( typeof( Resource ) ); }
        }

        public ProjectSettings ProjectSettings {
            get { return _ps; }
        }

        private bool CheckUpToDate( ConfigurationSettings cs ) {
            DateTime dtOutputTimeStamp;
            if ( File.Exists( cs.FullOutputFile ) )
                dtOutputTimeStamp = File.GetLastWriteTime( cs.FullOutputFile );
            else
                return false;

            // Check all of the input files
            foreach ( string strFile in _htFiles.Keys )
                if ( dtOutputTimeStamp < File.GetLastWriteTime( Path.Combine( _strProjectDirectory, strFile ) ) )
                    return false;

            // Check all of the input references
            foreach ( Reference reference in _htReferences.Values ) {
                reference.ConfigurationSettings = cs;
                if ( dtOutputTimeStamp < reference.Timestamp )
                    return false;
            }

            return true;
        }

        public bool Compile(string strConfiguration, ArrayList alCSCArguments, string strLogFile, bool bVerbose, bool bShowCommands ) {
            TempFileCollection tfc = new TempFileCollection();
            Directory.CreateDirectory( tfc.BasePath );
            bool bSuccess = true;

            ConfigurationSettings cs = ( ConfigurationSettings )_htConfigurations[ strConfiguration.ToLower() ];
            if ( cs == null ) {
                Console.WriteLine( "Configuration {0} does not exist, skipping.", strConfiguration );
                return true;
            }

            Log(Level.Info, _nanttask.LogPrefix + "Building {0} [{1}]...", Name, strConfiguration);
            Directory.CreateDirectory( cs.OutputPath );

            try {
                string strTempFile = tfc.BasePath + "\\" + COMMAND_FILE;
            
                using ( StreamWriter sw = File.CreateText( strTempFile ) ) {
                    if ( CheckUpToDate( cs ) ) {    
                        Log(Level.Verbose, _nanttask.LogPrefix + "Project is up-to-date" );
                        return true;
                    }

                    foreach ( string strSetting in alCSCArguments )
                        sw.WriteLine( strSetting );

                    foreach ( string strSetting in ProjectSettings.Settings )
                        sw.WriteLine( strSetting );

                    foreach ( string strSetting in cs.Settings )
                        sw.WriteLine( strSetting );

                    if ( _ps.Type == ProjectType.VBNet )
                        sw.WriteLine( _strImports );

                    Log(Level.Verbose, _nanttask.LogPrefix + "Copying references:" );
                    foreach ( Reference reference in _htReferences.Values ) {
                        Log(Level.Verbose, _nanttask.LogPrefix + " - " + reference.Name );

                        if ( reference.CopyLocal ) {
                            if ( reference.IsCreated ) {
                                string strProgram, strCommandLine;
                                reference.GetCreationCommand( cs, out strProgram, out strCommandLine );

                                Log(Level.Verbose, _nanttask.LogPrefix + strProgram + " " + strCommandLine);
                                ProcessStartInfo psiRef = new ProcessStartInfo( strProgram, strCommandLine );
                                psiRef.UseShellExecute = false;
                                psiRef.WorkingDirectory = cs.OutputPath;
                                try {
                                    Process pRef = Process.Start( psiRef );
                                    pRef.WaitForExit();
                                }
                                catch ( Win32Exception e ) {
                                    throw new BuildException(String.Format("Unable to start process with commandline: {0} {1}", strProgram, strCommandLine), e);
                                }
                            }
                            else {
                                string[] strFromFilenames = reference.GetReferenceFiles( cs );

                                CopyTask ct = new CopyTask();
                                foreach ( string strFile in strFromFilenames )
                                    ct.CopyFileSet.Includes.Add( strFile );
                                ct.CopyFileSet.BaseDirectory = reference.GetBaseDirectory( cs );
                                ct.ToDirectory = cs.OutputPath;
                                ct.Project = _nanttask.Project;
                                ct.Verbose = _nanttask.Verbose;

                                _nanttask.Project.Indent();
                                ct.Execute();
                                _nanttask.Project.Unindent();
                            }
                        }
                        sw.WriteLine( reference.Setting );
                    }

                    Log(Level.Verbose, _nanttask.LogPrefix + "Compiling resources:" );
                    foreach ( Resource resource in _htResources.Values ) {
                        Log(Level.Info, _nanttask.LogPrefix + " - {0}", resource.InputFile, Level.Verbose);
                        resource.Compile( cs, bShowCommands );
                        sw.WriteLine( resource.Setting );
                    }

                    // Add the compiled files
                    foreach ( string strFile in _htFiles.Keys )
                        sw.WriteLine( @"""" + strFile + @"""");
                }

                tfc.AddFile( strTempFile, false );
                if ( bShowCommands ) {
                    using ( StreamReader sr = new StreamReader( strTempFile ) ) {
                        Console.WriteLine( "Commands:" );
                        Console.WriteLine( sr.ReadToEnd() );
                    }
                }

                Log(Level.Verbose, _nanttask.LogPrefix + "Starting compiler...");
                ProcessStartInfo psi = null;
                if ( _ps.Type == ProjectType.CSharp ) {
                    psi = new ProcessStartInfo( Path.Combine( _nanttask.Project.CurrentFramework.FrameworkDirectory.FullName, "csc.exe" ), "@" + strTempFile );
                }

                if ( _ps.Type == ProjectType.VBNet ) {
                    psi = new ProcessStartInfo( Path.Combine( _nanttask.Project.CurrentFramework.FrameworkDirectory.FullName, "vbc.exe" ), "@" + strTempFile );
                }

                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.WorkingDirectory = _strProjectDirectory;

                Process p = Process.Start( psi );

                if ( strLogFile != null ) {
                    using ( StreamWriter sw = new StreamWriter( strLogFile, true ) ) {
                        sw.WriteLine( "Configuration: {0}", strConfiguration );
                        sw.WriteLine( "" );
                        while ( true ) {
                            string strLogContents = p.StandardOutput.ReadLine();
                            if ( strLogContents == null )
                                break;
                            sw.WriteLine( strLogContents );
                        }
                    }
                }
                else {
                    _nanttask.Project.Indent();
                    while ( true ) {                        
                        string strLogContents = p.StandardOutput.ReadLine();
                        if ( strLogContents == null )
                            break;                    
                        Log(Level.Info, "      [compile] " + strLogContents);
                    }
                    _nanttask.Project.Unindent();
                }

                p.WaitForExit();

                int nExitCode = p.ExitCode;
                Log(Level.Verbose, _nanttask.LogPrefix + "{0}! (exit code = {1})", ( nExitCode == 0 ) ? "Success" : "Failure", nExitCode );

                if ( nExitCode > 0 )
                    bSuccess = false;
                else {
                    if ( _bWebProject ) {
                        Log(Level.Info, _nanttask.LogPrefix + "Uploading output files");
                        WebDavClient wdc = new WebDavClient( new Uri( _strWebProjectBaseUrl ) );
                        //wdc.DeleteFile( cs.FullOutputFile, cs.RelOutputPath + "/" + _ps.OutputFile );
                        wdc.UploadFile( cs.FullOutputFile, cs.RelOutputPath + "/" + _ps.OutputFile );
                    }

                    // Copy any extra files over
                    foreach ( string strExtraOutputFile in cs.ExtraOutputFiles ) {
                        FileInfo fi = new FileInfo( strExtraOutputFile );
                        if ( _bWebProject ) {
                            WebDavClient wdc = new WebDavClient( new Uri( _strWebProjectBaseUrl ) );
                            wdc.UploadFile( strExtraOutputFile, cs.RelOutputPath + "/" + fi.Name );
                        }
                        else {
                            string strOutFile = cs.OutputPath + @"\" + fi.Name;

                            if ( File.Exists( strOutFile ) ) {
                                File.SetAttributes( strOutFile, FileAttributes.Normal );
                                File.Delete( strOutFile );
                            }

                            File.Copy( fi.FullName, strOutFile );
                        }
                    }
                }
            }
            finally {
                tfc.Delete();
                Directory.Delete( tfc.BasePath, true );
            }

            if ( !bSuccess )
                Console.WriteLine( "Build failed." );

            return bSuccess;
        }

        public string GUID {
            get { return _ps.GUID; }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to be logged.</param>
        /// <remarks>
        /// The actual logging is delegated to the task.
        /// </remarks>
        private void Log(Level messageLevel, string message) {
            if (_nanttask != null) {
                _nanttask.Log(messageLevel, message);
            }
        }

        /// <summary>
        /// Logs a message with the given priority.
        /// </summary>
        /// <param name="messageLevel">The message priority at which the specified message is to be logged.</param>
        /// <param name="message">The message to log, containing zero or more format items.</param>
        /// <param name="args">An <see cref="object" /> array containing zero or more objects to format.</param>
        /// <remarks>
        /// The actual logging is delegated to the task.
        /// </remarks>
        private void Log(Level messageLevel, string message, params object[] args) {
            if (_nanttask != null) {
                _nanttask.Log(messageLevel, message, args);
            }
        }

        private Hashtable    _htConfigurations;
        private Hashtable    _htReferences;
        private Hashtable    _htFiles;
        private Hashtable    _htResources;
        private Hashtable    _htAssemblies;
        private string         _strImports;
        private Task _nanttask;
        private bool        _bWebProject;

        private string            _strProjectDirectory;
        private string            _strWebProjectBaseUrl;
        private ProjectSettings        _ps;
    }
}
