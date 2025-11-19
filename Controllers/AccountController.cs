using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AutoMarket.Models;
using AutoMarket.Services;
using AutoMarket.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AutoMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Utilizador> _passwordHasher;
        private readonly IAuthService _authService;
        private readonly EmailService _emailService;

        public AccountController(AppDbContext context, IPasswordHasher<Utilizador> passwordHasher, IAuthService authService, EmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _authService = authService;
            _emailService = emailService;
        }

        // --- EMAIL DE ATIVAÇÃO ---
        private async Task EnviarEmailDeAtivacaoAsync(Utilizador user)
        {
            string ativacaoUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token = user.ActivationToken },
                Request.Scheme
            );

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "AtivacaoConta.html");

            var placeholders = new Dictionary<string, string>
            {
                { "Nome", user.Nome },
                { "AtivacaoUrl", ativacaoUrl }
            };

            await _emailService.EnviarEmailComTemplateAsync(user.Email, "Ativação de Conta", templatePath, placeholders);
        }

        // --- LOGIN ---
        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Account/Login.cshtml", new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _authService.GetUserByEmailAsync(model.Email);

            if (user != null && user.EmailConfirmed && _authService.ValidatePassword(user, model.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.TipoUser ?? "User"),
                    new Claim("FotoUrl", string.IsNullOrEmpty(user.FotoUrl) ? "/img/avatar.png" : user.FotoUrl)
                };


                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTime.UtcNow.AddDays(14) : DateTime.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                ViewBag.UserPhotoUrl = user.FotoUrl ?? "/img/avatar.png";

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email ou Password inválidos, ou conta não confirmada.");
            return View(model);
        }

        // --- REGISTO ---
        [HttpGet]
        public IActionResult Register()
        {
            return View("~/Views/Account/Register.cshtml", new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _authService.GetUserByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "O email já está em uso.");
                return View(model);
            }

            // Gerar token de ativação
            string token = Guid.NewGuid().ToString();

            var user = new Utilizador
            {
                Nome = model.Nome,
                Email = model.Email,
                Contacto = model.Contacto,
                Morada = model.Morada,
                Password = _passwordHasher.HashPassword(null, model.Password),
                EmailConfirmed = false,
                ActivationToken = token,
                ActivationTokenExpiry = DateTime.UtcNow.AddHours(24) // Token válido por 24h
            };

            _context.Utilizadores.Add(user);
            await _context.SaveChangesAsync();

            try
            {
                await EnviarEmailDeAtivacaoAsync(user);
                TempData["MensagemSucesso"] = "Registo concluído! Verifique o seu email para ativar a conta em até 24 horas.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de ativação: {ex.Message}");
                TempData["MensagemErro"] = "Ocorreu um problema ao enviar o email de ativação. Tente novamente mais tarde.";
                return View(model);
            }

            return RedirectToAction("Login");
        }

        // --- CONFIRMAÇÃO DE EMAIL ---
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            var user = await _context.Utilizadores.FindAsync(userId);
            if (user == null || user.EmailConfirmed)
                return BadRequest("Usuário inválido ou já confirmado.");

            if (user.ActivationToken != token || user.ActivationTokenExpiry < DateTime.UtcNow)
                return BadRequest("Token inválido ou expirado.");

            user.EmailConfirmed = true;
            user.ActivationToken = null;
            user.ActivationTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Conta ativada com sucesso!";
            return RedirectToAction("Login", "Account");
        }

        // --- LOGOUT ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // --- RECUPERAR PASSWORD ---
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Account/ForgotPassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _authService.GetUserByEmailAsync(email);

            if (user == null)
            {
                TempData["MensagemErro"] = "Email não encontrado.";
                return View();
            }

            string token = Guid.NewGuid().ToString();
            await _authService.GeneratePasswordResetTokenAsync(user, token);

            string resetUrl = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "ResetPassword.html");

            var placeholders = new Dictionary<string, string>
            {
                { "Nome", user.Nome },
                { "ResetUrl", resetUrl }
            };

            await _emailService.EnviarEmailComTemplateAsync(user.Email, "Recuperação de Password", templatePath, placeholders);

            TempData["MensagemSucesso"] = "Um email de recuperação foi enviado.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var success = await _authService.ResetPasswordAsync(model.Token, model.NewPassword);

            if (success)
            {
                TempData["MensagemSucesso"] = "Password alterada com sucesso!";
                return RedirectToAction("Login");
            }

            TempData["MensagemErro"] = "Falha ao redefinir a password (token inválido ou expirado).";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UserMenu()
        {
            // Obter o ID do utilizador a partir dos Claims do cookie
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Buscar o utilizador do BD
            var user = await _context.Utilizadores.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new UtilizadorViewModel
            {
                Nome = user.Nome,
                Email = user.Email,
                Morada = user.Morada,
                Contacto = user.Contacto,
                FotolUrl = string.IsNullOrEmpty(user.FotoUrl) ? "https://i.pravatar.cc/100" : user.FotoUrl,
                PedidoVendedorPendente = user.PedidoVendedorPendente,
                TipoUser = user.TipoUser
            };


            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarDefinicoes(UtilizadorViewModel model, IFormFile ProfileImage)
        {
            // Obter o ID do utilizador logado a partir dos Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Utilizadores.FindAsync(userId);

            if (user == null)
                return RedirectToAction("Login");

            // Atualizar os campos
            user.Email = model.Email;
            user.Morada = model.Morada;
            user.Contacto = model.Contacto;

            // Atualizar password se preenchida
            if (!string.IsNullOrEmpty(model.Password))
            {
                user.Password = _passwordHasher.HashPassword(user, model.Password);
            }

            // Atualizar foto se houver upload
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{ProfileImage.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(fileStream);
                }

                user.FotoUrl = $"/uploads/{uniqueFileName}";
            }

            // Atualizar no banco
            _context.Utilizadores.Update(user);
            await _context.SaveChangesAsync();

            // Atualizar os Claims no cookie para refletir a nova foto imediatamente
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.TipoUser ?? "User"),
        new Claim("FotoUrl", user.FotoUrl ?? "/img/avatar.png")
    };
            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["MensagemSucesso"] = "Alterações salvas com sucesso!";
            return RedirectToAction("UserMenu");
        }

        public async Task<IActionResult> UserMenuFavoritos()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .Include(c => c.Favoritos)
                    .ThenInclude(f => f.Anuncio)
                        .ThenInclude(a => a.Modelo)
                            .ThenInclude(m => m.Marca)
                .Include(c => c.Favoritos)
                    .ThenInclude(f => f.Anuncio.Imagens)
                .FirstOrDefaultAsync(c => c.Id == userId);

            if (comprador == null)
                return RedirectToAction("Login", "Account");

            var vm = new FavoritosViewModel
            {
                Utilizador = new UtilizadorViewModel
                {
                    Nome = comprador.Utilizador.Nome,
                    FotolUrl = comprador.Utilizador.FotoUrl,
                },
                Favoritos = comprador.Favoritos.Select(f => f.Anuncio).ToList()
            };

            return View(vm);
        }

        // --- REGISTAR VENDEDOR ---
        [HttpGet]
        public async Task<IActionResult> RegisterVendedor()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login");

            // Verifica se já é vendedor ou tem pedido pendente
            var user = await _context.Utilizadores.FindAsync(int.Parse(userIdClaim));
            if (user == null || user.TipoUser == "Vendedor" || user.PedidoVendedorPendente)
            {
                // Redirecionar para o menu ou mostrar uma mensagem de erro
                TempData["MensagemErro"] = "Já é vendedor ou o seu pedido está em análise.";
                return RedirectToAction("UserMenu");
            }

            return View(new RegisterVendedorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterVendedor(RegisterVendedorViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login");

            int userId = int.Parse(userIdClaim);

            var user = await _context.Utilizadores
                .Include(u => u.CompradorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.TipoUser == "Vendedor" || user.PedidoVendedorPendente)
            {
                TempData["MensagemErro"] = "Já é vendedor ou o seu pedido está em análise.";
                return RedirectToAction("UserMenu");
            }

            // Apenas marca que há um pedido pendente
            user.PedidoVendedorPendente = true;
            user.RejeitadoVendedor = false;

            // Criar registro de pedido de vendedor, mas sem mudar TipoUser ainda
            var vendedor = new Vendedor
            {
                Id = user.Id, // FK para Utilizador
                TipoVendedor = model.TipoVendedor,
                NIF = model.NIF,
                Utilizador = user // <- importante para não gerar erro de FK nula
            };

            // Copiar propriedades do Comprador
            if (user.CompradorInfo != null)
            {
                var compradorProps = user.CompradorInfo.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var vendedorProps = vendedor.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var cProp in compradorProps)
                {
                    var vProp = vendedorProps.FirstOrDefault(p => p.Name == cProp.Name && p.PropertyType == cProp.PropertyType && p.CanWrite);
                    if (vProp != null)
                    {
                        vProp.SetValue(vendedor, cProp.GetValue(user.CompradorInfo));
                    }
                }
            }

            _context.Vendedores.Add(vendedor);
            await _context.SaveChangesAsync();

            // Define a mensagem de sucesso no ViewBag
            ViewBag.MensagemSucesso = "O seu pedido para se tornar vendedor foi submetido com sucesso e aguarda confirmação do administrador.";

            // Retorna a mesma view para mostrar o popup
            return View(model);
        }
    }
}
