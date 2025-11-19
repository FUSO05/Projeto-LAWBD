using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMarket.Models;

namespace AutoMarket.Controllers
{
    public class SearchController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public SearchController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Página de resultados
        public async Task<IActionResult> SearchResults(
            string? marca, string? modelo, string? categoria,
            int? ano, string? combustivel, string? caixa, string? localizacao)
        {
            var query = _context.Anuncios
                .Include(a => a.Modelo)
                    .ThenInclude(m => m.Marca)
                .Include(a => a.Imagens)
                .AsQueryable();

            if (!string.IsNullOrEmpty(marca))
                query = query.Where(a => a.Modelo.Marca.Nome == marca);

            if (!string.IsNullOrEmpty(modelo))
                query = query.Where(a => a.Modelo.Nome == modelo);

            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(a => a.Categoria == categoria);

            if (ano.HasValue)
                query = query.Where(a => a.Ano == ano.Value);

            if (!string.IsNullOrEmpty(combustivel))
                query = query.Where(a => a.Combustivel == combustivel);

            if (!string.IsNullOrEmpty(caixa))
                query = query.Where(a => a.Caixa == caixa);

            if (!string.IsNullOrEmpty(localizacao))
                query = query.Where(a => a.Localizacao == localizacao);

            var anuncios = await query.ToListAsync();

            // Filtros dinâmicos continuam funcionando
            ViewBag.Marcas = await _context.Anuncios.Include(a => a.Modelo).ThenInclude(m => m.Marca)
                               .Select(a => a.Modelo.Marca.Nome).Distinct().ToListAsync();
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

                var comprador = _context.Compradores
                    .FirstOrDefault(c => c.Utilizador.Email == userEmail);

                if (comprador != null)
                {
                    isFavorito = _context.Favoritos
                        .Any(f => f.CompradorId == comprador.Id && f.AnuncioId == anuncio.Id);
                }
            }

            ViewBag.IsFavorito = isFavorito;
            ViewBag.GoogleMapsApiKey = _config.GetValue<string>("GoogleMaps:ApiKey");

            return View(anuncio);
        }


        [HttpGet]
        public async Task<JsonResult> GetModelosByMarca(string marca)
        {
            if (string.IsNullOrEmpty(marca))
                return Json(new List<string>());

            var modelos = await _context.Anuncios
                .Include(a => a.Modelo)
                    .ThenInclude(m => m.Marca)
                .Where(a => a.Modelo.Marca.Nome == marca)
                .Select(a => a.Modelo.Nome)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            return Json(modelos);
        }

    }
}

