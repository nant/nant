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
using System.Reflection;
using System.Runtime.Serialization;
using System.Globalization;

using NUnit.Framework;

using NAnt.Core;

namespace Tests.NAnt.Core {
    /// <summary>
    /// General purpose test that checks to see that all exceptions implement 
    /// required methods.
    /// </summary>
    /// <remarks>
    /// This class was inspired / stolen from the article "The Well-Tempered Exception",
    /// by Eric Gunnerson, Microsoft.
    ///
    /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp08162001.asp
    ///
    /// The test verifies:
    /// 1) The exception name ends in "Exception"...
    /// 2) The exception implements the 3 standard constructors:
    ///    class();
    ///    class(string message);
    ///    class(string message, Exception inner);
    /// 3) The exception implements the deserialization constructor:
    ///    class(SerializationInfo info, StreamingContext context);
    /// 4) The exception has no public fields
    /// 5) If the exception has private fields, that it implements GetObjectData()
    ///   (there's no guarantee it does it *correctly*)
    /// 6) If the exception has private fields, it overrides the Message property.
    /// 7) The exception is marked as serializable.
    /// </remarks>
    [TestFixture]
    public class ExceptionTest {
        #region Public Instance Methods

        [Test]
        public void Test_AllExceptions() {
            // For each assembly we want to check instantiate an object from 
            // that assembly and use the type to get the assembly.

            // NAnt.Core.dll
            ProcessAssembly(Assembly.GetAssembly(typeof(Project)));

            // Check the test exceptions to make sure test is valid - see bottom 
            // of this file.
            ProcessAssembly(Assembly.GetAssembly(this.GetType()));
        }

        public void ProcessAssembly(Assembly a) {
            foreach (Type t in a.GetTypes()) {
                if (IsException(t)) {
                    CheckException(a, t);
                }
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private bool IsException(Type type) {
            Type baseType = null;
            while ((baseType = type.BaseType) != null) {
                if (baseType == typeof(System.Exception)) {
                    return true;
                }
                type = baseType;
            }
            return false;
        }

        private void CheckPublicConstructor(Type t, string description, params Type[] parameters) {
            // locate constructor
            ConstructorInfo ci = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
            // fail if constructor does not exist
            Assert.IsNotNull(ci, t.Name + description + " is a required constructor.");
            // fail if constructor is private
            Assert.IsFalse(ci.IsPrivate, t.Name + description + " is private, must be public.");
            // fail if constructor is protected
            Assert.IsFalse(ci.IsFamily, t.Name + description + " is internal, must be public.");
            // fail if constructor is internal
            Assert.IsFalse(ci.IsAssembly, t.Name + description + " is internal, must be public.");
            // fail if constructor is protected internal
            Assert.IsFalse(ci.IsFamilyOrAssembly, t.Name + description + " is protected internal, must be public.");
            // sanity check to make sure the constructor is public
            Assert.IsTrue(ci.IsPublic, t.Name + description + " is not public, must be public.");
        }

        private void CheckProtectedConstructor(Type t, string description, params Type[] parameters) {
            // locate constructor
            ConstructorInfo ci = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
            // fail if constructor does not exist
            Assert.IsNotNull(ci, t.Name + description + " is a required constructor.");
            // fail if constructor is public
            Assert.IsFalse(ci.IsPublic, t.Name + description + " is public, must be protected.");
            // fail if constructor is private
            Assert.IsFalse(ci.IsPrivate, t.Name + description + " is private, must be public or protected.");
            // fail if constructor is internal
            Assert.IsFalse(ci.IsAssembly, t.Name + description + " is internal, must be protected.");
            // fail if constructor is protected internal
            Assert.IsFalse(ci.IsFamilyOrAssembly, t.Name + description + " is protected internal, must be protected.");
            // sanity check to make sure the constructor is protected
            Assert.IsTrue(ci.IsFamily, t.Name + description + " is not protected, must be protected.");
        }

        private void CheckPrivateConstructor(Type t, string description, params Type[] parameters) {
            // locate constructor
            ConstructorInfo ci = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, parameters, null);
            // fail if constructor does not exist
            Assert.IsNotNull(ci, t.Name + description + " is a required constructor.");
            // fail if constructor is public
            Assert.IsFalse(ci.IsPublic, t.Name + description + " is public, must be private.");
            // fail if constructor is protected
            Assert.IsFalse(ci.IsFamily, t.Name + description + " is protected, must be private.");
            // fail if constructor is internal
            Assert.IsFalse(ci.IsAssembly, t.Name + description + " is internal, must be private.");
            // fail if constructor is protected internal
            Assert.IsFalse(ci.IsFamilyOrAssembly, t.Name + description + " is protected internal, must be private.");
            // sainty check to make sure the constructor is private
            Assert.IsTrue(ci.IsPrivate, t.Name + description + " is not private, must be private.");
        }

        private void CheckException(Assembly assembly, Type t) {
            // check to see that the exception is correctly named, with "Exception" at the end
            t.Name.EndsWith("Exception");
            Assert.IsTrue(t.Name.EndsWith("Exception"), t.Name + " class name must end with Exception.");

            // Does the exception have the 3 standard constructors?

            // Default constructor
            CheckPublicConstructor(t, "()");

            // Constructor with a single string parameter
            CheckPublicConstructor(t, "(string message)", typeof(System.String));

            // Constructor with a string and an inner exception
            CheckPublicConstructor(t, "(string message, Exception inner)", 
                typeof(System.String), typeof(System.Exception));

            // check to see if the serialization constructor is present
            // if exception is sealed, constructor should be private
            // if exception is not sealed, constructor should be protected
            if (t.IsSealed) {
                // check to see if the private serialization constructor is present...
                CheckPrivateConstructor(t, "(SerializationInfo info, StreamingContext context)",
                    typeof(System.Runtime.Serialization.SerializationInfo),
                    typeof(System.Runtime.Serialization.StreamingContext));
            } else {
                // check to see if the protected serialization constructor is present...
                CheckProtectedConstructor(t, "(SerializationInfo info, StreamingContext context)",
                    typeof(System.Runtime.Serialization.SerializationInfo),
                    typeof(System.Runtime.Serialization.StreamingContext));
            }

            // check to see if the type is market as serializable
            Assert.IsTrue(t.IsSerializable, t.Name + " is not serializable, missing [Serializable]?");

            // check to see if there are any public fields. These should be properties instead...
            FieldInfo[] publicFields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
            if (publicFields.Length != 0) {
                foreach (FieldInfo fieldInfo in publicFields) {
                    Assert.Fail(t.Name + "." + fieldInfo.Name + " is a public field, should be exposed through property instead.");
                }
            }

            // If this exception has any fields, check to make sure it has a 
            // version of GetObjectData. If not, it does't serialize those fields.
            FieldInfo[] fields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length != 0) {
                if (t.GetMethod("GetObjectData", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) == null) {
                    Assert.Fail(t.Name + " does not implement GetObjectData but has private fields.");
                }

                // Make sure Message is overridden if there are private fields.

                // drieseng : commented out this test, as it does not always 
                // make sense.  Not all private fields should somehow be exposed 
                // as part of the message of the exception.
                //Assert.IsTrue(t.GetProperty("Message", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) != null, t.Name + " does not override the Message property.");
            }
        }

