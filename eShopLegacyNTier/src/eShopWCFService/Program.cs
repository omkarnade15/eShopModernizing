using System;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using eShopWCFService;
using eShopWCFService.Models.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure EF Core
var connectionString = Environment.GetEnvironmentVariable("ConnectionString")
    ?? builder.Configuration.GetConnectionString(CatalogConfiguration.ConnectionStringName)
    ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=eShopDatabase;Persist Security Info=True;";

builder.Services.AddDbContext<EntityModel>(options =>
    options.UseSqlServer(connectionString));

// Configure CoreWCF
builder.Services.AddServiceModelServices()
    .AddServiceModelMetadata();

builder.WebHost.UseUrls("http://0.0.0.0:62314");

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EntityModel>();
    db.Database.EnsureCreated();
    CatalogDBInitializer.Seed(db);
}

app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<CatalogService>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;
    });

    serviceBuilder.AddServiceEndpoint<CatalogService, ICatalogService>(
        new BasicHttpBinding(),
        "/CatalogService.svc");

    var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    serviceMetadataBehavior.HttpGetEnabled = true;
});

app.Run();
