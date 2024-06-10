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

        public Models.ModelData SelectedModelData { get { return selectedModelData; } set { selectedModelData = value; } }
        private Models.ModelData selectedModelData = null;

        #region ImageDataList

        [SerializeField] private List<ImageDataStorage.ImageData> imageDataList = new();

        public ImageDataStorage.ImageData GetImageDataAtIndex(int index)
        {
            return imageDataList[index];
        }

        public List<ImageDataStorage.ImageData> GetImageDataList()
        {
            return imageDataList;
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

        /// <summary>
        /// After inference ending, get image generated and fill the space reserved to it.
        /// </summary>
        /// <param name="inferenceId"> Id of the Inference </param>
        /// <param name="id"> Image Id </param>
        /// <param name="url"> URL Image </param>
        /// <param name="createdAt"> Date of creation </param>
        /// <param name="_scheduler"> Scheduler of the generation</param>
        /// <param name="_seed"> Seed of the generation </param>
        public void FillReservedSpaceForImageData(string inferenceId, string id, string url, DateTime createdAt, string _scheduler, string _seed)
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
            itm.Scheduler = _scheduler;
            itm.Seed = _seed;
            CommonUtils.FetchTextureFromURL(itm.Url, texture =>
            {
                itm.texture = texture;
            });
        }
        
        public void ReserveSpaceForImageDatas(int numImages, string inferenceId, string promptinputText,
            int samplesliderValue, float widthSliderValue, float heightSliderValue, float guidancesliderValue,
            string schedulerText, string seedinputText, string modelIdInput)
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
                    modelId = modelIdInput
                });
            }
        }

        #endregion

        public ImageDataStorage.ImageData GetImageDataByUrl(string url)
        {
            return imageDataList.FirstOrDefault(x => x.Url == url);
        }

        public ImageDataStorage.ImageData GetImageDataById(string _id)
        {
            return imageDataList.FirstOrDefault(x => x.Id == _id);
        }

        public int GetImageDataIndexByUrl(string url)
        {
            var data = imageDataList.FirstOrDefault(x => x.Url == url);
            var index = imageDataList.IndexOf(data);
            return index;
        }

        public void RemoveImageDataById(string _id)
        {
            imageDataList.RemoveAt(imageDataList.FindIndex(x => x.Id == _id));
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

        public string SelectedModelType
        {
            get => EditorPrefs.GetString("SelectedModelType", "");
            set => EditorPrefs.SetString("SelectedModelType", value);
        }

        public int GetReservedSpaceCount()
        {
            return imageDataList.Count(x => x.Id == "-1");
        }


        public List<ImageDataStorage.ImageData> GetImageDataByInferenceId(string _inferenceId)
        {
            return imageDataList.FindAll(x => x.InferenceId == _inferenceId).ToList();
        }
    }
}