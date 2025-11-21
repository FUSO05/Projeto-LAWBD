using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMarket.Data;
using AutoMarket.Models;
using AutoMarket.ViewModels;
using System.Security.Claims;

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
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "anuncios");

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
                return RedirectToAction("Details", new { id = anuncio.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao criar anúncio: {ex.Message}");
                viewModel.Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync();
                return View("~/Views/Anuncio/CreateAnuncio.cshtml", viewModel);
            }
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

        // GET: Anuncios/Details/5
        //[AllowAnonymous]
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var anuncio = await _context.Anuncios
        //        .Include(a => a.Modelo)
        //            .ThenInclude(m => m.Marca)
        //        .Include(a => a.Vendedor)
        //            .ThenInclude(v => v.Utilizador)
        //        .Include(a => a.Imagens)
        //        .FirstOrDefaultAsync(a => a.Id == id);

        //    if (anuncio == null)
        //    {
        //        return NotFound();
        //    }

        //    // Verificar se o anúncio está nos favoritos do utilizador (se estiver logado)
        //    if (User.Identity?.IsAuthenticated ?? false)
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        ViewBag.IsFavorito = await _context.Favoritos
        //            .AnyAsync(f => f.UtilizadorId == userId && f.AnuncioId == id);
        //    }
        //    else
        //    {
        //        ViewBag.IsFavorito = false;
        //    }

        //    // Para o mapa do Google (se você tiver a API key configurada)
        //    ViewBag.GoogleMapsApiKey = "SUA_API_KEY_AQUI"; // Substitua pela sua chave

        //    return View(anuncio);
        //}

        //// GET: Anuncios/MeusAnuncios
        //[HttpGet]
        //public async Task<IActionResult> MeusAnuncios()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    var vendedor = await _context.Vendedores
        //        .FirstOrDefaultAsync(v => v.UtilizadorId == userId);

        //    if (vendedor == null)
        //    {
        //        return RedirectToAction("Create");
        //    }

        //    var anuncios = await _context.Anuncios
        //        .Include(a => a.Modelo)
        //            .ThenInclude(m => m.Marca)
        //        .Include(a => a.Imagens)
        //        .Where(a => a.VendedorId == vendedor.Id)
        //        .OrderByDescending(a => a.DataCriacao)
        //        .ToListAsync();

        //    return View(anuncios);
        //}

        //// GET: Anuncios/Edit/5
        //[HttpGet]
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var vendedor = await _context.Vendedores
        //        .FirstOrDefaultAsync(v => v.UtilizadorId == userId);

        //    if (vendedor == null)
        //    {
        //        return Forbid();
        //    }

        //    var anuncio = await _context.Anuncios
        //        .Include(a => a.Modelo)
        //        .Include(a => a.Imagens)
        //        .FirstOrDefaultAsync(a => a.Id == id && a.VendedorId == vendedor.Id);

        //    if (anuncio == null)
        //    {
        //        return NotFound();
        //    }

        //    var viewModel = new CreateAnuncioViewModel
        //    {
        //        ModeloId = anuncio.ModeloId,
        //        MarcaId = anuncio.Modelo.MarcaId,
        //        Titulo = anuncio.Titulo,
        //        Descricao = anuncio.Descricao ?? string.Empty,
        //        Preco = anuncio.Preco ?? 0,
        //        Ano = anuncio.Ano ?? DateTime.Now.Year,
        //        Caixa = anuncio.Caixa ?? string.Empty,
        //        Quilometragem = anuncio.Quilometragem ?? 0,
        //        Combustivel = anuncio.Combustivel ?? string.Empty,
        //        Categoria = anuncio.Categoria ?? string.Empty,
        //        Cor = anuncio.Cor,
        //        Estado = anuncio.Estado ?? "Usado",
        //        Defeito = anuncio.Defeito,
        //        Localizacao = anuncio.Localizacao ?? string.Empty,
        //        Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync()
        //    };

        //    ViewBag.AnuncioId = id;
        //    ViewBag.ImagensExistentes = anuncio.Imagens;

        //    return View("Create", viewModel);
        //}

        //// POST: Anuncios/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, CreateAnuncioViewModel viewModel)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        viewModel.Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync();
        //        ViewBag.AnuncioId = id;
        //        return View("Create", viewModel);
        //    }

        //    try
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        var vendedor = await _context.Vendedores
        //            .FirstOrDefaultAsync(v => v.UtilizadorId == userId);

        //        if (vendedor == null)
        //        {
        //            return Forbid();
        //        }

        //        var anuncio = await _context.Anuncios
        //            .FirstOrDefaultAsync(a => a.Id == id && a.VendedorId == vendedor.Id);

        //        if (anuncio == null)
        //        {
        //            return NotFound();
        //        }

        //        // Atualizar os dados
        //        anuncio.ModeloId = viewModel.ModeloId;
        //        anuncio.Titulo = viewModel.Titulo;
        //        anuncio.Descricao = viewModel.Descricao;
        //        anuncio.Preco = viewModel.Preco;
        //        anuncio.Ano = viewModel.Ano;
        //        anuncio.Caixa = viewModel.Caixa;
        //        anuncio.Quilometragem = viewModel.Quilometragem;
        //        anuncio.Combustivel = viewModel.Combustivel;
        //        anuncio.Categoria = viewModel.Categoria;
        //        anuncio.Cor = viewModel.Cor;
        //        anuncio.Estado = viewModel.Estado;
        //        anuncio.Defeito = viewModel.Defeito;
        //        anuncio.Localizacao = viewModel.Localizacao;

        //        _context.Update(anuncio);
        //        await _context.SaveChangesAsync();

        //        // Processar novas imagens se houver
        //        if (viewModel.Imagens != null && viewModel.Imagens.Any())
        //        {
        //            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "anuncios");

        //            if (!Directory.Exists(uploadsFolder))
        //            {
        //                Directory.CreateDirectory(uploadsFolder);
        //            }

        //            foreach (var imagem in viewModel.Imagens)
        //            {
        //                if (imagem.Length > 0 && imagem.Length <= 5 * 1024 * 1024)
        //                {
        //                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(imagem.FileName)}";
        //                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //                    using (var stream = new FileStream(filePath, FileMode.Create))
        //                    {
        //                        await imagem.CopyToAsync(stream);
        //                    }

        //                    var imagemAnuncio = new Imagem
        //                    {
        //                        AnuncioId = anuncio.Id,
        //                        UrlImagem = $"/images/anuncios/{uniqueFileName}",
        //                        DataUpload = DateTime.Now
        //                    };

        //                    _context.Imagens.Add(imagemAnuncio);
        //                }
        //            }

        //            await _context.SaveChangesAsync();
        //        }

        //        TempData["Success"] = "Anúncio atualizado com sucesso!";
        //        return RedirectToAction("Details", new { id = anuncio.Id });
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", $"Erro ao atualizar anúncio: {ex.Message}");
        //        viewModel.Marcas = await _context.Marcas.OrderBy(m => m.Nome).ToListAsync();
        //        ViewBag.AnuncioId = id;
        //        return View("Create", viewModel);
        //    }
        //}

        //// POST: Anuncios/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    try
        //    {
        //        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        var vendedor = await _context.Vendedores
        //            .FirstOrDefaultAsync(v => v.UtilizadorId == userId);

        //        if (vendedor == null)
        //        {
        //            return Json(new { success = false, message = "Vendedor não encontrado" });
        //        }

        //        var anuncio = await _context.Anuncios
        //            .Include(a => a.Imagens)
        //            .FirstOrDefaultAsync(a => a.Id == id && a.VendedorId == vendedor.Id);

        //        if (anuncio == null)
        //        {
        //            return Json(new { success = false, message = "Anúncio não encontrado" });
        //        }

        //        // Apagar imagens físicas
        //        if (anuncio.Imagens != null)
        //        {
        //            foreach (var imagem in anuncio.Imagens)
        //            {
        //                var imagePath = Path.Combine(_environment.WebRootPath, imagem.UrlImagem.TrimStart('/'));
        //                if (System.IO.File.Exists(imagePath))
        //                {
        //                    System.IO.File.Delete(imagePath);
        //                }
        //            }
        //        }

        //        _context.Anuncios.Remove(anuncio);
        //        await _context.SaveChangesAsync();

        //        return Json(new { success = true, message = "Anúncio eliminado com sucesso" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = $"Erro ao eliminar anúncio: {ex.Message}" });
        //    }
        //}
    }
}