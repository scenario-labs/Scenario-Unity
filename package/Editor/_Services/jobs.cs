using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Scenario.Editor
{
    public class Jobs
    {
        // Now accepts a callback to be notified when the job completes
        public static void CheckJobStatus(string jobId, Action<JobAsset> onJobCompleted)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                Debug.LogError("Job ID cannot be null or empty.");
                return;
            }
            EditorCoroutineUtility.StartCoroutineOwnerless(GetJobStatusCoroutine(jobId, onJobCompleted));
        }

        private static IEnumerator GetJobStatusCoroutine(string jobId, Action<JobAsset> onJobCompleted)
        {
            bool jobInProgress = true;

            while (jobInProgress)
            {
                ApiClient.RestGet($"jobs/{jobId}", response =>
                {
                    var progressResponse = JsonConvert.DeserializeObject<JobRoot>(response.Content);

                    if (progressResponse!= null && progressResponse.job!= null &&!string.IsNullOrEmpty(progressResponse.job.status))
                    {
                        switch (progressResponse.job.status)
                        {
                            case "warming-up":
                            case "queued":
                            case "in-progress":
                                Debug.Log($"Job is {progressResponse.job.status}...");
                                break;

                            case "success":
                                Debug.Log("Job completed successfully!");
                                jobInProgress = false;

                                // Fetch asset details for all asset IDs
                                if (progressResponse.job.metadata!= null &&
                                    progressResponse.job.metadata.assetIds!= null &&
                                    progressResponse.job.metadata.assetIds.Count > 0)
                                {
                                    foreach (string assetId in progressResponse.job.metadata.assetIds)
                                    {
                                        GetAssetData(assetId.Trim(), onJobCompleted);
                                    }
                                }
                                else if (progressResponse.asset!= null)
                                {
                                    onJobCompleted?.Invoke(progressResponse.asset);
                                }
                                break;

                            case "failed":
                                Debug.LogError("Job failed!");
                                jobInProgress = false;
                                break;

                            case "canceled":
                                Debug.LogWarning("Job was canceled.");
                                jobInProgress = false;
                                break;

                            default:
                                Debug.LogWarning($"Unknown job status: {progressResponse.job.status}");
                                break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Invalid or incomplete job response. Retrying...");
                    }
                });

                yield return new WaitForSecondsRealtime(4);
            }
        }

        private static void GetAssetData(string assetId, Action<JobAsset> onJobCompleted)
        {
            ApiClient.RestGet($"assets/{assetId}", response =>
            {
                var assetResponse = JsonConvert.DeserializeObject<JobRoot>(response.Content);
                if (assetResponse != null && assetResponse.asset != null)
                {
                    Debug.Log($"Asset ID: {assetResponse.asset.id}, URL: {assetResponse.asset.url}");
                    onJobCompleted?.Invoke(assetResponse.asset);
                }
                else
                {
                    Debug.LogError($"Failed to retrieve asset data for ID: {assetId}");
                }
            });
        }
    }

    public class JobRoot
    {
        public JobAsset asset { get; set; }
        public Job job { get; set; }
    }

    public class Job
    {
        public string status { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Metadata
    {
        public List<string> assetIds { get; set; }
        public Thumbnail thumbnail { get; set; }
    }

    public class JobAsset
    {
        public string id { get; set; }
        public string url { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Thumbnail
    {
        public string assetId { get; set; }
        public string url { get; set; }
    }
}
