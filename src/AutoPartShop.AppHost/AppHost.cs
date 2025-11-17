var builder = DistributedApplication.CreateBuilder(args);

// ================================================
// 1️⃣ SQL SERVER DATABASE
// ================================================
var sql = builder.AddSqlServer("AutoPartSqlServer");
var db = sql.AddDatabase("AutoPartDb");

// ================================================
// 3️⃣ API PROJECT
// ================================================
var api = builder.AddProject<Projects.AutoPartShop_Api>("AutoPartApi")
                 .WithReference(db);        // API uses SQL database

// ================================================
// 4️⃣ BLAZOR PROJECT
// ================================================
builder.AddProject<Projects.AutoPartShop_Web>("AutoPartWeb")
                   .WithReference(api);       // Blazor calls the API

builder.Build().Run();
