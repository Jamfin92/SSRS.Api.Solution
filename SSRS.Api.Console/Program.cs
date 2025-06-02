using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Net;

// Using Windows Authentication (default credentials)
var client = new SSRSRestClient("http://your-ssrs-server");

// OR using specific credentials
// var client = new SSRSRestClient("http://your-ssrs-server", "domain", "username", "password");

try
{
    // Get all reports
    var reportsJson = await client.GetReportsAsync();
    var reports = JsonConvert.DeserializeObject<SSRSResponse<ReportItem>>(reportsJson);

    Console.WriteLine($"Found {reports.Value.Length} reports:");
    foreach (var report in reports.Value)
    {
        Console.WriteLine($"- {report.Name} (ID: {report.Id})");
    }

    // Get reports with filter
    var filteredReports = await client.GetReportsWithFilterAsync("contains(Name,'Sales')");
    Console.WriteLine("Filtered reports: " + filteredReports);

    // Create a folder
    var newFolder = await client.CreateFolderAsync("Test Folder");
    Console.WriteLine("Created folder: " + newFolder);

}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    client.Dispose();
}
public class SSRSRestClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public SSRSRestClient(string serverUrl, string domain = null, string username = null, string password = null)
    {
        _baseUrl = $"{serverUrl}/Reports/api/v2.0";

        // Create HttpClient with proper authentication
        var handler = new HttpClientHandler()
        {
            UseDefaultCredentials = string.IsNullOrEmpty(username)
        };

        // If credentials provided, use them instead of default credentials
        if (!string.IsNullOrEmpty(username))
        {
            handler.UseDefaultCredentials = false;
            handler.Credentials = new NetworkCredential(username, password, domain);
        }

        _httpClient = new HttpClient(handler);

        // Set common headers
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    // GET request - Retrieve reports
    public async Task<string> GetReportsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/Reports");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error retrieving reports: {ex.Message}", ex);
        }
    }

    // GET request - Retrieve folders
    public async Task<string> GetFoldersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/Folders");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error retrieving folders: {ex.Message}", ex);
        }
    }

    // GET request - Retrieve specific report by ID
    public async Task<string> GetReportByIdAsync(Guid reportId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/Reports({reportId})");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error retrieving report {reportId}: {ex.Message}", ex);
        }
    }

    // GET request with OData query parameters
    public async Task<string> GetReportsWithFilterAsync(string filter)
    {
        try
        {
            var encodedFilter = Uri.EscapeDataString(filter);
            var response = await _httpClient.GetAsync($"{_baseUrl}/Reports?$filter={encodedFilter}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error retrieving filtered reports: {ex.Message}", ex);
        }
    }

    // POST request - Create a folder
    public async Task<string> CreateFolderAsync(string folderName, string parentPath = "/")
    {
        try
        {
            var folderData = new
            {
                Name = folderName,
                Path = parentPath,
                Type = "Folder"
            };

            var jsonContent = JsonConvert.SerializeObject(folderData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/Folders", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error creating folder: {ex.Message}", ex);
        }
    }

    // PUT request - Update a report
    public async Task<string> UpdateReportAsync(Guid reportId, object updateData)
    {
        try
        {
            var jsonContent = JsonConvert.SerializeObject(updateData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"{_baseUrl}/Reports({reportId})", content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error updating report: {ex.Message}", ex);
        }
    }

    // DELETE request - Delete a folder
    public async Task<bool> DeleteFolderAsync(Guid folderId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/Folders({folderId})");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error deleting folder: {ex.Message}", ex);
        }
    }

    // Session-based authentication for custom security
    public async Task<bool> CreateSessionAsync(string username, string password)
    {
        try
        {
            var sessionData = new
            {
                UserName = username,
                Password = password
            };

            var jsonContent = JsonConvert.SerializeObject(sessionData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/Session", content);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Error creating session: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Data models for strongly-typed responses
public class ReportItem
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Path { get; set; }
    public string Type { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
}

public class SSRSResponse<T>
{
    [JsonProperty("@odata.context")]
    public string Context { get; set; }

    public T[] Value { get; set; }
}