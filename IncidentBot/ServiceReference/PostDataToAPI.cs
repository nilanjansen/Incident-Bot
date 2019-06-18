using IncidentBot.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IncidentBot.ServiceReference
{
    public static class PostDataToAPI
    {
        public static void AddIncident(Incident incident)
        {
           
            using (var httpClient = new HttpClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(incident), Encoding.UTF8, "application/json");

                using (var response = httpClient.PostAsync("https://reportincidentapi.azurewebsites.net/api/incident", content))
                {
                    string apiResponse = response.ToString();
                   
                }
            }
        }
    }
    
}
