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

// File Maintainers:
// Ian MacLean (ian@maclean.ms)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Xml;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

using NDoc.Core;

namespace Sourceforge.NAnt.Documenter {

	/// <summary>NAntDocumenterConfig Config class for NAntDocumenter.</summary>
	public class NAntTaskDocumenterConfig : BaseDocumenterConfig {
        string _outputDirectory = @"doc/help/tasks";
		
		/// <summary>Initializes a new instance of the NAntDocumenterConfig class.</summary>
		public NAntTaskDocumenterConfig() : base("NAntTask") {
            // set reasonable ndoc defaults so we don't have to do this in the build file

            CopyrightText = String.Format("Copyright (C) 2001-{0} Gerry Shaw", DateTime.Now.Year);
            CopyrightHref = "http://nant.sourceforge.net/";

            // These are used in the next versions of ndoc
            //DocumentAttributes = true; // This must be true so we can find classes marked as nant tasks
            //DocumentedAttributes = "";

            ShowMissingParams = false;
            ShowMissingRemarks = false;
            ShowMissingReturns = false;
            ShowMissingSummaries = false;
            ShowMissingValues = false;
            DocumentEmptyNamespaces = true;
            DocumentInternals = false;
            DocumentPrivates = false;
            DocumentProtected = true;
            IncludeAssemblyVersion = false;
            SkipNamespacesWithoutSummaries = false;
		}
		
		/// <summary>Gets or sets the OutputFile property.</summary>
		[
			Category("Output"),
			Description("The path to the Output Directory where the generated doc will be placed."),
#if (!BuildWithVSNet)
         Editor(typeof(FileNameEditor), typeof(UITypeEditor))
#endif
		]
		/// <summary>Gets or sets the OutputDirectory property.</summary>
		public string OutputDirectory {
			get { return _outputDirectory; }
			set  { 
				_outputDirectory = value; 
				SetDirty();
			}
		}
	}
}
