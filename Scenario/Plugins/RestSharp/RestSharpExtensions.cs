using System.Net;
using System.Text;
using System;
using System.Threading.Tasks;

namespace RestSharp {
    public static class RestSharpExtensions {
        /// <summary>
        /// Executes the request asynchronously using a Task
        /// </summary>
        /// <param name="request">The request to be executed</param>
        /// <returns>A Task for the response</returns>
        public static Task<IRestResponse> ExecuteAsync(this RestClient client, RestRequest request) {
            var task = new TaskCompletionSource<IRestResponse>();
            client.ExecuteAsync(request, response => task.SetResult(response));
            return task.Task;
        }

        /// <summary>
        /// Executes the request asynchronously using a Task
        /// </summary>
        /// <param name="request">The request to be executed</param>
        /// <param name="handle">A handle to allow aborting the execution</param>
        /// <returns>A Task for the response</returns>
        public static Task<IRestResponse> ExecuteAsync(this RestClient client, RestRequest request, out RestRequestAsyncHandle handle) {
            var task = new TaskCompletionSource<IRestResponse>();
            handle = client.ExecuteAsync(request, response => task.SetResult(response));
            return task.Task;
        }

        /// <summary>
        /// Returns if the Status Code implies success 
        /// </summary>
        /// <param name="responseCode"></param>
        /// <returns></returns>
        public static bool IsSuccess(this HttpStatusCode responseCode) {
            var numericResponse = (int)responseCode;
            const int statusCodeOk = (int)HttpStatusCode.OK;
            const int statusCodeBadRequest = (int)HttpStatusCode.BadRequest;

            return numericResponse >= statusCodeOk && numericResponse < statusCodeBadRequest;
        }

        /// <summary>
        /// Returns if the response was successful.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static bool IsSuccessful(this IRestResponse response) {
            return response.StatusCode.IsSuccess() && response.ResponseStatus == ResponseStatus.Completed;
        }

        /// <summary>
        /// Returns a <see cref="RestSharpException"/> exception if the response was not successful
        /// </summary>
        public static Exception GetException(this IRestResponse response) {
            if (response.IsSuccessful())
                return null;
            else
                return new RestSharpException(response.StatusCode, response.ResponseUri, response.Content, response.GetError(), response.ErrorException);
        }

        /// <summary>
        /// Returns the error string, if the response was not successful. 
        /// </summary>
        public static string GetError(this IRestResponse response) {
            if (response.IsSuccessful())
                return string.Empty;

            var sb = new StringBuilder();
            var uri = response.ResponseUri;

            sb.AppendLine(string.Format("Processing request [{0}] resulted with following errors:", uri));

            if (response.StatusCode.IsSuccess() == false) {
                sb.AppendLine("- Server responded with unsuccessfull status code: ")
                    .Append(response.StatusCode).Append(": ")
                    .Append(response.StatusDescription).Append("- ")
                    .Append(response.Content);
            }

            if (response.ErrorException != null)
                sb.AppendLine("- An exception occurred while processing request: " + response.ErrorMessage);

            return sb.ToString();
        }
    }
}

