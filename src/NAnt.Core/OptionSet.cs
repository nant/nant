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

// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Xml;

using SourceForge.NAnt.Attributes;

namespace SourceForge.NAnt 
{

   /// <summary>
   /// Represents an option in an optionSet
   /// </summary>
   public struct OptionValue {
      private string _name;
      private string _value;

      public string Name { 
         get { return _name; }
      }
      public string Value { 
         get { return _value; }
      }

      internal OptionValue(string name, string value)
      {
         _name = name;
         _value = value;
      }
   } // struct optionValue;

   /// <summary>
   /// Handles a set of options as a name/value collection.
   /// </summary>
   public class OptionSet : Element, IEnumerable
   {
      private ArrayList _options;

      /// <summary>
      /// Indexer, based on option index.
      /// </summary>
      public OptionValue this[int index] {
         get { return (OptionValue)_options[index]; }
      }

      /// <summary>
      /// Number of options in the set
      /// </summary>
      public int Count {
         get { return _options.Count; }
      }

      /// <summary>
      /// Initialize a new Instance
      /// </summary>
      public OptionSet()
      {
         _options = new ArrayList();
      }

      /// <summary>
      /// Initialize this element node
      /// </summary>
      /// <param name="elementNode"></param>
      protected override void InitializeElement(XmlNode elementNode)  
      {
         //
         // Check out whatever <propvalue> elements there are
         //
         foreach ( XmlNode node in elementNode ) 
         {
            if ( node.Name.Equals("option") ) {
               OptionElement v = new OptionElement();
               v.Project = Project;
               v.Initialize(node);
               _options.Add(new OptionValue(v.OptionName, v.Value));
            }
         }
      }


      public IEnumerator GetEnumerator()
      {
         return _options.GetEnumerator();
      }

   } // class OptionSet


   [ElementName("option")]
   class OptionElement : Element
   {
      private string _name = null;
      private string _value = null;

      /// <summary>
      /// Name of this property
      /// </summary>
      [TaskAttribute("name", Required=true)]
      public string OptionName {
         get { return _name; }
         set { _name = value; }
      }

      /// <summary>
      /// Value of this property. Default is null;
      /// </summary>
      [TaskAttribute("value")]
      public string Value {
         get { return _value; }
         set { _value = value; }
      }

   } // class PropvalueElement

} // namespace SourceForge.NAnt 

 