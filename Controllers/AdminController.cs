using AutoMarket.Models;
using AutoMarket.Models.ViewModels;
using AutoMarket.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace AutoMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Utilizador> _passwordHasher;
        private readonly EmailService _emailService; // Adicionar injeção
        private const int PageSize = 20; // 20 utilizadores por página

        public AdminController(AppDbContext context, EmailService emailService, IPasswordHasher<Utilizador> passwordHasher) : base(context)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
        }

        public async Task<IActionResult> GerirUtilizadores(
    string? searchTerm,
    string? typeFilter,
    string? sortField,
    string? sortOrder,
    int page = 1)
        {
            var query = _context.Utilizadores.AsQueryable();

            // Filtro por pesquisa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nome.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            // Filtro por tipo
            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Todos")
            {
                query = query.Where(u => u.TipoUser == typeFilter);
            }

            // Ordenação
            sortField ??= "Nome";      // campo padrão
            sortOrder ??= "asc";       // ordem padrão

            query = (sortField, sortOrder) switch
            {
                ("Nome", "asc") => query.OrderBy(u => u.Nome),
                ("Nome", "desc") => query.OrderByDescending(u => u.Nome),
                ("Email", "asc") => query.OrderBy(u => u.Email),
                ("Email", "desc") => query.OrderByDescending(u => u.Email),
                ("TipoUser", "asc") => query.OrderBy(u => u.TipoUser),
                ("TipoUser", "desc") => query.OrderByDescending(u => u.TipoUser),
                _ => query.OrderBy(u => u.Nome)
            };

            // Paginação
            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);

            var users = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var model = new GerirUtilizadoresAdminViewModel
            {
                Utilizadores = users
            };

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.SortField = sortField;
            ViewBag.SortOrder = sortOrder;
            ViewBag.IsPedidosVendedorPage = false; // ← ADD THIS LINE

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> FiltrarUtilizadores(string? searchTerm, string? typeFilter, string? sortField, string? sortOrder, int page = 1)
        {
            var query = _context.Utilizadores.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(u => u.Nome.Contains(searchTerm) || u.Email.Contains(searchTerm));

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "Todos")
                query = query.Where(u => u.TipoUser == typeFilter);

            sortField ??= "Nome";
            sortOrder ??= "asc";

            query = (sortField, sortOrder) switch
            {
                ("Nome", "asc") => query.OrderBy(u => u.Nome),
                ("Nome", "desc") => query.OrderByDescending(u => u.Nome),
                ("Email", "asc") => query.OrderBy(u => u.Email),
                ("Email", "desc") => query.OrderByDescending(u => u.Email),
                ("TipoUser", "asc") => query.OrderBy(u => u.TipoUser),
                ("TipoUser", "desc") => query.OrderByDescending(u => u.TipoUser),
                _ => query.OrderBy(u => u.Nome)
            };

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);

            var users = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Json(new { users, totalPages });
        }

        // --- NOVA ACTION: Gerir Pedidos de Vendedor ---
        public async Task<IActionResult> GerirPedidosVendedor(string? searchTerm, int page = 1)
        {
            var query = _context.Utilizadores
                .Where(u => u.PedidoVendedorPendente)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(u => u.Nome.Contains(searchTerm) || u.Email.Contains(searchTerm));

            var users = await query.ToListAsync();

            return View(new GerirUtilizadoresAdminViewModel
            {
                Utilizadores = users
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarVendedor([FromBody] ConfirmarVendedorViewModel model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Dados inválidos." });

            var user = await _context.Utilizadores.FindAsync(model.UserId);

            if (user == null || !user.PedidoVendedorPendente)
                return NotFound(new { success = false, message = "Utilizador ou pedido de vendedor não encontrado/pendente." });

            // --- BUSCAR ADMINISTRADOR LOGADO ---
            int userIdLogado = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var admin = await _context.Administradores
                .Include(a => a.Utilizador)
                .FirstOrDefaultAsync(a => a.Utilizador.Id == userIdLogado);

            if (admin == null)
                return Unauthorized(new { success = false, message = "Administrador não encontrado." });

            string emailTemplate = "";
            string emailSubject = "";
            string message = "";

            if (model.Aprovar)
            {
                // ===== APROVAR PEDIDO =====
                user.TipoUser = "Vendedor";
                user.PedidoVendedorPendente = false;
                user.RejeitadoVendedor = false;

                var vendedorExistente = await _context.Vendedores
                    .Include(v => v.Utilizador)
                    .FirstOrDefaultAsync(v => v.Utilizador.Id == model.UserId);

                if (vendedorExistente == null)
                {
                    var novoVendedor = new Vendedor
                    {
                        Utilizador = user,
                        AprovadoPorId = admin.Id
                    };
                    _context.Vendedores.Add(novoVendedor);
                }
                else
                {
                    vendedorExistente.AprovadoPorId = admin.Id;
                    _context.Vendedores.Update(vendedorExistente);
                }

                await RegistarAcaoAdmin($"Aprovou o pedido de vendedor do utilizador {user.Nome}", user.Id);

                emailSubject = "Parabéns! O Seu Pedido de Vendedor Foi Aceite";
                emailTemplate = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "VendedorAceite.html");
                message = "Vendedor aprovado com sucesso.";
            }
            else
            {
                // ===== RECUSAR PEDIDO =====
                user.TipoUser = "Comprador";
                user.PedidoVendedorPendente = false;
                user.RejeitadoVendedor = true;

                // Remover o registro de Vendedor se existir
                var vendedorExistente = await _context.Vendedores
                    .Include(v => v.Utilizador)
                    .FirstOrDefaultAsync(v => v.Utilizador.Id == model.UserId);

                if (vendedorExistente != null)
                {
                    _context.Vendedores.Remove(vendedorExistente);
                }

                await RegistarAcaoAdmin($"Rejeitou o pedido de vendedor do utilizador {user.Nome}", user.Id);

                emailSubject = "Atualização do Seu Pedido de Vendedor";
                emailTemplate = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "VendedorRecusado.html");
                message = "Pedido de vendedor recusado com sucesso.";
            }

            _context.Utilizadores.Update(user);
            await _context.SaveChangesAsync();

            // Enviar email de notificação
            try
            {
                var placeholders = new Dictionary<string, string>
        {
            { "Nome", user.Nome }
        };

                await _emailService.EnviarEmailComTemplateAsync(
                    user.Email,
                    emailSubject,
                    emailTemplate,
                    placeholders
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao enviar email: {ex.Message}");
            }

            return Json(new
            {
                success = true,
                message,
                user = new
                {
                    id = user.Id,
                    nome = user.Nome,
                    email = user.Email,
                    tipoUser = user.TipoUser,
                    ativo = user.Ativo
                }
            });
        }

        public async Task<IActionResult> Estatisticas()
        {
            var hoje = DateTime.Today;
            var inicioEsteAno = new DateTime(hoje.Year, 1, 1);
            var fimEsteAno = new DateTime(hoje.Year, 12, 31, 23, 59, 59);
            var inicioAnoPassado = new DateTime(hoje.Year - 1, 1, 1);
            var fimAnoPassado = new DateTime(hoje.Year - 1, 12, 31, 23, 59, 59);

            // Buscar vendas do ano passado agrupadas por mês
            var vendasAnoPassadoPorMes = await _context.Reservas
                .Where(r => r.Estado == "Pago" &&
                            r.DataHoraReserva >= inicioAnoPassado &&
                            r.DataHoraReserva <= fimAnoPassado)
                .GroupBy(r => r.DataHoraReserva.Month)
                .Select(g => new { Mes = g.Key, Total = g.Count() })
                .ToListAsync();

            // Buscar vendas deste ano agrupadas por mês
            var vendasEsteAnoPorMes = await _context.Reservas
                .Where(r => r.Estado == "Pago" &&
                            r.DataHoraReserva >= inicioEsteAno &&
                            r.DataHoraReserva <= fimEsteAno)
                .GroupBy(r => r.DataHoraReserva.Month)
                .Select(g => new { Mes = g.Key, Total = g.Count() })
                .ToListAsync();

            // Criar arrays de 12 meses (Janeiro a Dezembro)
            var vendasMensaisAnoPassado = new int[12];
            var vendasMensaisEsteAno = new int[12];

            // Preencher com os dados
            foreach (var venda in vendasAnoPassadoPorMes)
            {
                vendasMensaisAnoPassado[venda.Mes - 1] = venda.Total;
            }

            foreach (var venda in vendasEsteAnoPorMes)
            {
                vendasMensaisEsteAno[venda.Mes - 1] = venda.Total;
            }

            var model = new AdminEstatisticasViewModel
            {
                // Utilizadores
                TotalCompradores = await _context.Utilizadores.CountAsync(u => u.TipoUser == "Comprador"),
                TotalVendedores = await _context.Utilizadores.CountAsync(u => u.TipoUser == "Vendedor"),

                // Anúncios
                TotalAnunciosAtivos = await _context.Anuncios.CountAsync(a => a.Ativo),

                // Vendas (assumindo Estado == "Pago")
                VendasHoje = await _context.Reservas.CountAsync(r =>
                    r.Estado == "Pago" && r.DataHoraReserva.Date == hoje),

                VendasAnoPassado = vendasMensaisAnoPassado.Sum(),
                VendasEsteAno = vendasMensaisEsteAno.Sum(),

                // Vendas mensais
                VendasMensaisAnoPassado = vendasMensaisAnoPassado.ToList(),
                VendasMensaisEsteAno = vendasMensaisEsteAno.ToList(),

                // Top Marcas
                TopMarcas = await _context.Reservas
                    .Where(r => r.Estado == "Pago")
                    .Include(r => r.Anuncio)
                        .ThenInclude(a => a.Modelo)
                            .ThenInclude(m => m.Marca)
                    .GroupBy(r => r.Anuncio.Modelo.Marca.Nome)
                    .Select(g => new TopItemViewModel
                    {
                        Nome = g.Key,
                        Total = g.Count()
                    })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .ToListAsync(),

                // Top Modelos
                TopModelos = await _context.Reservas
                    .Where(r => r.Estado == "Pago")
                    .Include(r => r.Anuncio)
                        .ThenInclude(a => a.Modelo)
                    .GroupBy(r => r.Anuncio.Modelo.Nome)
                    .Select(g => new TopItemViewModel
                    {
                        Nome = g.Key,
                        Total = g.Count()
                    })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .ToListAsync()
            };

            ViewBag.IsEstatisticasPage = true;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Configuracoes()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var utilizador = await _context.Utilizadores
                .Where(u => u.Id == userId)
                .Select(u => new UtilizadorViewModel
                {
                    Nome = u.Nome,
                    Email = u.Email,
                    TipoUser = u.TipoUser,
                    Contacto = u.Contacto,
                    Morada = u.Morada,
                    FotolUrl = u.FotoUrl
                })
                .FirstOrDefaultAsync();

            if (utilizador == null)
                return NotFound();

            return View(utilizador);
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtualizarFoto(IFormFile ProfileImage)
        {
            if (ProfileImage == null || ProfileImage.Length == 0)
            {
                TempData["Erro"] = "Nenhuma imagem selecionada.";
                return RedirectToAction("Configuracoes");
            }

            // Validações
            var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extensao = Path.GetExtension(ProfileImage.FileName).ToLower();

            if (!extensoesPermitidas.Contains(extensao))
            {
                TempData["Erro"] = "Formato de imagem inválido.";
                return RedirectToAction("Configuracoes");
            }

            if (ProfileImage.Length > 2 * 1024 * 1024)
            {
                TempData["Erro"] = "A imagem não pode exceder 2MB.";
                return RedirectToAction("Configuracoes");
            }

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var utilizador = await _context.Utilizadores.FindAsync(userId);
            if (utilizador == null)
                return RedirectToAction("Configuracoes");

            // Pasta destino
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/perfis");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Nome único
            var fileName = $"admin_{userId}_{Guid.NewGuid()}{extensao}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ProfileImage.CopyToAsync(stream);
            }

            // Atualizar utilizador
            utilizador.FotoUrl = "/uploads/" + fileName;
            _context.Utilizadores.Update(utilizador);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Foto atualizada com sucesso.";
            return RedirectToAction("Configuracoes");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarAdmin(string Nome, string Email, string Password)
        {
            if (!await IsSuperAdmin())
                return Forbid();

            if (string.IsNullOrWhiteSpace(Nome) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                TempData["Erro"] = "Todos os campos são obrigatórios.";
                return RedirectToAction("Configuracoes");
            }

            if (await _context.Utilizadores.AnyAsync(u => u.Email == Email))
            {
                TempData["Erro"] = "Já existe um utilizador com esse email.";
                return RedirectToAction("Configuracoes");
            }

            // Criar utilizador
            var utilizador = new Utilizador
            {
                Nome = Nome,
                Email = Email,
                TipoUser = "Admin",
                Ativo = true,
                Password = _passwordHasher.HashPassword(null!, Password),
                EmailConfirmed = true
            };

            _context.Utilizadores.Add(utilizador);
            await _context.SaveChangesAsync();

            // Criar administrador
            var admin = new Administrador
            {
                Utilizador = utilizador,
                IsSuperAdmin = false
            };

            _context.Administradores.Add(admin);
            await _context.SaveChangesAsync();

            // Email de boas-vindas
            try
            {
                var templatePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "EmailTemplates",
                    "NovoAdmin.html"
                );

                await _emailService.EnviarEmailComTemplateAsync(
                    Email,
                    "Conta de Administrador Criada",
                    templatePath,
                    new Dictionary<string, string>
                    {
                { "Nome", Nome },
                { "Email", Email },
                { "Password", Password }
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao enviar email admin: " + ex.Message);
            }

            return RedirectToAction("Configuracoes");
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

            // ✅ Adicionar mensagem de sucesso para o pop-up
            TempData["Mensagem"] = "As suas definições foram salvas com sucesso!";
            TempData["TipoMensagem"] = "sucesso"; // pode ser "erro", "aviso", etc.

            return RedirectToAction("Configuracoes");
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CriarAdmin()
        {
            if (!await IsSuperAdmin())
                return Forbid();

            return View();
        }

        private async Task<bool> IsSuperAdmin()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return await _context.Administradores
                .AnyAsync(a => a.Utilizador.Id == userId && a.IsSuperAdmin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BloquearUtilizador(int id, string motivo)
        {
            var user = await _context.Utilizadores.Include(u => u.Bloqueios).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.Ativo = false;

            // Adicionar novo registo de bloqueio
            var bloqueio = new Bloqueio
            {
                UtilizadorId = id,
                Motivo = motivo,
                DataBloqueio = DateTime.Now
            };

            _context.Bloqueios.Add(bloqueio);

            await RegistarAcaoAdmin($"Bloqueou o utilizador {user.Nome} com motivo: {motivo}", user.Id);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtivarUtilizador(int id)
        {
            var user = await _context.Utilizadores.FindAsync(id);
            if (user == null) return NotFound();

            user.Ativo = true;
            await _context.SaveChangesAsync();

            await RegistarAcaoAdmin($"Ativou o utilizador {user.Nome}", user.Id);

            return Json(new { success = true });
        }

        private async Task RegistarAcaoAdmin(string acao, int? utilizadorAlvoId = null, int? anuncioAlvoId = null)
        {
            int adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var historico = new HistoricoAdmin
            {
                AdminId = adminId,
                UtilizadorAlvoId = utilizadorAlvoId,
                AnuncioAlvoId = anuncioAlvoId,
                Acao = acao,
                DataAcao = DateTime.Now
            };

            _context.HistoricoAdmins.Add(historico);
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> Historico(int page = 1)
        {
            const int PageSize = 20;

            var query = _context.HistoricoAdmins
                .Include(h => h.Admin)
                    .ThenInclude(a => a.Utilizador)
                .Include(h => h.UtilizadorAlvo)
                .Include(h => h.AnuncioAlvo)
                .OrderByDescending(h => h.DataAcao)
                .AsQueryable();

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);

            var historicos = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(historicos);
        }

    }
}

