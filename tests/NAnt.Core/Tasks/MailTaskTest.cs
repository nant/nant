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

// Jay Turpin (recipient2@sourceforge.net)
// Gerry Shaw (gerry_shaw@yahoo.com)

// this test or task is broken

#if false
using System;
using System.Collections.Specialized;
using System.IO;

using NUnit.Framework;

using NAnt.Core.Tasks;

namespace Tests.NAnt.Core.Tasks {
    [TestFixture]
    public class MailTaskTest  {

        string _from = "";
        string _tolist = "";
        string _cclist = "";
        string _bcclist = "";
        string _subject = "";
        string _message = "";
        string _mailhost = "";
        string _files = "";
        string _attachments = "";
        string _emailAddress1 = "";
        string _emailAddress2 = "";

        StringCollection _fileList = new StringCollection();
        string _baseDirectory = @"c:\Temp\MailTest";

        [SetUp]
        protected void SetUp() {

            _emailAddress1 = "nAnt1@sourceforge.net";
            _emailAddress2 = "nAnt2@sourceforge.net";

            _from = "nAnt@sourceforge.net";
            _mailhost="sourceforge.net";

            _tolist = "";
            _cclist = "";
            _bcclist = "";
            _subject = "";
            _message = "";
            _files = "";
            _attachments = "";

            // create test directory structure
            Directory.CreateDirectory(_baseDirectory);

            // add files
            _fileList.Add(_baseDirectory + @"\mail1.txt");
            _fileList.Add(_baseDirectory + @"\mail2.txt");
            _fileList.Add(_baseDirectory + @"\mail3.txt");

            // add some text to each file, just for fun ;)
            foreach (string fileName in _fileList) {
                StreamWriter writer = File.CreateText(fileName);
                writer.Write("It's OK to delete this file.");
                writer.Close();
            }

        }

        [TearDown]
        protected void TearDown() {
            try {
                Directory.Delete(_baseDirectory, true);
            } catch {
            }
        }

        /// <summary>
        /// Simple message
        /// </summary>
        /// <remarks>
        /// <mail 
        ///     from="nAnt@sourceforge.net" 
        ///     tolist="recipient1@sourceforge.net" 
        ///     subject="Msg 1: Simple Test" 
        ///     message="Test message" 
        ///     mailhost="smtpserver.anywhere.com"/>
        ///     
        /// </remarks>

        [Test]
        public void testSimpleMessage() {

            MailTask mailTask = new MailTask();
            mailTask.Project = new Project();

            _subject="Msg 1: Simple Test";
            _message="Test message";
            _tolist = _emailAddress1;

            try {
                mailTask.Mailhost = _mailhost;
                mailTask.From = _from;
                mailTask.ToList = _tolist;
                mailTask.Subject = _subject;
                mailTask.Message = _message;
                mailTask.CcList = _cclist;
                mailTask.BccList = _bcclist;
                mailTask.Attachments = _attachments;
                mailTask.Files = _files;

                mailTask.Execute();
            } catch (Exception e) {
                Assertion.Assert(_subject + ": " + e.Message, false);
            }
        }

            /// <summary>
            /// Multiple recipients in toList
            /// </summary>
            /// <remarks>
            /// <mail 
            ///     from="nAnt@sourceforge.net" 
            ///     tolist="recipient1@sourceforge.net;recipient2@sourceforge.net" 
            ///     subject="Msg 2: Test to 2 email addresses" 
            ///     message="Test message" 
            ///     mailhost="smtpserver.anywhere.com"/>
            ///     
            /// </remarks>

            [Test]
            public void testMultiToList() {

                MailTask mailTask = new MailTask();
                mailTask.Project = new Project();

                _tolist= _emailAddress1 + ";" + _emailAddress2;
                _subject="Msg 2: Test to 2 email addresses";
                _message="Test message";

                try {
                    mailTask.Mailhost = _mailhost;
                    mailTask.From = _from;
                    mailTask.ToList = _tolist;
                    mailTask.Subject = _subject;
                    mailTask.Message = _message;
                    mailTask.CcList = _cclist;
                    mailTask.BccList = _bcclist;
                    mailTask.Attachments = _attachments;
                    mailTask.Files = _files;

                    mailTask.Execute();
                } catch (Exception e) {
                    Assertion.Assert(_subject + ": " + e.Message, false);
                }
        }

        /// <summary>
        /// Multiple BCC recipients in toList
        /// </summary>
        /// <remarks>
        /// <mail 
        ///     from="nAnt@sourceforge.net" 
        ///     bcclist="recipient1@sourceforge.net;recipient2@sourceforge.net" 
        ///     subject="Msg 3: Test to 2 BCC addresses" 
        ///     message="Test message" 
        ///     mailhost="smtpserver.anywhere.com"/>
        ///     
        /// </remarks>

        [Test]
        public void testMultiBccList() {

            MailTask mailTask = new MailTask();
            mailTask.Project = new Project();

            _bcclist= _emailAddress1 + ";" + _emailAddress2;
            _subject="Msg 3: Test to 2 BCC addresses";
            _message="Test message";

            try {
                mailTask.Mailhost = _mailhost;
                mailTask.From = _from;
                mailTask.ToList = _tolist;
                mailTask.Subject = _subject;
                mailTask.Message = _message;
                mailTask.CcList = _cclist;
                mailTask.BccList = _bcclist;
                mailTask.Attachments = _attachments;
                mailTask.Files = _files;

                mailTask.Execute();
            } catch (Exception e) {
                Assertion.Assert(_subject + ": " + e.Message, false);
            }
        }

