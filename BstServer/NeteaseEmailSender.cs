using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace BstServer;

public class NeteaseEmailSender : SmtpClient, IEmailSender
{
    private string _userName;

    public NeteaseEmailSender(string userName, string password)
    {
        _userName = userName;
        this.UseDefaultCredentials = true;
        this.DeliveryFormat = SmtpDeliveryFormat.International;
        this.DeliveryMethod = SmtpDeliveryMethod.Network;
        Port = 25;
        Credentials = new NetworkCredential(userName, password);
        Host = "smtp.126.com";
        //Send($"{_userName}@126.com", "milkitic@126.com", "测试", "测试邮件");
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var msg = new MailMessage(new MailAddress($"{_userName}@126.com"), new MailAddress(email))
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        await SendMailAsync(msg);
    }
}