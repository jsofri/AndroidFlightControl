
/*
 * class for http client that get screenshot from simulator using http.
 * 
 * author: Jonathan Sofri.
 * date: 15/6/20.
 */
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightControlAndroid.Models
{
    public class HttpImageClient
    {
        static string _getImageURL = "/screenshot";
        private string _my_uri;

        /*
         * Ctor.
         */
        public HttpImageClient(string host, string port)
        {
            _my_uri = $"http://{host}:{port}{_getImageURL}";
        }

        /*
         * send a http request to relevant uri and return jpg screenshot.
         */
        public async Task<ActionResult> GetImage()
        {
            ActionResult answer = null;

            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(_my_uri);

                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    answer = new FileContentResult(bytes, "image/jpg");
                }
            }
            catch (Exception) { }

            return answer;
        }


    }
}
