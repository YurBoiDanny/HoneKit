using HoneKit.Roster.Clients;
using HoneKit.Roster.Data;
using HoneKit.Roster.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Slice 2 added members.showAnswerOnWrong. EnsureCreated only ever creates a missing file, so the
// file name carries the schema version: a container rebuilt onto a slice-1 volume starts a fresh
// seeded database instead of querying a column that is not there. Seed data only, nothing to lose.
builder.AddServiceSqlite<RosterDbContext>("roster-v2.db");

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
