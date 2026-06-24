using Microsoft.EntityFrameworkCore;
using SistemaFerias.Infrastructure.Data;
using SistemaFerias.Infrastructure.Services;
using SistemaFerias.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();