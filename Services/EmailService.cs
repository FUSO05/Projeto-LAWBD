using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace AutoMarket.Services
{
    public class EmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _fromEmail;
        private readonly bool _useSSL;

        public EmailService(IConfiguration configuration)
        {
            _host = configuration["SMTP:Host"];
            _port = int.Parse(configuration["SMTP:Port"]);
            _username = configuration["SMTP:Username"];
            _password = configuration["SMTP:Password"];
            _fromEmail = configuration["SMTP:FromEmail"];
            _useSSL = bool.Parse(configuration["SMTP:UseSSL"]);
        }

        public async Task EnviarEmailAsync(string paraEmail, string assunto, string mensagem)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("AutoMarket", _fromEmail));
            message.To.Add(new MailboxAddress("", paraEmail));
            message.Subject = assunto;
            message.Body = new TextPart("html") { Text = mensagem };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                await client.ConnectAsync(_host, _port, _useSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_username, _password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"E-mail enviado para: {paraEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
                throw;
            }
        }

        public async Task EnviarEmailComTemplateAsync(string paraEmail, string assunto, string templatePath, Dictionary<string, string> placeholders)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template não encontrado em: {templatePath}");

            string templateContent = await File.ReadAllTextAsync(templatePath);

            foreach (var placeholder in placeholders)
            {
                templateContent = templateContent.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }

            await EnviarEmailAsync(paraEmail, assunto, templateContent);
        }
    }
}
