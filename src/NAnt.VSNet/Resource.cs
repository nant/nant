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
using System.Xml;
using System.Collections;
using System.Text.RegularExpressions;

using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.DotNet.Tasks;

namespace NAnt.VSNet.Tasks {
    /// <summary>
    /// Summary description for Resouce.
    /// </summary>
    public class Resource {
        public Resource(Project p, string strResourceSourceFile, string strResourceSourceFileRelPath, string strDependentFile, Task nanttask) {
            _p = p;
            _ps = p.ProjectSettings;

            string strProjectRootNamespace = _ps.RootNamespace;
            _strResourceSourceFile = strResourceSourceFile;
            _strResourceSourceFileRelPath = strResourceSourceFileRelPath;
            FileInfo fiResource = new FileInfo( _strResourceSourceFile );
            _strDependentFile = strDependentFile;
            _nanttask = nanttask;
        }

        public void Compile( ConfigurationSettings cs, bool bShowCommands ) {
            _cs = cs;

            FileInfo fiResource = new FileInfo( _strResourceSourceFile );

            switch ( fiResource.Extension.ToLower() ) {
                case ".resx":
                    _strResourceFile = CompileResx();
                    break;
                case ".licx":
                    _strResourceFile = CompileLicx();
                    break;
                default:
                    _strResourceFile = CompileResource();
                    break;
            }
        }

        public string Setting {
            get { return @"/res:""" + _strResourceFile + @""""; }
        }

        public string InputFile {
            get { return _strResourceSourceFile; }
        }

        private string GetDependentResourceName( string strDependentFile ) {
            switch ( Path.GetExtension( strDependentFile ).ToLower() ) {
                case ".cs":
                    return GetDependentResourceNameCSharp( strDependentFile );
                case ".vb":
                    return GetDependentResourceNameVB( strDependentFile );
                default:
                    throw new ArgumentException( "Unknown file extension" );
            }
        }

        private string GetDependentResourceNameCSharp( string strDependentFile ) {
            Regex re = new Regex( @"
                (?>namespace(?<ns>(.|\s)*?){)
                    |
                (?>class(?<class>.*?):)
                    |
                }
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled );

            Match m;
            using ( StreamReader sr = new StreamReader( strDependentFile ) ) {
                m = re.Match( sr.ReadToEnd() );
            }

            Stack st = new Stack();

            while ( m.Success ) {            
                string strValue = m.Value;
                if ( strValue.StartsWith( "namespace" ) ) {
                    st.Push( m.Result( "${ns}" ).Trim() );
                }
                else if ( strValue.StartsWith( "class" ) ) {
                    st.Push( m.Result( "${class}" ).Trim() );
                    break;
                }
                else if ( strValue == "}" ) {
                    if ( st.Count > 0 )
                        st.Pop();
                }
                
                m = m.NextMatch();
            }
        
            Stack stReverse = new Stack();
            while ( st.Count > 0 )
                stReverse.Push( st.Pop() );

            ArrayList al = new ArrayList( stReverse.ToArray() );

            string strClassName = String.Join( ".", ( string[] )al.ToArray( typeof( string ) ) );
            string strResourceFilename = strClassName + ".resources";

            return strResourceFilename;
        }

        private string GetDependentResourceNameVB( string strDependentFile ) {
            Regex re = new Regex( @"
                (?>^\s*?(?!End)\s*Namespace\s*(?<ns>.*)\s*?$)
                    |
                (?>^(?>\s*)(?!End)([\w\s](?=(?!$)))*Class\s*(?<class>.*?)\s*?$)
                    |
                ^\s*End\s*(?:(Class|Namespace))\s*?$
            ", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Compiled );

            Match m;
            using ( StreamReader sr = new StreamReader( strDependentFile ) ) {
                m = re.Match( sr.ReadToEnd() );
            }

            Stack st = new Stack();

            while ( m.Success ) {            
                string strValue = m.Value.Trim();
		if ( strValue.StartsWith( "End " ) ) {
                    if ( st.Count > 0 )
                        st.Pop();
                }                
		else if ( strValue.StartsWith( "Namespace" ) ) {
                    st.Push( m.Result( "${ns}" ).Trim() );
                }
                else if ( strValue.IndexOf( "Class" ) >= 0 ) {
                    st.Push( m.Result( "${class}" ).Trim() );
                    break;
                }
                
                m = m.NextMatch();
            }
        
            Stack stReverse = new Stack();
            while ( st.Count > 0 )
                stReverse.Push( st.Pop() );

            ArrayList al = new ArrayList( stReverse.ToArray() );

            string strClassName = String.Join( ".", ( string[] )al.ToArray( typeof( string ) ) );
            string strResourceFilename = _ps.RootNamespace + "." + strClassName + ".resources";

            return strResourceFilename;
        }

        private string CompileResource() {
            string strOutputFile = _ps.GetTemporaryFilename( _ps.RootNamespace + "." + _strResourceSourceFileRelPath.Replace( "\\", "." ) );
            
            if ( File.Exists( strOutputFile ) ) {
                File.SetAttributes( strOutputFile, FileAttributes.Normal );
                File.Delete( strOutputFile );
            }

            File.Copy( _strResourceSourceFile, strOutputFile );

            return strOutputFile;
        }

        private string CompileLicx() {
            string strOutputFile = _ps.OutputFile;

            LicenseTask lt = new LicenseTask();

            lt.Input = _strResourceSourceFile;
            lt.Output = _ps.GetTemporaryFilename( strOutputFile + ".licenses" );
            lt.Target = strOutputFile;
            lt.Verbose = _nanttask.Verbose;
            lt.Assemblies = new FileSet();
            foreach ( Reference r in _p.References ) {
                if ( r.IsSystem )
                    lt.Assemblies.AsIs.Add( r.Name );
                else
                    lt.Assemblies.Includes.Add( r.Filename );
            }

            lt.Project = _nanttask.Project;

            lt.Project.Indent();
            lt.Execute();
            lt.Project.Unindent();

            return lt.Output;
        }

        private string CompileResx() {
            string strInFile = _strResourceSourceFile;
            string strOutFile;
            
            if ( _strDependentFile != null ) {
                strOutFile = GetDependentResourceName( _strDependentFile );
            }
            else {
                strOutFile = _ps.RootNamespace + "." + Path.GetDirectoryName( _strResourceSourceFileRelPath ).Replace( "\\", "." ) + "." + Path.GetFileNameWithoutExtension( _strResourceSourceFile ) + ".resources";
            }
            strOutFile = _ps.GetTemporaryFilename( strOutFile );

            ResGenTask rt = new ResGenTask();
            rt.Input = strInFile;
            rt.Output = strOutFile;
            rt.Verbose = false;
            rt.Project = _nanttask.Project;

            rt.Project.Indent();
            rt.Execute();
            rt.Project.Unindent();

            return strOutFile;
        }

        private string                    _strResourceFile;
        private string                    _strResourceSourceFile;
        private string                    _strDependentFile;
        private string                    _strResourceSourceFileRelPath;
        private ProjectSettings            _ps;
        private Project                    _p;
        private ConfigurationSettings    _cs;
        private Task _nanttask;
    }
}
