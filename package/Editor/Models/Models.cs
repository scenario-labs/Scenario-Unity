using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class Models : EditorWindow
    {

        #region Public Fields

        /// <summary>
        /// List containing models from quickstart action
        /// </summary>
        public static List<ModelData> modelsQuickStart = new();

        /// <summary>
        /// List containing models from private action
        /// </summary>
        public static List<ModelData> modelsPrivate = new();

        /// <summary>
        /// List containing models from public action
        /// </summary>
        public static List<ModelData> modelsPublic = new();

        /// <summary>
        /// List containing textures from models from quickstart action
        /// </summary>
        public static List<TexturePair> texturesQuickStart = new();

        /// <summary>
        /// List containing textures from models from private action
        /// </summary>
        public static List<TexturePair> texturesPrivate = new();

        /// <summary>
        /// List containing textures from models from public action
        /// </summary>
        public static List<TexturePair> texturesPublic = new();

        /// <summary>
        /// Setting string for privacy of quickstart action
        /// </summary>
        public static string privacyQuickStart = "quickstart";

        /// <summary>
        /// Setting string for privacy of private action
        /// </summary>
        public static string privacyPrivate = "private";

        /// <summary>
        /// Setting string for privacy of public action
        /// </summary>
        public static string privacyPublic = "public";

        /// <summary>
        /// Reference of compression url's extension to reduce memory storage and consumption
        /// </summary>
        internal static string cdnExtension = "&format=jpeg&quality=80&width=256";

        #endregion

        #region Private Fields

        private static readonly float MinimumWidth = 1000f;

        private static ModelsUI modelsUI = new();
        private static Models window;

        /// <summary>
        /// While process try to download models.
        /// </summary>
        private static bool isProcessing = false;

        #endregion

        #region EditorWindow Callbacks

        private void OnGUI()
        {
            modelsUI.OnGUI(this.position);
        }

        private void OnEnable()
        {
            SetTab(privacyQuickStart);
        }

        private void OnDestroy()
        {
            isProcessing = false;
        }

        #endregion

        #region Public Methods

        [MenuItem("Window/Scenario/Models", false, 11)]
        public static void ShowWindow()
        {
            window = GetWindow<Models>("Models");
            window.minSize = new Vector2(MinimumWidth, window.minSize.y);
        }

        public static void SetTab(string privacySetting)
        {
            CurrentPrivacy = privacySetting;
            isProcessing = false;
            switch (privacySetting)
            {
                case string str when str.Equals(privacyQuickStart):
                    PopulateQuickStartModels();
                    break;

                case string str when str.Equals(privacyPrivate):
                    PopulatePrivateModels();
                    break;

                case string str when str.Equals(privacyPublic):
                    PopulatePublicModels();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Check which tab is selected, is it Private one ?
        /// </summary>
        /// <returns> True or False </returns>
        public static bool IsPrivateTab()
        {
            return CurrentPrivacy == privacyPrivate;
        }

        /// <summary>
        /// Check which tab is selected, is it Quickstart one ?
        /// </summary>
        /// <returns> True or False </returns>
        public static bool IsQuickStartTab()
        {
            return CurrentPrivacy == privacyQuickStart;
        }

        /// <summary>
        /// Depending from the tab selected returned correct models list.
        /// </summary>
        /// <returns> Models list depending from the tab </returns>
        public static List<ModelData> GetModels()
        {
            if (IsQuickStartTab())
            {
                return modelsQuickStart;
            }
            else 
            { 
                return (IsPrivateTab()) ? modelsPrivate : modelsPublic;
            }
        }

        /// <summary>
        /// Depending from the tab selected returned correct textures list.
        /// </summary>
        /// <returns> Textures list depending from the tab </returns>
        public static List<TexturePair> GetTextures()
        {
            if (IsQuickStartTab())
            {
                return texturesQuickStart;
            }
            else
            {
                return (IsPrivateTab()) ? texturesPrivate : texturesPublic;
            }
        }

        /// <summary>
        /// Fetching model by is ID
        /// </summary>
        /// <param name="_modelId"> The id of the model </param>
        /// <returns></returns>
        public static async Task<ModelData> FetchModelById(string _modelId)
        {
            string endpoint = $"models/{_modelId}";
            string response = await ApiClient.RestGetAsync(endpoint);
            if (response is null) { return null; }

            ModelData modelResponse = JsonConvert.DeserializeObject<Dictionary<string, ModelData>>(response)["model"];
            if (modelResponse is null) { return null; }
            return modelResponse;
        }

        /// <summary>
        /// Fetchning Thumbnail for a model
        /// </summary>
        /// <param name="_modelData"> The model to fetch </param>
        /// <returns></returns>
        public static async Task<Texture2D> FetchThumbnailForModel(ModelData _modelData)
        {
            var tcs = new TaskCompletionSource<Texture2D>();
            try
            {
                CommonUtils.FetchTextureFromURL(_modelData.thumbnail.url, texture =>
                {
                    tcs.SetResult(texture); // This will complete the task with the result being the texture.
                });
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return await tcs.Task; // This will asynchronously wait until the task is completed.
        }

        public static string CurrentPrivacy
        {
            get => EditorPrefs.GetString("privacy", privacyQuickStart);
            set => EditorPrefs.SetString("privacy", value);
        }

        public static string PaginationTokenQuickStart
        {
            get => EditorPrefs.GetString("paginationTokenQuickStart", "");
            set => EditorPrefs.SetString("paginationTokenQuickStart", value);
        }

        public static string PaginationTokenPrivate
        {
            get => EditorPrefs.GetString("paginationTokenPrivate", "");
            set => EditorPrefs.SetString("paginationTokenPrivate", value);
        }

        public static string PaginationTokenPublic
        {
            get => EditorPrefs.GetString("paginationTokenPublic", "");
            set => EditorPrefs.SetString("paginationTokenPublic", value);
        }

        public class TexturePair
        {
            public Texture2D texture;
            public string name;
        }

        #endregion

        #region Private Methods

        private static async void PopulatePublicModels()
        {
            modelsPublic.Clear();
            await FetchAllPublicModels();
            FetchAllPublicTextures();

            modelsUI.RedrawPage(0);
        }

        private static async void PopulatePrivateModels()
        {
            modelsPrivate.Clear();
            await FetchAllPrivateModels();
            FetchAllPrivateTextures();

            modelsUI.RedrawPage(0);
        }

        /// <summary>
        /// On Selected quickstart models tab, launch a request and get all quickstart models
        /// </summary>
        private static async void PopulateQuickStartModels()
        {
            modelsQuickStart.Clear();

            await FetchAllQuickStartModels();
            FetchAllQuickStartTextures();
            modelsUI.RedrawPage(0);
        }

        /// <summary>
        /// Processing to get all quickstart textures
        /// </summary>
        private static void FetchAllQuickStartTextures()
        {
            foreach (var item in modelsQuickStart)
            {
                string downloadUrl = null;

                if (item.thumbnail != null && !string.IsNullOrEmpty(item.thumbnail.url))
                {
                    downloadUrl = item.thumbnail.url + cdnExtension;
                }
                else if (item.trainingImages != null && item.trainingImages.Count > 0)
                {
                    downloadUrl = item.trainingImages[0].downloadUrl + cdnExtension;
                }

                if (string.IsNullOrEmpty(downloadUrl)) continue;

                var texturePair = new TexturePair()
                {
                    name = item.name,
                    texture = null,
                };

                texturesQuickStart.Add(texturePair);

                CommonUtils.FetchTextureFromURL(downloadUrl, texture =>
                {
                    texturePair.texture = texture;
                });

                if (window != null) { window.Repaint(); }
            }
        }

        /// <summary>
        /// Processing to get all private textures
        /// </summary>
        private static void FetchAllPrivateTextures()
        {
            foreach (var item in modelsPrivate)
            {
                string downloadUrl = null;

                if (item.thumbnail != null && !string.IsNullOrEmpty(item.thumbnail.url))
                {
                    downloadUrl = item.thumbnail.url + cdnExtension;
                }
                else if (item.trainingImages != null && item.trainingImages.Count > 0)
                {
                    downloadUrl = item.trainingImages[0].downloadUrl + cdnExtension;
                }

                if (string.IsNullOrEmpty(downloadUrl)) continue;

                var texturePair = new TexturePair()
                {
                    name = item.name,
                    texture = null,
                };

                texturesPrivate.Add(texturePair);

                CommonUtils.FetchTextureFromURL(downloadUrl, texture =>
                {
                    texturePair.texture = texture;
                });

                if (window != null) { window.Repaint(); }
            }
        }

        /// <summary>
        /// Processing to get all public textures.
        /// </summary>
        private static void FetchAllPublicTextures()
        {

            foreach (var item in modelsPublic)
            {
                string downloadUrl = null;

                if (item.thumbnail != null && !string.IsNullOrEmpty(item.thumbnail.url))
                {
                    downloadUrl = item.thumbnail.url + cdnExtension;
                }
                else if (item.trainingImages != null && item.trainingImages.Count > 0)
                {
                    downloadUrl = item.trainingImages[0].downloadUrl + cdnExtension;
                }

                if (string.IsNullOrEmpty(downloadUrl)) continue;

                var texturePair = new TexturePair()
                {
                    name = item.name,
                    texture = null,
                };

                texturesPublic.Add(texturePair);

                CommonUtils.FetchTextureFromURL(downloadUrl, texture =>
                {
                    texturePair.texture = texture;
                });

                if (window != null) { window.Repaint(); }
            }
        }

        /// <summary>
        /// Processing to get all quickstart models
        /// </summary>
        /// <returns></returns>
        private static async Task FetchAllQuickStartModels()
        {
            if (!isProcessing)
            {
                isProcessing = true;
                while (isProcessing)
                {
                    string endpoint = $"models?pageSize=15&status=trained&privacy={privacyPublic}";

                    if (!string.IsNullOrEmpty(PaginationTokenQuickStart))
                    {
                        endpoint += $"&paginationToken={PaginationTokenQuickStart}";
                    }

                    string response = await ApiClient.RestGetAsync(endpoint);
                    if (response is null) { return; }

                    var modelsResponse = JsonConvert.DeserializeObject<ModelsResponse>(response);
                    if (modelsResponse is null) { return; }

                    // Treat incoming models
                    for (int i = 0; i < modelsResponse.models.Count; i++)
                    {
                        if (modelsResponse.models[i].tags.Contains("Unity") && !modelsQuickStart.Contains(modelsResponse.models[i]))
                        {
                            modelsQuickStart.Add(modelsResponse.models[i]);
                        }
                    }

                    if (modelsResponse.nextPaginationToken is null ||
                        PaginationTokenQuickStart == modelsResponse.nextPaginationToken)
                    {
                        PaginationTokenQuickStart = "";
                        Debug.Log("no next page to fetch.");
                        isProcessing = false;
                        return;
                    }
                    else
                    {
                        PaginationTokenQuickStart = modelsResponse.nextPaginationToken;
                        Debug.Log("fetching next page data...");
                    }

                }
            }
        }

        /// <summary>
        /// Processing to get all private models.
        /// </summary>
        /// <returns></returns>
        private static async Task FetchAllPrivateModels()
        {
            if (!isProcessing)
            {
                isProcessing = true;
                while (isProcessing)
                {
                    string endpoint = $"models?pageSize=15&status=trained&privacy={privacyPrivate}";

                    if (!string.IsNullOrEmpty(PaginationTokenPrivate))
                    {
                        endpoint += $"&paginationToken={PaginationTokenPrivate}";
                    }

                    string response = await ApiClient.RestGetAsync(endpoint);
                    if (response is null) { return; }

                    var modelsResponse = JsonConvert.DeserializeObject<ModelsResponse>(response);
                    if (modelsResponse is null) { return; }

                    modelsPrivate.AddRange(modelsResponse.models);

                    if (modelsResponse.nextPaginationToken is null ||
                        PaginationTokenPrivate == modelsResponse.nextPaginationToken)
                    {
                        PaginationTokenPrivate = "";
                        Debug.Log("no next page to fetch.");
                        isProcessing = false;
                        return;
                    }
                    else
                    {
                        PaginationTokenPrivate = modelsResponse.nextPaginationToken;
                        Debug.Log("fetching next page data...");
                    }
                }
            }
        }

        /// <summary>
        /// Processing to get all public models
        /// </summary>
        /// <returns></returns>
        private static async Task FetchAllPublicModels()
        {
            if (!isProcessing)
            {
                isProcessing = true;
                while (isProcessing)
                {
                    string endpoint = $"models?pageSize=15&status=trained&privacy={privacyPublic}";

                    if (!string.IsNullOrEmpty(PaginationTokenPublic))
                    {
                        endpoint += $"&paginationToken={PaginationTokenPublic}";
                    }

                    string response = await ApiClient.RestGetAsync(endpoint);
                    if (response is null) { return; }

                    var modelsResponse = JsonConvert.DeserializeObject<ModelsResponse>(response);
                    if (modelsResponse is null) { return; }

                    foreach (var model in modelsResponse.models)
                    {
                        if (modelsPublic.Contains(model))
                        {
                            continue;
                        }

                        modelsPublic.Add(model);
                    }

                    if (modelsResponse.nextPaginationToken is null ||
                        PaginationTokenPublic == modelsResponse.nextPaginationToken)
                    {
                        PaginationTokenPublic = "";
                        Debug.Log("no next page to fetch.");
                        isProcessing = false;
                        return;
                    }
                    else
                    {
                        PaginationTokenPublic = modelsResponse.nextPaginationToken;
                        Debug.Log("fetching next page data...");
                    }
                }
            }
        }

        #endregion
    
        #region API_DTO

        [Serializable]
        private class ModelsResponse
        {
            public List<ModelData> models { get; set; }
            public string nextPaginationToken { get; set; }
        }

        [Serializable]
        public class ModelData
        {
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string[] tags { get; set; }
            public ClassData classData { get; set; }
            public string privacy { get; set; }
            public string status { get; set; }
            public List<TrainingImageData> trainingImages { get; set; }
            public ThumbnailData thumbnail { get; set; } // Assuming you have a ThumbnailData class.
        }

        [Serializable]
        public class ClassData
        {
            public string modelId { get; set; }
        }

        [Serializable]
        public class TrainingImageData
        {
            public string downloadUrl { get; set; }
        }

        [Serializable]
        public class ThumbnailData
        {
            public string url { get; set; }
        }

        [Serializable]
        private class TokenResponse
        {
            public string nextPaginationToken { get; set; }
        }

        #endregion
    }
}