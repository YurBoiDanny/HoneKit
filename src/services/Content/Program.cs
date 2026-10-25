using HoneKit.Content.Data;
using HoneKit.Content.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddServiceSqlite<ContentDbContext>("content.db");

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
app.MapGet("/", () => "content");
app.MapContentEndpoints();

await app.EnsureCreatedAndSeedAsync<ContentDbContext>(ContentSeed.SeedAsync);

app.Run();
