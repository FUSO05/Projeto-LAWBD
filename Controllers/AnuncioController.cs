using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMarket.Data;
using AutoMarket.Models;
using AutoMarket.ViewModels;
using System.Security.Claims;
using AutoMarket.Models.ViewModels;

namespace AutoMarket.Controllers
{
    [Authorize]
    public class AnuncioController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AnuncioController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Anuncios/Create
        [HttpGet]
        [Authorize(Roles = "Vendedor")]
        public async Task<IActionResult> CreateAnuncio()
        {
            var viewModel = new CreateAnuncioViewModel
            {
                Marcas = await _context.Marcas
                    .OrderBy(m => m.Nome)
                    .ToListAsync(),
                Estado = "Disponível" // Valor padrão
            };

            return View(viewModel);
        }

        // POST: Anuncios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnuncio(CreateAnuncioViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Recarregar as listas se houver erro
                viewModel.Marcas = await _context.Marcas
                    .OrderBy(m => m.Nome)
                    .ToListAsync();
                return View("~/Views/Anuncio/CreateAnuncio.cshtml", viewModel);
            }

            try
            {
                // Obter o ID do utilizador logado
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Buscar o vendedor associado ao utilizador
                var vendedor = await _context.Vendedores
                    .FirstOrDefaultAsync(v => v.Id.ToString() == userId);

                if (vendedor == null)
                {
                    ModelState.AddModelError("", "Vendedor não encontrado. Por favor, complete o seu perfil primeiro.");
                    viewModel.Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync();
                    return View(viewModel);
                }

                // Criar o anúncio
                var anuncio = new Anuncio
                {
                    VendedorId = vendedor.Id,
                    ModeloId = viewModel.ModeloId,
                    Titulo = viewModel.Titulo,
                    Descricao = viewModel.Descricao,
                    Preco = viewModel.Preco,
                    Ano = viewModel.Ano,
                    Caixa = viewModel.Caixa,
                    Quilometragem = viewModel.Quilometragem,
                    Combustivel = viewModel.Combustivel,
                    Categoria = viewModel.Categoria,
                    Cor = viewModel.Cor,
                    Estado = viewModel.Estado,
                    Defeito = viewModel.Defeito,
                    Localizacao = viewModel.Localizacao,
                    //Latitude = viewModel.Latitude,
                    //Longitude = viewModel.Longitude,
                    DataCriacao = DateTime.Now
                };

                _context.Anuncios.Add(anuncio);
                await _context.SaveChangesAsync();

                // Processar e salvar imagens
                if (viewModel.Imagens != null && viewModel.Imagens.Any())
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "img", "Anuncios");

                    // Criar pasta se não existir
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    foreach (var imagem in viewModel.Imagens)
                    {
                        if (imagem.Length > 0)
                        {
                            // Validar tamanho da imagem (5MB)
                            if (imagem.Length > 5 * 1024 * 1024)
                            {
                                ModelState.AddModelError("Imagens", "Cada imagem deve ter no máximo 5MB");
                                continue;
                            }

                            // Gerar nome único para a imagem
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imagem.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            // Salvar o arquivo
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await imagem.CopyToAsync(stream);
                            }

                            // Criar registro da imagem no banco
                            var imagemAnuncio = new Imagem
                            {
                                AnuncioId = anuncio.Id,
                                UrlImagem = $"/img/Anuncios/{uniqueFileName}",
                            };

                            _context.Imagens.Add(imagemAnuncio);
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Anúncio criado com sucesso!";
                return RedirectToAction("ResultsInformationCar", "Search", new { id = anuncio.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao criar anúncio: {ex.Message}");
                viewModel.Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync();
                return View("~/Views/Anuncio/CreateAnuncio.cshtml", viewModel);
            }
        }

        [Authorize(Roles = "Vendedor")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var anuncio = await _context.Anuncios
                .Include(a => a.Reservas)
                .Include(a => a.Imagens)
                .FirstOrDefaultAsync(a => a.Id == id && a.VendedorId == userId);

            if (anuncio == null)
                return NotFound();

            if (anuncio.Reservas.Any(r => r.Estado == "Pago"))
                return Forbid();

            var vm = new EditAnuncioViewModel
            {
                Id = anuncio.Id,
                Titulo = anuncio.Titulo,
                Descricao = anuncio.Descricao,
                Preco = (decimal)anuncio.Preco,
                Ano = (int)anuncio.Ano,
                Caixa = anuncio.Caixa,
                Combustivel = anuncio.Combustivel,
                Categoria = anuncio.Categoria,
                Cor = anuncio.Cor,
                Defeito = anuncio.Defeito,
                Localizacao = anuncio.Localizacao
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Vendedor")]
        public async Task<IActionResult> Edit(EditAnuncioViewModel vm)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var anuncio = await _context.Anuncios
                .Include(a => a.Reservas)
                .FirstOrDefaultAsync(a => a.Id == vm.Id && a.VendedorId == userId);

            if (anuncio == null)
                return NotFound();

            if (anuncio.Reservas.Any(r => r.Estado == "Pago"))
                return Forbid();

            if (!ModelState.IsValid)
                return View(vm);

            anuncio.Titulo = vm.Titulo;
            anuncio.Descricao = vm.Descricao;
            anuncio.Preco = vm.Preco;
            anuncio.Ano = vm.Ano;
            anuncio.Caixa = vm.Caixa;
            anuncio.Combustivel = vm.Combustivel;
            anuncio.Categoria = vm.Categoria;
            anuncio.Cor = vm.Cor;
            anuncio.Defeito = vm.Defeito;
            anuncio.Localizacao = vm.Localizacao;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Anúncio atualizado com sucesso!";
            return RedirectToAction("VendedorMenuAnuncios", "Account");
        }


        // GET: API para buscar modelos por marca
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetModelosByMarca(int marcaId)
        {
            var modelos = await _context.Modelos
                .Where(m => m.MarcaId == marcaId)
                .OrderBy(m => m.Nome)
                .Select(m => new { id = m.Id, nome = m.Nome })
                .ToListAsync();

            return Json(modelos);
        }
    }
}