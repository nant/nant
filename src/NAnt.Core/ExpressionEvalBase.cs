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
            object val = ParseExpression();
            if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF) {
                throw BuildParseError("Unexpected token at the end of expression", _tokenizer.CurrentPosition);
            }
            return val;
        }

        public void CheckSyntax(string s) {
            _tokenizer = new ExpressionTokenizer();
            _evalMode = EvalMode.ParseOnly;

            _tokenizer.InitTokenizer(s);
            ParseExpression();
            if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF) {
                throw BuildParseError("Unexpected token at the end of expression", _tokenizer.CurrentPosition);
            }
        }

#region Parser

        bool SyntaxCheckOnly() {
            return _evalMode == EvalMode.ParseOnly;
        }

        private object ParseExpression() {
            return ParseBooleanOr();
        }

        private object ParseBooleanOr() {
            ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
            object o = ParseBooleanAnd();
            ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;
            EvalMode oldEvalMode = _evalMode;
            try {
                while (_tokenizer.IsKeyword("or")) {
                    bool v1 = true;

                    if (!SyntaxCheckOnly()) {
                        v1 = (bool)SafeConvert(typeof(bool), o, "the left hand side of the 'or' operator", p0, _tokenizer.CurrentPosition);

                        if (v1) {
                            // we're lazy - don't evaluate anything from now, we know that the result is 'true'
                            _evalMode = EvalMode.ParseOnly;
                        }
                    }

                    _tokenizer.GetNextToken();
                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseBooleanAnd();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;

                    if (!SyntaxCheckOnly()) {
                        bool v2 = (bool)SafeConvert(typeof(bool), o2, "the right hand side of the 'or' operator", p2, p3);
                        o = v1 || v2;
                    }
                }
                return o;
            } finally {
                _evalMode = oldEvalMode;
            }
        }

        private object ParseBooleanAnd() {
            ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
            object o = ParseRelationalExpression();
            ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;
            EvalMode oldEvalMode = _evalMode;

            try {
                while (_tokenizer.IsKeyword("and")) {
                    bool v1 = true;

                    if (!SyntaxCheckOnly()) {
                        v1 = (bool)SafeConvert(typeof(bool), o, "the left hand side of the 'and' operator", p0, _tokenizer.CurrentPosition);

                        if (!v1) {
                            // we're lazy - don't evaluate anything from now, we know that the result is 'true'
                            _evalMode = EvalMode.ParseOnly;
                        }
                    }

                    _tokenizer.GetNextToken();
                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseRelationalExpression();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;
                    if (!SyntaxCheckOnly()) {
                        bool v2 = (bool)SafeConvert(typeof(bool), o2, "the right hand side of the 'and' operator", p2, p3);

                        o = v1 && v2;
                    }
                }
                return o;
            } finally {
                _evalMode = oldEvalMode;
            }
        }

        private object ParseRelationalExpression() {
            ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
            object o = ParseAddSubtract();

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.EQ
             || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.NE
             || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LT
             || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.GT
             || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LE
             || _tokenizer.CurrentToken == ExpressionTokenizer.TokenType.GE) {
                
                ExpressionTokenizer.TokenType op = _tokenizer.CurrentToken;
                _tokenizer.GetNextToken();
                
                ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;
                object o2 = ParseAddSubtract();
                ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;

                if (SyntaxCheckOnly()) {
                    return null;
                }

                switch (op) {
                    case ExpressionTokenizer.TokenType.EQ:
                        return o.Equals(o2);
                        
                    case ExpressionTokenizer.TokenType.NE:
                        return !o.Equals(o2);
                        
                    case ExpressionTokenizer.TokenType.LT:
                        return ((IComparable) o).CompareTo(o2) < 0;
                        
                    case ExpressionTokenizer.TokenType.GT:
                        return ((IComparable) o).CompareTo(o2) > 0;
                        
                    case ExpressionTokenizer.TokenType.LE:
                        return ((IComparable) o).CompareTo(o2) <= 0;
                        
                    case ExpressionTokenizer.TokenType.GE:
                        return ((IComparable) o).CompareTo(o2) >= 0;
                }
            }
            return o;
        }

        private object ParseAddSubtract() {
            ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
            object o = ParseMulDiv();
            ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;

            while (true) {
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Plus) {
                    _tokenizer.GetNextToken();
                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseMulDiv();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;

                    if (!SyntaxCheckOnly()) {
                        if (o is string || o2 is string) {
                            // promote to strings and concatenate
                            //
                            string s1 = (string) SafeConvert(typeof(string), o, 
                                "the left hand side of the concatenation operator", p0, p1);
                            string s2 = (string) SafeConvert(typeof(string), o2, 
                                "the right hand side of the concatenation operator", p2, p3);
                            o = s1 + s2;
                        } else if (o is double || o2 is double) {
                            double d1 = (double) SafeConvert(typeof(double), o, 
                                "the left hand side of the addition operator", p0, p1);
                            double d2 = (double) SafeConvert(typeof(double), o2, 
                                "the right hand side of the addition operator", p2, p3);
                            o = d1 + d2;
                        } else if (o is int || o2 is int) {
                            int i1 = (int) SafeConvert(typeof(int), o, 
                                "the left hand side of the addition operator", p0, p1);
                            int i2 = (int) SafeConvert(typeof(int), o2, 
                                "the right hand side of the addition operator", p2, p3);
                            o = i1 + i2;
                        } else if (o is DateTime && o2 is TimeSpan) {
                            DateTime i1 = (DateTime) o; 
                            TimeSpan i2 = (TimeSpan) o2;
                            o = i1 + i2;
                        } else {
                            throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Addition not supported for arguments of type '{0}' and '{1}'.", 
                                GetSimpleTypeName(o.GetType()), GetSimpleTypeName(o2.GetType())), p0, p3);
                        }
                    }
                } else if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Minus) {
                    _tokenizer.GetNextToken();

                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseMulDiv();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;

                    if (!SyntaxCheckOnly()) {
                        if (o is double || o2 is double) {
                            double d1 = (double) SafeConvert(typeof(double), o, 
                                "the left hand side of the subtraction operator", p0, p1);
                            double d2 = (double) SafeConvert(typeof(double), o2, 
                                "the right hand side of the subtraction operator", p2, p3);
                            o = d1 - d2;
                        } else if (o is int || o2 is int) {
                            int i1 = (int) SafeConvert(typeof(int), o, 
                                "the left hand side of the subtraction operator", p0, p1);
                            int i2 = (int) SafeConvert(typeof(int), o2, 
                                "the right hand side of the subtraction operator", p2, p3);
                            o = i1 - i2;
                        } else if (o is DateTime && (o2 is TimeSpan || o2 is DateTime)) {
                            DateTime date1 = (DateTime) o;

                            if (o2 is TimeSpan) {
                                // result is DateTime
                                o = ((DateTime) o) - ((TimeSpan) o2);
                            } else if (o2 is DateTime) {
                                // result is TimeSpan
                                o = ((DateTime) o) - ((DateTime) o2);
                            }
                        } else {
                            throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                                "Subtraction not supported for arguments of type '{0}' and '{1}'.", 
                                GetSimpleTypeName(o.GetType()), GetSimpleTypeName(o2.GetType())), p0, p3);
                        }
                    }
                } else {
                    break;
                }
            }
            return o;
        }

        private object ParseMulDiv() {
            ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
            object o = ParseValue();
            ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;

            while (true) {
                if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Mul) {
                    _tokenizer.GetNextToken();
                    
                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseValue();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;

                    if (!SyntaxCheckOnly()) {
                        if (o is double || o2 is double) {
                            double d1 = (double)SafeConvert(typeof(double), o, "the left hand side of the mutliplication operator", p0, p1);
                            double d2 = (double)SafeConvert(typeof(double), o2, "the right hand side of the mutliplication operator", p2, p3);
                            o = d1 * d2;
                        } else if (o is int || o2 is int) {
                            int i1 = (int)SafeConvert(typeof(int), o, "the left hand side of the mutliplication operator", p0, p1);
                            int i2 = (int)SafeConvert(typeof(int), o2, "the right hand side of the mutliplication operator", p2, p3);
                            o = i1 * i2;
                        } else {
                            throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                                        "Multiplication not supported for arguments of type '{0}' and '{1}'.", 
                                        GetSimpleTypeName(o.GetType()), GetSimpleTypeName(o2.GetType())), p0, p3);
                        }
                    }
                } else if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Div) {
                    _tokenizer.GetNextToken();
                    
                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseValue();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;

                    if (!SyntaxCheckOnly()) {
                        if (o is double || o2 is double) {
                            double d1 = (double)SafeConvert(typeof(double), o, "the left hand side of the division operator", p0, p1);
                            double d2 = (double)SafeConvert(typeof(double), o2, "the right hand side of the division operator", p2, p3);
                            o = d1 / d2;
                        } else if (o is int || o2 is int) {
                            int i1 = (int)SafeConvert(typeof(int), o, "the left hand side of the division operator", p0, p1);
                            int i2 = (int)SafeConvert(typeof(int), o2, "the right hand side of the division operator", p2, p3);
                            if (i2 == 0) {
                                throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                                            "Attempt to divide by zero."), p2, p3);
                            }
                                
                            o = i1 / i2;
                        } else {
                            throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                                        "Division not supported for arguments of type '{0}' and '{1}'.", 
                                        GetSimpleTypeName(o.GetType()), GetSimpleTypeName(o2.GetType())), p0, p3);
                        }
                    }
                } else if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Mod) {
                    _tokenizer.GetNextToken();

                    ExpressionTokenizer.Position p2 = _tokenizer.CurrentPosition;
                    object o2 = ParseValue();
                    ExpressionTokenizer.Position p3 = _tokenizer.CurrentPosition;

                    if (!SyntaxCheckOnly()) {
                        int i1 = (int)SafeConvert(typeof(int), o, "the left hand side of the modulus operator", p0, p1);
                        int i2 = (int)SafeConvert(typeof(int), o2, "the right hand side of the modulus operator", p2, p3);
                        if (i2 == 0) {
                            throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                                        "Attempt to divide by zero."), p2, p3);
                        }
                        o = i1 % i2;
                    }
                } else {
                    break;
                }
            }
            return o;
        }

        private object ParseConditional() {
            // we're on "if" token - skip it 
            _tokenizer.GetNextToken();
            if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.LeftParen) {
                throw BuildParseError("'(' expected.", _tokenizer.CurrentPosition);
            }
            _tokenizer.GetNextToken();

            
            ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
            object val = ParseExpression();
            ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;
            
            bool cond = false;
            if (!SyntaxCheckOnly()) {
                cond = (bool) SafeConvert(typeof(bool), val, "the conditional expression", p0, p1);
            }

            // skip comma between condition value and then
            if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Comma) {
                throw BuildParseError("',' expected.", _tokenizer.CurrentPosition);
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
                    throw BuildParseError("',' expected.", _tokenizer.CurrentPosition);
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
                    throw BuildParseError("')' expected.", _tokenizer.CurrentPosition);
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
                        throw BuildParseError("Fractional part expected.", _tokenizer.CurrentPosition);
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
                ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
                object v = ParseValue();
                ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;
                if (!SyntaxCheckOnly()) {
                    if (v is int) {
                        return -((int) v);
                    }
                    if (v is double) {
                        return -((double) v);
                    }
                    throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                        "Unary minus not supported for arguments of type '{0}'.", 
                        GetSimpleTypeName(v.GetType())), p0, p1);
                }
                return null;
            }

            if (_tokenizer.IsKeyword("not")) {
                _tokenizer.GetNextToken();

                // unary boolean not
                ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;
                object v = ParseValue();
                ExpressionTokenizer.Position p1 = _tokenizer.CurrentPosition;
                if (!SyntaxCheckOnly()) {
                    bool value = (bool)SafeConvert(typeof(bool), v, "the argument of 'not' operator", p0, p1);
                    return !value;
                }
                return null;
            }

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.LeftParen) {
                _tokenizer.GetNextToken();
                object v = ParseExpression();
                if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen) {
                    throw BuildParseError("')' expected.", _tokenizer.CurrentPosition);
                }
                _tokenizer.GetNextToken();
                return v;
            }

            if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.Keyword) {
                ExpressionTokenizer.Position p0 = _tokenizer.CurrentPosition;

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
                        throw BuildParseError("Function name expected.", p0, _tokenizer.CurrentPosition);
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
                        throw BuildParseError("'(' expected.", _tokenizer.CurrentPosition);
                    }

                    _tokenizer.GetNextToken();

                    int currentArgument = 0;
                    ParameterInfo[] formalParameters = null;

                    try {
                        formalParameters = GetFunctionParameters(functionOrPropertyName);
                    } catch (Exception e) {
                        throw BuildParseError(e.Message, p0, _tokenizer.CurrentPosition);
                    }

                    while (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen &&
                            _tokenizer.CurrentToken != ExpressionTokenizer.TokenType.EOF) {
                        if (currentArgument >= formalParameters.Length) {
                            throw BuildParseError(string.Format(CultureInfo.InvariantCulture,
                                        "Too many actual parameters for '{0}'.", functionOrPropertyName), p0, _tokenizer.CurrentPosition);
                        }

                        ExpressionTokenizer.Position beforeArgument = _tokenizer.CurrentPosition;
                        object e = ParseExpression();
                        ExpressionTokenizer.Position afterArgument = _tokenizer.CurrentPosition;

                        if (!SyntaxCheckOnly()) {
                            object convertedValue = SafeConvert(formalParameters[currentArgument].ParameterType,
                                    e,
                                    string.Format(CultureInfo.InvariantCulture, "argument {1} ({0}) of {2}()", formalParameters[currentArgument].Name, currentArgument + 1, functionOrPropertyName),
                                    beforeArgument, afterArgument);
                            args.Add(convertedValue);
                        }
                        currentArgument++;
                        if (_tokenizer.CurrentToken == ExpressionTokenizer.TokenType.RightParen) {
                            break;
                        }
                        if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.Comma) {
                            throw BuildParseError("',' expected.", _tokenizer.CurrentPosition);
                        }
                        _tokenizer.GetNextToken();
                    }
                    if (currentArgument < formalParameters.Length) {
                        throw BuildParseError(string.Format(CultureInfo.InvariantCulture,
                                    "Too few actual parameters for '{0}'.", functionOrPropertyName), p0, _tokenizer.CurrentPosition);
                    }

                    if (_tokenizer.CurrentToken != ExpressionTokenizer.TokenType.RightParen) {
                        throw BuildParseError("')' expected.", _tokenizer.CurrentPosition);
                    }
                    _tokenizer.GetNextToken();
                }

                try {
                    if (!SyntaxCheckOnly()) {
                        if (isFunction) {
                            return EvaluateFunction(functionOrPropertyName, args.ToArray());
                        } else {
                            return EvaluateProperty(functionOrPropertyName);
                        }
                    } else {
                        return null; // this is needed because of short-circuit evaluation
                    }
                } catch (Exception e) {
                    if (isFunction) {
                        throw BuildParseError("Function call failed.", p0, _tokenizer.CurrentPosition, e);
                    } else {
                        throw BuildParseError("Property evaluation failed.", p0, _tokenizer.CurrentPosition, e);
                    }
                }
            }

            return UnexpectedToken();
        }

        protected ExpressionParseException BuildParseError(string desc, ExpressionTokenizer.Position p0) {
            return new ExpressionParseException(desc, p0.CharIndex);
        }
        
        protected ExpressionParseException BuildParseError(string desc, ExpressionTokenizer.Position p0, ExpressionTokenizer.Position p1) {
            return new ExpressionParseException(desc, p0.CharIndex, p1.CharIndex);
        }
        
        protected ExpressionParseException BuildParseError(string desc, ExpressionTokenizer.Position p0, ExpressionTokenizer.Position p1, Exception ex) {
            return new ExpressionParseException(desc, p0.CharIndex, p1.CharIndex, ex);
        }

        protected object SafeConvert(Type returnType, object source, string description, ExpressionTokenizer.Position p0, ExpressionTokenizer.Position p1) {
            try {
                //
                // TODO - Convert.ChangeType() is very liberal. It allows you to convert "true" to Double (1.0).
                // We shouldn't allow this. Add more cases like this here.
                //
                bool disallow = false;

                if (source == null) {
                    if (returnType == typeof(string)) {
                        return string.Empty;
                    }

                    throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                        "Cannot convert {0} to '{1}' (value was null).", 
                        description, GetSimpleTypeName(returnType)), p0, p1);
                }

                if (source.GetType() == typeof(bool)) {
                    if (returnType != typeof(string) && returnType != typeof(bool)) {
                        // boolean can only be converted to string or boolean
                        disallow = true;
                    }
                }

                if (returnType == typeof(bool)) {
                    if (!(source is string || source is bool)) {
                        // only string and boolean can be converted to boolean
                        disallow = true;
                    }
                }

                if (source.GetType() == typeof(DateTime)) {
                    if (returnType != typeof(string) && returnType != typeof(DateTime)) {
                        // DateTime can only be converted to string or DateTime
                        disallow = true;
                    }
                }

                if (returnType == typeof(DateTime)) {
                    if (!(source is DateTime || source is string)) {
                        // only string and DateTime can be converted to DateTime
                        disallow = true;
                    }
                }

                // Horrible hack to work around this mono bug:
                // http://bugs.ximian.com/show_bug.cgi?id=53919
                // Be sure to remove once that bug is fixed.
                if (returnType == typeof(TimeSpan) && source.GetType() == typeof(TimeSpan)) {
                    return (TimeSpan) source;
                }

                if (returnType == typeof(string)) {
                    if (source is DirectoryInfo) {
                        return ((DirectoryInfo) source).FullName;
                    } else if (source is FileInfo) {
                        return ((FileInfo) source).FullName;
                    }
                }

                if (disallow) {
                    throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                        "Cannot convert {0} to '{1}' (actual type was '{2}').", 
                        description, GetSimpleTypeName(returnType), 
                        GetSimpleTypeName(source.GetType())), p0, p1);
                }
                
                return Convert.ChangeType(source, returnType, CultureInfo.InvariantCulture);
            } catch (ExpressionParseException) {
                throw;
            } catch (Exception ex) {
                throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                    "Cannot convert {0} to '{1}' (actual type was '{2}').", 
                    description, GetSimpleTypeName(returnType), 
                    GetSimpleTypeName(source.GetType())), p0, p1, ex);
            }
        }

        protected string GetSimpleTypeName(Type t) {
            if (t == typeof(int)) {
                return "integer";
            } else if (t == typeof(double)) {
                return "double";
            } else if (t == typeof(string)) {                    
                return "string";
            } else if (t == typeof(bool)) {
                return "boolean";
            } else if (t == typeof(DateTime)) {
                return "datetime";
            } else if (t == typeof(TimeSpan)) {
                return "timespan";
            } else if (t == typeof(DirectoryInfo)) {
                return "directory";
            } else if (t == typeof(FileInfo)) {
                return "file";
            } else {
                return t.FullName;
            }
        }
        
#endregion

#region Overridables

        protected abstract object EvaluateFunction(string functionName, object[] args);
        protected abstract ParameterInfo[] GetFunctionParameters(string functionName);
        protected abstract object EvaluateProperty(string propertyName);

        protected virtual object UnexpectedToken() {
            throw BuildParseError(string.Format(CultureInfo.InvariantCulture, 
                "Unexpected token '{0}'.", _tokenizer.CurrentToken), _tokenizer.CurrentPosition);
        }
#endregion
    }
}
