using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMarket.Models;

namespace AutoMarket.Controllers
{
    public class SearchController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public SearchController(AppDbContext context, IConfiguration config) : base(context)
        {
            _context = context;
            _config = config;
        }

        // Página de resultados
        [HttpGet]
        public async Task<IActionResult> SearchResults(
            string? marca, string? modelo, string? categoria,
            int? anoMin, int? anoMax,
            decimal? precoMin, decimal? precoMax,
            string? combustivel, string? caixa, string? localizacao,
            string? ordenar
        )
        {
            var query = _context.Anuncios
                .Where(a => a.Ativo)
                .Include(a => a.Modelo)
                    .ThenInclude(m => m.Marca)
                .Include(a => a.Imagens)
                .Include(a => a.Reservas)
                .AsSplitQuery()
                .AsQueryable();

            // filtros existentes
            if (!string.IsNullOrEmpty(marca))
                query = query.Where(a => a.Modelo.Marca.Nome == marca);

            if (!string.IsNullOrEmpty(modelo))
                query = query.Where(a => a.Modelo.Nome == modelo);

            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(a => a.Categoria == categoria);

            if (anoMin.HasValue)
                query = query.Where(a => a.Ano >= anoMin.Value);

            if (anoMax.HasValue)
                query = query.Where(a => a.Ano <= anoMax.Value);

            if (precoMin.HasValue)
                query = query.Where(a => a.Preco >= precoMin.Value);

            if (precoMax.HasValue)
                query = query.Where(a => a.Preco <= precoMax.Value);

            if (!string.IsNullOrEmpty(combustivel))
                query = query.Where(a => a.Combustivel == combustivel);

            if (!string.IsNullOrEmpty(caixa))
                query = query.Where(a => a.Caixa == caixa);

            if (!string.IsNullOrEmpty(localizacao))
                query = query.Where(a => a.Localizacao == localizacao);

            // 🔥 ORDENAÇÃO
            query = ordenar switch
            {
                "preco_asc" => query.OrderBy(a => a.Preco),
                "preco_desc" => query.OrderByDescending(a => a.Preco),
                "ano_desc" => query.OrderByDescending(a => a.Ano),
                "ano_asc" => query.OrderBy(a => a.Ano),
                "recentes" => query.OrderByDescending(a => a.DataCriacao),
                _ => query.OrderByDescending(a => a.DataCriacao)
            };

            ViewBag.OrdenarAtual = ordenar;

            var anuncios = await query.ToListAsync();

            // ViewBags dos filtros
            ViewBag.Marcas = await _context.Marcas.OrderBy(m => m.Nome).Select(m => m.Nome).ToListAsync();
            ViewBag.Modelos = await _context.Anuncios.Select(a => a.Modelo.Nome).Distinct().ToListAsync();
            ViewBag.Categorias = await _context.Anuncios.Select(a => a.Categoria).Distinct().ToListAsync();
            ViewBag.Anos = await _context.Anuncios.Select(a => a.Ano).Distinct().ToListAsync();
            ViewBag.Combustiveis = await _context.Anuncios.Select(a => a.Combustivel).Distinct().ToListAsync();
            ViewBag.Caixas = await _context.Anuncios.Select(a => a.Caixa).Distinct().ToListAsync();
            ViewBag.Localizacoes = await _context.Anuncios.Select(a => a.Localizacao).Distinct().ToListAsync();

            return View(anuncios);
        }


        // Página de detalhes do carro
        public IActionResult ResultsInformationCar(int id)
        {
            var anuncio = _context.Anuncios
                .Where(a => a.Ativo)
                .Include(a => a.Modelo).ThenInclude(m => m.Marca)
                .Include(a => a.Vendedor).ThenInclude(v => v.Utilizador)
                .Include(a => a.Imagens)
                .FirstOrDefault(a => a.Id == id);

            if (anuncio == null)
                return NotFound();

            // Se estiver logado, verificar favoritos
            bool isFavorito = false;
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userEmail = User.Identity?.Name;
                var comprador = _context.Compradores.FirstOrDefault(c => c.Utilizador.Email == userEmail);
                if (comprador != null)
                {
                    isFavorito = _context.Favoritos.Any(f => f.CompradorId == comprador.Id && f.AnuncioId == anuncio.Id);
                }
            }

            // Verificar se existe alguma reserva ativa
            var reservaAtiva = _context.Reservas
                .Where(r => r.AnuncioId == id && (r.Estado == "Reservado" || r.Estado == "Pago"))
                .FirstOrDefault();

            ViewBag.IsFavorito = isFavorito;
            ViewBag.GoogleMapsApiKey = _config.GetValue<string>("GoogleMaps:ApiKey");
            ViewBag.ReservaAtiva = reservaAtiva;

            return View(anuncio);
        }


        [HttpGet]
        public async Task<JsonResult> GetModelosByMarca(string marca)
        {
            if (string.IsNullOrEmpty(marca))
                return Json(new List<string>());

            var modelos = await _context.Modelos
                .Include(m => m.Marca)
                .Where(m => m.Marca.Nome == marca)
                .Select(m => m.Nome)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            return Json(modelos);
        }

    }
}

