using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Swish;
using SwishTestWebAppCore.Models;

namespace SwishTestWebAppCore.Controllers
{
    public class SwishController : Controller
    {
        private SwishClient _client;
        private readonly IMemoryCache _memoryCache;
        private readonly SwishSettings _settings;

        public SwishController(SwishClient client, IMemoryCache memoryCache, IOptionsMonitor<SwishSettings> settingsAccessor)
        {
            _client = client;
            _memoryCache = memoryCache;
            _settings = settingsAccessor.CurrentValue;
        }

        public IActionResult ECommerce()
        {
            return View();
        }

        public IActionResult MCommerce()
        {
            return View();
        }

        public async Task<IActionResult> StartEPayment(EPaySwishViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("ECommerce");
            }
            // Make payment
            var ecommercePaymentModel = new ECommercePaymentModel(
                amount: model.Amount.ToString(),
                currency: "SEK",
                callbackUrl: $"{_settings.CallbackBaseUrl}/API/SwishAPI",
                payerAlias: model.PhoneNumber)
            {
                PayeePaymentReference = "0123456789",
                Message = model.Message
            };
            //swish://paymentrequest?token=<token>&callbackurl=<callbackURL>
            var paymentResponse = await _client.MakeECommercePaymentAsync(ecommercePaymentModel);
            return View(paymentResponse);
        }
        public async Task<IActionResult> StartMPayment(MPaySwishViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("MCommmerce");
            }
            // Make payment
            var mCommercePaymentModel = new MCommercePaymentModel(
                amount: model.Amount.ToString(),
                currency: "SEK",
                callbackUrl: $"{_settings.CallbackBaseUrl}/API/SwishAPI")
            {
                PayeePaymentReference = "0123456789",
                Message = model.Message
            };
            var paymentResponse = await _client.MakeMCommercePaymentAsync(mCommercePaymentModel);
            var paymentFinishedUrl = $"{_settings.CallbackBaseUrl}/Swish/MPaymentCompleted/{paymentResponse.Id}";
            var redirectUrl = SwishClient.GenerateSwishUrl(paymentResponse.Token, paymentFinishedUrl);
            return Redirect(redirectUrl);
        }

        [Route("Swish/MPaymentCompleted/{paymentId}")]
        public async Task<IActionResult> MPaymentCompleted([FromRoute]string paymentId)
        {
            var paymentStatus = await _client.GetPaymentStatus(paymentId);
            return View(paymentStatus);
        }
        [Route("Swish/CheckPayment/{paymentId}")]
        public async Task<IActionResult> CheckPayment([FromRoute]string paymentId)
        {
            var paymentStatus = await _client.GetPaymentStatus(paymentId);
            return View(paymentStatus);
        }

        [Route("Swish/GetPaymentPartial/{paymentId}")]
        public async Task<IActionResult> GetPaymentPartial([FromRoute]string paymentId)
        {
            try
            {
                Swish.PaymentStatusModel status;
                while (!_memoryCache.TryGetValue(paymentId, out status))
                {
                    await Task.Delay(500);
                }
                return PartialView("_GetPaymentPartial", status);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}