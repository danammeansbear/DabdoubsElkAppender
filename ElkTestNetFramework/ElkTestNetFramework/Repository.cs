using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElkTestNetFramework.Infrastructure;
using ElkTestNetFramework.Models;
using Uri = ElkTestNetFramework.Models.Uri;

namespace ElkTestNetFramework
{
    public interface IRepository
    {
        Task AddAsync(IEnumerable<logEvent> logEvents, int bufferSize);
    }

    public class Repository : IRepository
    {
        readonly Uri uri;
        readonly IHttpClient httpClient;

        // Constructor should be public
        public Repository(Uri uri, IHttpClient httpClient)
        {
            this.uri = uri;
            this.httpClient = httpClient;
        }

        public async Task AddAsync(IEnumerable<logEvent> logEvents, int bufferSize)
        {
            if (bufferSize <= 1)
            {
                foreach (var logEvent in logEvents)
                {
                    await httpClient.PostAsync(uri, logEvent);
                }
            }
            else
            {
                await httpClient.PostBulkAsync(uri, logEvents);
            }
        }


        public static IRepository Create(string connectionString, log4net.ElasticSearch.CustomDataContractResolver resolver)
        {
            return Create(connectionString, new HttpClient(resolver));
        }

        public static IRepository Create(string connectionString, IHttpClient httpClient)
        {
            return new Repository(Uri.For(connectionString), httpClient);
        }
    }
}
