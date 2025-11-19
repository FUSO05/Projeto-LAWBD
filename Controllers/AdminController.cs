using AutoMarket.Models;
using AutoMarket.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AutoMarket.Services;

namespace AutoMarket.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService; // Adicionar injeção
        private const int PageSize = 20; // 20 utilizadores por página

        public AdminController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
        public async Task<IActionResult> GerirPedidosVendedor(
            string? searchTerm,
            int page = 1)
        {
            var query = _context.Utilizadores
                // Filtra APENAS pelos utilizadores com pedido pendente
                .Where(u => u.PedidoVendedorPendente)
                // Inclui o objeto Vendedor para ver os detalhes do pedido
                .Include(u => u.VendedorInfo) // Assumindo que você tem uma propriedade de navegação chamada VendedorInfo
                .AsQueryable();

            // Filtro por pesquisa (apenas nome ou email)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nome.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            // Ordenação (opcional, mas bom para ordenar por data de pedido se existisse)
            query = query.OrderBy(u => u.DataRegisto); // Ordenar por quem se registou primeiro

            // Paginação
            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);

            var users = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var model = new GerirUtilizadoresAdminViewModel // Reutilizamos o mesmo ViewModel
            {
                Utilizadores = users
            };

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;

            // Adicionamos um ViewBag específico para a view
            ViewBag.IsPedidosVendedorPage = true;

            return View("GerirUtilizadores", model); // Reutilizamos a view, mas com o filtro aplicado
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
    }
}
