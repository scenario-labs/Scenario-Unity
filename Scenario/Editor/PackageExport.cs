using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

/// <summary>
/// Displays a Unity Editor window where you can select the package to export.
/// </summary>
public class PackageExport : EditorWindow
{
    [SerializeField] private static string _sourceFolder = string.Empty;
    [SerializeField] private string _info = string.Empty;
    private PackageInfo _selection;
    private PackRequest _packRequest;
    private string _destinationFolder = string.Empty;
    private bool _isWorking = false;

    [MenuItem("Tools/Export Package", false, -100)]
    private static void MenuItem_Export()
    {
        GetWindow<PackageExport>(false, "Package Export", true);
        
        _sourceFolder = EditorUtility.OpenFolderPanel
        (
            "Select folder of the package",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            string.Empty
        );
    }

    private void Awake()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDestroy()
    {
        _isWorking = false;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Package path");

        EditorGUILayout.TextField(_sourceFolder);

        if (GUILayout.Button("Export", GUILayout.Height(30.0f)))
        {
            if (!_isWorking)
            {
                _destinationFolder = EditorUtility.OpenFolderPanel
                (
                    "Select where to export the package",
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    string.Empty
                );

                _isWorking = true;
                _info = "Exporting package...";

                Export();
            }
        }

        EditorGUILayout.LabelField(_info);
    }

    private void Export()
    {
        if (string.IsNullOrWhiteSpace(_sourceFolder))
        {
            _info = "Package path is empty";
            return;
        }

        if (string.IsNullOrWhiteSpace(_destinationFolder))
        {
            _info = "Destination folder is empty";
            return;
        }

        if (!Directory.Exists(_sourceFolder))
        {
            _info = "Package folder does not exist";
            return;
        }

        if (!Directory.Exists(_destinationFolder))
        {
            _info = "Destination folder does not exist";
            return;
        }

        _packRequest = Client.Pack(_sourceFolder, _destinationFolder);
    }

    private void OnEditorUpdate()
    {
        PackPackage();
    }

    private async void PackPackage()
    {
        if (_packRequest != null && _packRequest.IsCompleted)
        {
            if (_packRequest.Status == StatusCode.Success)
            {
                _info = "Package exported!";

                await Task.Delay(300);

                System.Diagnostics.Process.Start(_destinationFolder);

                _isWorking = false;
            }
            else
            {
                _info = "There was an error exporting the package.";
            }

            _packRequest = null;
        }
    }
}