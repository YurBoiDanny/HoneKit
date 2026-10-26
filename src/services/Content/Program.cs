using HoneKit.Content.Data;
using HoneKit.Content.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Slice 9 added cards.ordinal so authored cards keep their insertion order. EnsureCreated only ever
// creates a missing file, so the file name carries the schema version: a container rebuilt onto an
// earlier volume starts a fresh seeded database. Content is seeded curriculum; authored decks begin
// with this slice.
builder.AddServiceSqlite<ContentDbContext>("content-v3.db");

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
