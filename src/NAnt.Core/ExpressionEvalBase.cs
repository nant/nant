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
// Jaroslaw Kowalski (jkowalski@users.sourceforge.net)

using System;
using System.IO;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace NAnt.Core {
    public abstract class ExpressionEvalBase {

        enum EvalMode {
            Evaluate,
            ParseOnly
        }

        private EvalMode _evalMode = EvalMode.Evaluate;

        private ExpressionTokenizer _tokenizer = null;
        public ExpressionEvalBase() {}

        public object Evaluate(ExpressionTokenizer tokenizer) {
            _evalMode = EvalMode.Evaluate;
            _tokenizer = tokenizer;
            return ParseExpression();
        }

        public object Evaluate(string s) {
            _tokenizer = new ExpressionTokenizer();
            _evalMode = EvalMode.Evaluate;

            _tokenizer.InitTokenizer(s);
            return ParseExpression();
        }

        public void CheckSyntax(string s) {
            _tokenizer = new ExpressionTokenizer();
            _evalMode = EvalMode.ParseOnly;

            _tokenizer.InitTokenizer(s);
            ParseExpression();
        }

#region Parser

        bool SyntaxCheckOnly() {
            return _evalMode == EvalMode.ParseOnly;
        }

        private object ParseExpression() {
            return ParseBooleanOr();
        }

        private object ParseBooleanOr() {
            object o = ParseBooleanAnd();
            EvalMode oldEvalMode = _evalMode;
            try {
                while (_tokenizer.IsKeyword("or")) {
                    if (!SyntaxCheckOnly()) {
                        if (((bool)o)) {
                            // we're lazy - don't evaluate anything from now, we know that the result is 'true'
                            _evalMode = EvalMode.ParseOnly;
                        }
                    }

                    if (!SyntaxCheckOnly()) {
                        if (!(o is bool)) {
                            return ReportParseError("Boolean value expected.");
                        }
                    }

                    _tokenizer.GetNextToken();
                    object o2 = ParseBooleanAnd();
                    if (!SyntaxCheckOnly()) {
                        if (!(o2 is bool)) {
                            return ReportParseError("Boolean value expected.");
                        }

                        o = (bool) o || (bool) o2;
                    }
                }
                return o;
            } finally {
                _evalMode = oldEvalMode;
            }
        }

        private object ParseBooleanAnd() {
            object o = ParseRelationalExpression();
            EvalMode oldEvalMode = _evalMode;

            try {
                while (_tokenizer.IsKeyword("and")) {
                    if (!SyntaxCheckOnly()) {
                        if (!((bool)o)) {
                            // we're lazy - don't evaluate anything from now, we know that the result is 'false'
                            _evalMode = EvalMode.ParseOnly;
                        }
                    }

                    if (!SyntaxCheckOnly()) {
                        if (!(o is bool)) {
                            ReportParseError("Boolean value expected.");
                        }
                    }

                    _tokenizer.GetNextToken();
                    object o2 = ParseRelationalExpression();
                    if (!SyntaxCheckOnly()) {
                        if (!(o2 is bool)) {
                            ReportParseError("Boolean value expected.");
                        }

                        o = (bool) o && (bool) o2;
                    }
                }
                return o;
            } finally {
                _evalMode = oldEvalMode;
            }
        }

        private object ParseRelationalExpression() {
            object o = ParseAddSubtract();
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.EQ) {
                _tokenizer.GetNextToken();
                object o2 = ParseAddSubtract();

                if (!SyntaxCheckOnly()) {
                    return o.Equals(o2);
                } else {
                    return null;
                }
            }
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.NE) {
                _tokenizer.GetNextToken();
                object o2 = ParseAddSubtract();
                if (!SyntaxCheckOnly()) {
                    return !o.Equals(o2);
                } else {
                    return null;
                }
            }
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LT) {
                _tokenizer.GetNextToken();
                object o2 = ParseAddSubtract();

                if (!SyntaxCheckOnly()) {
                    return ((IComparable) o).CompareTo(o2) < 0;
                } else {
                    return null;
                }
            }
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.GT) {
                _tokenizer.GetNextToken();
                object o2 = ParseAddSubtract();

                if (!SyntaxCheckOnly()) {
                    return ((IComparable) o).CompareTo(o2) > 0;
                } else {
                    return null;
                }
            }
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LE) {
                _tokenizer.GetNextToken();
                object o2 = ParseAddSubtract();

                if (!SyntaxCheckOnly()) {
                    return ((IComparable) o).CompareTo(o2) <= 0;
                } else {
                    return null;
                }
            }
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.GE) {
                _tokenizer.GetNextToken();
                object o2 = ParseAddSubtract();

                if (!SyntaxCheckOnly()) {
                    return ((IComparable) o).CompareTo(o2) >= 0;
                } else {
                    return null;
                }
            }
            return o;
        }

        private object ParseAddSubtract() {
            object o = ParseMulDiv();

            while (true) {
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Plus) {
                    _tokenizer.GetNextToken();
                    object o2 = ParseMulDiv();

                    if (!SyntaxCheckOnly()) {
                        if (o is string || o2 is string) {
                            // promote to strings and concatenate
                            o = Convert.ToString(o, CultureInfo.InvariantCulture) 
                                + Convert.ToString(o2, CultureInfo.InvariantCulture);
                        } else if (o is double || o2 is double) {
                            o = Convert.ToDouble(o, CultureInfo.InvariantCulture) 
                                + Convert.ToDouble(o2, CultureInfo.InvariantCulture);
                        } else if (o is int || o2 is int) {
                            o = Convert.ToInt32(o, CultureInfo.InvariantCulture) 
                                + Convert.ToInt32(o2, CultureInfo.InvariantCulture);
                        } else {
                            ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Addition not supported for arguments of type '{0}' and '{1}'.", 
                                o.GetType().Name, o2.GetType().Name));
                        }
                    }
                } else if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Minus) {
                    _tokenizer.GetNextToken();
                    object o2 = ParseMulDiv();

                    if (!SyntaxCheckOnly()) {
                        if (o is double || o2 is double) {
                            o = Convert.ToDouble(o, CultureInfo.InvariantCulture) 
                                - Convert.ToDouble(o2, CultureInfo.InvariantCulture);
                        } else if (o is int || o2 is int) {
                            o = Convert.ToInt32(o, CultureInfo.InvariantCulture) 
                                - Convert.ToInt32(o2, CultureInfo.InvariantCulture);
                        } else {
                            ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Subtraction not supported for arguments of type '{0}' and '{1}'.", 
                                o.GetType().Name, o2.GetType().Name));
                        }
                    }
                } else {
                    break;
                }
            }
            return o;
        }

        private object ParseMulDiv() {
            object o = ParseValue();

            while (true) {
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Mul) {
                    _tokenizer.GetNextToken();
                    object o2 = ParseValue();
                    if (!SyntaxCheckOnly()) {
                        if (o is double || o2 is double) {
                            o = Convert.ToDouble(o, CultureInfo.InvariantCulture) 
                                * Convert.ToDouble(o2, CultureInfo.InvariantCulture);
                        } else if (o is int || o2 is int) {
                            o = Convert.ToInt32(o, CultureInfo.InvariantCulture) 
                                * Convert.ToInt32(o2, CultureInfo.InvariantCulture);
                        } else {
                            ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Multiplication not supported for arguments of type '{0}' and '{1}'.", 
                                o.GetType().Name, o2.GetType().Name));
                        }
                    }
                } else if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Div) {
                    _tokenizer.GetNextToken();
                    object o2 = ParseValue();
                    if (!SyntaxCheckOnly()) {
                        if (o is double || o2 is double) {
                            o = Convert.ToDouble(o, CultureInfo.InvariantCulture) 
                                / Convert.ToDouble(o2, CultureInfo.InvariantCulture);
                        } else if (o is int || o2 is int) {
                            o = Convert.ToInt32(o, CultureInfo.InvariantCulture) 
                                / Convert.ToInt32(o2, CultureInfo.InvariantCulture);
                        } else {
                            ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Division not supported for arguments of type '{0}' and '{1}'.", 
                                o.GetType().Name, o2.GetType().Name));
                        }
                    }
                } else if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Mod) {
                    _tokenizer.GetNextToken();
                    object o2 = ParseValue();
                    if (!SyntaxCheckOnly()) {
                        if (o is int || o2 is int) {
                            o = Convert.ToInt32(o, CultureInfo.InvariantCulture) 
                                % Convert.ToInt32(o2, CultureInfo.InvariantCulture);
                        } else {
                            ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Modulo not supported for arguments of type '{0}' and '{1}'", 
                                o.GetType().Name, o2.GetType().Name));
                        }
                    }
                } else {
                    break;
                }
            }
            return o;
        }

        private object ParseConditional() {
            if (_tokenizer.TokenText != "if") {
                ReportParseError("'if' expected.");
            }
            _tokenizer.GetNextToken();
            if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.LeftParen) {
                ReportParseError("'(' expected.");
            }
            _tokenizer.GetNextToken();

            object val = ParseExpression();
            bool cond = false;
            if (!SyntaxCheckOnly()) {
                if (!(val is bool)) {
                    ReportParseError("Boolean value expected in conditional.");
                }
                cond = (bool)val;
            }

            // skip comma between condition value and then
            if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Comma) {
                ReportParseError("',' expected.");
            }
            _tokenizer.GetNextToken();

            EvalMode oldEvalMode = _evalMode;

            try {
                if (!cond) {
                    // evaluate 'then' clause without executing functions
                    _evalMode = EvalMode.ParseOnly;
                } else {
                    _evalMode = oldEvalMode;
                }
                object thenValue = ParseExpression();
                _evalMode = oldEvalMode;

                if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Comma) {
                    ReportParseError("',' expected.");
                }
                _tokenizer.GetNextToken(); // skip comma

                if (cond) {
                    // evaluate 'else' clause without executing functions
                    _evalMode = EvalMode.ParseOnly;
                } else {
                    _evalMode = oldEvalMode;
                }
                object elseValue = ParseExpression();

                _evalMode = oldEvalMode;

                // skip closing ')'
                if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen) {
                    ReportParseError("')' expected.");
                }
                _tokenizer.GetNextToken();

                return cond ? thenValue : elseValue;
            }
            finally {
                // restore evaluation mode - even on exceptions
                _evalMode = oldEvalMode;
            }
        }

        private object ParseValue() {
            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.String) {
                object v = _tokenizer.TokenText;
                _tokenizer.GetNextToken();
                return v;
            }

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Number) {
                string number = _tokenizer.TokenText;
                _tokenizer.GetNextToken();
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Dot) {
                    number += ".";
                    _tokenizer.GetNextToken();
                    if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Number) {
                        ReportParseError("Fractional part expected.");
                    }
                    number += _tokenizer.TokenText;
                    _tokenizer.GetNextToken();
                    return Double.Parse(number, CultureInfo.InvariantCulture);
                } else {
                    return Int32.Parse(number, CultureInfo.InvariantCulture);
                }
            }

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Minus) {
                _tokenizer.GetNextToken();

                // unary minus
                object v = ParseValue();
                if (!SyntaxCheckOnly()) {
                    if (v is int) {
                        return -((int) v);
                    }
                    if (v is double) {
                        return -((double) v);
                    }
                    ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                        "Unary minus not supported for arguments of type '{0}'.", 
                        v.GetType().Name));
                }
                return null;
            }

            if (_tokenizer.IsKeyword("not")) {
                _tokenizer.GetNextToken();

                // unary boolean not
                object v = ParseValue();
                if (!SyntaxCheckOnly()) {
                    bool value = Convert.ToBoolean(v, CultureInfo.InvariantCulture);
                    return !value;
                }
                return null;
            }

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LeftParen) {
                _tokenizer.GetNextToken();
                object v = ParseExpression();
                if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen) {
                    ReportParseError("')' expected.");
                }
                _tokenizer.GetNextToken();
                return v;
            }

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Keyword) {
                string functionOrPropertyName = _tokenizer.TokenText;
                if (functionOrPropertyName == "if") {
                    return ParseConditional();
                }

                if (functionOrPropertyName == "true") {
                    _tokenizer.GetNextToken();
                    return true;
                }

                if (functionOrPropertyName == "false") {
                    _tokenizer.GetNextToken();
                    return false;
                }

                // don't ignore whitespace - properties shouldn't be written with spaces in them

                _tokenizer.IgnoreWhitespace = false; 
                _tokenizer.GetNextToken();

                ArrayList args = new ArrayList();
                bool isFunction = false;

                // gather function or property name
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.DoubleColon) {
                    isFunction = true;
                    functionOrPropertyName += "::";
                    _tokenizer.GetNextToken();
                    if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Keyword) {
                        throw new ExpressionParseException("Function name expected.");
                    }
                    functionOrPropertyName += _tokenizer.TokenText;
                    _tokenizer.GetNextToken();
                } else {
                    while (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Dot
                           || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Minus
                           || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Keyword
                           || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Number) {
                        functionOrPropertyName += _tokenizer.TokenText;
                        _tokenizer.GetNextToken();
                    }
                }
                _tokenizer.IgnoreWhitespace = true;

                // if we've stopped on a whitespace - advance to the next token
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Whitespace) {
                    _tokenizer.GetNextToken();
                }

                if (isFunction) {
                    if ( _tokenizer.CurrentToken != ExpressionTokenizer.TokenType.LeftParen) {
                        throw new ExpressionParseException("'(' expected.");
                    }

                    _tokenizer.GetNextToken();

                    while (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen &&
                            _tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF) {
                        object e = ParseExpression();
                        args.Add(e);
                        if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.RightParen) {
                            break;
                        }
                        if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Comma) {
                            ReportParseError("',' expected.");
                        }
                        _tokenizer.GetNextToken();
                    }

                    if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen) {
                        ReportParseError("')' expected.");
                    }
                    _tokenizer.GetNextToken();
                }

                if (!SyntaxCheckOnly()) {
                    if (isFunction) {
                        return EvaluateFunction(functionOrPropertyName, args);
                    } else {
                        return EvaluateProperty(functionOrPropertyName);
                    }
                } else {
                    if (isFunction) {
                        ValidateFunction(functionOrPropertyName, args.Count);
                        return null;
                    } else {
                        // we cannot validate properties because of of the short-circuit evaluation
                        //ValidateProperty(functionOrPropertyName);
                        return null;
                    }
                }
            }

            UnexpectedToken();
            return null;
        }

#endregion

#region Overridables

        protected abstract object EvaluateFunction(string functionName, ArrayList args);
        protected abstract void ValidateFunction(string functionName, int argCount);
        protected abstract object EvaluateProperty(string propertyName);
        protected abstract void ValidateProperty(string propertyName);

        protected virtual void UnexpectedToken() {
            ReportParseError(string.Format(CultureInfo.InvariantCulture, 
                "Unexpected token '{0}'.", _tokenizer.CurrentToken));
        }

        protected virtual object ReportParseError(string desc) {
            throw new ExpressionParseException(desc, _tokenizer);
        }
#endregion
    }
}
