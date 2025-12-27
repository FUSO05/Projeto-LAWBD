using AutoMarket.Models;
using AutoMarket.Models.ViewModels;
using AutoMarket.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;

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

            var user = await _context.Utilizadores
                .Include(u => u.CompradorInfo)
                    .ThenInclude(c => c.Favoritos)
                        .ThenInclude(f => f.Anuncio)
                            .ThenInclude(a => a.Modelo)
                                .ThenInclude(m => m.Marca)
                .Include(u => u.CompradorInfo)
                    .ThenInclude(c => c.Favoritos)
                        .ThenInclude(f => f.Anuncio.Imagens)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var favoritos = user.CompradorInfo?.Favoritos.Select(f => f.Anuncio).ToList() ?? new List<Anuncio>();

            var vm = new FavoritosViewModel
            {
                Utilizador = new UtilizadorViewModel
                {
                    Nome = user.Nome,
                    FotolUrl = user.FotoUrl ?? "/img/avatar.png",
                    TipoUser = user.TipoUser,
                    PedidoVendedorPendente = user.PedidoVendedorPendente
                },
                Favoritos = favoritos
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

            // Obter ID do utilizador logado
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login");

            int userId = int.Parse(userIdClaim);

            // Buscar o utilizador com CompradorInfo
            var user = await _context.Utilizadores
                .Include(u => u.CompradorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.TipoUser == "Vendedor" || user.PedidoVendedorPendente)
            {
                TempData["MensagemErro"] = "Já é vendedor ou o seu pedido está em análise.";
                return RedirectToAction("UserMenu");
            }

            // Marcar pedido pendente
            user.PedidoVendedorPendente = true;
            user.RejeitadoVendedor = false;

            // Criar o objeto Vendedor
            var vendedor = new Vendedor
            {
                TipoVendedor = model.TipoVendedor,
                NIF = model.NIF,
                Utilizador = user,          // EF Core cuida do FK
                Morada = user.Morada,       // Herda Morada do Utilizador
                Contacto = user.Contacto    // Herda Contacto do Utilizador
            };

            // Se houver dados específicos do Comprador, copie também
            if (user.CompradorInfo != null)
            {
                var compradorProps = user.CompradorInfo.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var vendedorProps = vendedor.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var cProp in compradorProps)
                {
                    var vProp = vendedorProps.FirstOrDefault(p =>
                        p.Name == cProp.Name &&
                        p.PropertyType == cProp.PropertyType &&
                        p.CanWrite);

                    if (vProp != null)
                    {
                        vProp.SetValue(vendedor, cProp.GetValue(user.CompradorInfo));
                    }
                }
            }

            _context.Vendedores.Add(vendedor);
            await _context.SaveChangesAsync();

            ViewBag.MensagemSucesso = "O seu pedido para se tornar vendedor foi submetido com sucesso e aguarda confirmação do administrador.";

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> UserMenuVisitas()
        {
            // Obter o ID do utilizador a partir dos Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Buscar o utilizador com todas as informações necessárias
            var user = await _context.Utilizadores
                .Include(u => u.CompradorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Buscar as visitas agendadas do comprador
            List<Reserva> visitas = new List<Reserva>();

            if (user.CompradorInfo != null)
            {
                visitas = await _context.Reservas
                    .Include(r => r.Anuncio)
                        .ThenInclude(a => a.Modelo)
                            .ThenInclude(m => m.Marca)
                    .Include(r => r.Anuncio.Imagens)
                    .Where(r => r.CompradorId == user.CompradorInfo.Id &&
                               r.Estado == "Agendada")
                    .OrderBy(r => r.DataHoraReserva)
                    .ToListAsync();
            }

            // ✅ Criar o UtilizadorViewModel corretamente
            var utilizadorViewModel = new UtilizadorViewModel
            {
                Nome = user.Nome,
                Email = user.Email,
                Morada = user.Morada,
                Contacto = user.Contacto,
                FotolUrl = user.FotoUrl ?? "/img/avatar.png",
                TipoUser = user.TipoUser,
                PedidoVendedorPendente = user.PedidoVendedorPendente
            };

            // ✅ Criar o ViewModel com UtilizadorViewModel em vez de Utilizador
            var vm = new VisitasViewModel
            {
                UtilizadorViewModel = utilizadorViewModel,  // Use esta propriedade
                Visitas = visitas,
                Anuncio = null
            };

            return View(vm);
        }

        // No VisitsController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (!User.Identity!.IsAuthenticated)
                return Unauthorized();

            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .FirstOrDefaultAsync(c => c.Utilizador.Email == User.Identity.Name);

            if (comprador == null)
                return NotFound();

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.Id == id && r.CompradorId == comprador.Id);

            if (reserva == null)
                return NotFound();

            // Verificar se ainda pode cancelar (ex: não permitir cancelar visitas muito próximas)
            if (reserva.DataHoraReserva.AddHours(-2) < DateTime.Now)
            {
                TempData["MensagemErro"] = "Não é possível cancelar visitas com menos de 2 horas de antecedência.";
                return RedirectToAction("UserMenuVisitas", "Account");
            }

            reserva.Estado = "Cancelada";
            await _context.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Visita cancelada com sucesso.";
            return RedirectToAction("UserMenuVisitas", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> Reagendar(int id)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .FirstOrDefaultAsync(c => c.Utilizador.Email == User.Identity.Name);

            if (comprador == null)
                return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio.Imagens)
                .FirstOrDefaultAsync(r => r.Id == id && r.CompradorId == comprador.Id);

            if (reserva == null)
                return NotFound();

            // Cancelar a reserva antiga
            reserva.Estado = "Cancelada";
            await _context.SaveChangesAsync();

            // Redirecionar para a página de agendamento com o mesmo anúncio
            TempData["MensagemInfo"] = "Reagendamento iniciado. Selecione uma nova data e hora.";
            return RedirectToAction("BookVisit", new { id = reserva.AnuncioId });
        }

        [HttpGet]
        public async Task<IActionResult> VendedorMenuVisitas()
        {
            // Obter o ID do utilizador a partir dos Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Buscar o utilizador com todas as informações necessárias
            var user = await _context.Utilizadores
                .Include(u => u.VendedorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.VendedorInfo == null)
                return RedirectToAction("Login", "Account");

            // Buscar as visitas agendadas para os anúncios do vendedor
            var visitas = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio.Imagens)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r => r.Anuncio.VendedorId == user.VendedorInfo.Id &&
                           (r.Estado == "Agendada" || r.Estado == "Confirmada"))
                .OrderBy(r => r.DataHoraReserva)
                .ToListAsync();

            // Criar o UtilizadorViewModel
            var utilizadorViewModel = new UtilizadorViewModel
            {
                Nome = user.Nome,
                Email = user.Email,
                Morada = user.Morada,
                Contacto = user.Contacto,
                FotolUrl = user.FotoUrl ?? "/img/avatar.png",
                TipoUser = user.TipoUser,
                PedidoVendedorPendente = user.PedidoVendedorPendente
            };

            // Criar o ViewModel
            var vm = new VisitasViewModel
            {
                UtilizadorViewModel = utilizadorViewModel,
                Visitas = visitas,
                Anuncio = null
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarVisita(int id)
        {
            if (!User.Identity!.IsAuthenticated)
                return Unauthorized();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var vendedor = await _context.Vendedores
                .Include(v => v.Utilizador)
                .FirstOrDefaultAsync(v => v.Utilizador.Id == userId);

            if (vendedor == null)
                return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .FirstOrDefaultAsync(r => r.Id == id && r.Anuncio.VendedorId == vendedor.Id);

            if (reserva == null)
                return NotFound();

            // Verificar se a visita ainda está agendada
            if (reserva.Estado != "Agendada")
            {
                TempData["MensagemErro"] = "Esta visita já foi processada.";
                return RedirectToAction("UserMenuVisitasVendedor", "Account");
            }

            // Confirmar a visita
            reserva.Estado = "Confirmada";
            await _context.SaveChangesAsync();

            // Opcional: Enviar email de confirmação ao comprador
            try
            {
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "VisitaConfirmada.html");
                var placeholders = new Dictionary<string, string>
        {
            { "Nome", reserva.Comprador.Utilizador.Nome },
            { "Veiculo", $"{reserva.Anuncio.Modelo?.Marca?.Nome} {reserva.Anuncio.Modelo?.Nome}" },
            { "Data", reserva.DataHoraReserva.ToString("dd/MM/yyyy") },
            { "Hora", reserva.DataHoraReserva.ToString("HH:mm") },
            { "Local", reserva.Anuncio.Localizacao ?? "N/A" }
        };

                await _emailService.EnviarEmailComTemplateAsync(
                    reserva.Comprador.Utilizador.Email,
                    "Visita Confirmada",
                    templatePath,
                    placeholders
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de confirmação: {ex.Message}");
            }

            TempData["MensagemSucesso"] = "Visita confirmada com sucesso!";
            return RedirectToAction("UserMenuVisitasVendedor", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecusarVisita(int id)
        {
            if (!User.Identity!.IsAuthenticated)
                return Unauthorized();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var vendedor = await _context.Vendedores
                .Include(v => v.Utilizador)
                .FirstOrDefaultAsync(v => v.Utilizador.Id == userId);

            if (vendedor == null)
                return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .FirstOrDefaultAsync(r => r.Id == id && r.Anuncio.VendedorId == vendedor.Id);

            if (reserva == null)
                return NotFound();

            // Verificar se a visita ainda está agendada
            if (reserva.Estado != "Agendada")
            {
                TempData["MensagemErro"] = "Esta visita já foi processada.";
                return RedirectToAction("UserMenuVisitasVendedor", "Account");
            }

            // Recusar a visita
            reserva.Estado = "Recusada";
            await _context.SaveChangesAsync();

            // Opcional: Enviar email de notificação ao comprador
            try
            {
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "VisitaRecusada.html");
                var placeholders = new Dictionary<string, string>
        {
            { "Nome", reserva.Comprador.Utilizador.Nome },
            { "Veiculo", $"{reserva.Anuncio.Modelo?.Marca?.Nome} {reserva.Anuncio.Modelo?.Nome}" },
            { "Data", reserva.DataHoraReserva.ToString("dd/MM/yyyy") },
            { "Hora", reserva.DataHoraReserva.ToString("HH:mm") }
        };

                await _emailService.EnviarEmailComTemplateAsync(
                    reserva.Comprador.Utilizador.Email,
                    "Visita Recusada",
                    templatePath,
                    placeholders
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de recusa: {ex.Message}");
            }

            TempData["MensagemSucesso"] = "Visita recusada.";
            return RedirectToAction("UserMenuVisitasVendedor", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> HistoricoVisitasVendedor()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var user = await _context.Utilizadores
                .Include(u => u.VendedorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.VendedorInfo == null)
                return RedirectToAction("Login", "Account");

            // Buscar todas as visitas (incluindo canceladas, recusadas e concluídas)
            var visitas = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio.Imagens)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r => r.Anuncio.VendedorId == user.VendedorInfo.Id)
                .OrderByDescending(r => r.DataHoraReserva)
                .ToListAsync();

            var utilizadorViewModel = new UtilizadorViewModel
            {
                Nome = user.Nome,
                Email = user.Email,
                Morada = user.Morada,
                Contacto = user.Contacto,
                FotolUrl = user.FotoUrl ?? "/img/avatar.png",
                TipoUser = user.TipoUser,
                PedidoVendedorPendente = user.PedidoVendedorPendente
            };

            var vm = new VisitasViewModel
            {
                UtilizadorViewModel = utilizadorViewModel,
                Visitas = visitas,
                Anuncio = null
            };

            return View(vm);
        }

        [Authorize]
        public async Task<IActionResult> UserMenuReservas()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Utilizadores
                .FirstOrDefaultAsync(u => u.Id == userId);

            var reservasQuery = _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Imagens)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r => r.CompradorId == user.Id &&
                       (r.Estado == "Pendente" || r.Estado == "Reservado" || r.Estado == "Recusada"))
                .OrderByDescending(r => r.DataHoraReserva) // <--- Ordenar da mais recente para a mais antiga
                .AsQueryable();

            var reservas = await reservasQuery.ToListAsync();

            var model = new ReservasViewModel
            {
                UtilizadorViewModel = new UtilizadorViewModel
                {
                    Nome = user.Nome,
                    TipoUser = user.TipoUser
                },
                Reservas = reservas
            };

            return View(model);
        }


        [Authorize]
        public async Task<IActionResult> VendedorMenuReservas()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Utilizadores
                .FirstOrDefaultAsync(u => u.Id == userId);

            var reservasQuery = _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Imagens)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r => r.Anuncio.VendedorId == user.Id && (r.Estado == "Pendente" || r.Estado == "Reservado"))
                .AsQueryable();

            var reservas = await reservasQuery.ToListAsync();

            var model = new ReservasViewModel
            {
                UtilizadorViewModel = new UtilizadorViewModel
                {
                    Nome = user.Nome,
                    TipoUser = user.TipoUser
                },
                Reservas = reservas
            };

            return View(model);
        }


        // AprovarReserva
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprovarReserva(int reservaId)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva == null || reserva.Comprador?.Utilizador == null || reserva.Anuncio?.Modelo?.Marca == null)
                return NotFound();

            reserva.Estado = "Reservado";
            reserva.Anuncio.Estado = "Reservado";
            await _context.SaveChangesAsync();

            // Enviar email usando template
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "ReservaAprovada.html");
            var placeholders = new Dictionary<string, string>
                {
                    { "Nome", reserva.Comprador.Utilizador.Nome },
                    { "Veiculo", $"{reserva.Anuncio.Modelo.Marca.Nome} {reserva.Anuncio.Modelo.Nome}" },
                    { "Data", reserva.DataHoraReserva.ToString("dd/MM/yyyy") },
                    { "Hora", reserva.DataHoraReserva.ToString("HH:mm") },
                    { "Local", reserva.Anuncio.Localizacao ?? "N/A" }
                };

            await _emailService.EnviarEmailComTemplateAsync(reserva.Comprador.Utilizador.Email, "Reserva Aprovada - AutoMarket", templatePath, placeholders);

            return RedirectToAction("VendedorMenuReservas");
        }

        // RecusarReserva
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecusarReserva(int reservaId)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva == null || reserva.Comprador?.Utilizador == null || reserva.Anuncio?.Modelo?.Marca == null)
                return NotFound();

            reserva.Estado = "Recusada";
            reserva.Anuncio.Estado = "Disponivel";
            await _context.SaveChangesAsync();

            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "ReservaRecusada.html");
            var placeholders = new Dictionary<string, string>
                {
                    { "Nome", reserva.Comprador.Utilizador.Nome },
                    { "Veiculo", $"{reserva.Anuncio.Modelo.Marca.Nome} {reserva.Anuncio.Modelo.Nome}" },
                    { "Data", reserva.DataHoraReserva.ToString("dd/MM/yyyy") },
                    { "Hora", reserva.DataHoraReserva.ToString("HH:mm") }
                };

            await _emailService.EnviarEmailComTemplateAsync(reserva.Comprador.Utilizador.Email, "Reserva Recusada - AutoMarket", templatePath, placeholders);

            return RedirectToAction("VendedorMenuReservas");
        }
    }
}


