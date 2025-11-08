var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

// Very light-weight token forwarding: pages read token from cookie/localStorage via JS

app.MapRazorPages();
app.MapControllers();

// Default route to desktop and fallback redirect to avoid ambiguous matches
app.MapGet("/", async context =>
{
    context.Response.Redirect("/Desktop");
    await Task.CompletedTask;
});

app.MapFallback(async context =>
{
    context.Response.Redirect("/Desktop");
    await Task.CompletedTask;
});

app.Run();
