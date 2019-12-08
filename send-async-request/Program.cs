using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace send_async_request
{
    class Program
    {
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.89 Safari/537.36)";// "IOS-Tablet (IOS; 7.0.4; iPad; 711C3786-5A29-4391-9884-82BE11252756)"; //"MobileSite (Android; 4.4.2; web GT-I9300; mobil)"
        private const string Accept = "application/json";
        static async Task Main()
        {
            string getUrl, postUrl;
            Console.WriteLine("Get URL:");
           // Console.ReadKey();
            //getUrl = "https://swapi.co/api/people/1/";
            getUrl = "https://vast-beyond-41792.herokuapp.com/test/";
            //Console.ReadLine();
            //Console.WriteLine("Post URL:");
            //postUrl = Console.ReadLine();
            await SendGetRequest(getUrl);
            Console.ReadKey();
        }
        public static async Task SendGetRequest(string getUrl, int miliseconds = 0)
        {
            DateTime time;
            int count = 0;
            Task getRequest = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    time = DateTime.Now;
                    GetResponse<List<MockModel>>(getUrl, _cancellationTokenSource.Token);
                    Console.WriteLine($"GET Counter : {count} , Time : {DateTime.Now - time}");
                    count++;
                    await Task.Delay(miliseconds);
                }
            }
            , _cancellationTokenSource.Token);
        }
        public static async Task SendPostRequest(string postUrl, int miliseconds = 0)
        {
            DateTime time;
            int count = 0;
            Task postRequest = Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    PostJson<MockModel,MockModel>(postUrl, new MockModel() { name = "guven", schoolName = "KOU", order = 1 }, _cancellationTokenSource.Token);
                    time = DateTime.Now;
                    Console.WriteLine($"POST Counter : {count} , Time : {DateTime.Now - time}");
                    count++;
                    await Task.Delay(miliseconds);
                }
            }
            , _cancellationTokenSource.Token);
        }
        private static void GetWorkingThreads()
        {
            int maxThreads;
            int completionPortThreads;
            ThreadPool.GetMaxThreads(out maxThreads, out completionPortThreads);

            int availableThreads;
            ThreadPool.GetAvailableThreads(out availableThreads, out completionPortThreads);
            Console.WriteLine(maxThreads - availableThreads);
        }
        private static HttpClient CreateClient(string url, int timeoutSeconds = 0)
        {
            var httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
            });
            if (timeoutSeconds > 0)
            {
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            }
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(UserAgent);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd(Accept);
            return httpClient;
        }
        private static JsonSerializerSettings GetSerializeSettings(out List<string> deseralizeMsg)
        {
            var messages = new List<string>();
            var settings = new JsonSerializerSettings
            {
                Error = (s, e) =>
                {
                    messages.Add(string.Format("Object:{0}  Message:{1}", e.ErrorContext.Member,
                        e.ErrorContext.Error.Message));

                    e.ErrorContext.Handled = true;
                }
            };
            deseralizeMsg = messages;
            return settings;
        }
        public static async Task<T> GetResponse<T>(string url, CancellationToken ctn)
        {
            DateTime time = DateTime.Now;
            Uri uri = null;
            using (var client = CreateClient(url))
            {
                var model = default(T);
                string json = null;
                try
                {
                    if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri))
                    {
                        Console.WriteLine("method error");
                        throw new Exception();
                    }
                    var response = await client.GetAsync(uri, ctn);
                    json = await response.Content.ReadAsStringAsync();
                    ctn.ThrowIfCancellationRequested();
                    model = JsonConvert.DeserializeObject<T>(json, GetSerializeSettings(out var deseralizeMsg));
                    ctn.ThrowIfCancellationRequested();
                }
                catch (WebException e)
                {
                    ctn.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException e)
                {
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                //Console.WriteLine(JsonConvert.SerializeObject(model));
                Console.WriteLine($"Response Time : {DateTime.Now - time}");
                return model;
            }
        }
        public static async Task<TU> PostJson<T, TU>(string url, T myclass, CancellationToken ctn, int timeoutSeconds = 0)
        {
            Console.WriteLine("Post Json Called");
            var data = JsonConvert.SerializeObject(myclass);
            Uri uri = null;
            using (var client = CreateClient(url, timeoutSeconds))
            {
                var model = default(TU);
                string json = null;
                try
                {
                    if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri))
                    {
                        throw new Exception();
                    }
                    var response = await client.PostAsync(uri, new StringContent(data, Encoding.UTF8, Accept), ctn);
                    ctn.ThrowIfCancellationRequested();
                    json = await response.Content.ReadAsStringAsync();
                    model = JsonConvert.DeserializeObject<TU>(json, GetSerializeSettings(out var deseralizeMsg));
                    ctn.ThrowIfCancellationRequested();
                }
                catch (Exception ex)
                {

                }
                Console.WriteLine(JsonConvert.SerializeObject(model));
                return model;
            }
        }

    }
}
