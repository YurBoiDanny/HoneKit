using HoneKit.Content.Data;
using HoneKit.Content.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Slice 13 added the ordering and ptz columns. EnsureCreated only ever creates a missing file, so
// the file name carries the schema and seed version: a container rebuilt onto an earlier volume
// starts a fresh seeded database rather than querying columns that are not there.
builder.AddServiceSqlite<ContentDbContext>("content-v5.db");

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
