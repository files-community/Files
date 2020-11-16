using Files.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Files.Helpers
{
    public static class RecycleBinHelpers
    {
        public static async Task<List<ShellFileItem>> EnumerateRecycleBin(AppServiceConnection connection)
        {
            if (connection != null)
            {
                ValueSet value = new ValueSet
                {
                    { "Arguments", "RecycleBin" },
                    { "action", "Enumerate" }
                };
                AppServiceResponse response = await connection.SendMessageAsync(value);

                if (response.Status == AppServiceResponseStatus.Success
                    && response.Message.ContainsKey("Enumerate"))
                {
                    List<ShellFileItem> items = JsonConvert.DeserializeObject<List<ShellFileItem>>((string)response.Message["Enumerate"]);
                    return items;
                }
            }

            return null;
        }
    }
}
