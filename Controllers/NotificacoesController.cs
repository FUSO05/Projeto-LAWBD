using AutoMarket.Models;
using AutoMarket.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoMarket.Controllers
{
    [Authorize]
    public class NotificacoesController : BaseController
    {
        private readonly AppDbContext _context;
         
        public NotificacoesController(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Usuário não autenticado.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("ID de usuário inválido.");

            var notifications = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId)
                .OrderByDescending(n => n.DataCriada)
                .ToListAsync();

            // Contar não lidas
            ViewBag.UnreadCount = notifications.Count(n => !n.Lida);

            var model = new NotificacoesViewModel
            {
                Notificacoes = notifications
            };

            return View(model);
        }

        public async Task<int> GetUnreadCount()
        {
            if (!User.Identity.IsAuthenticated)
                return 0;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return 0;

            return await _context.Notificacoes.CountAsync(n => n.UtilizadorId == userId && !n.Lida);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var notif = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilizadorId == userId);

            if (notif == null)
                return NotFound();

            notif.Lida = true;
            await _context.SaveChangesAsync();

            return Ok();
        }

    }

}
