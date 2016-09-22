using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DreamforceIOTCloudApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading;
using System.Net;
using System.IO;
using System.Dynamic;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace DreamforceIOTCloudApp.Controllers
{
    public class HRMonitorController : Controller
    {
        public static string mDeviceID = "000909D";

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> SendData(int heartRate)
        {
            try
            {
                using (var httpClient1 = new HttpClient())
                {
                    HeartRateMessage hmMessage = null;
                    dynamic jsonData = new ExpandoObject();
                    jsonData.n = 1;
                    jsonData.timeout = 60000;
                    jsonData.wait = 0;
                    jsonData.delete = false;
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json"),
                        Method = HttpMethod.Post,
                        RequestUri = new Uri("https://mq-aws-eu-west-1-1.iron.io/3/projects/57dbdea41e0aa6000858dbae/queues/messages/reservations?oauth=pqYbPAL1MN3mf5PyBu2S")
                    };
                    //reserve message in iron message queue for 1 minute
                    var response = await httpClient1.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        IList<HeartRateMessage> messagesLst = null;
                        var ironResponse = await response.Content.ReadAsStringAsync();
                        var ironMessage = JsonConvert.DeserializeObject<IDictionary<string, object>>(ironResponse);
                        if (ironMessage.Count > 0 && ironMessage.ContainsKey("messages"))
                            messagesLst = JsonConvert.DeserializeObject<IList<HeartRateMessage>>(ironMessage["messages"].ToString());

                        if (messagesLst != null && messagesLst.LastOrDefault() != null)
                            hmMessage = messagesLst.LastOrDefault();

                        Parallel.Invoke(() =>
                        {
                            if (hmMessage != null)
                            {
                                //delete iron queue message after reading
                                DeleteIronMessageByID(hmMessage.id, hmMessage.reservation_id);
                            }
                        }, async () =>
                        {
                            if (hmMessage == null || (hmMessage != null && !string.IsNullOrEmpty(hmMessage.body) && !hmMessage.body.ToUpper().Contains("DONE")))
                            {
                                HeartRateMonitor hrMonitor = new HeartRateMonitor();
                                hrMonitor.deviceID = mDeviceID;
                                hrMonitor.heartRate = heartRate;
                                using (var httpClient2 = new HttpClient())
                                {
                                    httpClient2.DefaultRequestHeaders.Accept.Clear();
                                    httpClient2.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    httpClient2.DefaultRequestHeaders.Add("Authorization", "Bearer iguZgpnIwr8N60cD7cYgSMbYQm9QZXEY9AaqmeD6f4d2DoyvZNEhcQdzGSSFivDylWcXR5ShTu1AfMSCJi9sAj");
                                    request = new HttpRequestMessage
                                    {
                                        Content = new StringContent(JsonConvert.SerializeObject(hrMonitor), Encoding.UTF8, "application/json"),
                                        Method = HttpMethod.Post,
                                        RequestUri = new Uri("https://ingestion-xcdvudaz0dz3.us3.sfdcnow.com/streams/heart_rate_monito001/heart_rate_monito001/event")
                                    };
                                    response = await httpClient2.SendAsync(request);
                                }
                            }
                        });
                    }

                    if (hmMessage == null)
                        return Json(null);
                    else
                        return Json(hmMessage.body);
                }
            }
            catch (Exception ex)
            {
                return Json(null);
            }
        }

        private async void DeleteIronMessageByID(string pMessageID, string pReservationID)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    dynamic jsonData = new ExpandoObject();
                    jsonData.reservation_id = pReservationID;
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(jsonData), Encoding.UTF8, "application/json"),
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri("https://mq-aws-eu-west-1-1.iron.io/3/projects/57dbdea41e0aa6000858dbae/queues/messages/messages/" + pMessageID + "?oauth=pqYbPAL1MN3mf5PyBu2S")
                    };
                    var response = await httpClient.SendAsync(request);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
