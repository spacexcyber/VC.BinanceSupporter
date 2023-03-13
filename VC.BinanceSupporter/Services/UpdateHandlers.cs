using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Util;

namespace VC.BinanceSupporter.Services;

public class UpdateHandlers
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandlers> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _distributedCache;
    private readonly IBinanceConfigService _binanceConfigService;
    private readonly IPlaceOrderService _placeOrderService;

    public UpdateHandlers(ITelegramBotClient botClient,
        ILogger<UpdateHandlers> logger,
        IConfiguration configuration,
        IDistributedCache distributedCache,
        IPlaceOrderService placeOrderService,
        IBinanceConfigService binanceConfigService)
    {
        _botClient = botClient;
        _logger = logger;
        _configuration = configuration;
        _distributedCache = distributedCache;
        _binanceConfigService = binanceConfigService;
        _placeOrderService = placeOrderService;
    }

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    public Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(update.Type);
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            //{ EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            //{ CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            //{ InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            //{ ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        //_logger.LogInformation("Receive message type: {MessageType}", message.Type);
        //_logger.LogInformation("Receive message usernmae: {UserName}", message.From?.Username);
        if (message.Text is not { } messageText)
            return;
        var messAccpect = new string[] { "/long", "/short", "/cancel", "/orders", "/pos", "/addkey", "/close", "/help", "/start", "/menu" };
        if (!messAccpect.Any(x => message.Text.StartsWith(x)))
            return;
        var threeDigitSymbols = this._configuration.GetValue<string>("AppSettings:ThreeDigitSymbols");
        var twoDigitSymbols = this._configuration.GetValue<string>("AppSettings:TwoDigitSymbols");
        var zeroDigitSymbols = this._configuration.GetValue<string>("AppSettings:ZeroDigitSymbols");
        var action = messageText.Split(' ')[0] switch
        {
            "/long" => SendLongKeyboard(_botClient, threeDigitSymbols, twoDigitSymbols, zeroDigitSymbols, message, cancellationToken),
            "/short" => SendShortKeyboard(_botClient, threeDigitSymbols, twoDigitSymbols, zeroDigitSymbols, message, cancellationToken),
            "/cancel" => SendCancelKeyboard(_botClient, message, cancellationToken),
            "/orders" => SendOrdersKeyboard(_botClient, message, cancellationToken),
            "/pos" => SendPosKeyboard(_botClient, message, cancellationToken),
            "/addkey" => SendAddKeyKeyboard(_botClient, message, cancellationToken),
            "/close" => SendCloseKeyboard(_botClient, threeDigitSymbols, twoDigitSymbols, zeroDigitSymbols, message, cancellationToken),
            //"/photo" => SendFile(_botClient, message, cancellationToken),
            //"/request" => RequestContactAndLocation(_botClient, message, cancellationToken),
            //"/inline_mode" => StartInlineQuery(_botClient, message, cancellationToken),
            _ => Usage(_botClient, message, cancellationToken)
        };
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        _logger.LogInformation("The message was sent with id: {SentMessageId}", messageText);

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        async Task<Message> SendCloseKeyboard(ITelegramBotClient botClient, string threeDigitSymbolSetting, string twoDigitSymbolSetting, string zeroDigitSymbolSetting, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /close [OrderId] [% hoặc số coin đóng]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length != 3)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /close [OrderId] [% hoặc số coin đóng]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (!long.TryParse(data[1], out var orderId))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "OrderId phải là số nguyên - là số orderId trả về khi long short",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var order = this.GetOrder(orderId, message.From?.Username ?? "");
                if (order == null)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không tìm thấy thông tin order trên hệ thống bot, vào app quay tay để đóng vào",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                decimal quantity = 0;
                var decimals = 1;
                if (!string.IsNullOrEmpty(threeDigitSymbolSetting))
                {
                    var threeDigitSymbols = threeDigitSymbolSetting.Split(",");
                    if (threeDigitSymbols.FirstOrDefault(x => order.Symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        decimals = 3;
                    }
                }

                if (!string.IsNullOrEmpty(twoDigitSymbolSetting))
                {
                    var twoDigitSymbols = twoDigitSymbolSetting.Split(",");
                    if (twoDigitSymbols.FirstOrDefault(x => order.Symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        decimals = 2;
                    }
                }

                if (!string.IsNullOrEmpty(zeroDigitSymbolSetting))
                {
                    var zeroDigitSymbols = zeroDigitSymbolSetting.Split(",");
                    if (zeroDigitSymbols.FirstOrDefault(x => order.Symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        decimals = 0;
                    }
                }

                if (data[2].EndsWith("%"))
                {
                    if (!decimal.TryParse(data[2].Trim().Replace("%", ""), System.Globalization.NumberStyles.Float, null, out var rate))
                    {
                        return await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "Tỷ lệ đóng vị thế không đúng, nhập kết thúc bằng %",
                                replyMarkup: new ReplyKeyboardRemove(),
                                cancellationToken: cancellationToken);
                    }

                    quantity = decimal.Round(order.Quantity * rate / 100, decimals, MidpointRounding.AwayFromZero);
                }
                else
                {
                    if (!decimal.TryParse(data[2], out var inputQuantity))
                    {
                        return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Số lượng đóng vị thế không hợp lệ",
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                    }
                    quantity = decimal.Round(inputQuantity, decimals, MidpointRounding.AwayFromZero);
                }

                var positionSide = order.PositionSide == 0 ? PositionSide.Short : PositionSide.Long;
                var side = positionSide == PositionSide.Long ? OrderSide.Sell : OrderSide.Buy;

                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var apiKeyData = this.GetBinanceConfig(message.From?.Username ?? "");
                if (apiKeyData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Chưa cấu hình api key binance\nGõ /addkey để thực hiện hoặc /help để xem hướng dẫn",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }

                // khởi tạo kết nối API với Binance
                BinanceClient client = new BinanceClient(new BinanceClientOptions
                {
                    ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
                });

                //Console.WriteLine($"takeprofit: {takeprofit}");
                TimeInForce timeInForce = TimeInForce.GoodTillCanceled;  // loại giới hạn thời gian (nếu áp dụng): GoodTillCancel, ImmediateOrCancel, FillOrKill

                //Console.WriteLine($"Quantity: {quantity}");
                // tạo một đơn đặt hàng mới với thông tin tài sản, giá và số lượng cụ thể

                // gửi yêu cầu đặt hàng mới đến Binance API
                var orderResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(order.Symbol, side, FuturesOrderType.Market, quantity, null, closePosition: true, positionSide: positionSide, timeInForce: timeInForce);
                //Console.WriteLine(JsonConvert.SerializeObject(orderResult));
                if (orderResult.Data != null)
                {
                    //xóa order
                    await this.DeleteOrder(order);

                    return await botClient.SendTextMessageAsync(
                      chatId: message.Chat.Id,
                      text: $"Đóng vị thế {data[1]} {order.Symbol.ToLower()} thành công",
                      replyMarkup: new ReplyKeyboardRemove(),
                      cancellationToken: cancellationToken);
                }
                else
                {
                    return await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: "Không đóng được lệnh, quay tay thôi nào",
                  replyMarkup: new ReplyKeyboardRemove(),
                  cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Lỗi: {ex.Message}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
        }

        //lấy thông tin vị thế
        async Task<Message> SendPosKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /pos  [Tên cặp]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length != 2)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /pos  [Tên cặp]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                var symbol = data[1];
                if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
                {
                    symbol = $"{symbol}USDT";
                }
                // khởi tạo kết nối API với Binance
                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                //thông tin api binance đã config
                var apiKeyData = this.GetBinanceConfig(message.From?.Username ?? "");
                if (apiKeyData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Chưa cấu hình api key binance\nGõ /addkey để thực hiện hoặc /help để xem hướng dẫn",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }
                BinanceClient client = new BinanceClient(new BinanceClientOptions
                {
                    ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
                });

                var posResult = await client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
                if (posResult != null && posResult.Data != null)
                {
                    var posInfo = new StringBuilder();
                    if (posResult.Data.Any() && posResult.Data.FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.CurrentCultureIgnoreCase)) != null)
                    {
                        posInfo.Append($"Tên cặp: {symbol.ToUpper()}\n");
                        foreach (var item in posResult.Data.Where(x => x.Quantity > 0))
                        {
                            posInfo.Append($"Số lượng: {item.Quantity} Coin\n");
                            posInfo.Append($"Vị thế: {item.PositionSide.ToString()} - x{item.Leverage} - {item.MarginType.ToString()}\n");
                            posInfo.Append($"Entry: {item.EntryPrice}\n");
                            posInfo.Append($"Giá đánh dấu: {item.MarkPrice}\n");
                            posInfo.Append($"Giá thanh lý: {item.LiquidationPrice}\n");
                            posInfo.Append($"Lợi nhuận chưa ghi nhận: {item.UnrealizedPnl}\n");
                        }
                    }
                    else
                    {
                        posInfo.Append("Không lấy được thông tin vị thế\nVị thế chưa được mở");
                    }

                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: posInfo.ToString(),
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Không lấy được thông tin vị thế\nVị thế chưa được mở",
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Thiết lập cấu hình toang rồi, lỗi: " + ex.Message,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
            }
        }

        //Add key thao tác tài khoản binance
        async Task<Message> SendAddKeyKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /addkey [ApiKey Mã hóa] [SecretKey Mã hóa] [MaxVol USDT]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length < 4 && data.Length > 5)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /addkey [ApiKey Mã hóa] [SecretKey Mã hóa] [MaxVol USDT]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                if (!long.TryParse(data[3], out var maxvol))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được Maxvol",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                //set config vào cache và lưu db
                var config = new BinanceConfig()
                {
                    Name = message.From?.Username ?? "",
                    Key = data[1],
                    Secret = data[2],
                    MaxVol = maxvol,
                    EncryptionKey = data.Length == 5 ? data[4] : ""
                };
                await this.SetBinanceConfig(config);
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Thiết lập cấu hình thành công, bắt đầu long short luôn nào",
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Thiết lập cấu hình toang rồi, lỗi: " + ex.Message,
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
            }
        }

        // Send inline keyboard
        // You can process responses in BotOnCallbackQueryReceived handler
        async Task<Message> SendLongKeyboard(ITelegramBotClient botClient, string threeDigitSymbolSetting, string twoDigitSymbolSetting, string zeroDigitSymbolSetting, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /long [Tên cặp] [Volume] [Entry] [SL] [TP]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length < 4)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /long [Tên cặp] [Volume] [Entry] [SL] [TP]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                var symbol = data[1];
                var sQuantity = data[2];
                var sPrice = data[3];
                var sStoploss = "0";
                var sTakeprofit = "0";

                if (data.Length > 4)
                {
                    sStoploss = data[4];
                }

                if (data.Length > 5)
                {
                    sTakeprofit = data[5];
                }

                if (!decimal.TryParse(sQuantity, System.Globalization.NumberStyles.Float, null, out var quantity))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Volume nhập sai",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                //Console.WriteLine($"Quantity: {quantity}");

                if (!decimal.TryParse(sPrice, System.Globalization.NumberStyles.Float, null, out var price))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Entry nhập không đúng",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                //Console.WriteLine($"price: {price}");

                if (!decimal.TryParse(sStoploss, System.Globalization.NumberStyles.Float, null, out var stoploss))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Stoploss nhập không đúng",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                //Console.WriteLine($"stoploss: {stoploss}");

                if (!decimal.TryParse(sTakeprofit, System.Globalization.NumberStyles.Float, null, out var takeprofit))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Takeprofit nhập không đúng",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var apiKeyData = this.GetBinanceConfig(message.From?.Username ?? "");
                if (apiKeyData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Chưa cấu hình api key binance\nGõ /addkey để thực hiện hoặc /help để xem hướng dẫn",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }

                // khởi tạo kết nối API với Binance
                BinanceClient client = new BinanceClient(new BinanceClientOptions
                {
                    ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
                });

                //Console.WriteLine($"takeprofit: {takeprofit}");
                TimeInForce timeInForce = TimeInForce.GoodTillCanceled;  // loại giới hạn thời gian (nếu áp dụng): GoodTillCancel, ImmediateOrCancel, FillOrKill
                var positionSide = PositionSide.Long;
                var side = OrderSide.Buy;
                var type = FuturesOrderType.Limit;
                if (quantity <= 0)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Volume phải lớn hơn 0",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (price <= 0)
                {
                    type = FuturesOrderType.Market;
                }

                if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
                {
                    symbol = $"{symbol}USDT";
                }



                if (quantity > apiKeyData.MaxVol)
                {
                    quantity = apiKeyData.MaxVol;
                }
                //Console.WriteLine($"Quantity: {quantity}");
                if (type == FuturesOrderType.Limit)
                {
                    var decimals = 1;
                    if (!string.IsNullOrEmpty(threeDigitSymbolSetting))
                    {
                        var threeDigitSymbols = threeDigitSymbolSetting.Split(",");
                        if (threeDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            decimals = 3;
                        }
                    }

                    if (!string.IsNullOrEmpty(twoDigitSymbolSetting))
                    {
                        var twoDigitSymbols = twoDigitSymbolSetting.Split(",");
                        if (twoDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            decimals = 2;
                        }
                    }

                    if (!string.IsNullOrEmpty(zeroDigitSymbolSetting))
                    {
                        var zeroDigitSymbols = zeroDigitSymbolSetting.Split(",");
                        if (zeroDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            decimals = 0;
                        }
                    }
                    quantity = decimal.Round(quantity / price, decimals, MidpointRounding.AwayFromZero);
                }
                //Console.WriteLine($"Quantity: {quantity}");
                // tạo một đơn đặt hàng mới với thông tin tài sản, giá và số lượng cụ thể

                // gửi yêu cầu đặt hàng mới đến Binance API
                var orderResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity, type == FuturesOrderType.Market ? null : price, positionSide: positionSide, timeInForce: timeInForce);
                //Console.WriteLine(JsonConvert.SerializeObject(orderResult));
                if (orderResult.Data != null)
                {
                    //Lưu order để close
                    var order = new PlaceOrder()
                    {
                        OrderId = orderResult.Data.Id,
                        ClientOrderId = orderResult.Data.ClientOrderId,
                        UserName = message.From?.Username ?? "",
                        Pair = orderResult.Data.Pair,
                        Symbol = orderResult.Data.Symbol,
                        Price = orderResult.Data.Price,
                        Quantity = orderResult.Data.Quantity,
                        PositionSide = (int)orderResult.Data.PositionSide,
                        Side = (int)orderResult.Data.Side,
                    };
                    await this.AddOrder(order);
                    //Build thêm thông tin về tp, sl
                    var botResult = new StringBuilder();
                    botResult.Append($"OrderId: {orderResult.Data.Id}\n");
                    if (stoploss > 0 || takeprofit > 0)
                    {
                        var slSide = OrderSide.Sell;
                        var slPositionSide = PositionSide.Long;

                        if (stoploss > 0)
                        {
                            var slResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, slSide, FuturesOrderType.StopMarket, quantity, workingType: WorkingType.Mark, closePosition: true, stopPrice: stoploss, positionSide: slPositionSide, timeInForce: timeInForce);
                            if (slResult != null && slResult.Data != null)
                            {
                                botResult.Append($"Stoploss OrderId: {slResult.Data.Id}\n");
                            }
                            else
                            {
                                botResult.Append($"Stoploss: Chưa được thiết lập\n");
                            }
                        }

                        if (takeprofit > 0)
                        {
                            var tkResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, slSide, FuturesOrderType.TakeProfitMarket, quantity, workingType: WorkingType.Mark, closePosition: true, stopPrice: takeprofit, positionSide: slPositionSide, timeInForce: timeInForce);
                            if (tkResult != null && tkResult.Data != null)
                            {
                                botResult.Append($"TakeProfit OrderId: {tkResult.Data.Id}\n");
                            }
                            else
                            {
                                botResult.Append($"TakeProfit: Chưa được thiết lập\n");
                            }
                        }
                    }
                    return await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: botResult.ToString(),
                  replyMarkup: new ReplyKeyboardRemove(),
                  cancellationToken: cancellationToken);
                }
                else
                {
                    return await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: "Không đặt được lệnh",
                  replyMarkup: new ReplyKeyboardRemove(),
                  cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Lỗi: {ex.Message}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
        }

        async Task<Message> SendShortKeyboard(ITelegramBotClient botClient, string threeDigitSymbolSetting, string twoDigitSymbolSetting, string zeroDigitSymbolSetting, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /short [Tên cặp] [Volume] [Entry] [SL] [TP]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length < 4)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /short [Tên cặp] [Volume] [Entry] [SL] [TP]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                var symbol = data[1];
                var sQuantity = data[2];
                var sPrice = data[3];
                var sStoploss = "0";
                var sTakeprofit = "0";

                if (data.Length > 4)
                {
                    sStoploss = data[4];
                }

                if (data.Length > 5)
                {
                    sTakeprofit = data[5];
                }

                if (!decimal.TryParse(sQuantity, System.Globalization.NumberStyles.Float, null, out var quantity))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Volume nhập sai",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (!decimal.TryParse(sPrice, System.Globalization.NumberStyles.Float, null, out var price))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Entry nhập không đúng",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (!decimal.TryParse(sStoploss, System.Globalization.NumberStyles.Float, null, out var stoploss))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Stoploss nhập không đúng",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (!decimal.TryParse(sTakeprofit, System.Globalization.NumberStyles.Float, null, out var takeprofit))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Takeprofit nhập không đúng",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var apiKeyData = this.GetBinanceConfig(message.From?.Username ?? "");
                if (apiKeyData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Chưa cấu hình api key binance\nGõ /addkey để thực hiện hoặc /help để xem hướng dẫn",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }

                // khởi tạo kết nối API với Binance
                BinanceClient client = new BinanceClient(new BinanceClientOptions
                {
                    ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
                });

                TimeInForce timeInForce = TimeInForce.GoodTillCanceled;  // loại giới hạn thời gian (nếu áp dụng): GoodTillCancel, ImmediateOrCancel, FillOrKill
                var positionSide = PositionSide.Short;
                var side = OrderSide.Sell;
                var type = FuturesOrderType.Limit;
                if (quantity <= 0)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Volume phải lớn hơn 0",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (price <= 0)
                {
                    type = FuturesOrderType.Market;
                }

                if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
                {
                    symbol = $"{symbol}USDT";
                }



                if (quantity > apiKeyData.MaxVol)
                {
                    quantity = apiKeyData.MaxVol;
                }
                if (type == FuturesOrderType.Limit)
                {
                    var decimals = 1;
                    if (!string.IsNullOrEmpty(threeDigitSymbolSetting))
                    {
                        var threeDigitSymbols = threeDigitSymbolSetting.Split(",");
                        if (threeDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            decimals = 3;
                        }
                    }

                    if (!string.IsNullOrEmpty(twoDigitSymbolSetting))
                    {
                        var twoDigitSymbols = twoDigitSymbolSetting.Split(",");
                        if (twoDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            decimals = 2;
                        }
                    }

                    if (!string.IsNullOrEmpty(zeroDigitSymbolSetting))
                    {
                        var zeroDigitSymbols = zeroDigitSymbolSetting.Split(",");
                        if (zeroDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
                        {
                            decimals = 0;
                        }
                    }
                    quantity = decimal.Round(quantity / price, decimals, MidpointRounding.AwayFromZero);
                }
                // tạo một đơn đặt hàng mới với thông tin tài sản, giá và số lượng cụ thể

                // gửi yêu cầu đặt hàng mới đến Binance API
                var orderResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity, type == FuturesOrderType.Market ? null : price, positionSide: positionSide, timeInForce: timeInForce);
                if (orderResult.Data != null)
                {
                    //Lưu order để close
                    var order = new PlaceOrder()
                    {
                        OrderId = orderResult.Data.Id,
                        ClientOrderId = orderResult.Data.ClientOrderId,
                        UserName = message.From?.Username ?? "",
                        Pair = orderResult.Data.Pair,
                        Symbol = orderResult.Data.Symbol,
                        Price = orderResult.Data.Price,
                        Quantity = orderResult.Data.Quantity,
                        PositionSide = (int)orderResult.Data.PositionSide,
                        Side = (int)orderResult.Data.Side,
                    };
                    await this.AddOrder(order);
                    //Build thêm thông tin về tp, sl
                    var botResult = new StringBuilder();
                    botResult.Append($"OrderId: {orderResult.Data.Id}\n");
                    if (stoploss > 0 || takeprofit > 0)
                    {
                        var slSide = OrderSide.Buy;
                        var slPositionSide = PositionSide.Short;

                        if (stoploss > 0)
                        {
                            var slResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, slSide, FuturesOrderType.StopMarket, quantity, workingType: WorkingType.Mark, closePosition: true, stopPrice: stoploss, positionSide: slPositionSide, timeInForce: timeInForce);
                            if (slResult != null && slResult.Data != null)
                            {
                                botResult.Append($"Stoploss OrderId: {slResult.Data.Id}\n");
                            }
                            else
                            {
                                botResult.Append($"Stoploss: Chưa được thiết lập\n");
                            }
                        }

                        if (takeprofit > 0)
                        {
                            var tkResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, slSide, FuturesOrderType.TakeProfitMarket, quantity, workingType: WorkingType.Mark, closePosition: true, stopPrice: takeprofit, positionSide: slPositionSide, timeInForce: timeInForce);
                            if (tkResult != null && tkResult.Data != null)
                            {
                                botResult.Append($"TakeProfit OrderId: {tkResult.Data.Id}\n");
                            }
                            else
                            {
                                botResult.Append($"TakeProfit: Chưa được thiết lập\n");
                            }
                        }
                    }
                    return await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: botResult.ToString(),
                  replyMarkup: new ReplyKeyboardRemove(),
                  cancellationToken: cancellationToken);
                }
                else
                {
                    return await botClient.SendTextMessageAsync(
                  chatId: message.Chat.Id,
                  text: "Không đặt được lệnh",
                  replyMarkup: new ReplyKeyboardRemove(),
                  cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Lỗi: {ex.Message}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
        }

        async Task<Message> SendCancelKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /cancel [Tên cặp] [OrderId], lưu ý [OrderId] có thể truyền hoặc không",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length < 2)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /cancel [Tên cặp] [OrderId], lưu ý [OrderId] có thể truyền hoặc không",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                long orderId = 0;
                if (data.Length > 2)
                {
                    long.TryParse(data[2], out orderId);
                }

                var symbol = data[1];
                if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
                {
                    symbol = $"{symbol}USDT";
                }
                // khởi tạo kết nối API với Binance
                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var apiKeyData = this.GetBinanceConfig(message.From?.Username ?? "");
                if (apiKeyData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Chưa cấu hình api key binance\nGõ /addkey để thực hiện hoặc /help để xem hướng dẫn",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }
                BinanceClient client = new BinanceClient(new BinanceClientOptions
                {
                    ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
                });

                if (orderId == 0)
                {
                    var calcelAllResult = await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);
                    if (calcelAllResult.Data == null)
                    {
                        return await botClient.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: "Không đặt được lệnh",
                              replyMarkup: new ReplyKeyboardRemove(),
                              cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await this.DeleteOrders(symbol.ToLower(), message.From?.Username ?? "");
                        return await botClient.SendTextMessageAsync(
                                     chatId: message.Chat.Id,
                                      text: $"Hủy thành công tất cả order của cặp {symbol}",
                                     replyMarkup: new ReplyKeyboardRemove(),
                                     cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    var calcelResult = await client.UsdFuturesApi.Trading.CancelOrderAsync(symbol, orderId);
                    if (calcelResult.Data == null)
                    {
                        return await botClient.SendTextMessageAsync(
                              chatId: message.Chat.Id,
                              text: "Không đặt được lệnh",
                              replyMarkup: new ReplyKeyboardRemove(),
                              cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await this.DeleteOrder(new PlaceOrder() { OrderId = orderId, UserName = message.From?.Username ?? "" });
                        return await botClient.SendTextMessageAsync(
                     chatId: message.Chat.Id,
                     text: $"Hủy thành công order có id {orderId} của cặp {symbol}",
                     replyMarkup: new ReplyKeyboardRemove(),
                     cancellationToken: cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Lỗi: {ex.Message}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
        }

        //Lấy các vị thế đang mở
        async Task<Message> SendOrdersKeyboard(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /orders [Tên coin]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var data = message.Text.Split();
                if (data.Length != 2)
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Sai cú pháp\nCú pháp đúng /orders [Tên coin]",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var symbol = data[1];
                if (string.IsNullOrEmpty(symbol))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Thiếu thông tin cặp để xem orders",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }

                if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
                {
                    symbol = $"{symbol}USDT";
                }

                if (string.IsNullOrEmpty(message.From?.Username))
                {
                    return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Không lấy được username",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
                }
                var apiKeyData = this.GetBinanceConfig(message.From?.Username ?? "");
                if (apiKeyData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Chưa cấu hình api key binance\nGõ /addkey để thực hiện hoặc /help để xem hướng dẫn",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }
                // khởi tạo kết nối API với Binance
                BinanceClient client = new BinanceClient(new BinanceClientOptions
                {
                    ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
                });
                var usdFuturesSymbolData = await client.UsdFuturesApi.Trading.GetOrdersAsync(symbol);
                if (usdFuturesSymbolData == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Không lấy được thông tin orders",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }

                if (usdFuturesSymbolData.Data == null)
                {
                    return await botClient.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Không lấy được thông tin orders",
                   replyMarkup: new ReplyKeyboardRemove(),
                   cancellationToken: cancellationToken);
                }
                var orderStringBuilder = new StringBuilder();
                if (usdFuturesSymbolData.Data.Where(x => x.Status == OrderStatus.New).Any())
                {
                    foreach (var item in usdFuturesSymbolData.Data.Where(x => x.Status == OrderStatus.New))
                    {
                        orderStringBuilder.Append($"OrderId: {item.Id} - Quantity: {item.Quantity} - Price: {item.Price} - StopPrice: {item.StopPrice} - {item.UpdateTime.ToString("G")} \n");
                    }
                }
                else
                {
                    orderStringBuilder.Append("Không tìm thấy orders nào đang chờ thực hiện");
                }

                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: orderStringBuilder.ToString(),
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Lỗi: {ex.Message}",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken);
            }
        }

        //static async Task<Message> SendFile(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        //{
        //    await botClient.SendChatActionAsync(
        //        message.Chat.Id,
        //        ChatAction.UploadPhoto,
        //        cancellationToken: cancellationToken);

        //    const string filePath = "Files/tux.png";
        //    await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //    var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

        //    return await botClient.SendPhotoAsync(
        //        chatId: message.Chat.Id,
        //        photo: new InputFile(fileStream, fileName),
        //        caption: "Nice Picture",
        //        cancellationToken: cancellationToken);
        //}

        //static async Task<Message> RequestContactAndLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        //{
        //    ReplyKeyboardMarkup RequestReplyKeyboard = new(
        //        new[]
        //        {
        //            KeyboardButton.WithRequestLocation("Location"),
        //            KeyboardButton.WithRequestContact("Contact"),
        //        });

        //    return await botClient.SendTextMessageAsync(
        //        chatId: message.Chat.Id,
        //        text: "Who or Where are you?",
        //        replyMarkup: RequestReplyKeyboard,
        //        cancellationToken: cancellationToken);
        //}

        async Task<Message> Usage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            const string usage = "Usage:\n" +
                                 "/long   -   Mở vị thế long\nCú pháp /long [Tên cặp] [Volume] [Entry] [SL] [TP]\nVí dụ: /long SNX 100 2.95 2.8 4\n\n" +
                                 "/short  -   Mở vị thế short\nCú pháp /short [Tên cặp] [Volume] [Entry] [SL] [TP]\nVí dụ: /short SNX 100 4 4.1 3\n\n" +
                                 "/cancel -   Đóng vị thế đang order\nCú pháp /cancel [Tên cặp] [OrderId], chỉ truyền mỗi tên cặp sẽ đóng tất cả lệnh mở cho cặp đó\n\n" +
                                 "/orders -   Lấy tất cả các vị thế chưa mở\nCú pháp /orders  [Tên cặp]\nVí dụ: /orders SNX\n\n" +
                                 "/pos    -   Lấy thông tin vị thế\nCú pháp /pos  [Tên cặp]\nVí dụ: /pos SNX\n\n" +
                                 "/close  -   Đóng vị thế đang mở - dùng để chốt lời hoặc cắt lỗ ngay tức thì\nCú pháp /close [OrderId] [% hoặc số coin đóng]\nVí dụ: /close 126881424559 100% hoặc /close 126881424559 30\n\n" +
                                 "/addkey -   Thêm cấu hình key thao tác với tài khoản binance\nCú pháp /addkey [ApiKey] [SecretKey] [MaxVol] [EncryptionKey]\n- ApiKey - Lấy từ binance và mã hóa qua tool bot cung cấp\n- SecretKey - Lấy từ binance và mã hóa qua tool bot cung cấp\n- MaxVol - Volume tối đa khi mở 1 lệnh\n- EncryptionKey - Chuỗi bất kỳ được dùng khi thực hiện mã hóa ApiKey và SecretKey\nVí dụ: /addkey zVSYmdrHa2f39amIVjSKykIIDirwgNJAeybXVx8r1OH3K8... seHAtrY1lt4Thbzha3bOmZmhPZXK... 100 94Hfh48sjhd";

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: usage,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        //static async Task<Message> StartInlineQuery(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        //{
        //    InlineKeyboardMarkup inlineKeyboard = new(
        //        InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Inline Mode"));

        //    return await botClient.SendTextMessageAsync(
        //        chatId: message.Chat.Id,
        //        text: "Press the button to start Inline Query",
        //        replyMarkup: inlineKeyboard,
        //        cancellationToken: cancellationToken);
        //}
    }

    // Process Inline Keyboard callback data
    //private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    //{
    //    _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

    //    await _botClient.AnswerCallbackQueryAsync(
    //        callbackQueryId: callbackQuery.Id,
    //        text: $"Received {callbackQuery.Data}",
    //        cancellationToken: cancellationToken);

    //    await _botClient.SendTextMessageAsync(
    //        chatId: callbackQuery.Message!.Chat.Id,
    //        text: $"Received {callbackQuery.Data}",
    //        cancellationToken: cancellationToken);
    //}

    //#region Inline Mode

    //private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    //{
    //    _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

    //    InlineQueryResult[] results = {
    //        // displayed result
    //        new InlineQueryResultArticle(
    //            id: "1",
    //            title: "TgBots",
    //            inputMessageContent: new InputTextMessageContent("hello"))
    //    };

    //    await _botClient.AnswerInlineQueryAsync(
    //        inlineQueryId: inlineQuery.Id,
    //        results: results,
    //        cacheTime: 0,
    //        isPersonal: true,
    //        cancellationToken: cancellationToken);
    //}

    //private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    //{
    //    _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

    //    await _botClient.SendTextMessageAsync(
    //        chatId: chosenInlineResult.From.Id,
    //        text: $"You chose result with Id: {chosenInlineResult.ResultId}",
    //        cancellationToken: cancellationToken);
    //}

    //#endregion

    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lấy order
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns></returns>
    private PlaceOrder? GetOrder(long orderId, string userName)
    {
        try
        {
            var cacheKey = $"binord_{orderId}";
            var cacheData = this._distributedCache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(cacheData))
            {
                return JsonConvert.DeserializeObject<PlaceOrder>(cacheData);
            }
            var order = this._placeOrderService.GetOrder(orderId, userName);
            if (order == null) return null;
            //set vào cache tý lấy lại
            this._distributedCache.SetString(cacheKey, JsonConvert.SerializeObject(order));
            return order;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Xóa order
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    private async Task DeleteOrders(string symbol, string userName)
    {
        var orders = await this._placeOrderService.DeleteOrders(symbol, userName);
        if (orders != null && orders.Count > 0)
        {
            foreach (var item in orders)
            {
                var cacheKey = $"binord_{item.id}";
                //xóa cache
                this._distributedCache.Remove(cacheKey);
            }
        }
    }

    /// <summary>
    /// Xóa order
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    private async Task DeleteOrder(PlaceOrder order)
    {
        var cacheKey = $"binord_{order.id}";
        //xóa cache
        this._distributedCache.Remove(cacheKey);
        //delete trong db
        await this._placeOrderService.DeleteOrder(order);
    }

    /// <summary>
    /// Thêm order
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    private async Task AddOrder(PlaceOrder order)
    {
        var cacheKey = $"binord_{order.id}";
        //xóa cache
        this._distributedCache.Remove(cacheKey);
        this._distributedCache.SetString(cacheKey, JsonConvert.SerializeObject(order));
        //set vào db
        await this._placeOrderService.InsertOrder(order);
    }

    /// <summary>
    /// Lấy thông tin config theo user hiện tại
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private BinanceConfig? GetBinanceConfig(string name)
    {
        try
        {
            var cacheKey = $"binc_{name}";
            var configCacheData = this._distributedCache.GetString(cacheKey);
            if (!string.IsNullOrEmpty(configCacheData))
            {
                var binanceConfig = JsonConvert.DeserializeObject<BinanceConfig>(configCacheData);
                if (binanceConfig == null)
                {
                    return null;
                }
                return new BinanceConfig()
                {
                    Name = binanceConfig.Name,
                    MaxVol = binanceConfig.MaxVol,
                    Key = EncryptionHelper.Decrypt(binanceConfig.Key, binanceConfig.EncryptionKey),
                    Secret = EncryptionHelper.Decrypt(binanceConfig.Secret, binanceConfig.EncryptionKey)
                };
            }
            var config = this._binanceConfigService.GetConfig(name);
            if (config == null) return null;
            //set vào cache tý lấy lại
            this._distributedCache.SetString(cacheKey, JsonConvert.SerializeObject(config));
            return new BinanceConfig()
            {
                Name = config.Name,
                MaxVol = config.MaxVol,
                Key = EncryptionHelper.Decrypt(config.Key, config.EncryptionKey),
                Secret = EncryptionHelper.Decrypt(config.Secret, config.EncryptionKey)
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Thiết lập cấu hình config để trade
    /// </summary>
    /// <param name="binanceConfig"></param>
    /// <returns></returns>
    private async Task SetBinanceConfig(BinanceConfig binanceConfig)
    {
        var cacheKey = $"binc_{binanceConfig.Name}";
        //xóa cache
        this._distributedCache.Remove(cacheKey);
        this._distributedCache.SetString(cacheKey, JsonConvert.SerializeObject(binanceConfig));
        //set vào db
        await this._binanceConfigService.InsertConfig(binanceConfig);
    }
}
