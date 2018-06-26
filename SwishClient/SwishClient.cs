using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Swish
{
    /// <summary>
    /// Swish client
    /// </summary>
    public class SwishClient
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
                    "https://mss.swicpc.bankgirot.se/swish-cpcapi/api/v1/paymentrequests/";
            }
        }

        private string _refundsPath
        {
            get
            {
                return Environment == SwishEnvironment.Production ?
                    "https://cpc.getswish.net/swish-cpcapi/api/v1/refunds" :
                    "https://mss.swicpc.bankgirot.se/swish-cpcapi/api/v1/refunds/";
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="environment">Swish environment to use</param>
        /// <param name="P12CertificateCollection">The client P12 certificate</param>
        /// <param name="P12CertificateCollectionPassphrase">Password for certificate collection (can be null)</param>
        /// <param name="merchantId">Swish Merchant ID</param>
        public SwishClient(SwishEnvironment environment, byte[] P12CertificateCollection, string P12CertificateCollectionPassphrase, string merchantId)
        {
            Environment = environment;
            MerchantId = merchantId;
            
            var clientCerts = new X509Certificate2Collection();
            clientCerts.Import(P12CertificateCollection, P12CertificateCollectionPassphrase ?? "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            // assert CA certs in cert store, and get root CA 
            var rootCertificate = AssertCertsInStore(clientCerts);

            InitializeClient(clientCerts, rootCertificate);
        }

        private void InitializeClient(X509Certificate2Collection clientCerts, X509Certificate2 rootCertificate)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.AddRange(clientCerts);

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;
            }
            catch  { }

            try
            {
                handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            }
            catch { }
            
            handler.ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) =>
            {
                // for some reason, extracted test root certificate is not equal to the MSS server certificate
                // so for now, accept all server certificates
                // this should be fixed in the future
                return true;
                //var x509ChainElement = chain.ChainElements.OfType<X509ChainElement>().LastOrDefault();
                //if (x509ChainElement == null) return false;
                //var c = x509ChainElement.Certificate;

                //return c.Equals(rootCertificate);
            };

            _client = new HttpClient(handler);
        }

        /// <summary>
        /// Function that fixes the certificate so that you do not have to install it on the certificate store on the server
        /// Source https://www.wn.se/showthread.php?p=20516204#post20516204
        /// Author: Jack Jönsson from infringo.se 
        /// </summary>
        /// <param name="certs">Collection of certificates to get root CA from</param>
        /// <returns></returns>
        private static X509Certificate2 AssertCertsInStore(X509Certificate2Collection certs)
        {
            //Create typed array 
            var certArr = certs.OfType<X509Certificate2>().ToArray();

            //Build certificate chain 
            var chain = new X509Chain();

            chain.ChainPolicy.ExtraStore.AddRange(certArr.Where(o => !o.HasPrivateKey).ToArray());

            var privateCert = Array.Find(certArr, o => o.HasPrivateKey);

            if (privateCert == null) return null;

            var result = chain.Build(privateCert);

            //Get CA certs 
            var caCerts = chain.ChainElements.OfType<X509ChainElement>().Where(o => !o.Certificate.HasPrivateKey).Select(o => o.Certificate).ToArray();

            if (caCerts == null || caCerts.Length == 0) return null;

            //Assert CA certs in intermediate CA store 
            var intermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);

            intermediateStore.Open(OpenFlags.ReadWrite);

            foreach (var ca in caCerts)
            {
                if (!intermediateStore.Certificates.Contains(ca))
                    intermediateStore.Add(ca);
            }

            intermediateStore.Close();

            //Return last CA in chain (root CA) 
            return caCerts.LastOrDefault();
        }

        /// <summary>
        /// Initializes the swish client with initialized HttpClient 
        /// Only for testing purposes!
        /// </summary>
        /// <param name="httpClient">Initialized/mocked HttpClient</param>
        /// <param name="merchantId">Merchant Id</param>
        public SwishClient(HttpClient httpClient, string merchantId)
        {
            Environment = SwishEnvironment.Test;
            MerchantId = merchantId;
            _client = httpClient;
        }

        /// <summary>
        /// Makes a swish payment via the e-commerce flow
        /// </summary>
        /// <param name="payment">The payment details</param>
        /// <returns>Payment response containing payment status location</returns>
        public async Task<ECommercePaymentResponse> MakeECommercePaymentAsync(ECommercePaymentModel payment)
        {
            payment.PayeeAlias = MerchantId;

            var response = await Post(payment, _paymentRequestsPath).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode == (HttpStatusCode)422)
            {
                throw new HttpRequestException(responseContent);
            }
            response.EnsureSuccessStatusCode();

            return ExtractSwishResponse(response) as ECommercePaymentResponse;
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

            /*
            200 OK: Returned when Payment request was found. Will return Payment Request Object.
            401 Unauthorized: Returned when there are authentication problems with the certificate. Or the Swish number in the certificate is not enrolled. Will return nothing else.
            404 Not found: Returned when the Payment request was not found or it was not created by the merchant. Will return nothing else.
            500 Internal Server Error: Returned if there was some unknown/unforeseen error that occurred on the server, this should normally not happen. Will return nothing else.
            */

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
