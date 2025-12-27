using AutoMarket.Models;
using AutoMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoMarket.Controllers
{
    public class VisitsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly int _expHoras = 48; // prazo padrão

        // Adicione esta classe no topo do controller ou numa pasta Models
        public class ReservaRequest
        {
            public int anuncioId { get; set; }
        }

        public VisitsController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        // 1. GET – Página para marcar visita
        // ===========================
        [HttpGet]
        public async Task<IActionResult> BookVisit(int id)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var anuncio = await _context.Anuncios
                .Include(a => a.Modelo).ThenInclude(m => m.Marca)
                .Include(a => a.Vendedor).ThenInclude(v => v.Utilizador)
                .Include(a => a.Imagens)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (anuncio == null)
                return NotFound();

            anuncio.Imagens ??= new List<Imagem>();

            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .FirstOrDefaultAsync(c => c.Utilizador.Email == User.Identity.Name);

            if (comprador == null)
                return NotFound();

            // ✅ Adicione esta validação
            var jaTemVisita = await _context.Reservas
                .AnyAsync(r => r.CompradorId == comprador.Id &&
                               r.AnuncioId == id &&
                               r.Estado == "Agendada");

            if (jaTemVisita)
            {
                TempData["MensagemErro"] = "Já tem uma visita agendada para este veículo.";
                return RedirectToAction("Details", "Anuncios", new { id });
            }

            var vm = new VisitasViewModel
            {
                Anuncio = anuncio,
                Visitas = new List<Reserva>(), // ✅ Inicialize vazio
                Utilizador = comprador.Utilizador
            };

            return View(vm);
        }


        // ===========================
        // 2. POST – Submeter agendamento
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookVisit(int anuncioId, DateTime data, string hora)
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account");

            var comprador = await _context.Compradores
                    .Include(c => c.Utilizador)
                    .FirstOrDefaultAsync(c => c.Utilizador.Email == User.Identity.Name);

            // 1. Precisamos carregar o Anuncio COMPLETO novamente, 
            // caso contrário a View vai falhar ao tentar mostrar a imagem/titulo no return View
            var anuncio = await _context.Anuncios
                .Include(a => a.Modelo).ThenInclude(m => m.Marca)
                .Include(a => a.Vendedor).ThenInclude(v => v.Utilizador)
                .Include(a => a.Imagens)
                .FirstOrDefaultAsync(a => a.Id == anuncioId);

            if (comprador == null || anuncio == null) return NotFound();

            // Criar o ViewModel para caso precisemos retornar à View (seja erro ou sucesso)
            var vm = new VisitasViewModel
            {
                Anuncio = anuncio,
                Utilizador = comprador.Utilizador
            };

            // 1️⃣ Validar data passada
            if (data.Date < DateTime.Today)
            {
                TempData["MensagemErro"] = "Não pode marcar visitas para datas anteriores à atual.";
                return RedirectToAction("BookVisit", new { id = anuncioId });
            }

            // 2️⃣ Validar horas passadas no próprio dia
            var dataHora = Combine(data, hora);
            if (data.Date == DateTime.Today && dataHora < DateTime.Now)
            {
                TempData["MensagemErro"] = "A hora selecionada já passou.";
                return RedirectToAction("BookVisit", new { id = anuncioId });
            }

            // 3️⃣ Evitar duplicação (mesmo comprador, mesmo anúncio)
            var visitaExistente = await _context.Reservas
                .AnyAsync(r => r.CompradorId == comprador.Id &&
                               r.AnuncioId == anuncioId &&
                               r.Estado == "Agendada");

            if (visitaExistente)
            {
                TempData["MensagemErro"] = "Já tem uma visita agendada para este anúncio.";
                return RedirectToAction("BookVisit", new { id = anuncioId });
            }

            // 4️⃣ Hora já está ocupada?
            var indisponivel = await _context.Reservas
                .AnyAsync(r => r.AnuncioId == anuncioId &&
                               r.DataHoraReserva == dataHora);

            if (indisponivel)
            {
                TempData["MensagemErro"] = "A hora selecionada já foi reservada por outro utilizador.";
                return RedirectToAction("BookVisit", new { id = anuncioId });
            }

            // 5️⃣ Criar reserva
            var reserva = new Reserva
            {
                CompradorId = comprador.Id,
                AnuncioId = anuncioId,
                Estado = "Agendada",
                PrazoExpiracao = DateTime.Now.AddHours(_expHoras),
                DataHoraReserva = dataHora
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            TempData["MensagemSucesso"] = "Visita agendada com sucesso!";

            // O JavaScript na View vai detetar o TempData e mostrar o popup com o link de redirecionamento.
            return View(vm);
        }

        private DateTime Combine(DateTime data, string hora)
        {
            var h = TimeSpan.Parse(hora);
            return data.Date + h;
        }

        // Horario disponivel apenas
        [HttpGet]
        public async Task<IActionResult> GetHorasIndisponiveis(int anuncioId, DateTime data)
        {
            var horasIndisponiveis = await _context.Reservas
                .Where(r => r.AnuncioId == anuncioId && r.DataHoraReserva.Date == data.Date)
                .Select(r => r.DataHoraReserva.ToString("HH:mm"))
                .ToListAsync();

            // horas passadas
            if (data.Date == DateTime.Today)
            {
                var agora = DateTime.Now.TimeOfDay;

                var horasFixas = new[] { "09:00", "10:00", "11:00", "14:00", "15:00", "16:00" };

                horasIndisponiveis.AddRange(
                    horasFixas.Where(h => TimeSpan.Parse(h) < agora)
                );
            }

            return Json(horasIndisponiveis);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Visits/ReservarVeiculo")]
        public async Task<IActionResult> ReservarVeiculo(int anuncioId)
        {
            var anuncio = await _context.Anuncios.FirstOrDefaultAsync(a => a.Id == anuncioId);

            if (anuncio == null)
                return NotFound();

            if (anuncio.Estado != "Disponivel")
            {
                return RedirectToAction("ResultsInformationCar", "Search", new { id = anuncioId });
            }

            var userEmail = User.Identity.Name;
            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .FirstOrDefaultAsync(c => c.Utilizador.Email == userEmail);

            if (comprador == null)
                return NotFound();

            var reserva = new Reserva
            {
                CompradorId = comprador.Id,
                AnuncioId = anuncio.Id,
                Estado = "Pendente", // ✅ Estado Pendente
                PrazoExpiracao = DateTime.Now.AddHours(48),
                DataHoraReserva = DateTime.Now
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction("ResultsInformationCar", "Search", new { id = anuncioId });
        }

        [Authorize]
        [HttpPost]
        [Route("Visits/ReservarVeiculoJson")]
        public async Task<IActionResult> ReservarVeiculoJson([FromBody] ReservaRequest request)
        {
            Console.WriteLine($"AnuncioId recebido: {request?.anuncioId}");

            if (request == null || request.anuncioId == 0)
            {
                return Json(new { success = false, message = "ID inválido recebido." });
            }

            var anuncio = await _context.Anuncios.FirstOrDefaultAsync(a => a.Id == request.anuncioId);

            Console.WriteLine($"Anuncio encontrado: {anuncio != null}");

            if (anuncio == null)
                return Json(new { success = false, message = "Veículo não encontrado." });

            var userEmail = User.Identity.Name;
            var comprador = await _context.Compradores
                .Include(c => c.Utilizador)
                .FirstOrDefaultAsync(c => c.Utilizador.Email == userEmail);

            if (comprador == null)
                return Json(new { success = false, message = "Comprador não encontrado." });

            // Verificar se já existe reserva pendente ou aprovada
            var reservaExistente = await _context.Reservas
                .AnyAsync(r => r.AnuncioId == request.anuncioId &&
                               r.CompradorId == comprador.Id &&
                               (r.Estado == "Pendente" || r.Estado == "Reservado")); // ✅ Verifica ambos estados

            if (reservaExistente)
                return Json(new { success = false, message = "Você já tem uma reserva para este veículo." });

            // Criar reserva com estado "Pendente" ✅
            var reserva = new Reserva
            {
                CompradorId = comprador.Id,
                AnuncioId = anuncio.Id,
                Estado = "Pendente", // ✅ Estado inicial é Pendente
                PrazoExpiracao = DateTime.Now.AddHours(48),
                DataHoraReserva = DateTime.Now
            };

            _context.Reservas.Add(reserva);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Pedido de reserva enviado! Aguarde a aprovação do vendedor."
            });
        }

    }
}
