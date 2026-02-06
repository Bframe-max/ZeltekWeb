using Microsoft.AspNetCore.Authentication.Cookies;
using ZeltekWeb.Data;
using ZeltekWeb.Models.Entities;
using ZeltekWeb.Services;
using ZeltekWeb.Services.Interfaces;



var builder = WebApplication.CreateBuilder(args);

// =======================
// Servicios
// =======================

builder.Services.AddControllersWithViews();

// Autenticación por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

// Inyección de dependencias
builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Session (opcional, ya NO es auth)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// =======================
// Middleware
// =======================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔑 ORDEN CORRECTO
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Ruta por defecto CORRECTA
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();


