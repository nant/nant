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
// Gert Driesen (drieseng@users.sourceforge.net)

using System;
using System.ComponentModel;
using System.Globalization;
using NDoc.Core.Reflection;

namespace NDoc.Documenter.NAnt {
    /// <summary>
    /// NDoc configuration class for <see cref="NAntDocumenter" />.
    /// </summary>
    public class NAntDocumenterConfig : BaseReflectionDocumenterConfig {
        #region Private Instance Fields

        private string _outputDirectory = @"doc/help/tasks";
        private bool _sdkLinksOnWeb;
        private string _productName = "NAnt";
        private string _productVersion = "";
        private string _productUrl = "";
        private string _nantBaseUri = "";
        private string _namespaceFilter = "";

        #endregion Private Instance Fields

        #region Public Instance Constructors

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
            DocumentInternals = false;
            DocumentPrivates = false;
            DocumentProtected = true;
            SkipNamespacesWithoutSummaries = false;
            EditorBrowsableFilter = EditorBrowsableFilterLevel.HideAdvanced;
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
        /// Gets or sets a value indicating whether .NET Framework SDK links 
        /// should point to the online MSDN library.
        /// </summary>
        [Category("Documentation Main Settings")]
        [Description("Turning this flag on will point all SDK links to the online MSDN library")]
        [DefaultValue(false)]
        public bool SdkLinksOnWeb {
            get { return _sdkLinksOnWeb; }
            set {
                _sdkLinksOnWeb= value;
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the name of the product for which documentation 
        /// should be generated.
        /// </summary>
        /// <value>
        /// The name of the product for which documentation should be 
        /// generated. The default is "NAnt".
        /// </value>
        [Category("Output")]
        [Description("The name of the product for which documentation should be generated.")]
        [DefaultValue("NAnt")]
        public string ProductName {
            get { return _productName; }
            set { 
                _productName = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the version of the product for which documentation 
        /// should be generated.
        /// </summary>
        /// <value>
        /// The version of the product for which documentation should be 
        /// generated.
        /// </value>
        [Category("Output")]
        [Description("The version of the product for which documentation should be generated.")]
        [DefaultValue("")]
        public string ProductVersion {
            get { return _productVersion; }
            set { 
                _productVersion = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Gets or sets the URL of product website.
        /// </summary>
        /// <value>
        /// The URL of the website of the product for which documentation should
        /// be generated.
        /// </value>
        [Category("Output")]
        [Description("The URL of the website of the product for which documentation should be generated.")]
        [DefaultValue("")]
        public string ProductUrl {
            get { return _productUrl; }
            set { 
                _productUrl = value;
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
