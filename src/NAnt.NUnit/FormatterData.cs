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
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Collections;
using NUnit.Framework;

namespace SourceForge.NAnt.Tasks.NUnit.Formatters 
{

   /// <summary>
   /// Carries data specified through the formatter element
   /// </summary>
   [Serializable]
   public class FormatterData
   {
        string _classname = null;               
        string _extension = null;               
        bool _usefile = true;
        FormatterType _formatterType = FormatterType.Plain;

      public FormatterType Type {
         get { return _formatterType; }
         set { _formatterType = value; }
      }

      public string ClassName {
         get { return _classname; }
         set { _classname = value; }
      }

      public bool UseFile {
         get { return _usefile; }
         set { _usefile = value; }
      }

      public string Extension {
         get { return _extension; }
         set { _extension = value; }
      }
   }    
}