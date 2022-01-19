using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnitAlert;

public static class UpdateChecker
{
    private const string ApiVersionUrl = "https://api.github.com/repos/str1ng3r/unit-alert/releases/latest";
    private static JsonElement? _jsonData;
    private static List<int>? GetAppVersion()
    {
        // Gets the app version from the assembly and checks if it is null.
        var fileVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        if (fileVersion is null)
        {
            return null;
        }
        // App versions in .NET 6 follow the format 0.0.0.0, so we remove the last 2 characters (.0) to match ours.
        fileVersion = fileVersion.Remove(fileVersion.Length - 2);
        // We split the 0.0.0 string on the dots and create a list that contains the integer equivalent of each number.
        return fileVersion.Split('.').Select(int.Parse).ToList();
    }

    private static async Task GetDataFromApiAsync()
    {
        // Starts an HTTP client and adds the UserAgent as request, which is required for the GitHub API
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        // Makes the request and gets the result.
        var response = client.GetAsync(ApiVersionUrl).Result;
        // Checks if the API call fails and exits.
        if (!response.IsSuccessStatusCode)
        {
            return;
        }
        // Reads the content as a string then deserializes it into a JsonElement which is saved in _jsonData.
        var responseString = await response.Content.ReadAsStringAsync();
        _jsonData = JsonSerializer.Deserialize<JsonElement>(responseString);
    }

    private static async Task<List<int>?> GetLatestVersion()
    {
        // Runs GetDataFromApiAsync to have _jsonData set.
        await GetDataFromApiAsync();
        if (_jsonData is null)
        {
            return null;
        }
        // Grabs the tag (version) from the GitHub API (format v0.0.0) and just removes the v at the beginning.
        var versionString = _jsonData.Value.GetProperty("tag_name").ToString();
        versionString = versionString.TrimStart('v');
        // Returns it in the same format as GetAppVersion()
        return  versionString.Split('.').Select(int.Parse).ToList();
    }

    public static string? GetReleaseLink()
    {
        // Gets the html url from the data and returns it, or returns null is none is found.
        return _jsonData?.GetProperty("html_url").ToString();
    }

    public static async Task<bool?> IsUpToDate()
    {
        // Gets the current version and the latest versions and checks if they are null.
        var currentVersion = GetAppVersion();
        var latestVersion = await GetLatestVersion();
        if (currentVersion is null || latestVersion is null)
        {
            return null;
        }
        // Zips through both of the lists and compares each version increment in order (1.1.1 for example).
        // This will create a list of 3 booleans which will all be true if the version is up to date.
        var compareVersions = latestVersion.Zip(currentVersion, (first, second) => first <= second);

        // If all the elements in compareVersions are true then return true, otherwise return false.
        return compareVersions.All(versionEntry => versionEntry);
    }
    
    
}