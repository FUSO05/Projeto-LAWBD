using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMarket.Models;
using AutoMarket.Models.ViewModels;

namespace AutoMarket.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<Utilizador?> GetUserByEmailAsync(string email);
        bool ValidatePassword(Utilizador user, string password);
        Task<bool> ConfirmEmailAsync(int userId, string token);

        // Recuperação de password
        Task GeneratePasswordResetTokenAsync(Utilizador user, string token);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Utilizador> _passwordHasher;
        private readonly EmailService _emailService;

        public AuthService(AppDbContext context, IPasswordHasher<Utilizador> passwordHasher, EmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }


        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            // Verifica se o email já existe
            var existingUser = await _context.Utilizadores.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingUser != null)
                return false;

            var user = new Utilizador
            {
                Nome = model.Nome,
                Email = model.Email,
                Username = model.Email.Split('@')[0], // pode ajustar se quiser um campo separado
                Password = _passwordHasher.HashPassword(null!, model.Password),
                TipoUser = model.Tipo,
                DataRegisto = DateTime.Now,
                Ativo = true
            };

            _context.Utilizadores.Add(user);
            await _context.SaveChangesAsync();
            try
            {
                string confirmLink = $"https://localhost:7038/Account/ConfirmEmail?email={user.Email}";
                string body = $@"
        <h2>Bem-vindo ao AutoMarket, {user.Nome}!</h2>
        <p>Para ativar a sua conta, clique no link abaixo:</p>
        <p><a href='{confirmLink}'>Confirmar Conta</a></p>
        <br>
        <p>Se não criou esta conta, ignore este e-mail.</p>
    ";

                await _emailService.EnviarEmailAsync(user.Email, "Confirme a sua conta no AutoMarket", body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao enviar e-mail: {ex.Message}");
            }

            return true;
        }

        public async Task<Utilizador?> GetUserByEmailAsync(string email)
        {
            return await _context.Utilizadores.FirstOrDefaultAsync(u => u.Email == email);
        }

        public bool ValidatePassword(Utilizador user, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            return result == PasswordVerificationResult.Success;
        }

        // Neste exemplo, ConfirmEmailAsync é apenas ilustrativo — pode implementar tokens se quiser ativação real
        public async Task<bool> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _context.Utilizadores.FindAsync(userId);
            if (user == null)
                return false;

            user.Ativo = true;
            _context.Utilizadores.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task GeneratePasswordResetTokenAsync(Utilizador user, string token)
        {
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // válido por 1h
            _context.Utilizadores.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Utilizadores
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (user == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
                return false;

            user.Password = _passwordHasher.HashPassword(user, newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            _context.Utilizadores.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}