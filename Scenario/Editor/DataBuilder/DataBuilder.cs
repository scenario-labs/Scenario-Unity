using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public class DataBuilder : EditorWindow
{
    private enum ScreenshotType
    {
        Normal,
        Depth
    }

    private GameObject selectedObject;
    private GameObject instantiatedObject;
    private GameObject[] cameras;
    private Texture2D combinedScreenshot;
    private ScreenshotType screenshotType = ScreenshotType.Normal;
    private ScreenshotType previousScreenshotType;
    private static float minimumWidth = 800f;
    private static float minimumHeight = 750f;
    private const int ScreenshotSize = 1024;
    private float cameraDistance = 2f;

    [MenuItem("Window/Scenario/3d Data Builder")]
    public static void ShowWindow()
    {
        GetWindow<DataBuilder>("3d Data Builder");
        DataBuilder window = GetWindow<DataBuilder>("3d Data Builder");
        window.minSize = new Vector2(minimumWidth, window.minSize.y);
        window.minSize = new Vector2(window.minSize.x, minimumHeight);
    }

    private void Update()
    {
        if (previousScreenshotType != screenshotType && cameras != null)
        {
            foreach (GameObject cameraObject in cameras)
            {
                Camera cameraComponent = cameraObject.GetComponent<Camera>();
                if (screenshotType == ScreenshotType.Normal)
                {
                    cameraComponent.backgroundColor = Color.white;
                }
                else if (screenshotType == ScreenshotType.Depth)
                {
                    cameraComponent.backgroundColor = Color.black;
                }
            }

            previousScreenshotType = screenshotType;
        }
    }

    private void OnDestroy()
    {
        RemoveCameras();

        if (instantiatedObject != null)
        {
            DestroyImmediate(instantiatedObject);
            instantiatedObject = null;
        }
    }

    private void OnGUI()
    {
        Color backgroundColor = new Color32(26, 26, 26, 255);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        if (combinedScreenshot != null)
        {
            float maxWidth = 768f;
            float maxHeight = 768f;

            float aspectRatio = (float)combinedScreenshot.width / combinedScreenshot.height;
            float width = Mathf.Min(maxWidth, maxHeight * aspectRatio);
            float height = width / aspectRatio;

            Rect rect = GUILayoutUtility.GetRect(width, height);
            GUI.DrawTexture(rect, combinedScreenshot, ScaleMode.ScaleToFit);
        }

        GUILayout.Label("Select Object", EditorStyles.boldLabel);
        selectedObject = EditorGUILayout.ObjectField("Object", selectedObject, typeof(GameObject), true) as GameObject;

        float previousCameraDistance = cameraDistance;
        cameraDistance = EditorGUILayout.Slider("Camera Distance", cameraDistance, 0.1f, 100.0f);

        screenshotType = (ScreenshotType)EditorGUILayout.EnumPopup("Screenshot Type", screenshotType);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Place Object"))
        {
            PlaceObjectAndCameras();
        }

        if (GUILayout.Button("Remove Cameras"))
        {
            RemoveCameras();
        }

        if (GUILayout.Button("Take Screenshot"))
        {
            CaptureScreenshots();
        }
        EditorGUILayout.EndHorizontal();

        if (Mathf.Approximately(previousCameraDistance, cameraDistance) == false && instantiatedObject != null)
        {
            UpdateCameraPositions();
        }

        GUILayout.Space(20f);

        GUIStyle generateButtonStyle = new GUIStyle(GUI.skin.button);
        generateButtonStyle.normal.background = CreateColorTexture(new Color(0, 0.5333f, 0.8f, 1));
        generateButtonStyle.normal.textColor = Color.white;
        generateButtonStyle.active.background = CreateColorTexture(new Color(0, 0.3f, 0.6f, 1));
        generateButtonStyle.active.textColor = Color.white;

        if (GUILayout.Button("Use Image", generateButtonStyle, GUILayout.Height(40)))
        {
            if (combinedScreenshot == null)
            {
                Debug.Log("MUST HAVE A 3D IMAGE VIEW");
                return;
            }
            else
            {
                PromptWindowUI.imageUpload = combinedScreenshot;
            }
        }
    }

    private void PlaceObjectAndCameras()
    {
        if (selectedObject != null)
        {
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            Vector3 screenCenter = new Vector3(0.5f, 0.5f, 0.01f);
            Vector3 worldCenter = sceneCamera.ViewportToWorldPoint(screenCenter);

            instantiatedObject = Instantiate(selectedObject, worldCenter, Quaternion.identity);
            cameras = new GameObject[9];

            for (int i = 0; i < 9; i++)
            {
                cameras[i] = CreateCamera("Camera " + (i + 1));
                Camera cameraComponent = cameras[i].GetComponent<Camera>();

                cameraComponent.clearFlags = CameraClearFlags.SolidColor;
                
                if (screenshotType == ScreenshotType.Normal)
                {
                    cameraComponent.backgroundColor = Color.white;
                }
                else if (screenshotType == ScreenshotType.Depth)
                {
                    cameraComponent.backgroundColor = Color.black;
                }

                cameraComponent.fieldOfView = 54f;
            }

            UpdateCameraPositions();
        }
    }

    private void UpdateCameraPositions()
    {
        Vector3 objectCenter = GetObjectCenter(instantiatedObject);

        (float latitude, float longitude)[] sphericalCoords = new (float, float)[]
        {
            (Mathf.PI / 4, 0),
            (Mathf.PI / 4, 2 * Mathf.PI / 3),
            (Mathf.PI / 4, 4 * Mathf.PI / 3),
            (Mathf.PI / 2, Mathf.PI / 3),
            (Mathf.PI / 2, Mathf.PI),
            (Mathf.PI / 2, 5 * Mathf.PI / 3),
            (3 * Mathf.PI / 4, 0),
            (3 * Mathf.PI / 4, 2 * Mathf.PI / 3),
            (3 * Mathf.PI / 4, 4 * Mathf.PI / 3)
        };

        for (int i = 0; i < 9; i++)
        {
            float latitude = sphericalCoords[i].latitude;
            float longitude = sphericalCoords[i].longitude;

            Vector3 direction = new Vector3(
                Mathf.Sin(latitude) * Mathf.Cos(longitude),
                Mathf.Cos(latitude),
                Mathf.Sin(latitude) * Mathf.Sin(longitude)
            );

            cameras[i].transform.position = objectCenter + direction * cameraDistance;
            cameras[i].transform.LookAt(objectCenter);
        }
    }

    private void RemoveCameras()
    {
        if (cameras != null)
        {
            foreach (GameObject camera in cameras)
            {
                DestroyImmediate(camera);
            }
        }

        cameras = null;
    }

    private GameObject CreateCamera(string name)
    {
        GameObject cameraObject = new GameObject(name);
        Camera camera = cameraObject.AddComponent<Camera>();
        return cameraObject;
    }

    private Vector3 GetObjectCenter(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.bounds.size != Vector3.zero)
        {
            return renderer.bounds.center;
        }

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null && collider.bounds.size != Vector3.zero)
        {
            return collider.bounds.center;
        }

        return obj.transform.position;
    }

    private void CaptureScreenshots()
    {
        if (cameras != null && cameras.Length > 0)
        {
            int gridRows = 3;
            int gridCols = 3;
            int totalCameras = gridRows * gridCols;

            int screenshotSize = ScreenshotSize;
            int combinedWidth = screenshotSize * gridCols;
            int combinedHeight = screenshotSize * gridRows;

            combinedScreenshot = new Texture2D(combinedWidth, combinedHeight, TextureFormat.RGB24, false);

            string saveFolder = EditorPrefs.GetString("SaveFolder", "Assets");
            if (!Directory.Exists(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }

            for (int i = 0; i < totalCameras; i++)
            {
                if (i >= cameras.Length)
                {
                    continue;
                }

                Camera camera = cameras[i].GetComponent<Camera>();
                RenderTexture renderTexture = new RenderTexture(screenshotSize, screenshotSize, 24);
                camera.targetTexture = renderTexture;

                if (screenshotType == ScreenshotType.Depth)
                {
                    camera.depthTextureMode = DepthTextureMode.Depth;
                    camera.RenderWithShader(Shader.Find("Custom/DepthMap"), "");
                }
                else
                {
                    camera.Render();
                }

                RenderTexture.active = renderTexture;
                Texture2D screenshot = new Texture2D(screenshotSize, screenshotSize, TextureFormat.RGB24, false);
                screenshot.ReadPixels(new Rect(0, 0, screenshotSize, screenshotSize), 0, 0);
                screenshot.Apply();

                byte[] screenshotBytes = screenshot.EncodeToPNG();
                string screenshotPath = Path.Combine(saveFolder, camera.name + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png");
                File.WriteAllBytes(screenshotPath, screenshotBytes);
                Debug.Log("Saved Screenshot: " + screenshotPath);

                int row = i / gridCols;
                int col = i % gridCols;
                int x = col * screenshotSize;
                int y = (gridRows - row - 1) * screenshotSize;

                combinedScreenshot.SetPixels(x, y, screenshotSize, screenshotSize, screenshot.GetPixels());

                RenderTexture.active = null;
                camera.targetTexture = null;
                GameObject.DestroyImmediate(renderTexture);
                GameObject.DestroyImmediate(screenshot);
            }

            combinedScreenshot.Apply();

            byte[] combinedBytes = combinedScreenshot.EncodeToPNG();
            string combinedPath = Path.Combine(saveFolder, "CombinedScreenshot_" + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png");
            File.WriteAllBytes(combinedPath, combinedBytes);
            Debug.Log("Saved Combined Screenshot: " + combinedPath);
        }
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}