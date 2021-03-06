﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Swish.UnitTests
{
    public class SwishClientTests
    {
        private const string SwishId = "11A86BE70EA346E4B1C39C874173F088";
        private readonly string _merchantId = "1231181189";
        private readonly ECommercePaymentModel _defaultECommercePaymentModel;
        private readonly MCommercePaymentModel _defaultMCommercePaymentModel;
        private readonly RefundModel _defaultRefund;

        public SwishClientTests()
        {
            _defaultECommercePaymentModel = new ECommercePaymentModel(
                amount: "100",
                callbackUrl: "https://example.com/api/swishcb/paymentrequests",
                currency: "SEK",
                payerAlias: "467012345678")
            {
                PayeePaymentReference = "0123456789",
                Message = "Kingston USB Flash Drive 8 GB"
            };

            _defaultMCommercePaymentModel = new MCommercePaymentModel(
                amount: "100",
                callbackUrl: "https://example.com/api/swishcb/paymentrequests",
                currency: "SEK")
            {
                PayeePaymentReference = "0123456789",
                Message = "Kingston USB Flash Drive 8 GB"
            };

            _defaultRefund = new RefundModel(
                originalPaymentReference: "6D6CD7406ECE4542A80152D909EF9F6B",
                callbackUrl: "https://example.com/api/swishcb/refunds",
                payerAlias: "1231181189",
                amount: "100",
                currency: "SEK")
            {
                PayerPaymentReference = "0123456789",
                Message = "Refund for Kingston USB Flash Drive 8 GB"
            };
        }

        [Theory]
        [InlineData(500, "InternalServerError:")]
        [InlineData(401, "Unauthorized:")]
        [InlineData(404, "NotFound:")]
        public async Task GetPaymeentStatus_Returns_Correct_ErrorMessage(int status, string message)
        {
            var mockHttp = MockHttp.WithStatus(status);
            var client = new SwishClient(mockHttp, _merchantId);

            var result = await client.GetPaymentStatus(SwishId);

            result.ErrorMessage.Should().Contain(message);
            result.ErrorCode.Should().Be(status.ToString());
        }

        [Theory]
        [InlineData(400, "Bad Request:")]
        [InlineData(401, "Unauthorized:")]
        [InlineData(403, "Forbidden:")]
        [InlineData(415, "Unsupported Media Type:")]
        [InlineData(500, "Internal Server Error:")]
        public async Task MakeECommercePaymentAsync_Returns_Correct_ErrorMessage(int status, string message)
        {
            var mockHttp = MockHttp.WithStatus(status);
            var client = new SwishClient(mockHttp, _merchantId);

            var result = await client.MakeECommercePaymentAsync(_defaultECommercePaymentModel);

            result.ErrorMessage.Should().Contain(message);
            result.ErrorCode.Should().Be(status.ToString());
        }

        [Theory]
        [InlineData(400, "Bad Request:")]
        [InlineData(401, "Unauthorized:")]
        [InlineData(403, "Forbidden:")]
        [InlineData(415, "Unsupported Media Type:")]
        [InlineData(500, "Internal Server Error:")]
        public async Task MakeMCommercePaymentAsync_Returns_Correct_ErrorMessage(int status, string message)
        {
            var mockHttp = MockHttp.WithStatus(status);
            var client = new SwishClient(mockHttp, _merchantId);

            var result = await client.MakeMCommercePaymentAsync(_defaultMCommercePaymentModel);

            result.ErrorMessage.Should().Contain(message);
            result.ErrorCode.Should().Be(status.ToString());
        }

        [Theory]
        [InlineData("RP06", "A payment request already exist for that payer", "[{\"errorCode\":\"RP06\",\"errorMessage\":\"A payment request already exist for that payer\"}]")]
        [InlineData("ACMT03", "Payer not Enrolled", "[{\"errorCode\":\"ACMT03\",\"errorMessage\":\"Payer not Enrolled\"}]")]
        [InlineData("PA01", "Parameter is not correct.", "[{\"errorCode\":\"PA01\",\"errorMessage\":\"Parameter is not correct.\"}]")]
        [InlineData("RP01", "Missing merchant Swish Number", "[{\"errorCode\":\"RP01\",\"errorMessage\":\"Missing merchant Swish Number\"}]")]
        public async Task MakeECommercePaymentAsync_Returns_Correct_ErrorMessage_For_422(string errorCode, string errorMessage, string content)
        {
            var mockHttp = MockHttp.WithStatusAndContent(422, content);
            var client = new SwishClient(mockHttp, _merchantId);

            var result = await client.MakeECommercePaymentAsync(_defaultECommercePaymentModel);

            result.ErrorMessage.Should().Contain(errorMessage);
            result.ErrorCode.Should().Be(errorCode);
        }

        [Theory]
        [InlineData("RP06", "A payment request already exist for that payer", "[{\"errorCode\":\"RP06\",\"errorMessage\":\"A payment request already exist for that payer\"}]")]
        [InlineData("ACMT03", "Payer not Enrolled", "[{\"errorCode\":\"ACMT03\",\"errorMessage\":\"Payer not Enrolled\"}]")]
        [InlineData("PA01", "Parameter is not correct.", "[{\"errorCode\":\"PA01\",\"errorMessage\":\"Parameter is not correct.\"}]")]
        [InlineData("RP01", "Missing merchant Swish Number", "[{\"errorCode\":\"RP01\",\"errorMessage\":\"Missing merchant Swish Number\"}]")]
        public async Task MakeMCommercePaymentAsync_Returns_Correct_ErrorMessage_For_422(string errorCode, string errorMessage, string content)
        {
            var mockHttp = MockHttp.WithStatusAndContent(422, content);
            var client = new SwishClient(mockHttp, _merchantId);

            var result = await client.MakeMCommercePaymentAsync(_defaultMCommercePaymentModel);

            result.ErrorMessage.Should().Contain(errorMessage);
            result.ErrorCode.Should().Be(errorCode);
        }

        [Fact]
        public async Task MakeECommercePayment_Returns_Location_Header_Values()
        {
            string paymentId = "AB23D7406ECE4542A80152D909EF9F6B";
            string locationHeader = $"https://mss.swicpc.bankgirot.se/swishcpcapi/v1/paymentrequests/{paymentId}";
            var headerValues = new Dictionary<string, string>() { { "Location", locationHeader } };
            var responseMessage = Create201HttpJsonResponseMessage(_defaultECommercePaymentModel, headerValues);
            var client = new SwishClient(MockHttp.WithResponseMessage(responseMessage), _merchantId);

            // Act
            var response = await client.MakeECommercePaymentAsync(_defaultECommercePaymentModel);

            // Assert
            Assert.Equal(response.Location, locationHeader);
            Assert.Equal(response.Id, paymentId);
        }

        [Fact]
        public async Task MakeECommercePayment_Should_Fix_PayerAlias()
        {
            string paymentId = "AB23D7406ECE4542A80152D909EF9F6B";
            string locationHeader = $"https://mss.swicpc.bankgirot.se/swishcpcapi/v1/paymentrequests/{paymentId}";
            var headerValues = new Dictionary<string, string>() { { "Location", locationHeader } };
            _defaultECommercePaymentModel.PayerAlias = "0701234567";
            var responseMessage = Create201HttpJsonResponseMessage(_defaultECommercePaymentModel, headerValues);
            var mockHttp = MockHttp.WithResponseMessage(responseMessage);
            var client = new SwishClient(mockHttp, _merchantId);

            // Act
            var response = await client.MakeECommercePaymentAsync(_defaultECommercePaymentModel);

            // Assert
            var body = await mockHttp.LastRequest.Content.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<ECommercePaymentModel>(body);
            Assert.Equal("46701234567",request.PayerAlias);
        }

        [Fact]
        public async Task MakeMCommercePayment_Returns_Location_And_Token_Header_VaLues()
        {
            string paymentId = "AB23D7406ECE4542A80152D909EF9F6B";
            string locationHeader = $"https://mss.swicpc.bankgirot.se/swishcpcapi/v1/paymentrequests/{paymentId}";
            var headerValues = new Dictionary<string, string>()
            {
                { "Location", locationHeader },
                { "PaymentRequestToken", "f34DS34lfd0d03fdDselkfd3ffk21" }
            };
            var responseMessage = Create201HttpJsonResponseMessage(_defaultMCommercePaymentModel, headerValues);
            var client = new SwishClient(MockHttp.WithResponseMessage(responseMessage), _merchantId);

            // Act
            var response = await client.MakeMCommercePaymentAsync(_defaultMCommercePaymentModel);

            // Assert
            Assert.Equal(locationHeader, response.Location);
            Assert.Equal(paymentId, response.Id);
            Assert.Equal("f34DS34lfd0d03fdDselkfd3ffk21", response.Token);
        }

        [Fact]
        public async Task MakeRefund_Returns_Location_Header_Values()
        {
            string refundId = "ABC2D7406ECE4542A80152D909EF9F6B";
            string locationHeader = $"https://mss.swicpc.bankgirot.se/swishcpcapi/v1/refunds/{refundId}";
            var headerValues = new Dictionary<string, string>() { { "Location", locationHeader } };
            var responseMessage = Create201HttpJsonResponseMessage(_defaultRefund, headerValues);
            var client = new SwishClient(MockHttp.WithResponseMessage(responseMessage), _merchantId);

            // Act
            var response = await client.MakeRefundAsync(_defaultRefund);

            // Assert
            Assert.Equal(response.Location, locationHeader);
            Assert.Equal(response.Id, refundId);
        }

        [Fact]
        public async Task GenerateSwishUrl_Returns_Valid_Url()
        {
            // Arrange
            var token = "c28a4061470f4af48973bd2a4642b4fa";
            var redirectUrl = "https://example.com/api/swishcb/mpaymentcomplete/123";
            var client = new SwishClient(null, _merchantId);

            // Act
            string result = client.GenerateSwishUrl(token, redirectUrl);

            // Assert
            result.Should().MatchEquivalentOf("swish://paymentrequest?token=*&callbackurl=*");
            result.Should().Contain(token);
            result.Should().Contain(WebUtility.UrlEncode(redirectUrl));
        }

        [Theory]
        [InlineData(null, "https://www.backend.se/paymentComplete")]
        [InlineData("", "https://www.backend.se/paymentComplete")]
        [InlineData(" ", "https://www.backend.se/paymentComplete")]
        [InlineData("c28a4061470f4af48973bd2a4642b4fa", null)]
        [InlineData("c28a4061470f4af48973bd2a4642b4fa", "")]
        [InlineData("c28a4061470f4af48973bd2a4642b4fa", " ")]
        public async Task GenerateSwishUrl_Throws_Http_Exception_For_Invalid_Input(string token, string redirectUrl)
        {
            //Arrange 
            Action act = () => new SwishClient(null, _merchantId).GenerateSwishUrl(token, redirectUrl);

            //Act & Assert
            act.Should().Throw<ArgumentException>();
        }
        private HttpResponseMessage Create201HttpJsonResponseMessage<T>(T contentModel,
            Dictionary<string, string> headerValues)
        {
            var content = new StringContent(JsonConvert.SerializeObject(contentModel), Encoding.UTF8, "application/json");
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Created) { Content = content };
            foreach (var header in headerValues)
            {
                responseMessage.Headers.Add(header.Key, header.Value);
            }
            return responseMessage;
        }
    }
}
