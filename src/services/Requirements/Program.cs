using HoneKit.Requirements.Clients;
using HoneKit.Requirements.Data;
using HoneKit.Requirements.Endpoints;
using HoneKit.Requirements.Parsing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddServiceSqlite<RequirementsDbContext>("requirements-v2.db");

builder.Services.AddHttpClient<ContentClient>(client => client.BaseAddress = new Uri("http://content"));
builder.Services.AddSingleton<RequirementParser>();

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
app.MapGet("/", () => "requirements");
app.MapRequirementEndpoints();

await app.EnsureCreatedAndSeedAsync<RequirementsDbContext>(RequirementsSeed.SeedAsync);

app.Run();
