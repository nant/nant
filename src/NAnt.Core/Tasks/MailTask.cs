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
// Jay Turpin (jayturpin@hotmail.com)
// Gerry Shaw (gerry_shaw@yahoo.com)

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Mail;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Core.Tasks { 
    /// <summary>
    /// A task to send an SMTP message.
    /// </summary>
    /// <remarks>
    /// Text and text files to include in the message body may be specified as 
    /// well as binary attachments.
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Sends an email from <c>nant@sourceforge.net</c> to three recipients 
    ///   with a subject about the attachments. The body of the message will be
    ///   the combined contents of <c>body1.txt</c> through <c>body4.txt</c>.
    ///   The <c>body1.txt</c> through <c>body3.txt</c> files will also be 
    ///   included as attachments.  The message will be sent using the 
    ///   <c>smtpserver.anywhere.com</c> SMTP server.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <mail 
    ///     from="nAnt@sourceforge.net" 
    ///     tolist="recipient1@sourceforge.net" 
    ///     cclist="recipient2@sourceforge.net" 
    ///     bcclist="recipient3@sourceforge.net" 
    ///     subject="Msg 7: With attachments" 
    ///     files="body1.txt,body2.txt;body3.txt,body4.txt" 
    ///     attachments="body1.txt,body2.txt;,body3.txt" 
    ///     mailhost="smtpserver.anywhere.com" />
    ///     ]]>
    ///   </code>
    /// </example>
    [TaskName("mail")]
    public class MailTask : Task {
        #region Private Instance Fields

        private string _from = null;
        private string _toList = null;
        private string _ccList = null;
        private string _bccList = null;
        private string _mailHost = "localhost";
        private string _subject = "";
        private string _message = "";
        private string _files = "";
        private string _attachments = "";
        private MailFormat _mailFormat = MailFormat.Text;

        #endregion Private Instance Fields

        #region Public Instance Properties
  
        /// <summary>
        /// Email address of sender.
        /// </summary>
        [TaskAttribute("from", Required=true)]
        public string From {
            get { return _from; }
            set { _from = SetStringValue(value); }
        }
        
        /// <summary>
        /// Comma- or semicolon-separated list of recipient email addresses.
        /// </summary>
        [TaskAttribute("tolist", Required=true)]
        public string ToList {
            get { return _toList.Replace("," , ";"); }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    // convert to semicolon delimited
                    _toList = value.Replace("," , ";"); 
                } else {
                    _toList = null;
                }
            }
        }

        /// <summary>
        /// Comma- or semicolon-separated list of CC: recipient email addresses.
        /// </summary>
        [TaskAttribute("cclist")]
        public string CcList { 
            get { return _ccList; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    // convert to semicolon delimited
                    _ccList = value.Replace("," , ";");
                } else {
                    _ccList = null;
                }
            }
        }

        /// <summary>
        /// Comma- or semicolon-separated list of BCC: recipient email addresses.
        /// </summary>
        [TaskAttribute("bcclist")]
        public string BccList { 
            get { return _bccList; } 
            set { 
                if (value != null && value.Trim().Length != 0) {
                    // convert to semicolon delimited
                    _bccList = value.Replace("," , ";"); 
                } else {
                    _bccList = null;
                }
            }
        }

        /// <summary>
        /// Host name of mail server. Default is <c>localhost</c>.
        /// </summary>
        [TaskAttribute("mailhost")]
        public string Mailhost {
            get { return _mailHost; }
            set { _mailHost = SetStringValue(value); }
        }
  
        /// <summary>
        /// Text to send in body of email message.
        /// </summary>
        [TaskAttribute("message")]
        public string Message {
            get { return _message; }
            set { _message = value; }
        }

        /// <summary>
        /// Text to send in subject line of email message.
        /// </summary>
        [TaskAttribute("subject")]
        public string Subject {
            get { return _subject; }
            set { _subject = value; }
        }

        /// <summary>
        /// Format of the message - either <see cref="MailFormat.Html" />
        /// or <see cref="MailFormat.Text" />. Defaults is <see cref="MailFormat.Text" />.
        /// </summary>
        [TaskAttribute("format")]
        public MailFormat Format {
           get { return _mailFormat; }
           set {
               if (!Enum.IsDefined(typeof(MailFormat), value)) {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "An invalid format {0} was specified.", value)); 
                } else {
                    this._mailFormat = value;
                }
            } 
        }

        /// <summary>
        /// Name(s) of text files to send as part of body of the email message. 
        /// Multiple file names are comma- or semicolon-separated.
        /// </summary>
        [TaskAttribute("files")]
        public string Files {
            get { return _files; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    // convert to semicolon delimited
                    _files = value.Replace("," , ";");
                } else {
                    _files = null;
                }
            }
        }

        /// <summary>
        /// Name(s) of files to send as attachments to email message.
        /// Multiple file names are comma- or semicolon-separated.
        /// </summary>
        [TaskAttribute("attachments")]
        public string Attachments { 
            get { return _attachments; }
            set { 
                if (value != null && value.Trim().Length != 0) {
                    // convert to semicolon delimited
                    _attachments = value.Replace("," , ";");
                } else {
                    _attachments = null;
                }
            }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        ///<summary>
        ///Initializes task and ensures the supplied attributes are valid.
        ///</summary>
        ///<param name="taskNode">Xml node used to define this task instance.</param>
        protected override void InitializeTask(System.Xml.XmlNode taskNode) {
            if (From == null) {
                throw new BuildException("Mail attribute \"from\" is required.", Location);
            }

            if (ToList == null && CcList == null && BccList == null) {
                throw new BuildException("Mail must provide at least one of these attributes: \"tolist\", \"cclist\" or \"bcclist\".", Location);
            }
        }

        /// <summary>
        /// This is where the work is done.
        /// </summary>
        protected override void ExecuteTask() {
            MailMessage mailMessage = new MailMessage();
            
            mailMessage.From = this.From;
            mailMessage.To = this.ToList;
            mailMessage.Bcc = this.BccList;
            mailMessage.Cc = this.CcList;
            mailMessage.Subject = this.Subject;
            mailMessage.BodyFormat = this.Format;

            // Begin build message body
            StringWriter bodyWriter = new StringWriter(CultureInfo.InvariantCulture);
            
            if (Message != null && Message.Length > 0) {
                bodyWriter.WriteLine(Message);
                bodyWriter.WriteLine();
            }

            // Append file(s) to message body
            if (Files != null && Files.Length > 0) {
                string[] fileList = Files.Split(new char[]{';'});
                string content;
                if (fileList.Length == 1) {
                   content = ReadFile(fileList[0]);
                    if (content != null) {
                        bodyWriter.Write(content);
                    }
                } else {
                  foreach (string fileName in fileList) {
                     content = ReadFile(fileName);
                     if (content != null) {
                        bodyWriter.WriteLine(fileName);
                        bodyWriter.WriteLine(content);
                        bodyWriter.WriteLine("");
                     }
                  }
                }
            }

            // add message body to mailMessage
            string bodyText = bodyWriter.ToString();
            if (bodyText.Length != 0) {
                mailMessage.Body = bodyText;
            } else {
                throw new BuildException("Mail attribute \"file\" or \"message\" is required.");
            }

            // Append file(s) to message body
            if (Attachments != null && Attachments.Length > 0) {
                string[] attachList = Attachments.Split(new char[]{';'});
                foreach (string fileName in attachList) {
                    try {
                        // MailAttachment likes fully-qualified file names, use FileInfo to get them
                        FileInfo fileInfo = new FileInfo(fileName);
                        MailAttachment attach = new MailAttachment(fileInfo.FullName);
                        mailMessage.Attachments.Add(attach);
                    } catch {
                        string msg = "WARNING! File \"" + fileName + "\" NOT attached to message. File does not exist or cannot be accessed. Check: " + Location.ToString() + "attachments=\"" + Attachments + "\"";
                        Log(Level.Info, LogPrefix + msg);
                    }
                }
            }

            // send message
            try {
                Log(Level.Info, LogPrefix + "Sending mail to {0}.", mailMessage.To);
                SmtpMail.SmtpServer = this.Mailhost;
                SmtpMail.Send(mailMessage);
            } catch (Exception e) {
                StringBuilder msg = new StringBuilder();
                msg.Append(LogPrefix + "Error enountered while sending mail message.\n");
                msg.Append(LogPrefix + "Make sure that mailhost=" + this.Mailhost + " is valid\n");
                msg.Append(LogPrefix + "Stack Trace:\n");
                msg.Append(e.ToString() + "\n");
                throw new BuildException("Error sending mail:\n " + msg.ToString());
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Reads a text file and returns the content
        /// in a string.
        /// </summary>
        /// <param name="filename">The file to read content of.</param>
        /// <returns>The content of the specified file.</returns>
        private string ReadFile(string filename) {
            StreamReader reader = null;

            try {
                reader = new StreamReader(File.OpenRead(filename));
                return reader.ReadToEnd();
            } catch {
                string msg = "WARNING! File \"" + filename + "\" NOT added to message body. File does not exist or is not readable. Check: " + Location.ToString() + "files=\"" + Files + "\"";
                Log(Level.Warning, LogPrefix + msg);
                return null;
            } finally {
                if (reader != null) {
                    reader.Close();
                }
            }
        }

        #endregion Private Instance Methods
    }
}
