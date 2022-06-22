using Files.Shared.Services;
using Files.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Uwp.ServicesImplementation
{
    internal class FullTrustAsker : IFullTrustAsker
    {
        public async Task<IFullTrustResponse> GetResponseAsync(IDictionary<string, object> parameters)
        {
            var connection = await AppServiceConnectionHelper.Instance;
            if (connection is not null)
            {
                var set = parameters is ValueSet valueSet ? valueSet : ToValueSet(parameters);
                var (status, response) = await connection.SendMessageForResponseAsync(set);

                if (status is AppServiceResponseStatus.Success)
                {
                    return new Response(response);
                }
            }
            return new NoResponse();
        }

        private static ValueSet ToValueSet(IDictionary<string, object> values)
        {
            var set = new ValueSet();
            foreach (var value in values)
            {
                set.Add(value);
            }
            return set;
        }

        private class NoResponse : IFullTrustResponse
        {
            public bool IsSuccess => false;

            public bool ContainsKey(string key) => false;
            public T Get<T>(string key) => throw new ArgumentOutOfRangeException("This key does not exist.");
            public T Get<T>(string key, T defaultValue) => Get<T>(key);
        }

        private class Response : IFullTrustResponse
        {
            private readonly IDictionary<string, object> values;

            public bool IsSuccess => true;

            public Response(IDictionary<string, object> values) => this.values = values;

            public bool ContainsKey(string key) => values.ContainsKey(key);

            public T Get<T>(string key) => Get<T>(key, default);
            public T Get<T>(string key, T defaultValue)
            {
                if (!values.ContainsKey(key))
                {
                    throw new ArgumentOutOfRangeException("This key does not exist.");
                }
                if (values[key] is T t)
                {
                    return t;
                }
                return default;
            }
        }
    }
}
