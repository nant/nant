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

// Eric Gunnerson
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using NUnit.Framework;
using SourceForge.NAnt;

namespace SourceForge.NAnt.Tests {

    /// <summary>General purpose test that checks to see that all exceptions implement required methods.</summary>
    /// <remarks>
    /// This class was inspired / stolen from the article "The Well-Tempered Exception",
    /// by Eric Gunnerson, Microsoft.
    ///
    /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp08162001.asp
    ///
    /// The test verifies:
    /// 1) The exception name ends in "Exception"...
    /// 2) The exception implements the 3 standard constructors:
    ///		class();
    ///		class(string message);
    ///		class(string message, Exception inner);
    /// 3) The exception implements the deserialization constructor:
    ///		class(SerializationInfo info, StreamingContext context);
    /// 4) The exception has no public fields
    /// 5) If the exception has private fields, that it implements GetObjectData()
    ///	  (there's no guarantee it does it *correctly*)
    /// 6) If the exception has private fields, it overrides the Message property.
    /// 7) The exception is marked as serializable.
    /// </remarks>
    public class ExceptionTest : TestCase {

        public ExceptionTest(String name) : base(name) {
        }

        public void Test_AllExceptions() {
            // For each assembly we want to check instantiate an object from that assembly
            // and use the type to get the assembly.

            // NAnt.Core.dll
            ProcessAssembly(Assembly.GetAssembly(typeof(Project)));

            // Check the test exceptions to make sure test is valid - see bottom of this file.
            ProcessAssembly(Assembly.GetAssembly(this.GetType()));
        }
		
        // --------------------------------------------------------------------
        // Exception testing code follows

        bool IsException(Type type) {
            Type baseType = null;
            while ((baseType = type.BaseType) != null) {
                if (baseType == typeof(System.Exception)) {
                    return true;
                }
                type = baseType;
            }
            return false;
        }

        void CheckConstructor(Type t, string description, params Type[] parameters) {
            ConstructorInfo ci = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
            AssertNotNull(t.Name + description + " is a required constructor.", ci);
            Assert(t.Name + description + " is private, must be public or protected.", !ci.IsPrivate);
            Assert(t.Name + description + " is internal, must be public or protected.", !ci.IsFamily);
        }

        void CheckException(Assembly assembly, Type t) {
            // check to see that the exception is correctly named, with "Exception" at the end
            bool validName = t.Name.EndsWith("Exception");
            Assert(t.Name + " class name must end with Exception.", t.Name.EndsWith("Exception"));

            // Does the exception have the 3 standard constructors?

            // Default constructor
            CheckConstructor(t, "()");

            // Constructor with a single string parameter
            CheckConstructor(t, "(string message)", typeof(System.String));

            // Constructor with a string and an inner exception
            CheckConstructor(t, "(string message, Exception inner)", 
                    typeof(System.String), typeof(System.Exception));

            // check to see if the serialization constructor is present...
            CheckConstructor(t, "(SerializationInfo info, StreamingContext context)",
                    typeof(System.Runtime.Serialization.SerializationInfo),
                    typeof(System.Runtime.Serialization.StreamingContext));

            // check to see if the type is market as serializable
            Assert(t.Name + " is not serializable, missing [Serializable]?", t.IsSerializable);

            // check to see if there are any public fields. These should be properties instead...
            FieldInfo[] publicFields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            if (publicFields.Length != 0) {
                foreach (FieldInfo fieldInfo in publicFields) {
                    Fail(t.Name + "." + fieldInfo.Name + " is a public field, should be exposed through property instead.");
                }
            }

            // If this exception has any fields, check to make sure it has a 
            // version of GetObjectData. If not, it does't serialize those fields.
            FieldInfo[] fields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length != 0) {
                if (t.GetMethod("GetObjectData", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) == null) {
                    Fail(t.Name + " does not implement GetObjectData but has private fields.");
                }

                // Make sure Message is overridden if there are private fields.
                Assert(t.Name + " does not override the Message property.", t.GetProperty("Message", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) != null);
            }
        }

        public void ProcessAssembly(Assembly a) {
            foreach (Type t in a.GetTypes()) {
                if (IsException(t)) {
                    CheckException(a, t);
                }
            }
        }
    }

    // --------------------------------------------------------------------
    // Test exceptions used to test the test.

    /// <summary>Do nothing exception to verify that the exception tester is working correctly.</summary>
    [Serializable]
    public class SimpleTestException : ApplicationException
    {
        // Normal 3 constructors
        public SimpleTestException() {
        }

        public SimpleTestException(string message) : base(message) {
        }

        public SimpleTestException(string message, Exception inner) : base(message, inner) {
        }

        // deserialization constructor
        public SimpleTestException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }

    /// <summary>Do nothing exception to verify that the exception tester is working correctly.</summary>
    [Serializable]
    public class TestException : ApplicationException, ISerializable {
        int _value;

        // Normal 3 constructors
        public TestException() {
        }

        public TestException(string message) : base(message) {
        }

        public TestException(string message, Exception inner) : base(message, inner) {
        }

        // constructors that take the added value
        public TestException(string message, int value) : base(message) {
            _value = value;
        }

        // deserialization constructor
        public TestException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _value = info.GetInt32("Value");
        }

        public int Value {
            get { return _value; }
        }

        // Called by the frameworks during serialization
        // to fetch the data from an object.
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("Value", _value);		
        }

        // overridden Message property. This will give the
        // proper textual representation of the exception,
        // with the added field value.
        public override string Message {
            get {
                // NOTE: should be localized...
                string s = String.Format("Value: {0}", _value);
                return base.Message + Environment.NewLine + s;
            }
        }
    }
}
