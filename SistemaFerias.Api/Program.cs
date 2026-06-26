using Microsoft.EntityFrameworkCore;
using SistemaFerias.Domain.Interfaces;
using SistemaFerias.Infrastructure.Data;
using SistemaFerias.Infrastructure.Exchange;
using SistemaFerias.Infrastructure.Repositories;
using SistemaFerias.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();