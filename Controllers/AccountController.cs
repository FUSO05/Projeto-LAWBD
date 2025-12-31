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

            var vendedorExistente = await _context.Vendedores
                .FirstOrDefaultAsync(v => v.UtilizadorId == userId);

            if (vendedorExistente != null)
            {
                TempData["MensagemErro"] = "Já existe um registo de vendedor associado a esta conta.";
                return RedirectToAction("UserMenu");
            }

            var vendedor = new Vendedor
            {
                TipoVendedor = model.TipoVendedor,
                NIF = model.NIF,
                UtilizadorId = userId,
                Morada = user.Morada,
                Contacto = user.Contacto
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var user = await _context.Utilizadores
                .Include(u => u.CompradorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            List<Reserva> visitas = new List<Reserva>();
            DateTime agora = DateTime.Now;

            if (user.CompradorInfo != null)
            {
                visitas = await _context.Reservas
                    .Include(r => r.Anuncio)
                        .ThenInclude(a => a.Modelo)
                            .ThenInclude(m => m.Marca)
                    .Include(r => r.Anuncio.Imagens)
                    .Where(r =>
                        r.CompradorId == user.CompradorInfo.Id &&
                        (r.Estado == "Pendente" || r.Estado == "Confirmada") && // Apenas pendente ou confirmada
                        (r.PrazoExpiracao == null || r.PrazoExpiracao > agora)) // Não expiradas
                    .OrderBy(r => r.DataHoraReserva)
                    .ToListAsync();
            }

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
                Visitas = visitas
            };

            return View(vm);
        }


        [HttpGet]
        public async Task<IActionResult> UserMenuHistorico()
        {
            // Obter ID do utilizador
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Buscar o utilizador
            var user = await _context.Utilizadores
                .Include(u => u.CompradorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Buscar todas as visitas do comprador, incluindo confirmadas e recusadas
            List<Reserva> visitas = new List<Reserva>();
            if (user.CompradorInfo != null)
            {
                visitas = await _context.Reservas
                    .Include(r => r.Anuncio).ThenInclude(a => a.Modelo).ThenInclude(m => m.Marca)
                    .Include(r => r.Anuncio.Imagens)
                    .Where(r => r.CompradorId == user.CompradorInfo.Id &&
                                (r.Estado == "Confirmada" || r.Estado == "Recusada" || r.Estado == "Cancelada"))
                    .OrderByDescending(r => r.DataHoraReserva)
                    .ToListAsync();
            }

            // Criar UtilizadorViewModel
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

            // Criar ViewModel
            var vm = new VisitasViewModel
            {
                UtilizadorViewModel = utilizadorViewModel,
                Visitas = visitas
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

            return RedirectToAction("UserMenuVisitas", "Account");
        }

        [HttpGet]
        public async Task<IActionResult> ReagendarVisita(int id)
        {
            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .FirstOrDefaultAsync(c => c.Utilizador.Email == User.Identity!.Name);

            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Imagens)
                .Include(r => r.Anuncio.Modelo)
                    .ThenInclude(m => m.Marca)
                .FirstOrDefaultAsync(r => r.Id == id && r.CompradorId == comprador.Id);

            if (reserva == null)
                return NotFound();

            var vm = new ReagendarVisitaViewModel
            {
                ReservaOriginalId = reserva.Id,
                AnuncioId = reserva.AnuncioId,
                DataHoraAtual = reserva.DataHoraReserva,
                Anuncio = reserva.Anuncio
            };

            return View("ReagendarVisita", vm); // pode reutilizar a view BookVisit
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarReagendamento(
     int reservaOriginalId,
     int anuncioId,
     string data,
     string hora)
        {
            var reservaOriginal = await _context.Reservas.FindAsync(reservaOriginalId);
            if (reservaOriginal == null) return NotFound();

            if (!DateTime.TryParse($"{data} {hora}", out DateTime novaDataHora))
            {
                TempData["MensagemErro"] = "Data ou hora inválida.";
                return RedirectToAction("ReagendarVisita", "Account", new { id = reservaOriginalId });
            }

            // Verificar se já existe reserva confirmada ou pendente para o novo horário
            var existeConflito = await _context.Reservas
                .Where(r => r.AnuncioId == anuncioId &&
                            r.DataHoraReserva == novaDataHora &&
                            (r.Estado == "Confirmada" || r.Estado == "Agendada"))
                .AnyAsync();

            if (existeConflito)
            {
                TempData["MensagemErro"] = "O horário escolhido já não está disponível.";
                return RedirectToAction("ReagendarVisita", new { id = reservaOriginalId });
            }

            // Criar nova reserva como Pendente (não cancela a antiga)
            var novaReserva = new Reserva
            {
                AnuncioId = anuncioId,
                CompradorId = reservaOriginal.CompradorId,
                DataHoraReserva = novaDataHora,
                Estado = "Pendente"
            };

            _context.Reservas.Add(novaReserva);
            await _context.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Sua solicitação de reagendamento foi enviada. O vendedor precisa aprovar a nova data.";
            return RedirectToAction("UserMenuVisitas", "Account");
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

            DateTime agora = DateTime.Now;

            // Buscar as visitas nos veículos que pertencem a este vendedor
            var visitas = await _context.Reservas
                .Include(r => r.Comprador).ThenInclude(c => c.Utilizador) // Info do comprador para o card
                .Include(r => r.Anuncio).ThenInclude(a => a.Modelo).ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio.Imagens)
                .Where(r => r.Anuncio.VendedorId == userId && // Garante que o anúncio é do vendedor logado
                            (r.Estado == "Pendente" || r.Estado == "Confirmada") && // Apenas estas interessam no menu ativo
            r.Estado != "Pago" &&
                            (r.PrazoExpiracao == null || r.PrazoExpiracao > agora))
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

        [HttpGet]
        public async Task<IActionResult> VendedorMenuHistorico()
        {
            // Obter ID do utilizador a partir dos Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            // Buscar o utilizador com info de vendedor
            var user = await _context.Utilizadores
                .Include(u => u.VendedorInfo)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.VendedorInfo == null)
                return RedirectToAction("Login", "Account");

            // Buscar todas as visitas aos veículos do vendedor, incluindo confirmadas e recusadas
            var visitas = await _context.Reservas
                .Include(r => r.Comprador).ThenInclude(c => c.Utilizador)
                .Include(r => r.Anuncio).ThenInclude(a => a.Modelo).ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio.Imagens)
                .Where(r => r.Anuncio.VendedorId == userId &&
                            (r.Estado == "Confirmada" || r.Estado == "Recusada"))
                .OrderByDescending(r => r.DataHoraReserva)
                .ToListAsync();

            // Criar UtilizadorViewModel
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

            // Criar ViewModel
            var vm = new VisitasViewModel
            {
                UtilizadorViewModel = utilizadorViewModel,
                Visitas = visitas
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
        .Include(r => r.Anuncio).ThenInclude(a => a.Modelo).ThenInclude(m => m.Marca)
        .Include(r => r.Comprador).ThenInclude(c => c.Utilizador)
        .FirstOrDefaultAsync(r => r.Id == id && r.Anuncio.VendedorId == vendedor.Id);

            if (reserva == null || reserva.Estado != "Pendente")
            {
                return RedirectToAction("VendedorMenuVisitas");
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
                Console.WriteLine($"Tentando enviar email para {reserva.Comprador.Utilizador.Email}");
                await _emailService.EnviarEmailComTemplateAsync(
                    reserva.Comprador.Utilizador.Email,
                    "Visita Confirmada",
                    templatePath,
                    placeholders
                );
                Console.WriteLine($"Email enviado para {reserva.Comprador.Utilizador.Email}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de confirmação: {ex.Message}");
            }

            return RedirectToAction("VendedorMenuVisitas", "Account");
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
        .Include(r => r.Anuncio).ThenInclude(a => a.Modelo).ThenInclude(m => m.Marca)
        .Include(r => r.Comprador).ThenInclude(c => c.Utilizador)
        .FirstOrDefaultAsync(r => r.Id == id && r.Anuncio.VendedorId == vendedor.Id);

            if (reserva == null || reserva.Estado != "Pendente") return NotFound();

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
                Console.WriteLine($"Tentando enviar email de recusa para {reserva.Comprador.Utilizador.Email}");
                await _emailService.EnviarEmailComTemplateAsync(
                    reserva.Comprador.Utilizador.Email,
                    "Visita Recusada",
                    templatePath,
                    placeholders
                );
                Console.WriteLine($"Email de recusa enviado para {reserva.Comprador.Utilizador.Email}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email de recusa: {ex.Message}");
            }

            return RedirectToAction("VendedorMenuVisitas", "Account");
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
            await ExpirarReservasAsync(); 

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
                .Include(r => r.Anuncio) 
                    .ThenInclude(a => a.Vendedor)
                        .ThenInclude(v => v.Utilizador)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r =>
                    r.CompradorId == user.Id &&
                    (r.Estado == "Aguarde" ||
                     r.Estado == "Reservado"))
                .OrderByDescending(r => r.DataHoraReserva);

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
        public async Task<IActionResult> UserMenuHistoricoReservas()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var user = await _context.Utilizadores
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var reservas = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Imagens)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Vendedor) // Inclui o vendedor
                        .ThenInclude(v => v.Utilizador)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r =>
                    r.CompradorId == user.Id && // Aqui filtra reservas do usuário comprador
                    (
                        r.Estado == "Recusada" ||
                        r.Estado == "Cancelada" ||
                        r.Estado == "Expirada" ||
                        r.Estado == "Pago" ||
                        r.Estado == "Concluída"
                    ))
                .OrderByDescending(r => r.Estado == "Pago" || r.Estado == "Concluída")
                .ThenByDescending(r => r.DataHoraReserva)
                .ToListAsync();


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
            await ExpirarReservasAsync(); 

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
                .Where(r =>
                    r.Anuncio.VendedorId == user.Id &&
                    (r.Estado == "Aguarde" ||
                     r.Estado == "Reservado"))
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
        public async Task<IActionResult> VendedorMenuHistoricoReservas()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);

            var user = await _context.Utilizadores
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.TipoUser != "Vendedor")
                return RedirectToAction("Login", "Account");

            var reservas = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Imagens)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r =>
                    r.Anuncio.VendedorId == user.Id &&
                    (
                        r.Estado == "Recusada" ||
                        r.Estado == "Cancelada" ||
                        r.Estado == "Expirada" ||
                        r.Estado == "Pago" ||
                        r.Estado == "Concluída"
                    ))
                .OrderByDescending(r => r.Estado == "Pago" || r.Estado == "Concluída") // Prioriza Pago/Concluída
                .ThenByDescending(r => r.DataHoraReserva) // Depois ordena por data
                .ToListAsync();

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


        private async Task ExpirarReservasAsync()
        {
            var agora = DateTime.Now;

            var reservasExpiradas = await _context.Reservas
                .Include(r => r.Anuncio)
                .Where(r =>
                    r.PrazoExpiracao != null &&
                    r.PrazoExpiracao < agora &&
                    (r.Estado == "Pendente" ))
                .ToListAsync();

            foreach (var reserva in reservasExpiradas)
            {
                reserva.Estado = "Expirada";

                // Se estava reservada, volta a ficar disponível
                if (reserva.Anuncio != null)
                {
                    reserva.Anuncio.Estado = "Disponivel";
                }
            }

            await _context.SaveChangesAsync();
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReserva(int id) // O nome do parâmetro deve ser 'id' para bater com a rota padrão
        {
            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
                return NotFound();

            // Lógica de cancelamento
            reserva.Estado = "Cancelada";

            if (reserva.Anuncio != null)
            {
                reserva.Anuncio.Estado = "Disponivel";
            }

            await _context.SaveChangesAsync();

            // Redireciona de volta para a lista de reservas do utilizador
            return RedirectToAction("UserMenuReservas");
        }

        [Authorize]
        public async Task<IActionResult> UserMenuCompras()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Utilizadores
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Buscar apenas compras pagas
            var comprasQuery = _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Imagens)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Vendedor)
                        .ThenInclude(v => v.Utilizador)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .Where(r =>
                    r.CompradorId == user.Id &&
                    r.Estado == "Pago")  // só compras pagas
                .OrderByDescending(r => r.DataHoraReserva);

            var compras = await comprasQuery.ToListAsync();

            var model = new ReservasViewModel
            {
                UtilizadorViewModel = new UtilizadorViewModel
                {
                    Nome = user.Nome,
                    TipoUser = user.TipoUser
                },
                Reservas = compras
            };

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> VendedorMenuAnuncios()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var user = await _context.Utilizadores.FirstAsync(u => u.Id == userId);

            var anuncios = await _context.Anuncios
                .Include(a => a.Imagens)
                .Include(a => a.Modelo)
                    .ThenInclude(m => m.Marca)
                .Include(a => a.Reservas) 
                .Where(a => a.VendedorId == userId)
                .ToListAsync();

            var anunciosComEstado = anuncios.Select(a =>
            {
                string estadoCarro = "Disponível";

                if (!a.Ativo)
                    estadoCarro = "Desativado";
                else if (a.Reservas.Any(r => r.Estado == "Pago"))
                    estadoCarro = "Vendido";
                else if (a.Reservas.Any(r => r.Estado == "Reservado" || r.Estado == "Pendente"))
                    estadoCarro = "Reservado";

                return new AnuncioComEstadoViewModel
                {
                    Anuncio = a,
                    EstadoCarro = estadoCarro,
                    TotalReservas = a.Reservas.Count
                };
            }).ToList();

            var model = new VendedorAnunciosViewModel
            {
                UtilizadorViewModel = new UtilizadorViewModel
                {
                    Nome = user.Nome,
                    TipoUser = user.TipoUser
                },
                Anuncios = anunciosComEstado
            };

            return View(model);
        }


    }
}