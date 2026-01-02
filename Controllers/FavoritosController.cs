using AutoMarket.Models;
using AutoMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoMarket.Controllers
{
    public class FavoritosController : BaseController
    {
        private readonly AppDbContext _context;

        public FavoritosController(AppDbContext context) : base(context)
        {
            _context = context;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Toggle(int anuncioId)
        {
            try
            {
                if (anuncioId <= 0)
                    return BadRequest(new { error = "AnuncioId inválido" });

                // Verificar se está autenticado
                if (!User.Identity?.IsAuthenticated ?? true)
                    return Unauthorized(new { error = "Por favor, faça login para adicionar favoritos." });

                var userEmail = User.Identity?.Name;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { error = "Usuário não autenticado" });

                // Primeiro buscar o utilizador
                var utilizador = await _context.Utilizadores
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (utilizador == null)
                    return Unauthorized(new { error = "Utilizador não encontrado." });

                // Tentar encontrar como comprador
                var comprador = await _context.Compradores
                    .FirstOrDefaultAsync(c => c.Id == utilizador.Id);

                // Se não for comprador, tentar criar um perfil de comprador automaticamente
                if (comprador == null)
                {
                    // Verificar se é vendedor
                    var vendedor = await _context.Vendedores
                        .FirstOrDefaultAsync(v => v.Id == utilizador.Id);

                    // Criar perfil de comprador (tanto vendedores quanto utilizadores simples podem adicionar favoritos)
                    comprador = new Comprador
                    {
                        Id = utilizador.Id,
                        // Adicione outros campos necessários aqui se houver
                    };

                    _context.Compradores.Add(comprador);
                    await _context.SaveChangesAsync();
                }

                // Agora verificar se já existe nos favoritos
                var favorito = await _context.Favoritos
                    .FirstOrDefaultAsync(f => f.AnuncioId == anuncioId && f.CompradorId == comprador.Id);

                Console.WriteLine($"=== DEBUG FAVORITOS ===");
                Console.WriteLine($"AnuncioId: {anuncioId}");
                Console.WriteLine($"CompradorId: {comprador.Id}");
                Console.WriteLine($"Favorito existente: {favorito != null}");

                bool isAdding = false;

                if (favorito == null)
                {
                    // Adicionar aos favoritos
                    var novoFavorito = new Favorito
                    {
                        AnuncioId = anuncioId,
                        CompradorId = comprador.Id
                    };
                    _context.Favoritos.Add(novoFavorito);
                    isAdding = true;
                    Console.WriteLine("Ação: ADICIONAR aos favoritos");
                }
                else
                {
                    // Remover dos favoritos
                    _context.Favoritos.Remove(favorito);
                    isAdding = false;
                    Console.WriteLine("Ação: REMOVER dos favoritos");
                }

                var changesSaved = await _context.SaveChangesAsync();
                Console.WriteLine($"Alterações salvas: {changesSaved}");
                Console.WriteLine($"======================");

                return Json(new { success = true, isFavorited = isAdding });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro Toggle Favorito: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MarcasFavoritas()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Usuário não autenticado.");

            var userId = int.Parse(userIdClaim);

            var todasMarcas = await _context.Marcas.ToListAsync();
            var marcasFavoritas = await _context.MarcasFavoritas
                .Where(mf => mf.UtilizadorId == userId)
                .ToListAsync();

            var model = new MarcasFavoritasViewModel
            {
                TodasMarcas = todasMarcas,
                MarcasFavoritas = marcasFavoritas ?? new List<MarcaFavorita>() // previne null no View
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SalvarMarcasFavoritas([FromBody] List<int> marcaIds)
        {
            // Pega o ID do usuário autenticado
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "Usuário não autenticado." });

            var userId = int.Parse(userIdClaim);

            // Remover antigas
            var antigas = _context.MarcasFavoritas.Where(mf => mf.UtilizadorId == userId);
            _context.MarcasFavoritas.RemoveRange(antigas);

            // Adicionar novas
            foreach (var id in marcaIds)
            {
                _context.MarcasFavoritas.Add(new MarcaFavorita
                {
                    UtilizadorId = userId,
                    MarcaId = id
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


    }
}