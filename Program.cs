using Azure.Storage.Blobs;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddSingleton<AzureEmailService>();

builder.Services.AddDataProtection().SetApplicationName("MediDrop");

builder.Services.AddScoped<DevicePairingCodeService>();

// Configure BlobStorage:ConnectionString via user-secrets (local) or App Service settings (Azure).
builder.Services.AddSingleton(_ => new BlobServiceClient(
    builder.Configuration["BlobStorage:ConnectionString"]
    ?? throw new InvalidOperationException("BlobStorage:ConnectionString is missing.")));
builder.Services.AddScoped<BlobFileStorage>();

builder.Services.AddHostedService<MedidropMonitoringBackgroundService>();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Error/403";
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await IdentitySeeder.SeedAsync(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllers(); // ESP32 API endpoints

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();