// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

using System;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace WebApplication3.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
 {}


        public void SendEmail(System.Collections.Generic.IEnumerable<string> to, System.Collections.Generic.IEnumerable<string> cc,
                              string subject, string body, string replyTo = null)
 {}


        public void SendEmailWithAttachment(System.Collections.Generic.IEnumerable<string> to, string subject, string body,
                                             byte[] attachmentBytes, string attachmentFileName, string attachmentContentType = "application/pdf")
 {}

        public void SendEmail(string recipientEmail, string subject, string body)
 {}
    }
}