using HoneKit.Content.Data;
using HoneKit.Content.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Phase 2 reshaped the skills and decks. EnsureCreated only ever creates a missing file, so the
// file name carries the seed version: a container rebuilt onto an earlier volume starts a fresh
// seeded database rather than mixing the old skill ids with the new catalogue.
builder.AddServiceSqlite<ContentDbContext>("content-v4.db");

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
