using Cutter.Components;
using Cutter.Components.Account;
using Cutter.Components.Pages;
using Cutter.Data;
using Cutter.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net;
using static Cutter.Services.PdfCuttingService;


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
builder.Services.AddScoped<SvgLayoutService>();

builder.Services.AddScoped<IStorePricingService, StorePricingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStoreAccessService, StoreAccessService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IStoreContextService, StoreContextService>();

builder.Services.AddScoped<ICashierService, CashierService>();
builder.Services.AddScoped<IStoreAdminService, StoreAdminService>();
builder.Services.AddScoped<IStoreStatsService, StoreStatsService>();
builder.Services.AddScoped<IConstraintProfileService, ConstraintProfileService>();

builder.Services.AddScoped<IBlankSearchService, BlankSearchService>();

builder.Services.AddScoped<IExcelExportService, ExcelExportService>();


var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Применяем миграции (создаем таблицы в БД, если их нет)
        await context.Database.MigrateAsync();

        // 2. Запускаем заполнение начальными данными
        await SeedData.Initialize(context, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Произошла ошибка при применении миграций или сидировании данных.");
    }
}
// 

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
    ILogger<Program> logger,
    [FromQuery] string mode = "Full") => // <-- НОВЫЙ ПАРАМЕТР
{
    try
    {
        // Парсим строку в Enum (с защитой от некорреных значений)
        if (!Enum.TryParse<PdfPrintMode>(mode, true, out var printMode))
        {
            printMode = PdfPrintMode.Full;
        }

        var plans = await cuttingService.GetCuttingPlanUser(cuttingId.ToString());
        if (plans == null)
        {
            logger.LogWarning($"Планы раскроя не найдены для ID: {cuttingId}");
            return Results.NotFound();
        }

        // Сохраняем перед печатью (как было)
        cuttingService.SaveCuttangPlan(cuttingId.ToString());

        // Передаем режим в сервис
        var pdfBytes = pdfService.GeneratePdf(plans, printMode);

        return Results.File(
            fileContents: pdfBytes,
            contentType: "application/pdf",
            fileDownloadName: null
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Ошибка генерации PDF для {cuttingId}");
        return Results.Problem("Ошибка генерации PDF");
    }
});


app.MapGet("/api/export/store-stats", async (
    [FromQuery] Guid storeId,
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    IExcelExportService excelService,
    ILogger<Program> logger) =>
{
    try
    {
        var bytes = await excelService.ExportStoreStatisticsAsync(storeId, startDate, endDate);
        var fileName = $"Статистика_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.xlsx";

        return Results.File(
            fileContents: bytes,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileDownloadName: fileName
        );
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка экспорта статистики в Excel");
        return Results.Problem("Ошибка генерации Excel");
    }
});

app.Run();
