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
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Mail;

using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace NAnt.Core.Tasks { 
    /// <summary>
    /// Sends an SMTP message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Text and text files to include in the message body may be specified as 
    /// well as binary attachments.
    /// </para>
    /// </remarks>
    /// <example>
    ///   <para>
    ///   Sends an email from <c>nant@sourceforge.net</c> to three recipients 
    ///   with a subject about the attachments. The body of the message will be
    ///   the combined contents of all <c>.txt</c> files in the base directory.
    ///   All zip files in the base directory will be included as attachments.  
    ///   The message will be sent using the <c>smtpserver.anywhere.com</c> SMTP 
    ///   server.
    ///   </para>
    ///   <code>
    ///     <![CDATA[
    /// <mail 
    ///     from="nant@sourceforge.net" 
    ///     tolist="recipient1@sourceforge.net" 
    ///     cclist="recipient2@sourceforge.net" 
    ///     bcclist="recipient3@sourceforge.net" 
    ///     subject="Msg 7: With attachments" 
    ///     mailhost="smtpserver.anywhere.com">
    ///     <files>
    ///         <include name="*.txt" />
    ///     </files>   
    ///     <attachments>
    ///         <include name="*.zip" />
    ///     </attachments>
    /// </mail>
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
        private FileSet _files = new FileSet();
        private FileSet _attachments = new FileSet();
        private MailFormat _mailFormat = MailFormat.Text;

        #endregion Private Instance Fields

        #region Public Instance Properties
  
        /// <summary>
        /// Email address of sender.
        /// </summary>
        [TaskAttribute("from", Required=true)]
        [StringValidator(AllowEmpty=false)]
        public string From {
            get { return _from; }
            set { _from = StringUtils.ConvertEmptyToNull(value); }
        }
        
        /// <summary>
        /// Semicolon-separated list of recipient email addresses.
        /// </summary>
        [TaskAttribute("tolist")]
        public string ToList {
            get { return _toList; }
            set { _toList = value; }
        }

        /// <summary>
        /// Semicolon-separated list of CC: recipient email addresses.
        /// </summary>
        [TaskAttribute("cclist")]
        public string CcList { 
            get { return _ccList; }
            set { _ccList = value; }
        }

        /// <summary>
        /// Semicolon-separated list of BCC: recipient email addresses.
        /// </summary>
        [TaskAttribute("bcclist")]
        public string BccList { 
            get { return _bccList; }
            set { _bccList = value; }
        }

        /// <summary>
        /// Host name of mail server. The default is <c>localhost</c>.
        /// </summary>
        [TaskAttribute("mailhost")]
        public string Mailhost {
            get { return _mailHost; }
            set { _mailHost = StringUtils.ConvertEmptyToNull(value); }
        }
  
        /// <summary>
        /// Text to send in body of email message.
        /// </summary>
        [TaskAttribute("message")]
        public string Message {
            get { return _message; }
            set { _message = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Text to send in subject line of email message.
        /// </summary>
        [TaskAttribute("subject")]
        public string Subject {
            get { return _subject; }
            set { _subject = StringUtils.ConvertEmptyToNull(value); }
        }

        /// <summary>
        /// Format of the message. The default is <see cref="MailFormat.Text" />.
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
        /// Files that are transmitted as part of the body of the email message.
        /// </summary>
        [BuildElement("files")]
        public FileSet Files { 
            get { return _files; }
            set { _files = value; }
        }

        /// <summary>
        /// Attachments that are transmitted with the message.
        /// </summary>
        [BuildElement("attachments")]
        public FileSet Attachments { 
            get { return _attachments; }
            set { _attachments = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        /// <summary>
        /// Initializes task and ensures the supplied attributes are valid.
        /// </summary>
        /// <param name="taskNode">Xml node used to define this task instance.</param>
        protected override void InitializeTask(System.Xml.XmlNode taskNode) {
            if (StringUtils.IsNullOrEmpty(ToList) && StringUtils.IsNullOrEmpty(CcList) && StringUtils.IsNullOrEmpty(BccList)) {
                throw new BuildException("There must be at least one name in" 
                    + " the \"tolist\", \"cclist\" or \"bcclist\" attributes"
                    + " of the <mail> task.", Location);
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

            // ensure base directory is set, even if fileset was not initialized
            // from XML
            if (Files.BaseDirectory == null) {
                Files.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }
            if (Attachments.BaseDirectory == null) {
                Attachments.BaseDirectory = new DirectoryInfo(Project.BaseDirectory);
            }

            // begin build message body
            StringWriter bodyWriter = new StringWriter(CultureInfo.InvariantCulture);
            
            if (!StringUtils.IsNullOrEmpty(Message)) {
                bodyWriter.WriteLine(Message);
                bodyWriter.WriteLine();
            }

            // append file(s) to message body
            foreach (string fileName in Files.FileNames) {
                try {
                    string content = ReadFile(fileName);
                    if (!StringUtils.IsNullOrEmpty(content)) {
                        bodyWriter.Write(content);
                        bodyWriter.WriteLine(string.Empty);
                    }
                } catch (Exception ex) {
                    Log(Level.Warning, string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1135"), fileName, 
                        ex.Message));
                }
            }

            // add message body to mailMessage
            string bodyText = bodyWriter.ToString();
            if (bodyText.Length != 0) {
                mailMessage.Body = bodyText;
            }

            // add attachments to message
            foreach (string fileName in Attachments.FileNames) {
                try {
                    MailAttachment attachment = new MailAttachment(fileName);
                    mailMessage.Attachments.Add(attachment);
                } catch (Exception ex) {
                    Log(Level.Warning, string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1136"), fileName, 
                        ex.Message));
                }
            }

            // send message
            try {
                Log(Level.Info, "Sending mail to {0}.", mailMessage.To);
                SmtpMail.SmtpServer = this.Mailhost;
                SmtpMail.Send(mailMessage);
            } catch (Exception ex) {
                StringBuilder msg = new StringBuilder();
                msg.Append("Error enountered while sending mail message." 
                    + Environment.NewLine);
                msg.Append("Make sure that mailhost=" + this.Mailhost 
                    + " is valid" + Environment.NewLine);
                throw new BuildException("Error sending mail:" + Environment.NewLine 
                    + msg.ToString(), Location, ex);
            }
        }

        #endregion Override implementation of Task

        #region Private Instance Methods

        /// <summary>
        /// Reads a text file and returns the content
        /// in a string.
        /// </summary>
        /// <param name="filename">The file to read content of.</param>
        /// <returns>
        /// The content of the specified file.
        /// </returns>
        private string ReadFile(string filename) {
            using (StreamReader reader = new StreamReader(File.OpenRead(filename))) {
                return reader.ReadToEnd();
            }
        }

        #endregion Private Instance Methods
    }
}
