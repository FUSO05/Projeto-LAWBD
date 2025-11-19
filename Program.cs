using AutoMarket.Models;
using AutoMarket.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
//using AutoMarket.Data;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddControllersWithViews();

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<Utilizador>, PasswordHasher<Utilizador>>();
builder.Services.AddScoped<EmailService>();

// Authentication configuration (Cookies)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Error/403"; // 👈 redireciona se o utilizador não tiver permissão
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}"); //trata erros 404, 403, etc.
    app.UseHsts();
}
else
{
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//    // Só faz o seed se não existirem imagens ainda (opcional)
//    if (!context.Imagens.Any())
//    {
//        ImageSeeder.Seed(context);
//        Console.WriteLine("✅ Imagens inseridas na base de dados com sucesso!");
//    }
//    else
//    {
//        Console.WriteLine("ℹ️ As imagens já existem, seed ignorado.");
//    }
//}

//void SeedAdminUser(AppDbContext context)
//{
//    var passwordHasher = new PasswordHasher<Utilizador>();

//    if (!context.Utilizadores.Any(u => u.Email == "admin@email.com"))
//    {
//        var admin = new Utilizador
//        {
//            Nome = "Administrador",
//            Email = "admin@email.com",
//            Username = "admin",
//            TipoUser = "Admin",
//            Ativo = true,
//            DataRegisto = DateTime.Now,
//            EmailConfirmed = true
//        };

//        admin.Password = passwordHasher.HashPassword(admin, "Admin123!");

//        context.Utilizadores.Add(admin);
//        context.SaveChanges();
//        Console.WriteLine("✅ Admin criado com sucesso.");
//    }
//}

//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    SeedAdminUser(context);
//}

app.Run();
