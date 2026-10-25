using HoneKit.Progress.Data;
using HoneKit.Progress.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddServiceSqlite<ProgressDbContext>("progress.db");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapGet("/", () => "progress");
app.MapProgressEndpoints();

await app.EnsureCreatedAndSeedAsync<ProgressDbContext>(ProgressSeed.SeedAsync);

app.Run();
