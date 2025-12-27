using System.Diagnostics;
using System.Security.Claims;
using AutoMarket.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.Marcas = _context.Marcas
                .OrderBy(m => m.Nome)
                .Select(m => m.Nome)
                .ToList();

            ViewBag.Modelos = _context.Modelos
                .OrderBy(m => m.Nome)
                .Select(m => m.Nome)
                .ToList();

            ViewBag.Categorias = _context.Anuncios
                .Select(a => a.Categoria)
                .Distinct().OrderBy(c => c).ToList();

            ViewBag.Anos = _context.Anuncios
                .Select(a => a.Ano)
                .Distinct().OrderByDescending(a => a).ToList();

            ViewBag.Combustiveis = _context.Anuncios
                .Select(a => a.Combustivel)
                .Distinct().OrderBy(c => c).ToList();

            ViewBag.Caixas = _context.Anuncios
                .Select(a => a.Caixa)
                .Distinct().OrderBy(c => c).ToList();

            ViewBag.Localizacoes = _context.Anuncios
                .Select(a => a.Localizacao)
                .Distinct().OrderBy(l => l).ToList();


            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = _context.Utilizadores.FirstOrDefault(u => u.Id == userId);
                ViewBag.UserPhotoUrl = user?.FotoUrl ?? "/img/avatar.png";
            }

            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Cookies()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contacts()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Guia()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}