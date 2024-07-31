using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ElkTestNetFramework.Models;
using log4net.ElasticSearch;
using Newtonsoft.Json;
using Uri = ElkTestNetFramework.Models.Uri;

namespace ElkTestNetFramework.Infrastructure
{
    public interface IHttpClient
    {
        Task PostAsync(Uri uri, logEvent item);
        Task PostBulkAsync(Uri uri, IEnumerable<logEvent> items);
    }

    public class HttpClient : IHttpClient
    {
        private readonly CustomDataContractResolver resolver;

        public HttpClient(CustomDataContractResolver resolver)
        {
            this.resolver = resolver;
            // Enable log4net internal debugging
           // log4net.Util.LogLog.InternalDebugging = true;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Ignore SSL certificate errors (not recommended for production)
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        const string ContentType = "application/json";
        const string Method = "POST";

        public async Task PostAsync(Uri uri, logEvent item)
        {
            try
            {
                var httpWebRequest = RequestFor(uri);

                using (var streamWriter = GetRequestStream(httpWebRequest))
                {
                    await streamWriter.WriteAsync(item.ToJson(resolver));
                    await streamWriter.FlushAsync();

                    using (var httpResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync())
                    {
                        if (httpResponse.StatusCode != HttpStatusCode.Created)
                        {
                            throw new WebException($"Failed to post {item.GetType().Name} to {uri}. Status Code: {httpResponse.StatusCode}");
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"WebException: {ex.Message}");
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorText = await reader.ReadToEndAsync();
                            Console.WriteLine($"Error response: {errorText}");
                        }
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }

        public async Task PostBulkAsync(Uri uri, IEnumerable<logEvent> items)
        {
            var httpWebRequest = RequestFor(uri);

            var postBody = new StringBuilder();
            foreach (var item in items)
            {
                // Create the action metadata
                var indexAction = new { index = new { _index = uri.Index() } };
                var actionMetadata = JsonConvert.SerializeObject(indexAction);

                var json = item.ToJson(resolver).ToString();
                // Replace timeStamp with @timestamp
                json = json.Replace("\"timeStamp\"", "\"@timestamp\"");

                postBody.AppendLine(actionMetadata);
                postBody.AppendLine(json);
            }

            postBody.AppendLine(); // Ensure the bulk request ends with a newline

            string jsonPayload = postBody.ToString(); // Move this declaration outside of the try block

            try
            {
                using (var streamWriter = new StreamWriter(await httpWebRequest.GetRequestStreamAsync()))
                {
                   // Console.WriteLine(jsonPayload);
                    await streamWriter.WriteAsync(jsonPayload);
                    await streamWriter.FlushAsync();
                }

                using (var httpResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync())
                {
                   //Console.WriteLine(httpResponse.StatusCode);
                   // Console.WriteLine(httpResponse.ToString());
                    if (httpResponse.StatusCode != HttpStatusCode.Created && httpResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new WebException($"Failed to post {jsonPayload} to {uri}.");
                    }
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine($"WebException: {ex.Message}");
                if (ex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)ex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorText = await reader.ReadToEndAsync();
                            Console.WriteLine($"Error response: {errorText}");
                        }
                    }
                }
                Console.WriteLine($"WebException: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }

        public static HttpWebRequest RequestFor(Uri uri)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);

            httpWebRequest.ContentType = ContentType;
            httpWebRequest.Method = Method;

            // Debugging information
            //Console.WriteLine("Request URI: " + httpWebRequest.RequestUri);
            //Console.WriteLine("Content Type: " + httpWebRequest.ContentType);
            //Console.WriteLine("Method: " + httpWebRequest.Method);

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                httpWebRequest.Headers.Remove(HttpRequestHeader.Authorization);

#if NETFRAMEWORK
                var uriUserInfo = HttpUtility.UrlDecode(uri.UserInfo);
#else
                var uriUserInfo = WebUtility.UrlDecode(uri.UserInfo);
#endif

                var authHeader = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(uriUserInfo));
                httpWebRequest.Headers.Add(HttpRequestHeader.Authorization, authHeader);

                // Debugging information
                //Console.WriteLine("User Info: " + uriUserInfo);
                //Console.WriteLine("Authorization Header: " + authHeader);
            }

            // Debugging information: print all headers
            //Console.WriteLine("Headers:");
            //foreach (var key in httpWebRequest.Headers.AllKeys)
            //{
            //    Console.WriteLine(key + ": " + httpWebRequest.Headers[key]);
            //}

            return httpWebRequest;
        }

        static StreamWriter GetRequestStream(WebRequest httpWebRequest)
        {
            return new StreamWriter(httpWebRequest.GetRequestStream());
        }
    }
}
