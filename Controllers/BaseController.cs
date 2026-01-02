using AutoMarket.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

public class BaseController : Controller
{
    protected readonly AppDbContext _context;

    public BaseController(AppDbContext context)
    {
        _context = context;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Set unread notifications para layout
        if (User.Identity.IsAuthenticated)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                ViewBag.UnreadCount = await _context.Notificacoes
                    .CountAsync(n => n.UtilizadorId == userId && !n.Lida);
            }
            else
            {
                ViewBag.UnreadCount = 0;
            }
        }
        else
        {
            ViewBag.UnreadCount = 0;
        }

        await next(); // continua para a action
    }
}
