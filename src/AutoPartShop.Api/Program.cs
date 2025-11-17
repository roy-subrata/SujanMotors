using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    // This line reads the XML file you generated in the build folder
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoPart Shop",
        Version = "v1",
        Description = "AutoPart Shop API with Swagger & JWT"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoPart Shop API V1");
        c.RoutePrefix = "docs"; // set swagger path to /docs
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

// Ping the service is live or not
app.MapGet("/live", () => "I am live");

app.Run();
