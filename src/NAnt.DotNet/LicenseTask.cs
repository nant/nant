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

// Matthew Mastracci (mmastrac@canada.com)

using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt.Tasks
{
	/// <summary>
	/// Task to generate a .licence file from a .licx file.
	/// </summary>
	/// <remarks>
	/// If no output file is specified, the default filename is the name of the target file with the extension 
	/// ".licenses" appended.
	/// </remarks>
	/// <example>
	///   <para>Generate the file <c>component.exe.licenses</c> file from <c>component.licx</c>.</para>
	///   <code>
	///     <![CDATA[
	///         <resx input="component.licx" target="component.exe" />
	///     ]]>
	///   </code>
	/// </example>
	[TaskName("license")]
	public class LicenseTask : Task
	{
		public LicenseTask()
		{
			_assemblies = new FileSet();
		}

		protected override void ExecuteTask()
		{
			// Get the input .licx file
			string strLicxFilename = null;
			try {
				strLicxFilename = Project.GetFullPath( _input );
			} catch ( Exception e ) {
				string msg = String.Format( "Could not determine path from {0}", _input );
				throw new BuildException( msg, Location, e );
			}			

			// Get the output .licenses file
			string strResourceFilename = null;
			try {
				if ( _output == null )
					strResourceFilename = Project.GetFullPath( _strTarget + ".licenses" );
				else
					strResourceFilename = Project.GetFullPath( _output );
			} catch ( Exception e ) {
				string msg = String.Format( "Could not determine path from output file {0} and target {1}", _output, _strTarget );
				throw new BuildException( msg, Location, e );
			}

			Log.WriteLine( "Compiling license file {0} to {1} using target {2}", _input, strResourceFilename, _strTarget );

			ArrayList alAssemblies = new ArrayList();

			// First, load all the assemblies so that we can search for the licensed component
			foreach ( string strAssembly in _assemblies.Includes )
			{
				Assembly asm;

				try
				{
					string strRealAssemblyName = Project.GetFullPath( strAssembly );

					// See if we've got an absolute path to the assembly
					if ( File.Exists( strRealAssemblyName ) )
					{
						asm = Assembly.LoadFrom( strRealAssemblyName );
					}
					else
					{
						// No absolute path, ask .NET to load it for us (use the original assembly name)
						FileInfo fiAssembly = new FileInfo( strAssembly );
						asm = Assembly.LoadWithPartialName( Path.GetFileNameWithoutExtension( fiAssembly.Name ) );
					}

					alAssemblies.Add( asm );
				}
				catch ( Exception e )
				{
					throw new BuildException( String.Format( "Unable to load specified assembly: {0}", strAssembly ), e );
				}
			}

			// Create the license manager
			DesigntimeLicenseContext dlc = new DesigntimeLicenseContext();
			LicenseManager.CurrentContext = dlc;
			
			// Read in the input file
			using ( StreamReader sr = new StreamReader( strLicxFilename ) )
			{
				Hashtable htLicenses = new Hashtable();

				while ( true )
				{
					string strLine = sr.ReadLine();
					if ( strLine == null )
						break;
					strLine = strLine.Trim();
					// Skip comments and empty lines
					if ( strLine.StartsWith( "#" ) || strLine.Length == 0 || htLicenses.Contains( strLine ) )
						continue;

					if ( Verbose )
						Log.Write( strLine + ": " );

					// Strip off the assembly name, if it exists
					string strTypeName;

					if ( strLine.IndexOf( ',' ) == -1 )
						strTypeName = strLine.Trim();
					else
						strTypeName = strLine.Split( ',' )[ 0 ];

					Type tp = null;

					// Try to locate the type in each assembly
					foreach ( Assembly asm in alAssemblies )
					{
						tp = asm.GetType( strTypeName, false, true );
						if ( tp == null )
							continue;

						htLicenses[ strLine ] = tp;
						break;
					}

					if ( tp == null )
						throw new BuildException( String.Format( "Failed to locate type: {0}", strTypeName ), Location );

					if ( Verbose && tp != null )
						Log.WriteLine( ( ( Type )htLicenses[ strLine ] ).Assembly.CodeBase );

					// Ensure that we've got a licensed component
					if ( tp.GetCustomAttributes( typeof( LicenseProviderAttribute ), true ).Length == 0 )
						throw new BuildException( String.Format( "Type is not a licensed component: {0}", tp ), Location );

					// Now try to create the licensed component - this gives us a license
					try
					{
						LicenseManager.CreateWithContext( tp, dlc );
					}
					catch ( Exception e )
					{
						throw new BuildException( String.Format( "Failed to create license for type {0}", tp ), Location, e );
					}
				}
			}

			// Overwrite the existing file, if it exists - is there a better way?			
			if ( File.Exists( strResourceFilename ) )
			{
				File.SetAttributes( strResourceFilename, FileAttributes.Normal );
				File.Delete( strResourceFilename );
			}

			// Now write out the license file, keyed to the appropriate output target filename
			// This .license file will only be valid for this exe/dll
			using ( FileStream fs = new FileStream( _output, FileMode.Create ) )
			{
				DesigntimeLicenseContextSerializer.Serialize( fs, _strTarget, dlc );
			}
		}

		/// <summary>Input file to process.</summary>
		[TaskAttribute("input", Required=true)]
		public string Input 
		{ 
			get { return _input; } 
			set { _input = value;} 
		}

		/// <summary>Name of the resource file to output.</summary>
		[TaskAttribute("output", Required=false)]
		public string Output 
		{ 
			get { return _output; } 
			set {_output = value;} 
		}

		/// <summary>
		/// Names of the references to scan for the licensed component.
		/// </summary>
		[FileSet("assemblies")]
		public FileSet Assemblies      
		{ 
			get { return _assemblies; } 
			set { _assemblies = value; }
		}

		/// <summary>
		/// The output executable file for which the license will be generated.
		/// </summary>
		[TaskAttribute("licensetarget", Required=true)]
		public string Target
		{
			get { return _strTarget; }
			set { _strTarget = value; }
		}

		FileSet	_assemblies;
		string _input, _output, _strTarget;
	}
}
