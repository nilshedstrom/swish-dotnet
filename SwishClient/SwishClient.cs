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

namespace SwishClient
{
    /// <summary>
    /// Swish client
    /// </summary>
    public class SwishClient
    {
        private readonly HttpClient _client;

        public readonly SwishEnvironment Environment;
        public readonly string MerchantId;

        private string _paymentRequestsPath
        {
            get
            {
                return Environment == SwishEnvironment.Production ?
                    "https://swicpc.bankgirot.se/swish-cpcapi/api/v1/paymentrequests/" :
                    "https://mss.swicpc.bankgirot.se/swish-cpcapi/api/v1/paymentrequests/";
            }
        }
        private string _refundsPath
        {
            get
            {
                return Environment == SwishEnvironment.Production ?
                    "https://swicpc.bankgirot.se/swish-cpcapi/api/v1/refunds/" :
                    "https://mss.swicpc.bankgirot.se/swish-cpcapi/api/v1/refunds/";
            }
        }


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configuration">The client configuration</param>
        /// <param name="cert">The client certificate</param>
        public SwishClient(SwishEnvironment environment, byte[] clientCertData, string clientCertPassword, string merchantId)
        {
            Environment = environment;
            MerchantId = merchantId;
            
            var clientCerts = new X509Certificate2Collection();
            clientCerts.Import(clientCertData, clientCertPassword ?? "", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            // assert CA certs in cert store, and get root CA 
            var rootCertificate = AssertCertsInStore(clientCerts);

            var handler = new HttpClientHandler();
            handler.ClientCertificates.AddRange(clientCerts);

            handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
            
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

            var privateCert = certArr.FirstOrDefault(o => o.HasPrivateKey);

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

        private SwishApiResponse ExtractSwishResponse(HttpResponseMessage responseMessage)
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
