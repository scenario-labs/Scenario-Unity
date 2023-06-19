using Cysharp.Threading.Tasks;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ApiClient
{
    public static readonly string apiUrl = PluginSettings.ApiUrl;

    public static async UniTask<string> GetAsync(string endpoint)
    {
        using (var client = new HttpClient())
        {
            string encodedAuth = PluginSettings.EncodedAuth;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", PluginSettings.EncodedAuth);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(apiUrl + endpoint);

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                Debug.LogError(response.ReasonPhrase);
                return null;
            }
        }
    }

    public static async UniTask<string> PostAsync(string endpoint, string payload)
    {
        using (var client = new HttpClient())
        {
            string encodedAuth = PluginSettings.EncodedAuth;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", PluginSettings.EncodedAuth);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl + endpoint, requestContent);

            if (response.IsSuccessStatusCode)
            {
                string resultContent = await response.Content.ReadAsStringAsync();
                return resultContent;
            }
            else
            {
                Debug.LogError(response.ReasonPhrase);
                return null;
            }
        }
    }

    public static async UniTask<string> PutAsync(string endpoint, string payload)
    {
        using (var client = new HttpClient())
        {
            string encodedAuth = PluginSettings.EncodedAuth;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", PluginSettings.EncodedAuth);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PutAsync(apiUrl + endpoint, requestContent);

            if (response.IsSuccessStatusCode)
            {
                string resultContent = await response.Content.ReadAsStringAsync();
                return resultContent;
            }
            else
            {
                Debug.LogError(response.ReasonPhrase);
                return null;
            }
        }
    }
}