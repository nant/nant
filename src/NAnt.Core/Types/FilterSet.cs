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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Collections;
using System.Text;

using NAnt.Core.Attributes;

namespace NAnt.Core.Types {
    /// <summary>
    /// A set of filters to be applied to something.
    /// </summary>
    /// <remarks>
    /// A filter set may have begintoken and endtokens defined.
    /// </remarks>
    [ElementName("filterset")]
    public class FilterSet : DataTypeBase {
        #region Private Instance Fields

        private string _startOfToken = FilterSet.DefaultTokenStart;
        private string _endOfToken = FilterSet.DefaultTokenEnd;
        private FilterCollection _filters = new FilterCollection();

        #endregion Private Instance Fields

        #region Private Static Fields

        /// <summary>
        /// The default token start string.
        /// </summary>
        private const string DefaultTokenStart = "@";
    
        /// <summary>
        /// The default token end string.
        /// </summary>
        public const string DefaultTokenEnd = "@";

        #endregion Private Static Fields
   
        #region Public Instance Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterSet" /> class.
        /// </summary>
        public FilterSet() {
        }

        #endregion Public Instance Constructors
    
        #region Public Instance Properties

        /// <summary>
        /// The string used to identity the beginning of a token. The default is
        /// <c>@</c>.
        /// </summary>
        [TaskAttribute("begintoken", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string BeginToken {
            get { return _startOfToken; }
            set { _startOfToken = value; }
        }

        /// <summary>
        /// The string used to identify the end of a token. The default is
        /// <c>@</c>.
        /// </summary>
        [TaskAttribute("endtoken", Required=false)]
        [StringValidator(AllowEmpty=false)]
        public string EndToken {
            get { return _endOfToken; }
            set { _endOfToken = value; }
        }

        /// <summary>
        /// The filters to apply.
        /// </summary>
        [BuildElementArray("filter")]
        public FilterCollection Filters {
            get { return _filters; }
        }

        #endregion Public Instance Properties

        #region Public Instance Methods

        /// <summary>
        /// Does replacement on the given string with token matching.
        /// </summary>
        /// <param name="line">The line to process the tokens in.</param>
        /// <returns>
        /// The line with the tokens replaced.
        /// </returns>
        public string ReplaceTokens(string line) {
            int index = line.IndexOf(BeginToken);
            
            if (index > -1) {
                try {
                    StringBuilder b = new StringBuilder();
                    int i = 0;
                    string token = null;
                    string value = null;
                    
                    do {
                        int endIndex = line.IndexOf(EndToken, 
                            index + BeginToken.Length + 1);
                        if (endIndex == -1) {
                            break;
                        }

                        token = line.Substring(index + BeginToken.Length, endIndex);
                        b.Append(line.Substring(i, index));

                        if (Filters.Contains(token)) {
                            value = Filters[token].Value;
                            Log(Level.Verbose, "Replacing {0}{1}{2} -> {3}.", 
                                BeginToken, token, EndToken, value); 
                            b.Append(value);
                            i = index + BeginToken.Length + token.Length
                                + EndToken.Length;
                        } else {
                            // just append beginToken and search further
                            b.Append(BeginToken);
                            i = index + BeginToken.Length;
                        }
                    } while ((index = line.IndexOf(BeginToken, i)) > -1);
                    
                    b.Append(line.Substring(i));
                    return b.ToString();
                } catch (ArgumentOutOfRangeException) {
                    return line;
                }
            } else {
                return line;
            }
        }

        #endregion Public Instance Methods
    }
}
