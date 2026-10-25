using HoneKit.Progress.Clients;
using HoneKit.Progress.Data;
using HoneKit.Progress.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Slice 2 added sessions, attempts and reviews. EnsureCreated only ever creates a missing file, so
// the file name carries the schema version: a container rebuilt onto a slice-1 volume starts a
// fresh seeded database instead of querying tables that are not there. Seed data only, nothing to lose.
builder.AddServiceSqlite<ProgressDbContext>("progress-v2.db");

builder.Services.AddHttpClient<ContentClient>(client => client.BaseAddress = new Uri("http://content"));

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
