using HoneKit.Content.Data;
using HoneKit.Content.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Slice 8 added the assignments table and a second seeded deck. EnsureCreated only ever creates a
// missing file, so the file name carries the schema version: a container rebuilt onto an earlier
// volume starts a fresh seeded database instead of querying a table that is not there. Content is
// seeded curriculum, so there is nothing a leader authored to lose.
builder.AddServiceSqlite<ContentDbContext>("content-v2.db");

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
