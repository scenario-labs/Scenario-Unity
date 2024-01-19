using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public class Models : EditorWindow
    {
        private static readonly float MinimumWidth = 1000f;

        public static List<ModelData> modelsPrivate = new();
        public static List<ModelData> modelsPublic = new();
    
        public static List<TexturePair> texturesPrivate = new();
        public static List<TexturePair> texturesPublic = new();

        public static string privacyPrivate = "private";
        public static string privacyPublic = "public";
    
        private static ModelsUI modelsUI = new();
        private static Models window;
    
        public static bool IsPrivateTab()
        {
            return CurrentPrivacy == privacyPrivate;
        }

        public static List<ModelData> GetModels()
        {
            return (IsPrivateTab()) ? modelsPrivate : modelsPublic;
        }
    
        public static List<TexturePair> GetTextures()
        {
            return (IsPrivateTab()) ? texturesPrivate : texturesPublic;
        }
    
        [MenuItem("Window/Scenario/Models")]
        public static void ShowWindow()
        {
            SetTab(CurrentPrivacy);
        
            window = GetWindow<Models>("Models");
            window.minSize = new Vector2(MinimumWidth, window.minSize.y);
        }

        public static void SetTab(string privacySetting)
        {
            CurrentPrivacy = privacySetting;
        
            if (privacySetting == privacyPrivate)
            {
                PopulatePrivateModels();
            }
            else
            {
                PopulatePublicModels();
            }
        }

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

        private static void FetchAllPrivateTextures()
        {
            foreach (var item in modelsPrivate)
            {
                string downloadUrl = null;
            
                if (item.thumbnail != null && !string.IsNullOrEmpty(item.thumbnail.url))
                {
                    downloadUrl = item.thumbnail.url;
                }
                else if (item.trainingImages != null && item.trainingImages.Count > 0)
                {
                    downloadUrl = item.trainingImages[0].downloadUrl;
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
    
        private static void FetchAllPublicTextures()
        {

            foreach (var item in modelsPublic)
            {
                string downloadUrl = null;
            
                if (item.thumbnail != null && !string.IsNullOrEmpty(item.thumbnail.url))
                {
                    downloadUrl = item.thumbnail.url;
                }
                else if (item.trainingImages != null && item.trainingImages.Count > 0)
                {
                    downloadUrl = item.trainingImages[0].downloadUrl;
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
    
        private static async Task FetchAllPrivateModels()
        {

            while (true)
            {
                string endpoint = $"models?pageSize=15&status=trained&privacy={privacyPrivate}";

                if (!string.IsNullOrEmpty(PagniationTokenPrivate))
                {
                    endpoint += $"&paginationToken={PagniationTokenPrivate}";
                }

                string response = await ApiClient.RestGetAsync(endpoint);
                if (response is null) { return; }

                var modelsResponse = JsonConvert.DeserializeObject<ModelsResponse>(response);
                if (modelsResponse is null) { return; }
            
                modelsPrivate.AddRange(modelsResponse.models);

                if (modelsResponse.nextPaginationToken is null ||
                    PagniationTokenPrivate == modelsResponse.nextPaginationToken)
                {
                    PagniationTokenPrivate = "";
                    Debug.Log("no next page to fetch.");
                }
                else
                {
                    PagniationTokenPrivate = modelsResponse.nextPaginationToken;
                    Debug.Log("fetching next page data...");
                    continue;
                }

                break;
            }
        }
    
        private static async Task FetchAllPublicModels()
        {
            while (true)
            {
                string endpoint = $"models?pageSize=15&status=trained&privacy={privacyPublic}";

                if (!string.IsNullOrEmpty(PagniationTokenPublic))
                {
                    endpoint += $"&paginationToken={PagniationTokenPublic}";
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

                //* modelsPublic.AddRange(modelsResponse.models);

                if (modelsResponse.nextPaginationToken is null ||
                    PagniationTokenPublic == modelsResponse.nextPaginationToken)
                {
                    PagniationTokenPublic = "";
                    Debug.Log("no next page to fetch.");
                }
                else
                {
                    PagniationTokenPublic = modelsResponse.nextPaginationToken;
                    Debug.Log("fetching next page data...");
                    continue;
                }

                break;
            }
        }

        private void OnGUI()
        {
            modelsUI.OnGUI(this.position);
        }

        public static string CurrentPrivacy
        {
            get => EditorPrefs.GetString("privacy", privacyPrivate);
            set => EditorPrefs.SetString("privacy", value);
        }
    
        public static string PagniationTokenPrivate
        {
            get => EditorPrefs.GetString("paginationTokenPrivate", "");
            set => EditorPrefs.SetString("paginationTokenPrivate", value);
        }
    
        public static string PagniationTokenPublic
        {
            get => EditorPrefs.GetString("paginationTokenPublic", "");
            set => EditorPrefs.SetString("paginationTokenPublic", value);
        }

        public class TexturePair
        {
            public Texture2D texture;
            public string name;
        }
    
        #region API_DTO

        private class ModelsResponse
        {
            public List<ModelData> models { get; set; }
            public string nextPaginationToken { get; set; }
        }

        public class ModelData
        {
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public ClassData classData { get; set; }
            public string privacy { get; set; }
            public string status { get; set; }
            public List<TrainingImageData> trainingImages { get; set; }
            public ThumbnailData thumbnail { get; set; } // Assuming you have a ThumbnailData class.
        }

        public class ClassData
        {
            public string modelId { get; set; }
        }

        public class TrainingImageData
        {
            public string downloadUrl { get; set; }
        }

        public class ThumbnailData
        {
            public string url { get; set; }
        }

        private class TokenResponse
        {
            public string nextPaginationToken { get; set; }
        }

        #endregion
    }
}