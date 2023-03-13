using Telegram.Bot;
using Telegram.Bot.Controllers;
using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Repository;
using VC.BinanceSupporter.Repository.Base;
using VC.BinanceSupporter.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Setup Bot configuration
var botConfigurationSection = builder.Configuration.GetSection(BotConfiguration.Configuration);
builder.Services.Configure<BotConfiguration>(botConfigurationSection);
var botConfiguration = botConfigurationSection.Get<BotConfiguration>();

builder.Services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration? botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });
// Dummy business-logic service
#if !DEBUG
builder.Services.AddScoped<UpdateHandlers>();
builder.Services.AddHostedService<ConfigureWebhook>();
#endif

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConStr");
    options.InstanceName = "BeeCryptoBot";
});

//mongo
builder.Services.AddSingleton<IMongoContext, MongoContext>();
builder.Services.AddScoped<IBinanceConfigRepository, BinanceConfigRepository>();
builder.Services.AddScoped<IBinanceConfigService, BinanceConfigService>();
builder.Services.AddScoped<IPlaceOrderRepository, PlaceOrderRepository>();
builder.Services.AddScoped<IPlaceOrderService, PlaceOrderService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseAuthorization();
app.MapBotWebhookRoute<BotController>(route: botConfiguration.Route);
app.MapControllers();

app.Run();
