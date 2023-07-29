using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
using UnityEditor.UIElements;
using RestSharp;
using Newtonsoft.Json;

public class ContextMenuActions {

  private LayerEditor layerEditor;

  public ContextMenuActions(LayerEditor layerEditor) {
    this.layerEditor = layerEditor;  
  }

  public void CreateContextMenu(int index) {
    
    GenericMenu menu = new GenericMenu();

    menu.AddItem(new GUIContent("Move Up"), false, () => MoveLayerUp(index));
    menu.AddItem(new GUIContent("Move Down"), false, () => MoveLayerDown(index));
    menu.AddItem(new GUIContent("Clone"), false, () => CloneLayer(index));
    menu.AddItem(new GUIContent("Delete"), false, () => DeleteLayer(index));
    menu.AddSeparator("");
    menu.AddItem(new GUIContent("Flip/Horizontal"), false, () => FlipHorizontal(index));
    menu.AddItem(new GUIContent("Flip/Vertical"), false, () => FlipVertical(index));
    menu.AddItem(new GUIContent("Remove/Background"), false, () => RemoveBackground(index));
    menu.AddItem(new GUIContent("Set As Background"), false, () => SetAsBackground(index));
    
    menu.ShowAsContext();
  }

  private void MoveLayer(int fromIndex, int toIndex) {
    if (fromIndex >= 0 && fromIndex < layerEditor.uploadedImages.Count && 
        toIndex >= 0 && toIndex < layerEditor.uploadedImages.Count) {
          
        Texture2D image = layerEditor.uploadedImages[fromIndex];
        Vector2 position = layerEditor.imagePositions[fromIndex];
        bool isDragging = layerEditor.isDraggingList[fromIndex];
        Vector2 size = layerEditor.imageSizes[fromIndex];

        layerEditor.uploadedImages.RemoveAt(fromIndex);
        layerEditor.imagePositions.RemoveAt(fromIndex);
        layerEditor.isDraggingList.RemoveAt(fromIndex);
        layerEditor.imageSizes.RemoveAt(fromIndex);

        layerEditor.uploadedImages.Insert(toIndex, image);
        layerEditor.imagePositions.Insert(toIndex, position);
        layerEditor.isDraggingList.Insert(toIndex, isDragging);  
        layerEditor.imageSizes.Insert(toIndex, size);

        GameObject spriteObj = layerEditor.spriteObjects[fromIndex];
        layerEditor.spriteObjects.RemoveAt(fromIndex);
        layerEditor.spriteObjects.Insert(toIndex, spriteObj);

        for (int i = 0; i < layerEditor.spriteObjects.Count; i++)
        {
            GameObject obj = layerEditor.spriteObjects[i];
            SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = i;
        }

        layerEditor.selectedLayerIndex = toIndex;

        layerEditor.Repaint();
    }
}


  private void SetAsBackground(int index) {
    
    if (index >= 0 && index < layerEditor.uploadedImages.Count) {
    
      Texture2D selectedImage = layerEditor.uploadedImages[index];
      layerEditor.backgroundImage = selectedImage;
    
    }
  }

  private void MoveLayerUp(int index) {
    
    MoveLayer(index, index + 1);
  
  }

  private void MoveLayerDown(int index) {
  
    MoveLayer(index, index - 1);
  
  }

  private void CloneLayer(int index) {
    if (index >= 0 && index < layerEditor.uploadedImages.Count) {
      Texture2D originalImage = layerEditor.uploadedImages[index];
      Texture2D clonedImage = new Texture2D(originalImage.width, originalImage.height);
      clonedImage.SetPixels(originalImage.GetPixels());
      clonedImage.Apply();

      layerEditor.uploadedImages.Insert(index + 1, clonedImage);
      layerEditor.imagePositions.Insert(index + 1, layerEditor.imagePositions[index]);
      layerEditor.isDraggingList.Insert(index + 1, false);
      layerEditor.imageSizes.Insert(index + 1, layerEditor.imageSizes[index]);

      Rect rect = new Rect(0, 0, clonedImage.width, clonedImage.height);
      Vector2 pivot = new Vector2(0.5f, 0.5f);
      Sprite sprite = Sprite.Create(clonedImage, rect, pivot);

      string originalName = layerEditor.spriteObjects[index].name;
      GameObject spriteObj = new GameObject(originalName + "-clone");
      SpriteRenderer renderer = spriteObj.AddComponent<SpriteRenderer>();
      renderer.sprite = sprite;

      spriteObj.transform.position = new Vector3(layerEditor.spriteObjects[index].transform.position.x, 
        layerEditor.spriteObjects[index].transform.position.y, 
        0);

      layerEditor.spriteObjects.Insert(index + 1, spriteObj);

      for (int i = 0; i < layerEditor.spriteObjects.Count; i++)
      {
          GameObject obj = layerEditor.spriteObjects[i];
          SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
          spriteRenderer.sortingOrder = i;
      }
    }
  }

