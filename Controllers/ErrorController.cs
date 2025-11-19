using Microsoft.AspNetCore.Mvc;

namespace AutoMarket.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewData["Title"] = "Página Não Encontrada";
                    return View("Error404");
                case 403:
                    ViewData["Title"] = "Acesso Negado";
                    return View("Error403");
                default:
                    ViewData["Title"] = "Erro";
                    return View("Error");
            }
        }
    }
}
