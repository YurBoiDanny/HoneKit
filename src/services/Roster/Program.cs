using HoneKit.Roster.Clients;
using HoneKit.Roster.Data;
using HoneKit.Roster.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddServiceSqlite<RosterDbContext>("roster.db");

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
