// NAnt - A .NET build tool
// Copyright (C) 2003 Gerry Shaw
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
// Gerry Shaw (gerry_shaw@yahoo.com)
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml;

using NDoc.Core;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// NDoc configuration class for <see cref="NAntDocumenter" />.
    /// </summary>
    public class NAntDocumenterConfig : BaseDocumenterConfig {
        #region Private Instance Fields

        private string _outputDirectory = @"doc/help/tasks";
        private SdkDocVersion _linkToSdkDocVersion = SdkDocVersion.MsdnOnline;
        private string _applicationName = "NAnt";
        private string _nantBaseUri = "";
        private string _namespaceFilter = "";

        #endregion Private Instance Fields

        #region Public Instance Constructors

        protected NAntDocumenterConfig(string name) : base(name){
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="NAntDocumenterConfig" /> 
        /// class.
        /// </summary>
        public NAntDocumenterConfig() : base("NAnt") {
            // set reasonable ndoc defaults so we don't have to do this in the build file
            CopyrightText = String.Format(CultureInfo.InvariantCulture, "Copyright (C) 2001-{0} Gerry Shaw", DateTime.Now.Year);
            CopyrightHref = "http://nant.sourceforge.net/";
            ShowMissingParams = false;
            ShowMissingRemarks = false;
            ShowMissingReturns = false;
            ShowMissingSummaries = false;
            ShowMissingValues = false;
            DocumentAttributes = true; 
            DocumentEmptyNamespaces = false;
            DocumentInternals = true;
            DocumentPrivates = true;
            DocumentProtected = true;
            IncludeAssemblyVersion = false;
            SkipNamespacesWithoutSummaries = false;
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// Gets or sets the output directory.
        /// </summary>
        /// <value>
        /// The output directory.
        /// </value>
        [Category("Documentation Main Settings")]
        [Description("The path to the output directory where the generated docs will be placed.")]
        public string OutputDirectory {
            get { return _outputDirectory; }
            set { 
                _outputDirectory = value; 
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the .NET Framework SDK version to provide links to for
        /// system types.
        /// </summary>
        /// <value>
        /// The .NET Framework SDK version to provide links to for system types.
        /// The default is <see cref="SdkDocVersion.MsdnOnline" />.
        /// </value>
        [Category("Output")]
        [Description("Specifies to which version of the .NET Framework SDK documentation the links to system types will be pointing.")]
        public SdkDocVersion LinkToSdkDocVersion {
            get { return _linkToSdkDocVersion; }
            set {
                _linkToSdkDocVersion = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the name of the application for which documentation 
        /// should be generated.
        /// </summary>
        /// <value>
        /// The name of the application for which documentation should be 
        /// generated.
        /// </value>
        [Category("Output")]
        [Description("The name of the application for which documentation should be generated.")]
        public string ApplicationName {
            get { return _applicationName; }
            set { 
                _applicationName = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the base URI for linking to NAnt docs.
        /// </summary>
        /// <value>
        /// The base URI for linking to NAnt docs.
        /// </value>
        [Category("Output")]
        [Description("The base URI for linking to NAnt docs.")]
        public string NAntBaseUri {
            get { return _nantBaseUri; }
            set { 
                _nantBaseUri = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the root namespace to document.
        /// </summary>
        /// <value>
        /// The root namespace to document, or a empty <see cref="string" />
        /// if no restriction should be set on the namespace to document.
        /// </value>
        [Category("Output")]
        [Description("The root namespace to document, or an empty string to document all namespaces.")]
        public string NamespaceFilter {
            get { return _namespaceFilter; }
            set { 
                _namespaceFilter = value;
                SetDirty();
            }
        }

        #endregion Public Instance Properties
    }
}
