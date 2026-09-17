using Microsoft.EntityFrameworkCore;
using PriorAuthorization.Infrastructure;
using PriorAuthorization.Infrastructure.Data;
using PriorAuthorization.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPriorAuthorizationPoc(builder.Configuration);
builder.Services.AddHostedService<PAProcessingWorker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PADbContext>();
    db.Database.Migrate();
}

host.Run();
