using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Infrastructure.Services;
using BolNews.Persistence.Context;
using BolNews.Web;
using BolNews.Web.Interfaces;
using BolNews.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); ////Connection string for SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});     ////connection string for MySQL
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IUrlService, UrlService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddMemoryCache();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

// ✅ SEO Friendly Public Route
app.MapControllerRoute(
    name: "articleDetails",
    pattern: "news/{categorySlug}/{slug}",
    defaults: new { controller = "Article", action = "Details" }
);

// 2️⃣ LESS SPECIFIC AFTER
app.MapControllerRoute(
    name: "categoryListing",
    pattern: "news/{categorySlug}",
    defaults: new { controller = "Category", action = "Details" }
);

// ✅ Area Route (Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
// ✅ Search Route

app.MapControllerRoute(
    name: "search",
    pattern: "search",
    defaults: new { controller = "Search", action = "Index" }
);

// ✅ Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" }
);
app.Run();