        /// <summary>
        /// Multiple BCC recipients in toList
        /// </summary>
        /// <remarks>
        /// <mail 
        ///     from="nAnt@sourceforge.net" 
        ///     cclist="recipient1@sourceforge.net;recipient2@sourceforge.net" 
        ///     subject="Msg 4: Test to 2 CC addresses" 
        ///     message="Test message" 
        ///     mailhost="smtpserver.anywhere.com"/>
        ///     
        /// </remarks>

        [Test]
        public void testMultiCcList() {

            MailTask mailTask = new MailTask();
            mailTask.Project = new Project();

            _cclist= _emailAddress1 + ";" + _emailAddress2;
            _subject="Msg 4: Test to 2 CC addresses";
            _message="Test message";

            try {
                mailTask.Mailhost = _mailhost;
                mailTask.From = _from;
                mailTask.ToList = _tolist;
                mailTask.Subject = _subject;
                mailTask.Message = _message;
                mailTask.CcList = _cclist;
                mailTask.BccList = _bcclist;
                mailTask.Attachments = _attachments;
                mailTask.Files = _files;

                mailTask.Execute();
            } catch (Exception e) {
                Assertion.Assert(_subject + ": " + e.Message, false);
            }
        }

        /// <summary>
        /// Message to all recipient lists
        /// </summary>
        /// <remarks>
        /// <mail 
        ///     from="nAnt@sourceforge.net" 
        ///     tolist="recipient2@sourceforge.net" 
        ///     cclist="recipient2@sourceforge.net" 
        ///     bcclist="recipient1@sourceforge.net" 
        ///     subject="Msg 5: Test to all addresses" 
        ///     message="Test message" 
        ///     mailhost="smtpserver.anywhere.com"/>
        ///     
        /// </remarks>

        [Test]
        public void testAllLists() {

            MailTask mailTask = new MailTask();
            mailTask.Project = new Project();

            _tolist= _emailAddress1;
            _cclist= _emailAddress2;
            _bcclist= _emailAddress1;
            _subject="Msg 5: Test to all addresses";
            _message="Test message";

            try {
                mailTask.Mailhost = _mailhost;
                mailTask.From = _from;
                mailTask.ToList = _tolist;
                mailTask.Subject = _subject;
                mailTask.Message = _message;
                mailTask.CcList = _cclist;
                mailTask.BccList = _bcclist;
                mailTask.Attachments = _attachments;
                mailTask.Files = _files;

                mailTask.Execute();
            } catch (Exception e) {
                Assertion.Assert(_subject + ": " + e.Message, false);
            }
        }

        /// <summary>
        /// Message with files as body text
        /// </summary>
        /// <remarks>
        /// <mail 
        ///     from="nAnt@sourceforge.net" 
        ///     tolist="recipient1@sourceforge.net" 
        ///     subject="Msg 6: Files for message" 
        ///     files="body1.txt,body2.txt;body3.txt" 
        ///     mailhost="smtpserver.anywhere.com"/>
        ///     
        /// </remarks>

        [Test]
        public void testFilesAsBody() {

            MailTask mailTask = new MailTask();
            mailTask.Project = new Project();

            _tolist= _emailAddress1;
            _subject="Msg 6: Files for message";
            _message="Test message";
            foreach (string fileName in _fileList) {
                _files += fileName + ";";
            }
            // add bogus entry
            _files += "BogusFile.txt";

            try {
                mailTask.Mailhost = _mailhost;
                mailTask.From = _from;
                mailTask.ToList = _tolist;
                mailTask.Subject = _subject;
                mailTask.Message = _message;
                mailTask.CcList = _cclist;
                mailTask.BccList = _bcclist;
                mailTask.Attachments = _attachments;
                mailTask.Files = _files;

                mailTask.Execute();
            } catch (Exception e) {
                Assertion.Assert(_subject + ": " + e.Message, false);
            }
        }

        /// <summary>
        /// Message with attachments
        /// </summary>
        /// <remarks>
        /// <mail 
        ///     from="nAnt@sourceforge.net" 
        ///     tolist="recipient1@sourceforge.net" 
        ///     subject="Msg 7: With attachments" 
        ///     files="body1.txt,body2.txt;body3.txt,body4.txt" 
        ///     attachments="body1.txt,body2.txt;,body3.txt" 
        ///     mailhost="smtpserver.anywhere.com"/>
        ///     
        /// </remarks>

        [Test]
        public void testFilesAsAttach() {

            MailTask mailTask = new MailTask();
            mailTask.Project = new Project();

            _tolist= _emailAddress1;
            _subject="Msg 7: With attachments";
            _message="Test message";
            foreach (string fileName in _fileList) {
                _files += fileName + ";";
            }
            // add bogus entry
            _files += "BogusFile.txt";


            foreach (string fileName in _fileList) {
                _attachments += fileName + ",";
            }
            // add bogus entries - empty files
            _files += ",;";

            try {
                mailTask.Mailhost = _mailhost;
                mailTask.From = _from;
                mailTask.ToList = _tolist;
                mailTask.Subject = _subject;
                mailTask.Message = _message;
                mailTask.CcList = _cclist;
                mailTask.BccList = _bcclist;
                mailTask.Attachments = _attachments;
                mailTask.Files = _files;

                mailTask.Execute();
            } catch (Exception e) {
                Assertion.Assert(_subject + ": " + e.Message, false);
            }
        }

    }
}
#endif