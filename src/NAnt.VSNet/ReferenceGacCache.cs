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
// Matt Mastracci <mmastrac@canada.com>

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.IO;

using NAnt.Core;
using NAnt.VSNet.Tasks;

namespace NAnt.VSNet {
    /// <summary>
    /// Factory class for VS.NET projects.
    /// </summary>
    public sealed class ReferenceGACCache : IDisposable 
    {
        #region Public Instance Constructors
        /// <summary>
        /// Initializes a new <see cref="ReferenceGACCache"/>.
        /// </summary>
        public ReferenceGACCache()
        {
            _appDomain = AppDomain.CreateDomain("temporaryDomain");
            _gacResolver = 
                ((GACResolver) _appDomain.CreateInstanceFrom(Assembly.GetExecutingAssembly().Location,
                typeof(GACResolver).FullName).Unwrap());
            _gacQueryCache = CollectionsUtil.CreateCaseInsensitiveHashtable();
        }
        #endregion Public Instance Constructors

        #region Public Instance Destructors
        ~ReferenceGACCache()
        {
            Dispose();
        }
        #endregion Public Instance Destructors


        public void Dispose()
        {
            if (_appDomain != null)
            {
                AppDomain.Unload(_appDomain);
                _appDomain = null;
                GC.SuppressFinalize(this);
            }
        }

        public bool IsAssemblyInGac(string filename)
        {
            string strPath = Path.GetFullPath(filename);
            if (_gacQueryCache.Contains(filename))
                return (bool)_gacQueryCache[filename];

            _gacQueryCache[filename] = _gacResolver.IsAssemblyInGAC(filename);
            return (bool)_gacQueryCache[filename];
        }

        public class GACResolver : MarshalByRefObject
        {
            public bool IsAssemblyInGAC(string filename)
            {
                Assembly asm = Assembly.LoadFrom(filename);
                return asm.GlobalAssemblyCache;
            }
        }

        private AppDomain _appDomain;
        private Hashtable _gacQueryCache;
        private GACResolver _gacResolver;
    }
}