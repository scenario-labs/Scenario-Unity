using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;
using System.Collections;

public class PixelEditorUI
{
    public static Texture2D currentImage = null;
    public static ImageDataStorage.ImageData imageData = null;

    public List<Texture2D> pixelatedImages = new List<Texture2D>();
    public static List<ImageDataStorage.ImageData> imageDataList = new List<ImageDataStorage.ImageData>();

    public string imageDataUrl = "";
    public string assetId = "";
    public Texture2D selectedTexture = null;
    public float pixelGridSize = 32f;
    public string name = "";
    public bool removeNoise = false;
    public bool removeBackground = false;
    public bool returnImage = true;
    public int itemsPerRow = 1;
    public float padding = 10f;
    public Vector2 scrollPosition = Vector2.zero;

    private int selectedTextureIndex = 0;
    private float leftSectionWidth = 150;
    private readonly int[] allowedPixelGridSizes = new int[] { 32, 64, 128, 256 };
    private int selectedGridSizeIndex = 0;

    public void OnGUI(Rect position)
    {

        Color backgroundColor = new Color32(18, 18, 18, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        GUILayout.BeginHorizontal();

        // Left section
        GUILayout.BeginVertical(GUILayout.Width(position.width * 0.85f));
        float requiredWidth = itemsPerRow * (256 + padding) + padding;
        scrollPosition = GUI.BeginScrollView(new Rect(0, 20, requiredWidth, position.height - 20), scrollPosition, new Rect(0, 0, requiredWidth, position.height - 20));
        itemsPerRow = 5;

        for (int i = 0; i < pixelatedImages.Count; i++)
        {
            int rowIndex = Mathf.FloorToInt((float)i / itemsPerRow);
            int colIndex = i % itemsPerRow;

            Rect boxRect = new Rect(colIndex * (256 + padding), rowIndex * (256 + padding), 256, 256);
            Texture2D texture = pixelatedImages[i];

            if (texture != null)
            {
                if (GUI.Button(boxRect, ""))
                {
                    selectedTexture = texture;
                    selectedTextureIndex = i;
                }
                GUI.DrawTexture(boxRect, texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Box(boxRect, "Loading...");
            }
        }
        GUI.EndScrollView();
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Right section
        GUILayout.BeginVertical(GUILayout.Width(position.width * 0.15f));
    
        GUILayout.Label("Pixelate Image", EditorStyles.boldLabel);
        if (currentImage == null)
        {   
            Rect dropArea = GUILayoutUtility.GetRect(0f, 150f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop an image here");

            Rect buttonRect = new Rect(dropArea.center.x - 50f, dropArea.center.y - 15f, 100f, 30f);
            if (GUI.Button(buttonRect, "Choose Image")) 
            {
                string imagePath = EditorUtility.OpenFilePanel("Choose Image", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(imagePath)) 
                {
                    currentImage = new Texture2D(2, 2);
                    byte[] imageData = File.ReadAllBytes(imagePath);
                    currentImage.LoadImage(imageData);

                    PixelEditorUI.imageData = new ImageDataStorage.ImageData();
                }
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
            {
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (currentEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        string path = DragAndDrop.paths[0];
                        if (System.IO.File.Exists(path) && (System.IO.Path.GetExtension(path).ToLower() == ".png" || System.IO.Path.GetExtension(path).ToLower() == ".jpg" || System.IO.Path.GetExtension(path).ToLower() == ".jpeg"))
                        {
                            currentImage = new Texture2D(2, 2);
                            byte[] imageData = File.ReadAllBytes(path);
                            currentImage.LoadImage(imageData);
                        }
                    }
                    currentEvent.Use();
                }
            }
        }
        else
        {
            Rect rect = GUILayoutUtility.GetRect(leftSectionWidth, leftSectionWidth, GUILayout.Width(300), GUILayout.Height(300));
            GUI.DrawTexture(rect, currentImage, ScaleMode.ScaleToFit);

            if (GUILayout.Button("Clear Image")) 
            {
                currentImage = null;
            }
        }


        GUILayout.Label("Pixel Grid Size:");
        int pixelGridSizeIndex = Array.IndexOf(allowedPixelGridSizes, (int)pixelGridSize);
        if (pixelGridSizeIndex == -1)
        {
            pixelGridSizeIndex = 0;
        }
        selectedGridSizeIndex = GUILayout.SelectionGrid(selectedGridSizeIndex, Array.ConvertAll(allowedPixelGridSizes, x => x.ToString()), allowedPixelGridSizes.Length);
        pixelGridSize = allowedPixelGridSizes[selectedGridSizeIndex];
        removeNoise = EditorGUILayout.Toggle("Remove Noise", removeNoise);
        removeBackground = EditorGUILayout.Toggle("Remove Background", removeBackground);

        if (GUILayout.Button("Pixelate Image"))
        {
            if (currentImage != null)
            {
                var imgBytes = currentImage.EncodeToPNG();
                string base64String = Convert.ToBase64String(imgBytes);
                imageDataUrl = $"data:image/png;base64,{base64String}";
                
                assetId = imageData.Id;
                FetchPixelatedImage(imageDataUrl);
            }
        }

        if (selectedTexture != null)
        {
            if (GUILayout.Button("Download"))
            {
                string fileName = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
                DownloadImage(fileName, selectedTexture.EncodeToPNG());
            }
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    private void DownloadImage(string fileName, byte[] pngBytes)
    {
        var downloadPath = EditorPrefs.GetString("SaveFolder", "Assets");
        string filePath = downloadPath + "/" + fileName;
        File.WriteAllBytes(filePath, pngBytes);
        EditorCoroutineUtility.StartCoroutineOwnerless(RefreshDatabase());
        Debug.Log("Downloaded image to: " + filePath);
    }

    IEnumerator RefreshDatabase()
    {
        yield return null;
        AssetDatabase.Refresh();
    }

    private async void FetchPixelatedImage(string imgUrl)
    {
        try
        {
            string json = "";

            if (assetId == "")
            {
                var payload = new
                {
                    image = imgUrl,
                    pixelGridSize = pixelGridSize,
                    removeNoise = removeNoise,
                    removeBackground = removeBackground,
                    returnImage = returnImage,
                    name = "",
                    colorPalette = ""
                };
                json = JsonConvert.SerializeObject(payload);
            }
            else
            {
                var payload = new
                {
                    image = imgUrl,
                    assetId = assetId,
                    pixelGridSize = pixelGridSize,
                    removeNoise = removeNoise,
                    removeBackground = removeBackground,
                    returnImage = returnImage,
                    name = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png",
                    colorPalette = ""
                };
                json = JsonConvert.SerializeObject(payload);
            }

            Debug.Log(json);

            var client = new RestClient(ApiClient.apiUrl + "/images/pixelate");
            var request = new RestRequest(Method.PUT);
            request.AddHeader("accept", "application/json");
            request.AddHeader("content-type", "application/json");
            request.AddHeader("Authorization", "Basic " + PluginSettings.EncodedAuth);
            request.AddParameter("application/json", json, ParameterType.RequestBody);
            IRestResponse response = await client.ExecuteAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Debug.Log(response);
                var pixelatedResponse = JsonConvert.DeserializeObject<Root>(response.Content);
                string base64 = pixelatedResponse.image.Replace("data:image/png;base64,","");
                byte[] pngBytes = Convert.FromBase64String(base64);
                Texture2D texture = new Texture2D(1,1);
                ImageConversion.LoadImage(texture, pngBytes);
                ImageDataStorage.ImageData newImageData = new ImageDataStorage.ImageData
                {
                    Id = pixelatedResponse.asset.id,
                    Url = pixelatedResponse.image, 
                    InferenceId = pixelatedResponse.asset.ownerId,
                };
                pixelatedImages.Insert(0, texture);
                imageDataList.Insert(0, newImageData);
            }
            else
            {
                Debug.LogError(response.ResponseStatus + " " + response.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

}

[System.Serializable]
public class Asset
{
    public string id { get; set; }
    public string mimeType { get; set; }
    public Type type { get; set; }
    public string ownerId { get; set; }
    public string authorId { get; set; }
    public DateTime createdAt { get; set; }
    public DateTime updatedAt { get; set; }
    public string privacy { get; set; }
    public List<object> tags { get; set; }
    public List<object> collectionIds { get; set; }
}

[System.Serializable]
public class Root
{
    public Asset asset { get; set; }
    public string image { get; set; }
}

[System.Serializable]
public class Type
{
    public string source { get; set; }
    public string parentId { get; set; }
    public string rootParentId { get; set; }
    public string kind { get; set; }
    public int pixelGridSize { get; set; }
    public bool removeNoise { get; set; }
    public bool removeBackground { get; set; }
}

