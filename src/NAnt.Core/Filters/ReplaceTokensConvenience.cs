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

namespace NAnt.Core.Filters
{

	/// <summary>
	/// <para>Allows users to specifies properties elements as attributes.</para>
	/// </summary>
	///
	/// <remarks>See <see cref="ReplaceTokens"/> for usage</remarks>
	/// 
	[ElementName("replacetokens")] 
	public class ReplaceTokensConvenience : FilterElement
	{
		/// <summary>
		/// Marks the beginning of a token. 
		/// </summary>
		[TaskAttribute("begintoken")]
		public string BeginToken
		{
			set
			{
				base.Parameters.Add("begintoken", value);
			}
		}

		/// <summary>
		/// Marks the end of a token.
		/// </summary>
		[TaskAttribute("endtoken")]
		public string EndToken
		{
			set
			{
				base.Parameters.Add("endtoken", value);			
			}
		}

		/// <summary>
		/// Array of tokens and replacement values
		/// </summary>
		//private ReplaceTokensToken[] _tokens = null;
		[BuildElementArray("token")]
		public ReplaceTokensToken[] Tokens
		{
			set
			{
				//Copy each token into the list of parameters
				foreach (ReplaceTokensToken token in value)
				{
					base.Parameters.Add(token.Key, token.Value);
				}
			}
		}

		public ReplaceTokensConvenience() : base()
		{
			base.AssemblyName = "NAnt.Core";
			base.ClassName = "NAnt.Core.Filters.ReplaceTokens";
		}



	}
}
