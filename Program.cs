using Microsoft.AspNetCore.Authentication.Cookies;
using ZeltekWeb.Data;
using ZeltekWeb.Repositories;
using ZeltekWeb.Repositories.Interfaces;
using ZeltekWeb.Services;
using ZeltekWeb.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Servicios
// =======================

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor(); // 👈 Essential for accessing User Claims in Services

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true; // ✅ Security: Javascript cannot access cookie
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // ✅ Works on HTTP and HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; // ✅ Relaxed CSRF for HTTP/Dev
    });

// ... (Services)

// Configure ForwardedHeaders for Nginx/Apache Proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});

builder.Services.AddScoped<UsuarioRepository>();
// builder.Services.AddSingleton<IConnectionFactory, ConnectionFactory>(); // Removed old one
builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>(); // Correct new one
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IInventarioRepository, InventarioRepository>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IGarantiaRepository, GarantiaRepository>();
builder.Services.AddScoped<IGarantiaService, GarantiaService>();
builder.Services.AddScoped<IReporteRepository, ReporteRepository>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IGastoRepository, GastoRepository>();
builder.Services.AddScoped<IGastoService, GastoService>();

// En Program.cs, antes del builder.Build()
var app = builder.Build();

// =======================
// Middleware
// =======================

// ✅ Use Forwarded Headers Middleware (MUST be before others like HttpsRedirection)
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// ✅ Habilitar HTTPS Redirect (Optional if handling on Proxy, but good to keep)
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
