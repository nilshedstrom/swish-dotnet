using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Swish;

namespace SwishTestWebApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var _merchantPFXForTest = System.IO.File.ReadAllBytes(Server.MapPath("~/App_Data/1231181189.p12"));
            var _merchantCertificateDataInPEM = System.IO.File.ReadAllBytes(Server.MapPath("~/App_Data/prod.pem"));
            var _merchantPrivateKey = System.IO.File.ReadAllText(Server.MapPath("~/App_Data/private.key"));
            var _merchantCertificatePassword = "";
            var _merchantId = "test";

            bool test1, test2 = false, test3;

            var bytes = CertificateGenerator.GenerateP12(_merchantPrivateKey, _merchantCertificateDataInPEM, "");

            var client = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: bytes,
                P12CertificateCollectionPassphrase: _merchantCertificatePassword,
                merchantId: _merchantId);

            var paymentStatus = await client.GetPaymentStatus("anything");
            test1 = true;

            var client2 = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: _merchantPFXForTest,
                P12CertificateCollectionPassphrase: "swish",
                merchantId: _merchantId);

            try
            {
                var paymentStatus2 = await client2.GetPaymentStatus("anything");
            }
            catch (Exception ex)
            {
                test2 = true;
            }

            var paymentStatus3 = await client.GetPaymentStatus("anything");

            var client4 = new SwishClient(SwishEnvironment.Production,
                P12CertificateCollectionBytes: bytes,
                P12CertificateCollectionPassphrase: _merchantCertificatePassword,
                merchantId: _merchantId);

            var paymentStatus4 = await client.GetPaymentStatus("anything");
            test3 = true;
            
            return test1 && test2 && test3 ? View() : View("Error");
        }
    }
}