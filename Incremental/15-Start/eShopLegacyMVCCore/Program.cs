
using eShopLegacy.Models;
using eShopLegacyMVC.Models;
using eShopLegacyMVC.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SystemWebAdapters.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSystemWebAdapters()
    .AddJsonSessionSerializer(options =>
    {
        options.RegisterKey<string>("MachineName");
        options.RegisterKey<DateTime>("SessionStartTime");
        options.RegisterKey<SessionDemoModel>("DemoItem");
    })
    .AddRemoteAppClient(options =>
    {
        options.RemoteAppUrl = new(builder.Configuration["ReverseProxy:Clusters:fallbackCluster:Destinations:fallbackApp:Address"]);
        options.ApiKey = builder.Configuration["RemoteAppApiKey"];
    })
    .AddSessionClient()
    .AddAuthenticationClient(true);

var keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DPKeys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
    .SetApplicationName("eShop");

builder.Services.AddAuthentication(RemoteAppAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = ".AspNet.ApplicationCookie";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(120);
    });

// Enable WebOptimizer for bundling and minification
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddJavaScriptBundle("/bundles/jquery", "Scripts/jquery-3.3.1.js").UseContentRoot();
    pipeline.AddJavaScriptBundle("/bundles/jqueryval", "Scripts/jquery.validate.*").UseContentRoot();
    pipeline.AddJavaScriptBundle("/bundles/modernizr", "Scripts/modernizr-*").UseContentRoot();
    pipeline.AddJavaScriptBundle("/bundles/bootstrap",
        "Scripts/bootstrap.js",
        "Scripts/respond.js").UseContentRoot();

    pipeline.AddCssBundle("/Content/css",
        "Content/bootstrap.css",
        "Content/custom.css",
        "Content/base.css",
        "Content/site.css").UseContentRoot();
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<CatalogDBContext>();
builder.Services.AddSingleton<CatalogItemHiLoGenerator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use WebOptimizer in place of previous bundling/minification
// Note that it must come before UseStaticFiles
app.UseWebOptimizer();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();
app.UseSystemWebAdapters();

app.MapControllerRoute("Default", "{controller=Catalog}/{action=Index}/{id?}")
    .RequireSystemWebAdapterSession();
app.MapReverseProxy();

app.Run();
