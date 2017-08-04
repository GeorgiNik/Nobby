using System.Threading.Tasks;

namespace Nobby.Web.Client.Server.Services.Abstract
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(MailType type, EmailModel emailModel, string extraData);
        bool SendEmail(EmailModel emailModel);
    }
}