using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class GithubPackage
{
    public string name;
    public string packageId;
    public string path;
    public List<Release> releases = new List<Release>();
    public string[] releasesString = new string[] { };
    public int releaseIndex = 0;
    public string onRelease = null;
    public bool isInstalled = false;

    public bool isNewPackage = false;

    public GithubPackage(string _name, string _packageId, string _path = null, string _onRelease = null)
    {
        name = _name;
        packageId = _packageId;
        path = _path;
        onRelease = _onRelease;

        if (Directory.Exists(Application.dataPath + $"/../Packages/{name}"))
        {
            isInstalled = true;
        }
    }

    public string Url
    {
        get
        {
            string temp = packageId.Split('@')[1].Replace(".git", "");
            // case when the package is from a ssh url
            if (temp == "git")
            {
                temp = packageId.Split('@')[2].Replace(".git", "");
            }
            return temp;
        }
    }

    public string OrganizationName
    {
        get
        {
            string temp = this.Url.Replace("https://github.com/", "").Split('/')[0];
            // case when the package is from a ssh url
            if (temp.IndexOf(':') > -1)
            {
                temp = temp.Split(':')[1];
            }
            return temp;
        }
    }

    public string PackageName
    {
        get { return this.Url.Replace("https://github.com/", "").Split('/')[1].Replace(".git", ""); }
    }

    public bool CheckReleaseOnGithub(GithubApiHelper gah)
    {
        releases = gah.GetReleases(this.OrganizationName, this.PackageName);
        releases = releases.OrderByDescending(r => r.published_at).ToList(); // décroissant
        Array.Resize(ref releasesString, 0);
        foreach (Release r in releases)
        {
            Array.Resize(ref releasesString, releasesString.Length + 1);

            if (onRelease == r.published_at)
            {
                releaseIndex = releasesString.Length - 1;
            }

            // show if there is new update: ✔ = everything is up to date or ☄️ = can be update
            string releaseName = r.name;
            if (string.IsNullOrEmpty(onRelease)) // do not use GithubPackageManager
            {
                releaseName = $"☄️ | {releaseName}";
            }
            else if (onRelease == r.published_at && releasesString.Length == 1) // is up to date
            {
                releaseName = $"✔ | {releaseName}";
            }
            else if (onRelease == r.published_at) // can be update
            {
                releaseName = $"☄️ | {releaseName}";
            }

            releasesString[releasesString.GetUpperBound(0)] = releaseName;
        }

        if (releases.Count > 0)
        {
            return true;
        }
        return false;
    }

    public void GoToVersion()
    {
        if (releasesString.Length > 0)
        {
            onRelease = releases[releaseIndex].published_at;
        }
    }

    public string GoToVersion(GithubApiHelper gah, bool isSelf = false)
    {
        string assetPath = null;
        if (releasesString.Length > 0)
        {
            string zipPath = Application.dataPath + $"/githubManager/{name}";
            string extractPath = Application.dataPath + $"/../Packages/{name}";
            if (isSelf)
            {
                extractPath = zipPath;
            }
            assetPath = gah.DownloadRelease(releases[releaseIndex].zipball_url, zipPath, extractPath);
            onRelease = releases[releaseIndex].published_at;
        }
        return assetPath;
    }

    override public string ToString()
    {
        string result = "{"
        + "\"packageId\":\"" + packageId + "\","
        + "\"name\":\"" + name + "\","
        + "\"onRelease\":\"" + onRelease + "\"";


        if (isNewPackage)
        {
            result += "," + "\"isNewPackage\":\"" + isNewPackage + "\"";

        }

        result += "}";

        return result;
    }
}

