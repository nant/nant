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


using System;
using NAnt.Core.Attributes;

namespace NAnt.Core.Filters {
	/// <summary>
	/// ReplaceTokens filter token
	/// </summary>
	/// <remarks>
	/// Represents a token that is used by the <see cref="ReplaceTokens"/> filter.
	/// </remarks>
	[ElementName("token")]
	public class ReplaceTokensToken : Element {
		/// <summary>
		/// Default constructor
		/// </summary>
		public ReplaceTokensToken() : base(){}

		/// <summary>
		/// Token to be replaced
		/// </summary>
		private string _key = null;
		[TaskAttribute("key")]
		public string Key {
			get { return _key; }
			set	{_key = value; }
		}


		/// <summary>
		/// New value of token
		/// </summary>
		private string _value = null;
		[TaskAttribute("value")]
		public string Value {
			get { return _value;  }
			set	{ _value = value; }
		}
	}
}
