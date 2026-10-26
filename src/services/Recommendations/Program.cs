using HoneKit.Recommendations.Clients;
using HoneKit.Recommendations.Data;
using HoneKit.Recommendations.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddServiceSqlite<RecommendationsDbContext>("recommendations.db");

builder.Services.AddHttpClient<RequirementsClient>(client => client.BaseAddress = new Uri("http://requirements"));
builder.Services.AddHttpClient<RosterClient>(client => client.BaseAddress = new Uri("http://roster"));
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
app.MapGet("/", () => "recommendations");
app.MapRecommendationEndpoints();

await app.EnsureCreatedAndSeedAsync<RecommendationsDbContext>(RecommendationsSeed.SeedAsync);

app.Run();
