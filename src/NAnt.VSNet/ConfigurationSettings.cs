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

using SourceForge.NAnt;

namespace SourceForge.NAnt.Tasks
{
    /// <summary>
    /// Summary description for ConfigurationSettings.
    /// </summary>
    public class ConfigurationSettings
    {
        public ConfigurationSettings( ProjectSettings ps, XmlElement elemConfig )
        {
            _alSettings = new ArrayList();
            _strRelOutputPath = elemConfig.Attributes[ "OutputPath" ].Value;
            _strOutputPath = new DirectoryInfo( ps.ProjectRootDirectory + @"\" + elemConfig.Attributes[ "OutputPath" ].Value ).FullName;
            _ps = ps;

            _strName = elemConfig.GetAttribute( "Name" ).ToLower();

            _strDocFilename = null;
            if ( ( elemConfig.Attributes[ "DocumentationFile" ] != null ) &&
               ( elemConfig.Attributes[ "DocumentationFile" ].Value.Length > 0 ))
            {
                FileInfo fiDocumentation = new FileInfo( ps.ProjectRootDirectory + @"/" + elemConfig.Attributes[ "DocumentationFile" ].Value );
                _strDocFilename = fiDocumentation.FullName;
                _alSettings.Add( @"/doc:""" + _strDocFilename + @"""" );

                Directory.CreateDirectory( fiDocumentation.DirectoryName );
            }

            Hashtable htStringSettings = new Hashtable();
            Hashtable htBooleanSettings = new Hashtable();

            htStringSettings[ "BaseAddress" ] = @"/baseaddress:{0}";
            htStringSettings[ "FileAlignment" ] = @"/filealign:{0}";
            
            if ( ps.Type == ProjectType.CSharp )
            {
                htStringSettings[ "WarningLevel" ] = @"/warn:{0}";
                htBooleanSettings[ "IncrementalBuild" ] = "/incremental";
            }

            htBooleanSettings[ "AllowUnsafeBlocks" ] = "/unsafe";
            htBooleanSettings[ "DebugSymbols" ] = "/debug";
            htBooleanSettings[ "CheckForOverflowUnderflow" ] = "/checked";
            htBooleanSettings[ "TreatWarningsAsErrors" ] = "/warnaserror";
            htBooleanSettings[ "Optimize" ] = "/optimize";

            foreach ( DictionaryEntry de in htStringSettings )
            {
                string strValue = elemConfig.GetAttribute( de.Key.ToString() );
                if ( strValue != null && strValue.Length > 0 )
                    _alSettings.Add( String.Format( de.Value.ToString(), strValue ) );
            }

            foreach ( DictionaryEntry de in htBooleanSettings )
            {
                string strValue = elemConfig.GetAttribute( de.Key.ToString() );
                if ( strValue != null && strValue.Length > 0 )
                {
                    if ( strValue == "true" )
                        _alSettings.Add( de.Value.ToString() + "+" );
                    else if ( strValue == "false" )
                        _alSettings.Add( de.Value.ToString() + "-" );
                }
            }

            _alSettings.Add( String.Format( @"/out:""{0}{1}""", OutputPath, ps.OutputFile ) );
        }

        public Task[] GetRequiredTasks()
        {
            return new Task[ 0 ];
        }

        public string[] ExtraOutputFiles
        {
            get
            {
                if ( _strDocFilename == null )
                    return new string[ 0 ];

                return new string[] { _strDocFilename };
            }
        }

        public string RelOutputPath
        {
            get { return _strRelOutputPath; }
        }

        public string OutputPath
        {
            get { return _strOutputPath; }
        }

        public string FullOutputFile
        {
            get { return Path.Combine( _strOutputPath, _ps.OutputFile ); }
        }

        public string[] Settings
        {
            get { return ( string[] )_alSettings.ToArray( typeof( string ) ); }
        }

        public string Name
        {
            get { return _strName; }
        }

        ArrayList            _alSettings;
        string                _strDocFilename;
        string                _strRelOutputPath;
        string                _strOutputPath;
        string                _strName;
        ProjectSettings        _ps;
    }
}
