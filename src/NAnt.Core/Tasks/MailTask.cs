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
// Gert Driesen (drieseng@users.sourceforge.net)
// Ryan Boggs (rmboggs@users.sourceforge.net)

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Mail;

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

        private string _from;
        private string _replyTo;
        private string _toList;
        private string _ccList;
        private string _bccList;
        private string _mailHost = "localhost";
        private string _subject = "";
        private string _message = "";
        private string _userName = "";
        private string _passWord = "";
        private bool _isBodyHtml = false;
        private bool _enableSsl = false;
        private int _portNumber = 25;
        private FileSet _files = new FileSet();
        private FileSet _attachments = new FileSet();

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
        /// Reply to email address.
        /// </summary>
        [TaskAttribute("replyto")]
        public string ReplyTo 
        {
            get { return _replyTo; }
            set { _replyTo = value; }
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
        /// The port number used to connect to the mail server.
        /// The default is <c>25</c>.
        /// </summary>
        [TaskAttribute("mailport")]
        [Int32Validator]
        public int Port
        {
            get { return _portNumber; }
            set { _portNumber = value; }
        }

        /// <summary>
        /// Indicates whether or not ssl should be used to
        /// connect to the smtp host.
        /// </summary>
        [TaskAttribute("ssl")]
        [BooleanValidator]
        public bool EnableSsl
        {
            get { return _enableSsl; }
            set { _enableSsl = value; }
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
        /// Indicates whether or not the body of the email is in
        /// html format. The default value is false.
        /// </summary>
        [TaskAttribute("isbodyhtml")]
        [BooleanValidator]
        public bool IsBodyHtml
        {
            get { return _isBodyHtml; }
            set { _isBodyHtml = value; }
        }

        /// <summary>
        /// The username to use when connecting to the smtp host.
        /// </summary>
        [TaskAttribute("user")]
        public string UserName
        {
            get { return _userName; }
            set { _userName = value; }
        }

        /// <summary>
        /// The password to use when connecting to the smtp host.
        /// </summary>
        [TaskAttribute("password")]
        public string Password
        {
            get { return _passWord; }
            set { _passWord = value; }
        }

        /// <summary>
        /// Format of the message. The default is <see cref="MailFormat.Text" />.
        /// </summary>
        [TaskAttribute("format")]
        [Obsolete("The format attribute is depreciated. Please use isbodyhtml instead", false)]
        public MailFormat Format
        {
            get
            {
                if (IsBodyHtml)
                {
                    return MailFormat.Html;
                }
                return MailFormat.Text;
            }
            set
            {
                if (!Enum.IsDefined(typeof(MailFormat), value))
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture,
                        "An invalid format {0} was specified.", value));
                }
                else
                {
                    if (value.Equals(MailFormat.Html)) {
                        IsBodyHtml = true;
                    }
                    else
                    {
                        IsBodyHtml = false;
                    }
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
        protected override void Initialize() {
            if (String.IsNullOrEmpty(ToList) && String.IsNullOrEmpty(CcList) && String.IsNullOrEmpty(BccList)) {
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
            MailAddressCollection toAddrs = ParseAddresses(ToList);
            MailAddressCollection ccAddrs = ParseAddresses(CcList);
            MailAddressCollection bccAddrs = ParseAddresses(BccList);

            if (toAddrs.Count > 0)
            {
                foreach (MailAddress toAddr in toAddrs) {
                    mailMessage.To.Add(toAddr);
                }
            }

            if (ccAddrs.Count > 0) {
                foreach (MailAddress ccAddr in ccAddrs) {
                    mailMessage.CC.Add(ccAddr);
                }
            }

            if (bccAddrs.Count > 0) {
                foreach (MailAddress bccAddr in bccAddrs) {
                    mailMessage.Bcc.Add(bccAddr);
                }
            }
            
            if (!String.IsNullOrEmpty(ReplyTo))
            {
#if NET_4_0
                MailAddressCollection replyAddrs = ParseAddresses(ReplyTo);
                
                if (replyAddrs.Count > 0) {
                    foreach (MailAddress replyAddr in replyAddrs) {
                        mailMessage.ReplyToList.Add(replyAddr);
                    }
                }
#else
                mailMessage.ReplyTo = ConvertStringToMailAddress(ReplyTo);
#endif
            }

            mailMessage.From = new MailAddress(this.From);
            mailMessage.Subject = this.Subject;
            mailMessage.IsBodyHtml = this.IsBodyHtml;

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
            
            if (!String.IsNullOrEmpty(Message)) {
                bodyWriter.WriteLine(Message);
                bodyWriter.WriteLine();
            }

            // append file(s) to message body
            foreach (string fileName in Files.FileNames) {
                try {
                    string content = ReadFile(fileName);
                    if (!String.IsNullOrEmpty(content)) {
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
                    Attachment attachment = new Attachment(fileName);
                    mailMessage.Attachments.Add(attachment);
                } catch (Exception ex) {
                    Log(Level.Warning, string.Format(CultureInfo.InvariantCulture,
                        ResourceUtils.GetString("NA1136"), fileName, 
                        ex.Message));
                }
            }

            // send message
            try {
                Log(Level.Info, "Sending mail...");
                Log(Level.Info, "To: {0}", mailMessage.To);
                Log(Level.Info, "Cc: {0}", mailMessage.CC);
                Log(Level.Info, "Bcc: {0}", mailMessage.Bcc);
                SmtpClient smtp = new SmtpClient(this.Mailhost);

                if (!String.IsNullOrEmpty(this.UserName) &&
                    !String.IsNullOrEmpty(this.Password))
                {
                    smtp.Credentials =
                        new NetworkCredential(this.UserName, this.Password);
                }
                else
                {
                    smtp.UseDefaultCredentials = true;
                }

                smtp.EnableSsl = this.EnableSsl;
                smtp.Port = this.Port;
                smtp.Send(mailMessage);

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

        /// <summary>
        /// Converts an email address or a series of email addresses from
        /// a <see cref="System.String"/> object to a new
        /// <see cref="System.Net.Mail.MailAddressCollection"/> object.
        /// </summary>
        /// <param name='addresses'>
        /// A list of email addresses separated by a semicolon.
        /// </param>
        /// <returns>
        /// A new <see cref="System.Net.Mail.MailAddressCollection"/> object
        /// containing the addresses from <paramref name="addresses"/>.
        /// </returns>
        private MailAddressCollection ParseAddresses(string addresses)
        {
            // Initialize the MailAddressCollection object that will be
            // returned by this method.
            MailAddressCollection results = new MailAddressCollection();

            // Make sure the addresses string is not null before attempting
            // to parse.
            if (!String.IsNullOrEmpty(addresses))
            {
                // If the addresses parameter contains a semicolon, that means
                // that more than one email address is present and needs to be parsed.
                if (addresses.Contains(";")) {
                    string[] parsedAddresses = addresses.Split(new char[] { ';' });
    
                    foreach (string item in parsedAddresses)
                    {
                        results.Add(ConvertStringToMailAddress(item));
                    }
                }
    
                // Otherwise, pass the addresses param string to the new
                // MailAddressCollection if it is not null or empty.
                else
                {
                    results.Add(ConvertStringToMailAddress(addresses));
                }
            }

            return results;
        }
        
        /// <summary>
        /// Converts a <see cref="System.String"/> object containing
        /// email address information to a 
        /// <see cref="System.Net.Mail.MailAddress" /> object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Email address information passed to this method should be in
        /// one of five formats.
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>Full Name &lt;address@abcxyz.com&gt;</description>
        /// </item>
        /// <item>
        /// <description>&lt;address@abcxyz.com&gt; Full Name</description>
        /// </item>
        /// <item>
        /// <description>(Full Name) address@abcxyz.com</description>
        /// </item>
        /// <item>
        /// <description>address@abcxyz.com (Full Name)</description>
        /// </item>
        /// <item>
        /// <description>address@abcxyz.com</description>
        /// </item>
        /// </list>
        /// <para>
        /// If the full name of the intended recipient (or sender) is provided,
        /// that information is included in the resulting 
        /// <see cref="System.Net.Mail.MailAddress" /> object.
        /// </para>
        /// </remarks>
        /// <param name="address">
        /// The string that contains the address to parse.
        /// </param>
        /// <returns>
        /// A new MailAddress object containing the information from
        /// <paramref name="address"/>.
        /// </returns>
        private MailAddress ConvertStringToMailAddress(string address)
        {
            // Convert the email address parameter from html encoded to 
            // normal string.  Makes validation easier.
            string escAddress = StringUtils.HtmlDecode(address);
            
            // String array containing all of the regex strings used to
            // locate the email address in the parameter string.
            string[] validators = new string[]
            {
                // Format: Full Name <address@abcxyz.com>
                @"^(?<fullname>.+)\s<(?<email>[^<>\(\)\s]+@[^<>\(\)\s]+\.[^<>\(\)\s]+)>$",
                
                // Format: <address@abcxyz.com> Full Name
                @"^<(?<email>[^<>\(\)\s]+@[^<>\(\)\s]+\.[^\s]+)>\s(?<fullname>.+)$",
                
                // Format: (Full Name) address@abcxyz.com
                @"^\((?<fullname>.+)\)\s(?<email>[^<>\(\)\s]+@[^<>\(\)\s]+\.[^<>\(\)\s]+)$",
                
                // Format: address@abcxyz.com (Full Name)
                @"^(?<email>[^<>\(\)\s]+@[^<>\(\)\s]+\.[^\s]+)\s\((?<fullname>.+)\)$",
                
                // Format: address@abcxyz.com
                @"(?<email>[^<>\(\)\s]+@[^<>\(\)\s]+\.[^<>\(\)\s]+)"
            };
            
            // Loop through each regex string to find the one that the
            // email address matches.
            foreach (string reg in validators) 
            {
                // Create the regex object and try to match
                // the email address with the current regex
                // string.
                Regex currentRegex = new Regex(reg);
                Match email = currentRegex.Match(escAddress);
                
                // If the match is considered successful, return
                // a new MailAddress object.  If a name was 
                // paired with an email address in the parameter,
                // add it to the MailAddress object that is returned.
                if (email.Success)
                {
                    if (email.Groups["fullname"].Success)
                    {
                        return new MailAddress(
                            email.Groups["email"].Value.Trim(),
                            email.Groups["fullname"].Value.Trim());
                    }
                    
                    return new MailAddress(email.Groups["email"].Value.Trim());
                }
            }
            
            // If none of the regex strings matches the address parameter,
            // throw a build exception.
            throw new BuildException(
                String.Format(CultureInfo.InvariantCulture,
                              "{0} is not a recognized email address",
                              address));
        }

        #endregion Private Instance Methods

        /// <summary>
        /// Temporary enum replacement of <see cref="System.Web.Mail.MailFormat"/>
        /// to ease transition to newer property flags.
        /// </summary>
        public enum MailFormat
        {
            /// <summary>
            /// Indicates the body of the email is formatted in plain text.
            /// </summary>
            Text = 0,

            /// <summary>
            /// Indicates the body of the email is formatted in html.
            /// </summary>
            Html = 1
        }
    }
}
