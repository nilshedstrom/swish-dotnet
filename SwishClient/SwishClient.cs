using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Swish
{
    public enum SwishEnvironment
    {
        Test,
        Production
    }

    public interface ISwishClient
    {
        /// <summary>
        /// Makes a swish payment via the e-commerce flow
        /// </summary>
        /// <param name="payment">The payment details</param>
        /// <returns>Payment response containing payment status location</returns>
        Task<ECommercePaymentResponse> MakeECommercePaymentAsync(ECommercePaymentModel payment);

        /// <summary>
        /// Make a swish payment via the m-commerce flow
        /// </summary>
        /// <param name="payment">The payment details</param>
        /// <returns>Payment response containing payment status location</returns>
        Task<MCommercePaymentResponse> MakeMCommercePaymentAsync(MCommercePaymentModel payment);

        /// <summary>
        /// Get the current status of a payment
        /// </summary>
        /// <param name="id">The location id</param>
        /// <returns>The payment status</returns>
        Task<PaymentStatusModel> GetPaymentStatus(string id);

        /// <summary>
        /// Makes a refund request
        /// </summary>
        /// <param name="refund">The refund details</param>
        /// <returns>The refund response containing the location of the refund status</returns>
        Task<SwishApiResponse> MakeRefundAsync(RefundModel refund);

        /// <summary>
        /// Get the current status of a refund
        /// </summary>
        /// <param name="id">The refund location id</param>
        /// <returns>The refund status</returns>
        Task<RefundStatusModel> GetRefundStatus(string id);
    }

    /// <summary>
    /// Swish client
    /// </summary>
    public class SwishClient : ISwishClient
    {
        private HttpClient _client;

        public readonly SwishEnvironment Environment;
        public readonly string MerchantId;

        private string _paymentRequestsPath
        {
            get
            {
                return Environment == SwishEnvironment.Production ?
                    "https://cpc.getswish.net/swish-cpcapi/api/v1/paymentrequests" :
                    "https://mss.cpc.getswish.net/swish-cpcapi/api/v1/paymentrequests/";
            }
        }

        private string _refundsPath
        {
            get
            {
                return Environment == SwishEnvironment.Production ?
                    "https://cpc.getswish.net/swish-cpcapi/api/v1/refunds" :
                    "https://mss.cpc.getswish.net/swish-cpcapi/api/v1/refunds/";
            }
        }

        /// <summary>
        /// Initializes the swish client with initialized HttpClient 
        /// Only for testing purposes!
        /// </summary>
        /// <param name="httpClient">Initialized/mocked HttpClient</param>
        /// <param name="merchantId">Merchant Id</param>
        /// <param name="environment">Swish env to use</param>
        public SwishClient(HttpClient httpClient, string merchantId, SwishEnvironment environment = SwishEnvironment.Test)
        {
            Environment = environment;
            MerchantId = merchantId;
            _client = httpClient;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="environment">Swish environment to use</param>
        /// <param name="P12CertificateCollectionBytes">The client P12 certificate as a byte array</param>
        /// <param name="P12CertificateCollectionPassphrase">Password for certificate collection (can be null)</param>
        /// <param name="merchantId">Swish Merchant ID</param>
        public SwishClient(SwishEnvironment environment, byte[] P12CertificateCollectionBytes, string P12CertificateCollectionPassphrase, string merchantId)
        {
            Environment = environment;
            MerchantId = merchantId;
            var clientCerts = new X509Certificate2Collection();
            clientCerts.Import(P12CertificateCollectionBytes, P12CertificateCollectionPassphrase ?? "", X509KeyStorageFlags.Exportable);
            CreateClient(clientCerts);
        }

        private void CreateClient(X509Certificate2Collection clientCerts)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.AddRange(clientCerts);
            handler.Credentials = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;
            }
            catch
            {
            }

            try
            {
                handler.SslProtocols = SslProtocols.Tls12;
            }
            catch
            {
            }
            handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;

            _client = new HttpClient(handler);
        }

        /// <summary>
        /// Makes a swish payment via the e-commerce flow
        /// </summary>
        /// <param name="payment">The payment details</param>
        /// <returns>Payment response containing payment status location</returns>
        public async Task<ECommercePaymentResponse> MakeECommercePaymentAsync(ECommercePaymentModel payment)
        {
            payment.PayeeAlias = MerchantId;
            payment.PayerAlias = FixPayerAlias(payment.PayerAlias);
            var response = await Post(payment, _paymentRequestsPath).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode == (HttpStatusCode)422)
            {
                throw new HttpRequestException(responseContent);
            }
            response.EnsureSuccessStatusCode();

            return ExtractSwishResponse(response) as ECommercePaymentResponse;
        }

        private static string FixPayerAlias(string payerAlias)
        {
            if (String.IsNullOrWhiteSpace(payerAlias))
                return payerAlias;
            if (payerAlias.StartsWith("07"))
                return $"467{payerAlias.Substring(2)}";
            return payerAlias;
        }

        /// <summary>
        /// Make a swish payment via the m-commerce flow
        /// </summary>
        /// <param name="payment">The payment details</param>
        /// <returns>Payment response containing payment status location</returns>
        public async Task<MCommercePaymentResponse> MakeMCommercePaymentAsync(MCommercePaymentModel payment)
        {
            payment.PayeeAlias = MerchantId;
            var response = await Post(payment, _paymentRequestsPath).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode == (HttpStatusCode)422)
            {
                throw new HttpRequestException(responseContent);
            }

            /*
            Potential Http status codes returned:
            201 Created: Returned when Payment request was successfully created. Will return a Location header and if it is Swish m-commerce case, it will also return PaymentRequestToken header.
            400 Bad Request: Returned when the Create Payment Request operation was malformed.
            401 Unauthorized: Returned when there are authentication problems with the certificate. Or the Swish number in the certificate is not enrolled. Will return nothing else.
            403 Forbidden: Returned when the payeeAlias in the payment request object is not the same as merchant’s Swish number.
            415 Unsupported Media Type: Returned when Content-Type header is not “application/json”. Will return nothing else.
            422 Unprocessable Entity: Returned when there are validation errors. Will return an Array of Error Objects.
            500 Internal Server Error: Returned if there was some unknown/unforeseen error that occurred on the server, this should normally not happen. Will return nothing else.
            Potential Error codes returned on Error Objects when validation fails (HTTP status code 422 is returned):
            FF08 – PaymentReference is invalid
            RP03 – Callback URL is missing or does not use Https
            BE18 – Payer alias is invalid
            RP01 – Missing Merchant Swish Number
            PA02 – Amount value is missing or not a valid number
            AM06 – Specified transaction amount is less than agreed minimum
            AM02 – Amount value is too large
            AM03 – Invalid or missing Currency
            RP02 – Wrong formated message
            RP06 – A payment request already exist for that payer. Only applicable for Swish ecommerce.
            ACMT03 – Payer not Enrolled
            ACMT01 – Counterpart is not activated
            ACMT07 – Payee not Enrolled
             */
            // TODO: handle error codes like in the GetPaymentStatus(...) method

            response.EnsureSuccessStatusCode();

            return ExtractMCommerceResponse(response);
        }

        /// <summary>
        /// Get the current status of a payment
        /// </summary>
        /// <param name="id">The location id</param>
        /// <returns>The payment status</returns>
        public async Task<PaymentStatusModel> GetPaymentStatus(string id)
        {
            var uri = $"{_paymentRequestsPath}/{id}";

            var response = await Get(uri).ConfigureAwait(false);

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return new PaymentStatusModel()
                    {
                        ErrorMessage = "Unauthorized: There are authentication problems with the certificate. Or the Swish number in the certificate is not enrolled.",
                    };
                case HttpStatusCode.NotFound:
                    return new PaymentStatusModel()
                    {
                        ErrorMessage = "NotFound: Payment request was not found or it was not created by the merchant."
                    };

                case HttpStatusCode.InternalServerError:
                    return new PaymentStatusModel()
                    {
                        ErrorMessage = "InternalServerError: There was some unknown/unforeseen error that occurred on the server, this should normally not happen."
                    };
            }

            // should be only OK here
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<PaymentStatusModel>(responseContent);
        }

        /// <summary>
        /// Makes a refund request
        /// </summary>
        /// <param name="refund">The refund details</param>
        /// <returns>The refund response containing the location of the refund status</returns>
        public async Task<SwishApiResponse> MakeRefundAsync(RefundModel refund)
        {
            var response = await Post(refund, _refundsPath).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode == (HttpStatusCode)422)
            {
                throw new HttpRequestException(responseContent);
            }
            response.EnsureSuccessStatusCode();

            return ExtractSwishResponse(response);
        }

        /// <summary>
        /// Get the current status of a refund
        /// </summary>
        /// <param name="id">The refund location id</param>
        /// <returns>The refund status</returns>
        public async Task<RefundStatusModel> GetRefundStatus(string id)
        {
            var uri = $"{_refundsPath}/{id}";
            var response = await Get(uri).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<RefundStatusModel>(responseContent);
        }
        /// <summary>
        /// Generates an URL to the Swish application. 
        /// Used in the MCommerce-flow to start the Swish Application on the smart phone.
        /// </summary>
        /// <param name="token">The payment request token that the merchant has received from the CPC. Example: c28a4061470f4af48973bd2a4642b4fa</param>
        /// <param name="redirectUrl">This callback URL is called after the payment is finished. It can be for example an app URL or a web URL. Example: https://www.mysite.com/paymentComplete/1234</param>
        /// <returns></returns>
        public static string GenerateSwishUrl(string token, string redirectUrl)
        {
            if(string.IsNullOrWhiteSpace(token))
                throw new ArgumentException($"{nameof(token)} should not be empty", nameof(token));
            if (string.IsNullOrWhiteSpace(redirectUrl))
                throw new ArgumentException($"{nameof(redirectUrl)} should not be empty", nameof(redirectUrl));
            var encodedRedirectUrl = WebUtility.UrlEncode(redirectUrl);
            return $"swish://paymentrequest?token={token}&callbackurl={encodedRedirectUrl}";
        }

        private static SwishApiResponse ExtractSwishResponse(HttpResponseMessage responseMessage)
        {
            var location = responseMessage.Headers.GetValues("Location").FirstOrDefault();
            var swishResponse = new ECommercePaymentResponse();
            if (location != null)
            {
                var id = location.Split('/').LastOrDefault();
                swishResponse.Location = location;
                swishResponse.Id = id;
            }

            return swishResponse;
        }

        private MCommercePaymentResponse ExtractMCommerceResponse(HttpResponseMessage responseMessage)
        {
            var token = responseMessage.Headers.GetValues("PaymentRequestToken").FirstOrDefault();
            var paymentResponse = ExtractSwishResponse(responseMessage);

            return new MCommercePaymentResponse()
            {
                Id = paymentResponse.Id,
                Location = paymentResponse.Location,
                Token = token
            };
        }

        private Task<HttpResponseMessage> Post<T>(T model, string path)
        {
            var json = JsonConvert.SerializeObject(model, new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return _client.PostAsync(path, content);
        }

        private Task<HttpResponseMessage> Get(string path) => _client.GetAsync(path);
    }
}