        #endregion Private Instance Methods
    }

    /// <summary>Do nothing exception to verify that the exception tester is working correctly.</summary>
    [Serializable]
    public class SimpleTestException : ApplicationException {
        #region Public Instance Constructors

        public SimpleTestException() {
        }

        public SimpleTestException(string message) : base(message) {
        }

        public SimpleTestException(string message, Exception inner) : base(message, inner) {
        }

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        // deserialization constructor
        protected SimpleTestException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        #endregion Protected Instance Constructors
    }

    /// <summary>
    /// Exception to verify that the exception tester is working correctly.
    /// </summary>
    [Serializable]
    public class TestException : ApplicationException, ISerializable {
        #region Private Instance Fields

        private int _value;

        #endregion Private Instance Fields

        #region Public Instance Constructors

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

        #endregion Public Instance Constructors

        #region Protected Instance Constructors

        // deserialization constructor
        protected TestException(SerializationInfo info, StreamingContext context) : base(info, context) {
            _value = info.GetInt32("Value");
        }

        #endregion Protected Instance Constructors

        #region Public Instance Properties

        public int Value {
            get { return _value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of ApplicationException

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
                string s = String.Format(CultureInfo.InvariantCulture, "Value: {0}", _value);
                return base.Message + Environment.NewLine + s;
            }
        }

        #endregion Override implementation of ApplicationException
    }

    /// <summary>
    /// Exception to verify that the exception tester is working on sealed exception.
    /// </summary>
    [Serializable]
    public sealed class SealedTestException : TestException {
        #region Public Instance Constructors

        public SealedTestException() {
        }

        public SealedTestException(string message) : base(message) {
        }

        public SealedTestException(string message, Exception inner) : base(message, inner) {
        }

        // constructors that take the added value
        public SealedTestException(string message, int value) : base(message, value) {
        }

        #endregion Public Instance Constructors

        #region Private Instance Constructors

        // deserialization constructor
        private SealedTestException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        #endregion Private Instance Constructors
    }
}
