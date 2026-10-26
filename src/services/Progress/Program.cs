using HoneKit.Progress.Clients;
using HoneKit.Progress.Data;
using HoneKit.Progress.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The file name carries the schema and seed version. EnsureCreated only ever creates a missing file,
// so a container rebuilt onto an older volume starts fresh instead of mixing old skill ids with the
// new catalogue. Seed data only, nothing to lose.
builder.AddServiceSqlite<ProgressDbContext>("progress-v3.db");

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
