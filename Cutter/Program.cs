using Cutter.Components;
using Cutter.Components.Account;
using Cutter.Components.Pages;
using Cutter.Data;
using Cutter.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net;


var ruCulture = (CultureInfo)CultureInfo.GetCultureInfo("ru-RU").Clone();
ruCulture.NumberFormat.NumberGroupSeparator = "\u00A0"; // неразрывный пробел
ruCulture.NumberFormat.NumberDecimalSeparator = ",";
CultureInfo.DefaultThreadCurrentCulture = ruCulture;
CultureInfo.DefaultThreadCurrentUICulture = ruCulture;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("CutterConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString ?? throw new InvalidOperationException("Connection string 'Context' not found.")));


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
builder.Services.AddScoped<CuttingService>();
builder.Services.AddScoped<ThreeJSInterop>();
//builder.Services.AddSingleton<CuttingState>();
builder.Services.AddScoped<CuttingState>();
builder.Services.AddScoped<SheetService>();
builder.Services.AddScoped<PdfCuttingService>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStoreAccessService, StoreAccessService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IStoreContextService, StoreContextService>();


var app = builder.Build();

/*app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Доверяем прокси (твоему Nginx)
    ForwardedForHeaderName = "X-Forwarded-For",
    ForwardedProtoHeaderName = "X-Forwarded-Proto",
    // Если используешь Cloudflare или доверенный прокси:
     KnownNetworks = { new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Any, 0) },
     KnownProxies = { IPAddress.Any }
});*/


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapGet("/api/pdf/{cuttingId:guid}", async (
    Guid cuttingId,
    CuttingService cuttingService,
    PdfCuttingService pdfService,
    ILogger<Program> logger) =>
{
    try
    {
        var plans = await cuttingService.GetCuttingPlanUser(cuttingId.ToString());
        if (plans == null)
        {
            logger.LogWarning($"Планы раскроя не найдены для ID: {cuttingId}");
            return Results.NotFound();
        }

        cuttingService.SaveCuttangPlan(cuttingId.ToString());

        var pdfBytes = pdfService.GeneratePdf(plans);

        return Results.File(
            fileContents: pdfBytes,
            contentType: "application/pdf",
            fileDownloadName: null // ← открыть в браузере, не скачивать
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Ошибка генерации PDF для {cuttingId}");
        return Results.Problem("Ошибка генерации PDF");
    }
});


app.Run();
