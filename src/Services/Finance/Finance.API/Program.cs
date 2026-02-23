using Finance.Application;
using Finance.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFinanceApplication();
builder.Services.AddFinanceInfrastructure(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddSingleton<Finance.API.Services.SignalRNotificationService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

app.MapHub<Finance.API.Hubs.WalletHub>("/hubs/wallet");

app.Run();

