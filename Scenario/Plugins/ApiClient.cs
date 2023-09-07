using System;
using System.Net;
using RestSharp;
using UnityEngine;

public static class ApiClient
{
    public static readonly string APIUrl = PluginSettings.ApiUrl;

    public static async void RestPost( string appendUrl,
        string jsonPayload,
        Action<IRestResponse> responseAction, 
        Action<string> errorAction = null)
    {
        var client = new RestClient(APIUrl + "/" + appendUrl);
        var request = new RestRequest(Method.POST);
        request.AddHeader("accept", "application/json");
        request.AddHeader("content-type", "application/json");
        request.AddHeader("Authorization", "Basic " + PluginSettings.EncodedAuth);
        request.AddParameter("application/json", jsonPayload, ParameterType.RequestBody);
        IRestResponse response = await client.ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.OK && response.ErrorException == null)
        {
            Debug.Log($"Response: {response.Content}");
            responseAction?.Invoke(response);
        }
        else
        {
            Debug.Log($"Error: {response.Content}");
            errorAction?.Invoke(response.ErrorMessage);
        }
    }
    
    public static async void RestPut( string appendUrl,
        string jsonPayload,
        Action<IRestResponse> responseAction, 
        Action<string> errorAction = null)
    {
        var client = new RestClient(APIUrl + "/" + appendUrl);
        var request = new RestRequest(Method.PUT);
        request.AddHeader("accept", "application/json");
        request.AddHeader("content-type", "application/json");
        request.AddHeader("Authorization", "Basic " + PluginSettings.EncodedAuth);
        request.AddParameter("application/json", jsonPayload, ParameterType.RequestBody);
        IRestResponse response = await client.ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.OK && response.ErrorException == null)
        {
            Debug.Log($"Response: {response.Content}");
            responseAction?.Invoke(response);
        }
        else
        {
            Debug.Log($"Error: {response.Content}");
            errorAction?.Invoke(response.ErrorMessage);
        }
    }
    
    public static async void RestDelete( string appendUrl,
        Action<IRestResponse> responseAction, 
        Action<string> errorAction = null)
    {
        var client = new RestClient(APIUrl + "/" + appendUrl);
        var request = new RestRequest(Method.DELETE);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", "Basic " + PluginSettings.EncodedAuth);

        IRestResponse response = await client.ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.OK && response.ErrorException == null)
        {
            Debug.Log($"Response: {response.Content}");
            responseAction?.Invoke(response);
        }
        else
        {
            Debug.Log($"Error: {response.Content}");
            errorAction?.Invoke(response.ErrorMessage);
        }
    }
    
    public static async void RestGet( string appendUrl,
        Action<IRestResponse> responseAction,
        Action<string> errorAction = null)
    {
        var client = new RestClient(APIUrl + "/" + appendUrl);
        var request = new RestRequest(Method.GET);
        request.AddHeader("accept", "application/json");
        request.AddHeader("Authorization", "Basic " + PluginSettings.EncodedAuth);

        IRestResponse response = await client.ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.OK && response.ErrorException == null)
        {
            Debug.Log($"Response: {response.Content}");
            responseAction?.Invoke(response);
        }
        else
        {
            Debug.Log($"Error: {response.Content}");
            errorAction?.Invoke(response.ErrorMessage);
        }
    }
}