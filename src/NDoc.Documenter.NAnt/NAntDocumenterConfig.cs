// NAnt - A .NET build tool
// Copyright (C) 2001 Gerry Shaw
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

using System;
using System.Xml;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

using NDoc.Core;

namespace Sourceforge.NAnt.Documenter {
	/// <summary>
	///  NAntDocumenterConfig Config class for NAntDocumenter.
	/// </summary>
	public class NAntTaskDocumenterConfig : BaseDocumenterConfig {
		
		string _outputDirectory;
		
		/// <summary>Initializes a new instance of the NAntDocumenterConfig class.</summary>
		public NAntTaskDocumenterConfig() : base("NAntTask") {
			_outputDirectory =  @".\docs\Tasks";
		}
		
		/// <summary>Gets or sets the OutputFile property.</summary>
		[
			Category("Output"),
			Description("The path to the Output Directory where the generated doc will be placed."),
			Editor(typeof(FileNameEditor), typeof(UITypeEditor))
		]
		/// <summary>Gets or sets the OutputDirectory property.</summary>
		public string OutputDirectory
		{
			get { return _outputDirectory; }

			set 
			{ 
				_outputDirectory = value; 
				SetDirty();
			}
		}
	}
}
