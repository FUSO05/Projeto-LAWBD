using AutoMarket.Models;
using AutoMarket.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Controllers
{
    [Authorize]
    public class CheckoutController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;

        public CheckoutController(AppDbContext context, EmailService emailService) : base(context)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckoutCar(int reservaId)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Modelo)
                        .ThenInclude(m => m.Marca)
                .FirstOrDefaultAsync(r => r.Id == reservaId && r.Estado == "Reservado");

            if (reserva == null)
                return NotFound();

            var model = new CheckoutCarViewModel
            {
                ReservaId = reserva.Id,
                Veiculo = $"{reserva.Anuncio.Modelo.Marca.Nome} {reserva.Anuncio.Modelo.Nome}",
                Preco = (decimal)reserva.Anuncio.Preco
            };

            return View(model);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutCar(CheckoutCarViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var reservaReload = await _context.Reservas
                    .Include(r => r.Anuncio)
                        .ThenInclude(a => a.Modelo)
                            .ThenInclude(m => m.Marca)
                    .FirstOrDefaultAsync(r => r.Id == model.ReservaId);

                model.Veiculo = $"{reservaReload.Anuncio.Modelo.Marca.Nome} {reservaReload.Anuncio.Modelo.Nome}";
                model.Preco = (decimal)reservaReload.Anuncio.Preco;

                return View(model);
            }

            var reserva = await _context.Reservas
                .Include(r => r.Anuncio)
                    .ThenInclude(a => a.Vendedor)
                        .ThenInclude(v => v.Utilizador)
                .Include(r => r.Comprador)
                    .ThenInclude(c => c.Utilizador)
                .FirstOrDefaultAsync(r => r.Id == model.ReservaId && r.Estado == "Reservado");

            if (reserva == null)
                return NotFound();

            // 💳 PAGAMENTO SIMULADO
            reserva.Estado = "Pago";
            reserva.Anuncio.Estado = "Vendido";

            await _context.SaveChangesAsync();

            // 📧 EMAIL COMPRADOR
            await _emailService.EnviarEmailAsync(
                reserva.Comprador.Utilizador.Email,
                "Pagamento efetuado com sucesso",
                "O pagamento do veículo foi realizado com sucesso. Obrigado pela sua compra!"
            );

            // 📧 EMAIL VENDEDOR
            await _emailService.EnviarEmailAsync(
                reserva.Anuncio.Vendedor.Utilizador.Email,
                "Veículo vendido",
                "O seu veículo foi vendido com sucesso através da AutoMarket."
            );

            TempData["Sucesso"] = "Pagamento realizado com sucesso!";
            return RedirectToAction("UserMenuReservas", "Account");
        }

    }

}