  private void DeleteLayer(int index) {

    if (index >= 0 && index < layerEditor.uploadedImages.Count) {

      try {
      
        layerEditor.uploadedImages.RemoveAt(index);
        layerEditor.imagePositions.RemoveAt(index);
        layerEditor.isDraggingList.RemoveAt(index);
        layerEditor.imageSizes.RemoveAt(index);
    
        GameObject spriteObj = layerEditor.spriteObjects[index];
        GameObject.DestroyImmediate(spriteObj);
        layerEditor.spriteObjects.RemoveAt(index);
      
      } catch (Exception ex) {
      
        Debug.LogError("Error deleting layer: " + ex.Message);
        return;
      
      }

      if (layerEditor.selectedLayerIndex == index) {
      
        layerEditor.selectedLayerIndex = -1;
      
      } else if (layerEditor.selectedLayerIndex > index) {
      
        layerEditor.selectedLayerIndex--;
      
      }
    }
  }

  private void FlipHorizontal(int index) {

    Texture2D texture = layerEditor.uploadedImages[index];

    Texture2D flipped = new Texture2D(texture.width, texture.height);

    for (int y = 0; y < texture.height; y++) {
    
      for (int x = 0; x < texture.width; x++) {
      
        flipped.SetPixel(x, y, texture.GetPixel(texture.width - x - 1, y));  
      
      }
    
    }

    flipped.Apply();
    layerEditor.uploadedImages[index] = flipped;

    Rect spriteRect = new Rect(0, 0, flipped.width, flipped.height);
    Vector2 pivot = new Vector2(0.5f, 0.5f);
    Sprite newSprite = Sprite.Create(flipped, spriteRect, pivot);

    GameObject spriteObj = layerEditor.spriteObjects[index];
    SpriteRenderer renderer = spriteObj.GetComponent<SpriteRenderer>();
    renderer.sprite = newSprite;

  }

  private void FlipVertical(int index) {

    Texture2D texture = layerEditor.uploadedImages[index];

    Texture2D flipped = new Texture2D(texture.width, texture.height);

    for (int y = 0; y < texture.height; y++) {
    
      for (int x = 0; x < texture.width; x++) {
      
        flipped.SetPixel(x, y, texture.GetPixel(x, texture.height - y - 1));
      
      }
    
    }

    flipped.Apply();
    layerEditor.uploadedImages[index] = flipped;

    Rect spriteRect = new Rect(0, 0, flipped.width, flipped.height);
    Vector2 pivot = new Vector2(0.5f, 0.5f);
    Sprite newSprite = Sprite.Create(flipped, spriteRect, pivot);

    GameObject spriteObj = layerEditor.spriteObjects[index];
    SpriteRenderer renderer = spriteObj.GetComponent<SpriteRenderer>();
    renderer.sprite = newSprite;

  }
  
  internal void RemoveBackground(int index)
  {
    if (index >= 0 && index < layerEditor.uploadedImages.Count)
    {
      Texture2D texture2D = layerEditor.uploadedImages[index];
      var imgBytes = texture2D.EncodeToPNG();
      string base64String = Convert.ToBase64String(imgBytes);
      string dataUrl = $"data:image/png;base64,{base64String}";
      EditorCoroutineUtility.StartCoroutineOwnerless(PutRemoveBackground(dataUrl, index));
    }
  }

  private IEnumerator PutRemoveBackground(string dataUrl, int index)
  {
      string name = "image" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";

      string url = $"{PluginSettings.ApiUrl}/images/erase-background";

      RestClient client = new RestClient(url);
      RestRequest request = new RestRequest(Method.PUT);

      string param = $"{{\"image\":\"{dataUrl}\",\"name\":\"{name}\",\"backgroundColor\":\"\",\"format\":\"png\",\"returnImage\":\"false\"}}";
      Debug.Log(param);

      request.AddHeader("accept", "application/json");
      request.AddHeader("content-type", "application/json");
      request.AddHeader("Authorization", $"Basic {PluginSettings.EncodedAuth}");
      request.AddParameter("application/json", param, ParameterType.RequestBody);

      yield return client.ExecuteAsync(request, response =>
      {
        if (response.ErrorException != null)
        {
          Debug.Log($"Error: {response.ErrorException.Message}");
        }
        else
        {
          try
          {
            dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);
            string imageUrl = jsonResponse.asset.url;

            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadImageIntoMemory(imageUrl, index));
          }
          catch (Exception ex)
          {
            Debug.LogError("An error occurred while processing the response: " + ex.Message);
          }
        }
      });
  }

  private IEnumerator DownloadImageIntoMemory(string imageUrl, int index)
  {
    using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imageUrl))
    {
      yield return uwr.SendWebRequest();

      if (uwr.result != UnityWebRequest.Result.Success)
      {
        Debug.Log(uwr.error);
      }
      else
      {
        Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
        layerEditor.uploadedImages[index] = texture;

        // Update the sprite
        Rect spriteRect = new Rect(0, 0, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        Sprite newSprite = Sprite.Create(texture, spriteRect, pivot);

        GameObject spriteObj = layerEditor.spriteObjects[index];
        SpriteRenderer renderer = spriteObj.GetComponent<SpriteRenderer>();
        renderer.sprite = newSprite;
      }
    }
  }
}