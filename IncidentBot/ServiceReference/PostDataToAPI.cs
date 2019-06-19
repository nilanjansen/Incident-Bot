using IncidentBot.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace IncidentBot.ServiceReference
{
    public static class PostDataToAPI
    {
        public static HttpResponseMessage AddIncident(Incident incident)
        {
            var httpClient = new HttpClient();
            var content = JsonConvert.SerializeObject(incident);
            var buffer =  Encoding.UTF8.GetBytes(content);
            var byteContent = new ByteArrayContent(buffer);

            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var result = httpClient.PostAsync("https://reportincidentapi.azurewebsites.net/api/Incident", byteContent).Result;

            
            return result;
        }
    }
}

