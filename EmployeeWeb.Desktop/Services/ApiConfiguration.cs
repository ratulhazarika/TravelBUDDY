namespace EmployeeWeb.Desktop.Services
{
    /// <summary>
    /// Base API URL - matches React frontend data.jsx
    /// </summary>
    public static class ApiConfiguration
    {
        // export const BASE_API_URL = 'http://localhost:5000/';
        public const string BaseUrl = "http://localhost:5000/";

        public static string ApiUrl(string path) => BaseUrl + path.TrimStart('/');
    }
}
