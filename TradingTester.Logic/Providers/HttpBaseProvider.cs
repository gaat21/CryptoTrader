using System;
using System.Net.Http;

namespace TradingTester.Logic.Providers
{
    public class HttpBaseProvider
    {
        private readonly string _baseUrl;

        public HttpBaseProvider(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public HttpClient GetClient()
        {
            return new HttpClient { BaseAddress = new Uri(_baseUrl) };
        }
    }
}
