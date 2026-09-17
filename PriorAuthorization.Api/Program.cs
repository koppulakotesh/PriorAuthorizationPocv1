using Microsoft.EntityFrameworkCore;
using PriorAuthorization.Api.Middleware;
using PriorAuthorization.Infrastructure;
using PriorAuthorization.Infrastructure.Data;
using PriorAuthorization.Worker;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Prior Authorization FHIR API (POC)",
        Version = "v1",
        Description = "Learning POC for an asynchronous Prior Authorization flow: API -> Validation -> SQL -> Queue -> Worker -> QAR -> ServiceNow -> Audit."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddPriorAuthorizationPoc(builder.Configuration);

// Host the worker inside the API so the in-memory Channel is shared.
// This is what makes POST -> worker -> GET Submitted work with one process.
builder.Services.AddHostedService<PAProcessingWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PADbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Prior Authorization FHIR API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
