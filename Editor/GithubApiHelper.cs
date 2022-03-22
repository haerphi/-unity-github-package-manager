using System.Net;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using UnityEngine;

[System.Serializable]
public class Release
{
    public string url;
    public string name;
    public string zipball_url;
    public string published_at;
}

public class GithubApiHelper
{
    private string token;
    WebClient webClient = new WebClient();

    public string SetToken(string _token)
    {
        token = _token;
        if (webClient != null)
        {
            webClient = null;
        }
        // TODO try to fetch the user page to verify the token
        return "success";
    }

    void prepareWebClient()
    {
        if (webClient == null)
        {
            webClient = new WebClient();
        }

        if (webClient.Headers.Get("Authorization") == null)
        {
            webClient.Headers.Add(HttpRequestHeader.Authorization, "Token " + token);
        }
        if (webClient.Headers.Get("UserAgent") == null)
        {
            webClient.Headers.Add(HttpRequestHeader.UserAgent, "My app.");
        }
    }

    public List<Release> GetReleases(string organizationName, string projectName)
    {
        prepareWebClient();
        string uri = "https://api.github.com/repos/" + organizationName + "/" + projectName + "/releases";

        string releasesString = null;
        try
        {
            releasesString = webClient.DownloadString(uri);
        }
        catch (WebException e)
        {
            Debug.Log("Ignore this: " + e); ;
        }
        List<Release> releases = new List<Release>();
        if (releasesString != null)
        {
            Release[] _releases = JsonHelper.FromJson<Release>(releasesString);
            releases = _releases.ToList();
        }

        return releases;
    }

    public string DownloadRelease(string uri, string zipPath, string extractPath)
    {
        prepareWebClient();
        // clean the final folder of the package
        try
        {
            Directory.Delete(extractPath, true);
        }
        catch (IOException e)
        {
            Debug.Log("Ignore this: " + e);
        }

        // clean the unzip folder of the package
        try
        {
            Directory.Delete(zipPath, true);
        }
        catch (IOException e)
        {
            Debug.Log("Ignore this: " + e); ;
        }

        // download the zip file
        webClient.DownloadFile(uri, $"{zipPath}.zip");

        // extract it near the zip file
        ZipFile.ExtractToDirectory($"{zipPath}.zip", zipPath);

        // move everything from the folder into the Packages foler
        string[] directiories = Directory.GetDirectories(zipPath);
        foreach (string directory in directiories)
        {
            if (extractPath != zipPath)
            {
                // move folder
                Directory.Move(directory, extractPath);
                // delete folder
                Directory.Delete(zipPath, true);
                try
                {
                    File.Delete($"{zipPath}.meta");
                }
                catch (IOException e)
                {
                    Debug.Log("Ignore this: " + e); ;
                }

            }
        }

        // delete zip file
        File.Delete($"{zipPath}.zip");
        return extractPath;
    }
}