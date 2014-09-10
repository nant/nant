// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
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
// Martin Aliger (martin_aliger@myrealbox.com)

using System.Xml;
using System.IO;
using NAnt.Core.Util;

using NAnt.VSNet;

namespace NAnt.MSBuild {
    internal class MSBuildAssemblyReference : AssemblyReferenceBase {
        private readonly string _name;
        private readonly MSBuildReferenceHelper _helper;
        private readonly string _assemblyFile;
        private readonly string _hintpath;

        public MSBuildAssemblyReference(XmlElement xe, ReferencesResolver referencesResolver, ProjectBase parent, GacCache gacCache, string name, string priv, string hintpath, string extension)
            : base(new DummyXmlElement(xe.OwnerDocument), referencesResolver, parent, gacCache) {
            if (extension == null || extension.Length == 0) {
                extension = ".dll";
            }
            if (name.Contains(",")) {
                //fully specified reference. Hmmm - just ignore it for now.
                name = name.Split(',')[0];
                //if (hintpath.Length == 0)  //hintpath workaround
                //    hintpath = "." + Path.DirectorySeparatorChar + name + extension; // ".dll";
            }
            _name = name;
            _helper = new MSBuildReferenceHelper(priv, false);
            _hintpath = hintpath;
            _assemblyFile = ResolveAssemblyReference();
        }

        public string HintPath {
            get { return _hintpath; }
        }

        protected override string ResolveAssemblyReference() {
            // check if assembly reference was resolved before
            if (_assemblyFile != null) {
                // if assembly file actually exists, there's no need to resolve
                // the assembly reference again
                if (File.Exists(_assemblyFile)) {
                    return _assemblyFile;
                }
            }

            XmlElement referenceElement = XmlDefinition;

            string assemblyFileName = Name + ".dll";

            // 1. The project directory
            // NOT SURE IF THIS IS CORRECT

            // 2. The ReferencePath
            // NOT SURE WE SHOULD DO THIS ONE

            // 3. The .NET Framework directory
            string resolvedAssemblyFile = ResolveFromFramework(assemblyFileName);
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // 4. AssemblyFolders
            resolvedAssemblyFile = ResolveFromAssemblyFolders(referenceElement,
                assemblyFileName);
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // ResolveFromRelativePath will return a path regardless of 
            // whether the file actually exists
            //
            // the file might actually be created as result of building
            // a project
            resolvedAssemblyFile = ResolveFromRelativePath( HintPath);
            if (resolvedAssemblyFile != null) {
                return resolvedAssemblyFile;
            }

            // resolve from outputPath
            if (Parent is MSBuildProject) {
                resolvedAssemblyFile = ResolveFromRelativePath(Path.Combine(((MSBuildProject)Parent).OutputPath, assemblyFileName));
                if (resolvedAssemblyFile != null) {
                    return resolvedAssemblyFile;
                }
            }
            // assembly reference could not be resolved
            return null;
        }

        protected override bool IsPrivate {
            get { return _helper.IsPrivate; }
        }

        protected override bool IsPrivateSpecified {
            get { return _helper.IsPrivateSpecified; }
        }

        public override string Name {
            get { return _name; }
        }
    }
}
