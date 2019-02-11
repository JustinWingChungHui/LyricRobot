
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace GetSongsFunction
{
    public static class GetSongs
    {
        [FunctionName("GetSongs")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            try
            {
                log.Info("Getting song ids..");
                var httpClient = new HttpClient();

                var response = await httpClient.GetStringAsync("https://musicdemons.com/api/v1/song");

                var json = JArray.Parse(response);
                var ids = json.Select(j => long.Parse(j["id"].ToString()));

                return new OkObjectResult($"Done");
                    
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}