 var builder = DistributedApplication.CreateBuilder(args);

// ================================================
// 1️⃣ SQL SERVER DATABASE
// ================================================

var password=builder.AddParameter("PASSWORD", "BeignHuman!1990");
// Define a Parameter resource for SA password

var sql = builder.AddSqlServer("AutoPartSqlServer")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHostPort(57395)
    //.WithVolume("AutoPartSqlData", "/var/opt/mssql/data")
    .WithPassword(password);


var db = sql.AddDatabase("AutoPartDb");

// ================================================
// 3️⃣ API PROJECT
// ================================================
var api = builder.AddProject<Projects.AutoPartShop_Api>("AutoPartApi")
                 .WithReference(db);        // API uses SQL database



// ================================================
// // 4️⃣ BLAZOR PROJECT
// // ================================================
// builder.AddProject<Projects.AutoPartShop_Web>("AutoPartWeb")
//                    .WithReference(api);       // Blazor calls the API

builder.Build().Run();
