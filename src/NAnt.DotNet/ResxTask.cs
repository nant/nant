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
using System.Resources;
using System.Collections;
using SourceForge.NAnt.Attributes;


namespace SourceForge.NAnt.Tasks
{
	/// <summary>
	/// Task to generate a .resources file from a .resx file.
	/// </summary>
	/// <example>
	///   <para>Generate a .resources file for <c>translations.resx</c>.</para>
	///   <code>
	///     <![CDATA[
	///         <resx input="translations.resx" />
	///     ]]>
	///   </code>
	/// </example>
	[TaskName("resx")]
	public class ResxTask : Task
	{
		public ResxTask()
		{
		}

		/// <summary>Input file to process.</summary>
		[TaskAttribute("input", Required=true)]
		public string InputFile
		{
			set { _strInput = value; }
		}
		
		/// <summary>Name of the license resource file to output.</summary>
		[TaskAttribute("output")]
		public string OutputFile
		{
			set { _strOutput = value; }
		}

		protected override void ExecuteTask()
		{
			if ( !File.Exists( _strInput ) )
				throw new BuildException( String.Format( "Unable to find file: {0}", _strInput ), Location );

			string strOutput = _strOutput;
			if ( strOutput == null )
				strOutput = Path.ChangeExtension( _strInput, ".resources" );

			if ( Verbose )
				Log.WriteLine( "Compiling {0} to {1}...", _strInput, strOutput );

			// Open in the input .resx file
			using ( ResXResourceReader rrr = new ResXResourceReader( _strInput ) )
			{
				// Open the output .resources file
				using ( ResourceWriter rw = new ResourceWriter( strOutput ) )
				{
					// Now add each of the input resources to the output resource file
					foreach ( DictionaryEntry de in rrr )
					{
						rw.AddResource( ( string )de.Key, de.Value );
						if ( Verbose )
							Log.WriteLine( "{0}: {1}", de.Key, de.Value.GetType() );
					}
				}
			}
		}

		string _strInput, _strOutput;
	}
}
