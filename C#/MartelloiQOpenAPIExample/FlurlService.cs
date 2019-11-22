using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using LazyCache;
using Newtonsoft.Json;
using NLog;
using Polly;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;


namespace iQOpenApiExample
{
    /// <summary>
    /// We using a Flurl library for calling REST.
    /// Flurl is a modern, fluent, asynchronous, testable, portable, buzzword-laden URL builder and HTTP client library for .NET.
    /// https://flurl.dev/
    /// </summary>
    public class FlurlService
    {
        private readonly IAppCache _cache;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string _endpoint;


        public FlurlService(string baseUrl)
        {
            BaseUrl = baseUrl;
        }
        public void SetEndPoint(string endPoint)
        {
            _endpoint = endPoint;
        }
        public string BaseUrl { get; set; }

  

        public async Task<T> PostJsonAsync<T>(object parameters, object body,bool httpOverride = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var policy = RetryPolicy();

            var request = BuildUrl()
                .SetQueryParams(parameters).ToString(true)
                .WithHeader("Accept", "application/json")
                .ConfigureRequest(JsonSettings);


            var response = await PostJsonAsyncWithPolicy<T>(request, body, policy, cancellationToken);

            return response;
        }
        public async Task<T> PutJsonAsync<T>(object parameters, object body, bool httpOverride = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var policy = RetryPolicy();

            var request = BuildUrl()
                .SetQueryParams(parameters).ToString(true)
                .WithHeader("Accept", "application/json")
                .ConfigureRequest(JsonSettings);


            var response = await PutJsonAsyncWithPolicy<T>(request, body, policy, cancellationToken);

            return response;
        }
        public async Task<T> GetJsonAsync<T>(object parameters,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var policy = RetryPolicy();

            var request = BuildUrl()
                .SetQueryParams(parameters).ToString(true)
            .ConfigureRequest(JsonSettings);

            var response = await GetJsonAsyncWithPolicy<T>(request, policy, cancellationToken);

            return response;
        }
        private Url BuildUrl()
        {
            var url = new Url(_endpoint);
            return url.IsValid() ? url : new Url(BaseUrl).AppendPathSegment(_endpoint);
        }
        private static void JsonSettings(FlurlHttpSettings settings)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            settings.JsonSerializer = new NewtonsoftJsonSerializer(jsonSettings);
        }

        public async Task<T> GetJsonAsyncWithPolicy<T>(IFlurlRequest request, AsyncPolicy policy,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsyncWithPolicy(async ct => await request.GetJsonAsync<T>(ct), policy,
                cancellationToken);
        }
        public async Task<T> PostJsonAsyncWithPolicy<T>(IFlurlRequest request, object body, AsyncPolicy policy,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsyncWithPolicy(async ct => await request.PostJsonAsync(body, ct).ReceiveJson<T>(), policy,
                cancellationToken);
        }

        public async Task<T> PutJsonAsyncWithPolicy<T>(IFlurlRequest request, object body, AsyncPolicy policy,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsyncWithPolicy(async ct => await request.PutJsonAsync(body, ct).ReceiveJson<T>(), policy,
                cancellationToken);
        }
        public async Task<T> ExecuteAsyncWithPolicy<T>(Func<CancellationToken, Task<T>> action, AsyncPolicy policy,
            CancellationToken cancellationToken)
        {
            var policyResult = await policy.ExecuteAndCaptureAsync(action, cancellationToken);

            if (policyResult.Outcome == OutcomeType.Successful)
                return policyResult.Result;

            if (policyResult.FinalException is FlurlHttpException exception)
            {
                LogError(exception.Call);
            }

            throw policyResult.FinalException;
        }
        private void LogError(HttpCall call)
        {
            //Get the values of the parameters passed to the API
            var queryList = HttpUtility.ParseQueryString(call.Request.RequestUri.Query);
            var parameters = string
                .Join(", ", queryList.Cast<string>().Select(x => x + "=" + (queryList[x] ?? "NULL")).ToArray());

            string httpStatus = call.HttpStatus.HasValue ? call.HttpStatus.ToString() : "?";
            string content = call.Response != null && call.Response.Content != null ? call.Response.Content.ToString() : call.Exception != null ? call.Exception.Message : "Unknown";
            //Set up the information message with the URL, the status code, and the parameters.
            string info =
                $"Request to '{call}' failed with status code '{httpStatus}', " +
                $"parameters: '{parameters}', and content: {content}";

            //Acquire the actual exception
            Exception ex;
            if (call.Exception != null)
            {
                ex = call.Exception;
            }
            else
            {
                ex = new Exception(info);
                info = string.Empty;
            }

            //Log the exception and info message
            Logger.Error(ex, info);
        }
        private AsyncPolicy RetryPolicy()
        {
            var policy = Policy
                .Handle<FlurlHttpException>(IsWorthRetrying)
                .Or<WebException>()
                .WaitAndRetryAsync(
                    // number of retries
                    3,
                    // exponential backoff
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    // on retry
                    (exception, timeSpan, retryCount, context) =>
                    {
                        var msg = $"Retry {retryCount} implemented with Polly's RetryPolicy " +
                                  $"of {context.PolicyKey} " +
                                  $"at {context.OperationKey}, " +
                                  $"due to: {exception}.";
                        Logger.Debug(msg);
                    });
            return policy;
        }
        private bool IsWorthRetrying(FlurlHttpException ex)
        {
            if (ex.Call.Response == null) return false;
            switch ((int)ex.Call.Response.StatusCode)
            {
                case 408:
                case 500:
                case 502:
                case 504:
                    return true;
                default:
                    return false;
            }
        }
    }
}
