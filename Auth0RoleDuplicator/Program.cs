using System.Net.Http.Headers;
using System.Text;

namespace Auth0RoleDuplicator
{
    internal class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Welcome to the Auth0 Role Duplicator Utility");

            string clientSecret = "";
            while (string.IsNullOrEmpty(clientSecret))
            {
                Console.Write("Enter Auth0 client secret: ");
                clientSecret = Console.ReadLine();
            }

            string domain = "";
            while (string.IsNullOrEmpty(domain))
            {
                Console.Write("Enter Auth0 domain: ");
                domain = Console.ReadLine();
            }

            string clientId = "";
            while (string.IsNullOrEmpty(clientId))
            {
                Console.Write("Enter Auth0 client ID: ");
                clientId = Console.ReadLine();
            }

            string sourceRoleId = "";
            while (string.IsNullOrEmpty(sourceRoleId))
            {
                Console.Write("Enter source role ID: ");
                sourceRoleId = Console.ReadLine();
            }

            string targetRoleName = "";
            while (string.IsNullOrEmpty(targetRoleName))
            {
                Console.Write("Enter target role name: ");
                targetRoleName = Console.ReadLine();
            }

            Console.WriteLine($"Obtaining the access token");
            string accessToken = await GetAccessToken(domain, clientId, clientSecret);

            Console.WriteLine($"Fetching permissions from source role {sourceRoleId}");
            List<Permission> permissions = await GetRolePermissions(domain, accessToken, sourceRoleId);

            Console.WriteLine($"Creating target role {targetRoleName}");
            var targetRoleId = await CreateRoleAndGetId(domain, clientId, clientSecret, targetRoleName);
            Console.WriteLine($"Target role created, id {targetRoleId}");

            Console.WriteLine($"Adding ${permissions.Count} permissions in the target role id {targetRoleId}");
            await AddPermissionsToRole(domain, accessToken, targetRoleId, permissions);

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished, Permissions added successfully.");
            Console.ForegroundColor = originalColor;
        }

        private static async Task<string> GetAccessToken(string domain, string clientId, string clientSecret)
        {
            using var httpClient = new HttpClient();
            var tokenEndpoint = $"https://{domain}/oauth/token";
            var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "audience", $"https://{domain}/api/v2/" },
                { "grant_type", "client_credentials" }
            };

            var tokenResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
            var tokenResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(await tokenResponse.Content.ReadAsStringAsync());

            return tokenResult["access_token"];
        }

        private static async Task<List<Permission>> GetRolePermissions(string domain, string accessToken, string roleId)
        {
            using var httpClient = new HttpClient();
            var roleEndpoint = $"https://{domain}/api/v2/roles/{roleId}/permissions";
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var roleResponse = await httpClient.GetStringAsync(roleEndpoint);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<Permission>>(roleResponse);
        }

        private static async Task AddPermissionsToRole(string domain, string accessToken, string roleId, List<Permission> permissions)
        {
            using var httpClient = new HttpClient();
            var roleEndpoint = $"https://{domain}/api/v2/roles/{roleId}/permissions";
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var roleData = new { permissions };
            string content = Newtonsoft.Json.JsonConvert.SerializeObject(roleData);
            var roleResponse = await httpClient.PostAsync(roleEndpoint, new StringContent(content, Encoding.UTF8, "application/json"));
            roleResponse.EnsureSuccessStatusCode();
        }

        private static async Task<string> CreateRoleAndGetId(string domain, string clientId, string clientSecret, string roleName)
        {
            using var httpClient = new HttpClient();
            var rolesEndpoint = $"https://{domain}/api/v2/roles";
            var roleData = new { name = roleName };
            string content = Newtonsoft.Json.JsonConvert.SerializeObject(roleData);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessToken(domain, clientId, clientSecret));
            var response = await httpClient.PostAsync(rolesEndpoint, new StringContent(content, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var roleResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync());
            return roleResult["id"];
        }
    }
}