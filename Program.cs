using BauManagement.Components;
using BauManagement.Components.Account;
using BauManagement.Data;
using BauManagement.Services;
using BauManagement.Services.Email;
using BauManagement.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------
// Razor Components
// ------------------------------------------------------------
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// ------------------------------------------------------------
// Authentication / Identity
// ------------------------------------------------------------
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<
    AuthenticationStateProvider,
    IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

// ------------------------------------------------------------
// Database
// ------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ------------------------------------------------------------
// Application services
// ------------------------------------------------------------
builder.Services.AddScoped<ICurrentCompanyContext, CurrentCompanyContext>();
builder.Services.AddScoped<CompanyCancellationService>();
builder.Services.AddScoped<BauManagement.Services.SubscriptionService>();
builder.Services.AddScoped<SubscriptionAccessService>();
builder.Services.AddHostedService<CompanyDataCleanupService>();

// ------------------------------------------------------------
// Stripe
// ------------------------------------------------------------
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe"));

var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}
else
{
    // Stripe operations should validate configuration before use.
}

builder.Services.AddScoped<StripeService>();

// ------------------------------------------------------------
// SMTP email
// ------------------------------------------------------------
builder.Services.Configure<SmtpEmailOptions>(
    builder.Configuration.GetSection("SmtpEmail"));

builder.Services.AddScoped<
    IEmailSender<ApplicationUser>,
    SmtpEmailSender>();

// ------------------------------------------------------------
// Lexware Office API
// Expected configuration in appsettings.Production.json:
// "Lexware": {
//   "BaseUrl": "https://api.lexware.io/",
//   "ApiKey": "YOUR_API_KEY"
// }
// ------------------------------------------------------------
var lexwareApiKey = builder.Configuration["Lexware:ApiKey"];
if (string.IsNullOrWhiteSpace(lexwareApiKey))
{
    throw new InvalidOperationException(
        "Lexware API key is missing. Configure 'Lexware:ApiKey' in appsettings.Production.json.");
}

var lexwareBaseUrl = builder.Configuration["Lexware:BaseUrl"];
if (string.IsNullOrWhiteSpace(lexwareBaseUrl))
{
    lexwareBaseUrl = "https://api.lexware.io/";
}

if (!Uri.TryCreate(lexwareBaseUrl, UriKind.Absolute, out var lexwareUri) ||
    (lexwareUri.Scheme != Uri.UriSchemeHttps && builder.Environment.IsProduction()))
{
    throw new InvalidOperationException(
        "Lexware:BaseUrl must be a valid HTTPS URL in Production.");
}

builder.Services.AddHttpClient<LexwareService>(client =>
{
    client.BaseAddress = new Uri(lexwareUri.ToString().TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", lexwareApiKey.Trim());

    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.InitializeAsync(services);
}

// ------------------------------------------------------------
// HTTP pipeline
// ------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();

