using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using UnityEngine;

using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

[System.Serializable]
public class PreGithubPackage
{
    public string packageId;
    public string name;
    public string onRelease;
    public string path;
}

public class GithubPackageManager : EditorWindow
{
    string SELFT_NAME = "com.haerphi.com.haerphi.githubpackagemanager";
    private string gitTokenField;
    private string gitToken;
    private string packageUrl = "https://";
    private string packageListStatus = "Fetch first please";
    private List<GithubPackage> githubDependencies = new List<GithubPackage>();
    private GithubApiHelper gah = new GithubApiHelper();
    // Help to know if the editor has reloaded
    private int gahHash;
    private Request packageListResquest;
    private Request packageRemoveResquest;

    #region Gui params variables
    private Vector2 scrollPos;
    #endregion

    // Add menu item named "My Window" to the Window menu
    [MenuItem("Window/Github/Manage my packages")]

    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(GithubPackageManager));
    }

    void Reset()
    {
        UnityEngine.Debug.Log("RESET!");
        EventSubscribingExample_RegisteringPackages();
        packageUrl = "https://";
        packageListStatus = "Fetch first please";
        githubDependencies = new List<GithubPackage>();
        gahHash = gah.GetHashCode();
    }
    void OnGUI()
    {
        scrollPos =
            EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(0), GUILayout.Width(0));

        if (GUILayout.Button("Refresh"))
        {
            this.Reset();
        }
        EditorGUILayout.Space();

        GUILayout.Label("Git token", EditorStyles.boldLabel);
        gitTokenField = EditorGUILayout.TextField("Git token", gitTokenField);
        if (GUILayout.Button("Save gitToken"))
        {
            this.SaveGitToken();
        }
        EditorGUILayout.Space();

        // TODO Add package function
        // GUILayout.Label("Add new package", EditorStyles.boldLabel);
        // packageUrl = EditorGUILayout.TextField("Git url", packageUrl);
        // if (GUILayout.Button("Add package from url"))
        // {
        //     this.AddPackageFromUrl();
        // }
        // EditorGUILayout.Space();

        if (GUILayout.Button("Check packages and updates"))
        {
            this.GetListOfPackages();
        }

        EditorGUILayout.Space();

        GUILayout.Label("Packages list", EditorStyles.boldLabel);
        // Check releasesString
        if (githubDependencies.Count > 0)
        {
            foreach (GithubPackage gp in githubDependencies)
            {
                if (gp.name != SELFT_NAME)
                {
                    EditorGUILayout.LabelField(gp.name + ":");
                    if (gp.releases.Count > 0)
                    {
                        gp.releaseIndex = EditorGUILayout.Popup(gp.releaseIndex, gp.releasesString);
                        if (GUILayout.Button("Go to selected release"))
                        {
                            GoToVersion(gp);
                        }
                    }
                    else
                    {
                        gp.releaseIndex = EditorGUILayout.Popup(gp.releaseIndex, new String[] { "No release found  ¯\\_(ツ)_/¯" });
                    }
                }
            }
        }
        else
        {
            // automatic fetch if it is the first time
            if (packageListStatus == "Fetch first please")
            {
                gah.SetToken(gitToken);
                this.GetListOfPackages();
            }
            EditorGUILayout.LabelField(packageListStatus);
        }
        EditorGUILayout.EndScrollView();

        // Did the editor reload ?
        if (gahHash != gah.GetHashCode())
        {
            // if it has reloaded then we reset all the variables
            this.Reset();
        }
    }

    void SaveGitToken()
    {
        string result = gah.SetToken(gitTokenField);
        if (result == "success")
        {
            gitToken = gitTokenField;
            gitTokenField = "";
            GUI.FocusControl(null);
        }
        // TODO manage error later
    }

    void AddPackageFromUrl()
    {
        UnityEngine.Debug.Log("ADD PACKAGE FORM URL");
        // "clone" the package in Packages folder
    }

    void GoToVersion(GithubPackage gp)
    {
        // check if it is not is self
        if (string.IsNullOrEmpty(gp.onRelease) && gp.name == SELFT_NAME)
        {
            UnityEngine.Debug.Log("CANNOT SELF UPDATE");
            return;
        }
        // if we have onRelease it means that it is in the GithubPackageManger
        else if (!string.IsNullOrEmpty(gp.onRelease))
        {
            string assetPath = gp.GoToVersion(gah);
            // save package in json file
            SaveGithubDepenciesToJson();
        }
        else
        {
            gp.GoToVersion();
            // save package in json file
            SaveGithubDepenciesToJson();

            // remove packages from package manager
            removePackages.Enqueue(gp.name);

            RemovePackages();
        }
    }

    #region Get package's List
    void GetListOfPackages()
    {
        githubDependencies = new List<GithubPackage>();
        // List packages installed for the project
        packageListStatus = "Loading...";



        // read the githubManager/Packages.json file
        if (AssetDatabase.IsValidFolder("Assets/githubManager"))
        {
            string path = Application.dataPath + "/githubManager/Packages.json";
            // read the text from directly from the Packages.json file
            StreamReader reader = new StreamReader(path);
            string packageJson = reader.ReadToEnd();
            reader.Close();

            PreGithubPackage[] prePackageObjs = JsonHelper.FromJson<PreGithubPackage>(packageJson);
            foreach (PreGithubPackage prePackage in prePackageObjs)
            {
                AddDependencieToArray(prePackage.name, prePackage.packageId, prePackage.path, prePackage.onRelease);
            }
        }

        // check package manager
        packageListResquest = Client.List();
        EditorApplication.update += GetListOfPackagesProgress;
    }
    void GetListOfPackagesProgress()
    {
        if (packageListResquest.IsCompleted)
        {
            if (packageListResquest.Status == StatusCode.Success)
            {
                CheckExistingGitDepencies((packageListResquest as Request<PackageCollection>).Result);
            }
            else if (packageListResquest.Status >= StatusCode.Failure)
            {
                UnityEngine.Debug.Log(packageListResquest.Error.message);
            }

            EditorApplication.update -= GetListOfPackagesProgress;
        }
    }
    #endregion

    void CheckExistingGitDepencies(PackageCollection packages)
    {
        foreach (UnityEditor.PackageManager.PackageInfo package in packages)
        {
            if (package.git != null)
            {
                AddDependencieToArray(package.name, package.packageId, package.resolvedPath);
            }
        }

        // save package in json file
        SaveGithubDepenciesToJson();

        if (githubDependencies.Count == 0)
        {
            packageListStatus = "No package found :(";
        }
    }

    bool AddDependencieToArray(string name, string packageId, string resolvedPath = null, string onRelease = null)
    {
        bool haveRelease = false;
        bool found = false;
        foreach (GithubPackage gp in githubDependencies)
        {
            if (gp.packageId == packageId)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            githubDependencies.Add(new GithubPackage(name, packageId, resolvedPath, onRelease));
            haveRelease = githubDependencies[githubDependencies.Count - 1].CheckReleaseOnGithub(gah);

            // install the package from the release if it is not already installed
            if (!githubDependencies[githubDependencies.Count - 1].isInstalled)
            {
                GoToVersion(githubDependencies[githubDependencies.Count - 1]);
            }
        }
        return haveRelease;
    }

    void SaveGithubDepenciesToJson()
    {
        bool needFolder = AssetDatabase.IsValidFolder("Assets/githubManager");
        if (!needFolder)
        {
            AssetDatabase.CreateFolder("Assets", "githubManager");
        }

        using (StreamWriter outputFile = new StreamWriter(Path.Combine(Application.dataPath + "/githubManager", "Packages.json")))
        {
            List<String> el = new List<string>();
            foreach (GithubPackage gp in githubDependencies)
            {
                if (gp.onRelease != null)
                {
                    el.Add(gp.ToString());
                }
            }

            outputFile.WriteLine("[");
            for (int i = 0; i < el.Count; i++)
            {
                outputFile.WriteLine(el[i]);
                if (i + 1 < el.Count)
                {
                    outputFile.WriteLine(",");
                }
            }
            outputFile.WriteLine("]");
        }
    }

    #region Remove package from list
    Queue<string> removePackages = new Queue<string>();
    void RemovePackages()
    {
        if ((packageRemoveResquest == null || packageRemoveResquest.IsCompleted) && removePackages.Count > 0)
        {
            packageRemoveResquest = Client.Remove(removePackages.Dequeue());
            EditorApplication.update += RemovePackagesProgress;
        }
    }
    void RemovePackagesProgress()
    {
        if (packageRemoveResquest.IsCompleted)
        {
            if (packageRemoveResquest.Status == StatusCode.Success)
            {
                if (removePackages.Count > 0)
                {
                    packageRemoveResquest = Client.Remove(removePackages.Dequeue());
                }
                else
                {
                    EditorApplication.update -= RemovePackagesProgress;
                }
            }
            else if (packageRemoveResquest.Status >= StatusCode.Failure)
            {
                UnityEngine.Debug.Log(packageRemoveResquest.Error.message);
            }
            EditorApplication.update -= RemovePackagesProgress;
        }
    }
    #endregion

    string GetCurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = null)
    {
        return fileName;
    }

    #region Trigger on events from the Package Manager
    /**
        Source: https://docs.unity3d.com/Manual/upm-api.html
    */
    public void EventSubscribingExample_RegisteringPackages()
    {
        Events.registeringPackages += RegisteringPackagesEventHandler;
    }

    void RegisteringPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
    {
        UnityEngine.Debug.Log("RegisteringPackagesEventHandler");
        foreach (var removedPackage in packageRegistrationEventArgs.removed)
        {
            UnityEngine.Debug.Log($"Removing {removedPackage.name}");
            int removeIndex = -1;
            for (int i = 0; i < githubDependencies.Count; i++)
            {
                UnityEngine.Debug.Log(removedPackage.name + " == " + githubDependencies[i].name);
                if (removedPackage.name == githubDependencies[i].name)
                {
                    removeIndex = i;
                    break;
                }
            }
            if (removeIndex > -1)
            {
                githubDependencies.RemoveAt(removeIndex);
            }
        }
        UnityEngine.Debug.Log("Cleaned list");
        foreach (GithubPackage gp in githubDependencies)
        {
            UnityEngine.Debug.Log(gp.name);
        }
        SaveGithubDepenciesToJson();
    }
    #endregion
}