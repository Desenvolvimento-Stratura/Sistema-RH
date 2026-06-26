using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Interfaces;
using SistemaFerias.Infrastructure.Data;
using SistemaFerias.Infrastructure.Exchange;
using SistemaFerias.Infrastructure.Repositories;
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
builder.Services.AddScoped<IExchangeAutoReplyBackupRepository, ExchangeAutoReplyBackupRepository>();
builder.Services.AddTransient<IPowerShellExecutor, PowerShellExecutor>();
builder.Services.AddScoped<IExchangeOnlineService, ExchangeOnlineService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();