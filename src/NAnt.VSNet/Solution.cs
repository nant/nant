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
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NAnt.Core;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Summary description for Solution.
    /// </summary>
    public class Solution {
        public Solution( string strSolutionFilename, ArrayList alAdditionalProjects, ArrayList alReferenceProjects, Task nanttask ) {
            _strFilename = strSolutionFilename;
            _htProjects = new Hashtable();
            _htProjectDirectories = new Hashtable();
            _htOutputFiles = new Hashtable();
            _htProjectFiles = new Hashtable();
            _htProjectDependencies = new Hashtable();
            _htReferenceProjects = new Hashtable();
            _nanttask = nanttask;

            string strFileContents;

            using ( StreamReader sr = new StreamReader( strSolutionFilename ) ) {
                strFileContents = sr.ReadToEnd();
            }

            Regex re = new Regex( @"Project\(\""(?<package>\{.*?\})\"".*?\""(?<name>.*?)\"".*?\""(?<project>.*?)\"".*?\""(?<guid>.*?)\""" );
            MatchCollection mc = re.Matches( strFileContents );
            FileInfo fiSolution = new FileInfo( strSolutionFilename );

            foreach ( Match m in mc ) {
                string strPackage = m.Groups[ "package" ].Value;
                string strName = m.Groups[ "name" ].Value;
                string strProject = m.Groups[ "project" ].Value;
                string strGUID = m.Groups[ "guid" ].Value;

                string strFullPath;
                try {
                    Uri uri = new Uri( strProject );
                    if ( uri.Scheme == Uri.UriSchemeFile )
                        strFullPath = Path.Combine( fiSolution.DirectoryName, uri.LocalPath );
                    else
                        strFullPath = strProject;
                }
                catch ( UriFormatException ) {
                    strFullPath = Path.Combine( fiSolution.DirectoryName, strProject );
                }
                
                if ( Project.IsEnterpriseTemplateProject( strFullPath ) )
                    RecursiveLoadTemplateProject( strFullPath );
                else
                    _htProjectFiles[ strGUID ] = strFullPath;
            }

            Regex reDependencies = new Regex( @"^\s+(?<guid>\{[0-9a-zA-Z]{8}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{12}\}).\d+\s+=\s+(?<dep>\{[0-9a-zA-Z]{8}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{4}-[0-9a-zA-Z]{12}\})", RegexOptions.Multiline );
            mc = reDependencies.Matches( strFileContents );

            foreach ( Match m in mc ) {
                string strGUID = m.Groups[ "guid" ].Value;
                string strDependency = m.Groups[ "dep" ].Value;

                AddProjectDependency( strGUID, strDependency );
            }

            //Console.WriteLine( "Loading project GUIDs..." );
            LoadProjectGUIDs( alAdditionalProjects, false );
            LoadProjectGUIDs( alReferenceProjects, true );
            //Console.WriteLine( "Loading projects..." );
            LoadProjects();
            //Console.WriteLine( "Gathering additional dependencies..." );
            GetDependenciesFromProjects();
        }

        public void RecursiveLoadTemplateProject( string strFilename ) {
            XmlDocument doc = new XmlDocument();
            doc.Load( strFilename );

            foreach ( XmlNode node in doc.SelectNodes( "//Reference" ) ) {
                string strSubProjectFilename = node.SelectSingleNode( "FILE" ).InnerText;
                string strGUID = node.SelectSingleNode( "GUIDPROJECTID" ).InnerText;

                string strFullPath = Path.Combine( Path.GetDirectoryName( strFilename ), strSubProjectFilename );
                if ( Project.IsEnterpriseTemplateProject( strFullPath ) )
                    RecursiveLoadTemplateProject( strFullPath );
                else
                    _htProjectFiles[ strGUID ] = strFullPath;
            }
        }

        public Solution( ArrayList alProjects, ArrayList alReferenceProjects, Task nanttask ) {
            _htProjects = new Hashtable();
            _htProjectDirectories = new Hashtable();
            _htOutputFiles = new Hashtable();
            _htProjectFiles = new Hashtable();
            _htProjectDependencies = new Hashtable();
            _htReferenceProjects = new Hashtable();
            _nanttask = nanttask;

            //Console.WriteLine( "Loading project GUIDs..." );
            LoadProjectGUIDs( alProjects, false );
            LoadProjectGUIDs( alReferenceProjects, true );
            //Console.WriteLine( "Loading projects..." );
            LoadProjects();
            //Console.WriteLine( "Gathering additional dependencies..." );
            GetDependenciesFromProjects();
        }

        private void LoadProjectGUIDs( ArrayList alProjects, bool bIsReferenceProject ) {
            foreach ( string strProjectFilename in alProjects ) {
                //Console.WriteLine( "{0} -> {1}", strProjectFilename, Project.LoadGUID( strProjectFilename ) );
                string strGUID = Project.LoadGUID( strProjectFilename );
                _htProjectFiles[ strGUID ] = strProjectFilename;
                if ( bIsReferenceProject )
                    _htReferenceProjects[ strGUID ] = null;
            }
        }

        private void AddProjectDependency( string strProjectGUID, string strDependencyGUID ) {
            //Console.WriteLine( "{0}->{1}", strProjectGUID, strDependencyGUID );
            if ( !_htProjectDependencies.Contains( strProjectGUID ) )
                _htProjectDependencies[ strProjectGUID ] = new Hashtable();

            ( ( Hashtable )_htProjectDependencies[ strProjectGUID ] )[ strDependencyGUID ] = null;
        }

        private void RemoveProjectDependency( string strProjectGUID, string strDependencyGUID ) {
            if ( !_htProjectDependencies.Contains( strProjectGUID ) )
                return;

            ( ( Hashtable )_htProjectDependencies[ strProjectGUID ] ).Remove( strDependencyGUID );
        }

        private bool HasProjectDependency( string strProjectGUID, string strDependencyGUID ) {
            if ( !_htProjectDependencies.Contains( strProjectGUID ) )
                return false;

            return ( ( Hashtable )_htProjectDependencies[ strProjectGUID ] ).Contains( strDependencyGUID );
        }

        private string[] GetProjectDependencies( string strProjectGUID ) {
            if ( !_htProjectDependencies.Contains( strProjectGUID ) )
                return new string[ 0 ];

            return ( string[] )new ArrayList( ( ( Hashtable )_htProjectDependencies[ strProjectGUID ] ).Keys ).ToArray( typeof( string ) );
        }

        private void LoadProjects() {
            foreach ( DictionaryEntry de in _htProjectFiles ) {
                Project p = new Project( _nanttask );
                //Console.WriteLine( "  {0}", de.Value );
                p.Load( this, ( string )de.Value );
                _htProjects[ de.Key ] = p;
            }
        }

        private void GetDependenciesFromProjects() {
            // First get all of the output files
            foreach ( DictionaryEntry de in _htProjects ) {
                string strGUID = ( string )de.Key;
                Project p = ( Project )de.Value;

                foreach ( string strConfiguration in p.Configurations ) {
                    //Console.WriteLine( "{0} [{1}] -> {2}", p.Name, strConfiguration, p.GetConfigurationSettings( strConfiguration ).FullOutputFile.ToLower() );
                    _htOutputFiles[ p.GetConfigurationSettings( strConfiguration ).FullOutputFile.ToLower() ] = strGUID;
                }
            }

            // Then build the dependency list
            foreach ( DictionaryEntry de in _htProjects ) {
                string strGUID = ( string )de.Key;
                Project p = ( Project )de.Value;

                foreach ( Reference r in p.References ) {
                    if ( r.IsProjectReference )
                        AddProjectDependency( strGUID, r.ProjectReferenceGUID );
                    else if ( _htOutputFiles.Contains( r.Filename.ToLower() ) )
                        AddProjectDependency( strGUID, ( string )_htOutputFiles[ r.Filename.ToLower() ] );
                }
            }
        }

        public string GetProjectFileFromGUID( string strProjectGUID ) {
            return ( string )_htProjectFiles[ strProjectGUID ];
        }

        public Project GetProjectFromGUID( string strProjectGUID ) {
            return ( Project )_htProjects[ strProjectGUID ];
        }

        public bool Compile( string strConfiguration, ArrayList alCSCArguments, string strLogFile, bool bVerbose, bool bShowCommands ) {
            Hashtable htDeps = ( Hashtable )_htProjectDependencies.Clone();
            Hashtable htProjectsDone = new Hashtable();
            Hashtable htFailedProjects = new Hashtable();

            bool bSuccess = true;
            while ( true ) {
                bool bCompiledThisRound = false;

                foreach ( Project p in _htProjects.Values ) {
                    if ( htProjectsDone.Contains( p.GUID ) )
                        continue;

                    //Console.WriteLine( "{0} {1}: {2} dep(s)", p.Name, p.GUID, GetProjectDependencies( p.GUID ).Length );
                    //foreach ( string strDep in GetProjectDependencies( p.GUID ) )
                    //    Console.WriteLine( "  " + ( ( Project )_htProjects[ strDep ] ).Name );
                    if ( GetProjectDependencies( p.GUID ).Length == 0 ) {
                        bool bFailed = htFailedProjects.Contains( p.GUID );

                        if ( !bFailed ) {
                            // Fixup references
                            //Console.WriteLine( "Fixing up references..." );
                            foreach ( Reference r in p.References ) {
                                //Console.WriteLine( "Original: {0}", r.Filename );
                                if ( r.IsProjectReference ) {
                                    Project pRef = ( Project )_htProjects[ r.ProjectReferenceGUID ];
                                    if ( pRef == null )
                                        throw new Exception( "Unable to locate referenced project while loading " + p.Name );
                                    if ( pRef.GetConfigurationSettings( strConfiguration ) == null )
                                        throw new Exception( "Unable to find appropriate configuration for project reference" );
                                    if ( pRef != null )
                                        r.Filename = pRef.GetConfigurationSettings( strConfiguration ).FullOutputFile.ToLower();
                                } 
                                else if ( _htOutputFiles.Contains( r.Filename.ToLower() ) ) {
                                    Project pRef = ( Project )_htProjects[ ( string )_htOutputFiles[ r.Filename.ToLower() ] ];
                                    if ( pRef != null && pRef.GetConfigurationSettings( strConfiguration ) != null )
                                        r.Filename = pRef.GetConfigurationSettings( strConfiguration ).FullOutputFile.ToLower();
                                }
                                
                                //Console.WriteLine( "   Now: {0}", r.Filename );
                            }
                        }

                        if ( !_htReferenceProjects.Contains( p.GUID ) && ( bFailed || !p.Compile( strConfiguration, alCSCArguments, strLogFile, bVerbose, bShowCommands ) ) ) {
                            if ( !bFailed ) {
                                Console.WriteLine( "*** Project {0} failed!", p.Name );
                                Console.WriteLine( "*** Continuing build with non-dependent projects:" );
                            }

                            bSuccess = false;
                            htFailedProjects[ p.GUID ] = null;

                            // Mark the projects referencing this one as failed
                            foreach ( Project pFailed in _htProjects.Values )
                                if ( HasProjectDependency( pFailed.GUID, p.GUID ) )
                                    htFailedProjects[ pFailed.GUID ] = null;
                        }

                        bCompiledThisRound = true;

                        // Remove all references to this project
                        foreach ( Project pRemove in _htProjects.Values )
                            RemoveProjectDependency( pRemove.GUID, p.GUID );
                        htProjectsDone[ p.GUID ] = null;
                    }
                }

                if ( _htProjects.Count == htProjectsDone.Count )
                    break;
                if ( !bCompiledThisRound )
                    throw new Exception( "Circular dependency detected" );
            }

            return bSuccess;
        }

        public string Filename {
            get { return _strFilename; }
        }

        private string    _strFilename;
        private Hashtable _htProjectFiles;
        private Hashtable _htProjects;
        private Hashtable _htProjectDirectories;
        private Hashtable _htProjectDependencies;
        private Hashtable _htOutputFiles;
        private Hashtable _htReferenceProjects;
        private Task _nanttask;
    }
}
