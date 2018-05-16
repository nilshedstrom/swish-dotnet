using System;
using System.Net;
using System.Net.Http;

namespace SwishClient.UnitTests
{
    public class MockHttp : HttpClient
    {
        private readonly TestHttpMessageHandler _handler;

        public MockHttp(TestHttpMessageHandler handler) : base(handler)
        {
            _handler = handler;
            this.BaseAddress = new Uri("http://test");
        }

        public HttpRequestMessage LastRequest => _handler.Request;

        public static MockHttp WithStatusAndContent(int code, string content)
        {
            return new MockHttp(new TestHttpMessageHandler((HttpStatusCode)code, content));
        }

        public static MockHttp WithStatus(int status)
        {
            return new MockHttp(new TestHttpMessageHandler((HttpStatusCode)status));
        }

        public static MockHttp WithResponseMessage(HttpResponseMessage msg)
        {
            return new MockHttp(new TestHttpMessageHandler(msg));
        }
    }
}