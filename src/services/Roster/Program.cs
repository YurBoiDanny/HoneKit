using HoneKit.Roster.Clients;
using HoneKit.Roster.Data;
using HoneKit.Roster.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The file name carries the schema and seed version. EnsureCreated only ever creates a missing file,
// so a container rebuilt onto an older volume starts fresh instead of mixing old skills with the new
// catalogue. Seed data only, nothing to lose.
builder.AddServiceSqlite<RosterDbContext>("roster-v3.db");

builder.Services.AddHttpClient<ProgressClient>(client => client.BaseAddress = new Uri("http://progress"));
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
app.MapGet("/", () => "roster");
app.MapRosterEndpoints();

await app.EnsureCreatedAndSeedAsync<RosterDbContext>(RosterSeed.SeedAsync);

app.Run();
