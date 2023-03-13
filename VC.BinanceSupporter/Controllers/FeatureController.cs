using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Microsoft.AspNetCore.Mvc;
using VC.BinanceSupporter.Constant;
using VC.BinanceSupporter.Model;
using VC.BinanceSupporter.Util;

namespace VC.BinanceSupporter.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FeatureController : ControllerBase
    {
        //private readonly List<ApiKey> _apiKeys;
        //private readonly IConfiguration _configuration;
        //public FeatureController(List<ApiKey> apiKeys, IConfiguration configuration)
        //{
        //    this._apiKeys = apiKeys;
        //    this._configuration = configuration;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="symbol"></param>
        ///// <returns></returns>
        //[HttpGet("brackets")]
        //public async Task<IActionResult> GetBracketsAsync(string symbol)
        //{
        //    var res = new ServiceResult();
        //    if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
        //    {
        //        symbol = $"{symbol}USDT";
        //    }

        //    var key = "";
        //    var secret = "";
        //    // khởi tạo kết nối API với Binance
        //    BinanceClient client = new BinanceClient(new BinanceClientOptions
        //    {
        //        ApiCredentials = new BinanceApiCredentials(key, secret),
        //    });
        //    var result = await client.UsdFuturesApi.Account.GetBracketsAsync(symbol);
        //    return Ok(result.Data.FirstOrDefault()?.Brackets.FirstOrDefault(x => x.MaintAmount == 0));
        //}

        ///// <summary>
        ///// GetOrder
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //[HttpGet("orders")]
        //public async Task<IActionResult> GetOrder(string name, string symbol)
        //{
        //    var res = new ServiceResult();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(symbol))
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require;
        //            return Ok(res);
        //        }

        //        if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
        //        {
        //            symbol = $"{symbol}USDT";
        //        }

        //        var apiKeyData = GetApiKey(name);
        //        if (apiKeyData == null)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.NotApiKey;
        //            return Ok(res);
        //        }
        //        // khởi tạo kết nối API với Binance
        //        BinanceClient client = new BinanceClient(new BinanceClientOptions
        //        {
        //            ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
        //        });
        //        var usdFuturesSymbolData = await client.UsdFuturesApi.Trading.GetOrdersAsync(symbol);
        //        if (usdFuturesSymbolData == null)
        //        {
        //            res.code = ServiceResultConstant.NoData;
        //            res.status = 0;
        //            return Ok(res);
        //        }
        //        res.data = usdFuturesSymbolData.Data.Where(x => x.Status == OrderStatus.New);
        //    }
        //    catch (Exception ex)
        //    {
        //        res.status = 0;
        //        res.code = ServiceResultConstant.Exception;
        //        res.message = ex.Message;
        //        Console.WriteLine(ex);
        //    }
        //    return Ok(res);
        //}

        ///// <summary>
        ///// Order
        ///// </summary>
        ///// <param name="symbol"></param>
        ///// <param name="quantity"></param>
        ///// <param name="price"></param>
        ///// <param name="side"></param>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //[HttpPost("orders")]
        //public async Task<IActionResult> PlaceOrder([FromBody] OrderParam orderParam)
        //{
        //    var res = new ServiceResult();
        //    try
        //    {
        //        if (orderParam.price < 0)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require;
        //            return Ok(res);
        //        }
        //        if (orderParam.quantity < 0)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require;
        //            return Ok(res);
        //        }
        //        if (orderParam.price == 0 && orderParam.type == FuturesOrderType.Limit)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require;
        //            return Ok(res);
        //        }
        //        if (orderParam.quantity == 0 && orderParam.type == FuturesOrderType.Limit && !orderParam.market)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require; ;
        //            return Ok(res);
        //        }
        //        if (string.IsNullOrEmpty(orderParam.symbol))
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require;
        //            return Ok(res);
        //        }
        //        var apiKeyData = GetApiKey(orderParam.ApiName);
        //        if (apiKeyData == null)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.NotApiKey;
        //            return Ok(res);
        //        }
        //        var symbol = orderParam.symbol;
        //        if (!orderParam.symbol.EndsWith("USDT") || !orderParam.symbol.EndsWith("BUSD"))
        //        {
        //            symbol = $"{symbol}USDT";
        //        }
        //        // khởi tạo kết nối API với Binance
        //        BinanceClient client = new BinanceClient(new BinanceClientOptions
        //        {
        //            ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
        //        });
        //        decimal quantity = orderParam.quantity;
        //        if (orderParam.type == FuturesOrderType.Limit)
        //        {
        //            var aquantity = orderParam.quantity / orderParam.price;
        //            var decimals = 1;
        //            var threeDigitSymbolSetting = this._configuration.GetValue<string>("AppSettings:ThreeDigitSymbols");
        //            if (!string.IsNullOrEmpty(threeDigitSymbolSetting))
        //            {
        //                var threeDigitSymbols = threeDigitSymbolSetting.Split(",");
        //                if (threeDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
        //                {
        //                    decimals = 3;
        //                }
        //            }

        //            var twoDigitSymbolSetting = this._configuration.GetValue<string>("AppSettings:TwoDigitSymbols");
        //            if (!string.IsNullOrEmpty(twoDigitSymbolSetting))
        //            {
        //                var twoDigitSymbols = twoDigitSymbolSetting.Split(",");
        //                if (twoDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
        //                {
        //                    decimals = 2;
        //                }
        //            }

        //            var zeroDigitSymbolSetting = this._configuration.GetValue<string>("AppSettings:ZeroDigitSymbols");
        //            if (!string.IsNullOrEmpty(zeroDigitSymbolSetting))
        //            {
        //                var zeroDigitSymbols = zeroDigitSymbolSetting.Split(",");
        //                if (zeroDigitSymbols.FirstOrDefault(x => symbol.Contains(x, StringComparison.CurrentCultureIgnoreCase)) != null)
        //                {
        //                    decimals = 0;
        //                }
        //            }
        //            quantity = decimal.Round(orderParam.quantity / orderParam.price, decimals, MidpointRounding.AwayFromZero);
        //        }
        //        // tạo một đơn đặt hàng mới với thông tin tài sản, giá và số lượng cụ thể
        //        TimeInForce timeInForce = TimeInForce.GoodTillCanceled;  // loại giới hạn thời gian (nếu áp dụng): GoodTillCancel, ImmediateOrCancel, FillOrKill
        //        var positionSide = PositionSide.Short;
        //        if (orderParam.side == OrderSide.Buy)
        //        {
        //            positionSide = PositionSide.Long;
        //        }
        //        // gửi yêu cầu đặt hàng mới đến Binance API
        //        var orderResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, orderParam.side, orderParam.market ? FuturesOrderType.Market : orderParam.type, orderParam.market ? null : quantity, orderParam.price, positionSide: positionSide, timeInForce: timeInForce);
        //        if (orderResult.Data != null)
        //        {
        //            res.data = orderResult.Data;
        //            if ((orderParam.stoploss != null && orderParam.stoploss > 0) || (orderParam.takeprofit != null && orderParam.takeprofit > 0))
        //            {
        //                var slSide = OrderSide.Sell;
        //                var slPositionSide = PositionSide.Short;
        //                if (orderParam.side == OrderSide.Sell)
        //                {
        //                    slSide = OrderSide.Buy;
        //                    slPositionSide = PositionSide.Long;
        //                }

        //                if (orderParam.stoploss != null && orderParam.stoploss > 0)
        //                {
        //                    var slResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, slSide, FuturesOrderType.StopMarket, quantity, workingType: WorkingType.Mark, stopPrice: orderParam.stoploss, positionSide: slPositionSide, timeInForce: timeInForce);                
        //                }

        //                if (orderParam.takeprofit != null && orderParam.takeprofit > 0)
        //                {
        //                    var tkResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, slSide, FuturesOrderType.TakeProfitMarket, quantity, workingType: WorkingType.Mark, stopPrice: orderParam.takeprofit, positionSide: slPositionSide, timeInForce: timeInForce);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            res.code = ServiceResultConstant.NoData;
        //            res.status = 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        res.status = 0;
        //        res.code = ServiceResultConstant.Exception;
        //        res.message = ex.Message;
        //        Console.WriteLine(ex);
        //    }
        //    return Ok(res);
        //}


        ///// <summary>
        ///// Cancel
        ///// </summary>
        ///// <param name="cancelParam"></param>
        ///// <returns></returns>
        //[HttpPost("cancel")]
        //public async Task<IActionResult> Cancel([FromBody] CancelParam cancelParam)
        //{
        //    var res = new ServiceResult();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(cancelParam.symbol))
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.Require;
        //            return Ok(res);
        //        }
        //        var symbol = cancelParam.symbol;
        //        if (!symbol.EndsWith("USDT") || !symbol.EndsWith("BUSD"))
        //        {
        //            symbol = $"{symbol}USDT";
        //        }
        //        // khởi tạo kết nối API với Binance
        //        var apiKeyData = GetApiKey(cancelParam.ApiName);
        //        if (apiKeyData == null)
        //        {
        //            res.status = 0;
        //            res.code = ServiceResultConstant.NotApiKey;
        //            return Ok(res);
        //        }
        //        BinanceClient client = new BinanceClient(new BinanceClientOptions
        //        {
        //            ApiCredentials = new BinanceApiCredentials(apiKeyData.Key, apiKeyData.Secret),
        //        });

        //        if (cancelParam.orderId == null || cancelParam.orderId == 0)
        //        {
        //            var calcelAllResult = await client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);
        //            if (calcelAllResult.Data == null)
        //            {
        //                res.status = 0;
        //                return Ok(res);
        //            }
        //            res.data = calcelAllResult.Data;
        //        }
        //        else
        //        {
        //            var calcelResult = await client.UsdFuturesApi.Trading.CancelOrderAsync(symbol, cancelParam.orderId);
        //            if (calcelResult.Data == null)
        //            {
        //                res.status = 0;
        //                return Ok(res);
        //            }
        //            res.data = calcelResult.Data;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        res.status = 0;
        //        res.code = ServiceResultConstant.Exception;
        //        res.message = ex.Message;
        //        Console.WriteLine(ex);
        //    }
        //    return Ok(res);
        //}

        //private ApiKey? GetApiKey(string name)
        //{
        //    if (string.IsNullOrEmpty(name))
        //    {
        //        name = "hthiep";
        //    }
        //    var apiKey = this._apiKeys.Find(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        //    if (apiKey == null)
        //    {
        //        return null;
        //    }
        //    return new ApiKey() { Name = name, Key = EncryptionHelper.Decrypt(apiKey.Key), Secret = EncryptionHelper.Decrypt(apiKey.Secret) };
        //}
    }
}
