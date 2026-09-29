#:sdk Aspire.AppHost.Sdk@13.5.4
#:package Aspire.Hosting.PostgreSQL@13.5.4
#:property AspireUseCliBundle=true

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var db = postgres.AddDatabase("Default", "shopping_list");

builder.AddProject("shoppinglistapp", "../ShoppingListApp/ShoppingListApp.csproj")
    .WithReference(db)
    .WithHttpHealthCheck("/health")
    .WaitFor(db);

builder.Build().Run();