// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
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
// Gert Driesen (drieseng@users.sourceforge.net)
// Tim Noll (tim.noll@gmail.com)

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using NAnt.Core.Attributes;
using NAnt.Core.Util;

namespace NAnt.Core.Types {
    /// <summary>
    /// Represents an XSLT extension object. The object should have a default
    /// parameterless constructor and the return value should be one of the 
    /// four basic XPath data types of number, string, Boolean or node set.
    /// </summary>
    [ElementName("xsltextensionobject")]
    public class XsltExtensionObject : Element, IConditional {
        #region Private Instance Fields

        private string _namespaceUri = string.Empty;
        private string _typeName;
        private FileInfo _assemblyPath;
        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XsltExtensionObject" /> 
        /// class.
        /// </summary>
        public XsltExtensionObject() {
        }

        #endregion Public Instance Constructors

        #region Public Instance Properties

        /// <summary>
        /// The namespace URI to associate with the extension object.
        /// </summary>
        /// <value>
        /// The namespace URI to associate with the extension object, or 
        /// <see cref="string.Empty" /> if not set.
        /// </value>
        [TaskAttribute("namespaceuri")]
        public string NamespaceUri {
            get { return _namespaceUri; }
            set { _namespaceUri = value; }
        }

        /// <summary>
        /// The full type name of the XSLT extension object.
        /// </summary>
        [TaskAttribute("typename", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string TypeName {
            get { return _typeName; }
            set { _typeName = value; }
        }
        
        /// <summary>
        /// The assembly which contains the XSLT extension object.
        /// </summary>
        [TaskAttribute("assembly", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public FileInfo AssemblyPath 
        {
            get { return _assemblyPath; }
            set { _assemblyPath = value; }
        }

        /// <summary>
        /// Indicates if the extension object should be added to the XSLT argument
        /// list. If <see langword="true" /> then the extension object will be
        /// added; otherwise, skipped. The default is <see langword="true" />.
        /// </summary>
        [TaskAttribute("if")]
        [BooleanValidator()]
        public bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        /// <summary>
        /// Indicates if the extension object should not be added to the XSLT argument
        /// list. If <see langword="false" /> then the extension object will be 
        /// added; otherwise, skipped. The default is <see langword="false" />.
        /// </summary>
        [TaskAttribute("unless")]
        [BooleanValidator()]
        public bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        public object CreateInstance() {
            // test whether extension assembly exists
            if (!AssemblyPath.Exists) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1166"),
                    AssemblyPath.FullName), Location);
            }

            // load extension object from assembly
            object extensionInstance = null;
            try {
                Assembly extensionAssembly = Assembly.LoadFrom(AssemblyPath.FullName);
                extensionInstance = extensionAssembly.CreateInstance(TypeName);
                if ( extensionInstance == null){
                    throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1168"),
                    TypeName, AssemblyPath.FullName), Location );
                }
            } catch (Exception ex) {
                throw new BuildException(string.Format(CultureInfo.InvariantCulture,
                    ResourceUtils.GetString("NA1167"),
                    TypeName, AssemblyPath.FullName), Location, ex);
            }
            return extensionInstance;
        }

        #endregion
    }
}

