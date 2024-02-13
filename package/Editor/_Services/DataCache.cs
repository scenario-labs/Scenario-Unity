using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    /// <summary>
    /// Store the cache of images that has been newly generated
    /// </summary>
    public class DataCache : ScriptableSingleton<DataCache>
    {
        #region ImageDataList

        [SerializeField] private List<ImageDataStorage.ImageData> imageDataList = new();

        public ImageDataStorage.ImageData GetImageDataAtIndex(int index)
        {
            return imageDataList[index];
        }

        public void AddImageDataInFront(ImageDataStorage.ImageData imageData)
        {
            //Debug.Log($"add image data in front {imageData}");
            imageDataList.Insert(0, imageData);
        }
        
        public void AddImageDataInBack(ImageDataStorage.ImageData imageData)
        {
            //Debug.Log($"add image data in back {imageData}");
            imageDataList.Add(imageData);
        }

        public bool DoesImageIdExist(string id)
        {
            return imageDataList.Exists(x => x.Id == id);
        }

        public int GetImageDataCount()
        {
            if (imageDataList == null)
            {
                return 0;
            }
            return imageDataList.Count;
        }

        public void ClearAllImageData()
        {
            Debug.Log("cleared all image data");
            imageDataList.Clear();
        }

        public List<ImageDataStorage.ImageData> GetRangeOfImageData(int firstIndex, int count)
        {
            return imageDataList.GetRange(firstIndex, count);
        }

        public void FillReservedSpaceForImageData(string inferenceId, string id, string url, DateTime createdAt)
        {
            var itm = imageDataList.FirstOrDefault(x =>
            {
                if (x.InferenceId == inferenceId && x.Id == "-1") return true;
                return false;
            });
            if (itm == null)
            {
                Debug.LogError("Invalid inferenceId && Id combo");
                return;
            }
            itm.Id = id;
            itm.Url = url;
            itm.CreatedAt = createdAt;
        }
        
        public void ReserveSpaceForImageDatas(int numImages, string inferenceId, string promptinputText,
            float samplesliderValue, float widthSliderValue, float heightSliderValue, float guidancesliderValue,
            string schedulerText, string seedinputText)
        {
            for (int i = 0; i < numImages; i++)
            {
                AddImageDataInFront(new ImageDataStorage.ImageData()
                {
                    Id = "-1",
                    InferenceId = inferenceId,
                    Prompt = promptinputText,
                    Steps = samplesliderValue,
                    Size = new Vector2(widthSliderValue, heightSliderValue),
                    Guidance = guidancesliderValue,
                    Scheduler = schedulerText,
                    Seed = seedinputText,
                });
            }
        }

        #endregion

        public ImageDataStorage.ImageData GetImageDataByUrl(string url)
        {
            var data = imageDataList.FirstOrDefault(x => x.Url == url);
            return data;
        }
        
        public int GetImageDataIndexByUrl(string url)
        {
            var data = imageDataList.FirstOrDefault(x => x.Url == url);
            var index = imageDataList.IndexOf(data);
            return index;
        }

        public void RemoveImageDataAtIndex(int index)
        {
            imageDataList.RemoveAt(index);
        }

        public void RemoveInferenceData(string inferenceId)
        {
            imageDataList.RemoveAll(x => x.InferenceId == inferenceId);
        }

        public string SelectedModelId
        {
            get => EditorPrefs.GetString("SelectedModelId", "");
            set => EditorPrefs.SetString("SelectedModelId", value);
        }

        public int GetReservedSpaceCount()
        {
            return imageDataList.Count(x => x.Id == "-1");
        }
    }
}