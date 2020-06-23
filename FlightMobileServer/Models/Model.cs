/*
 * Concrete class of modal for excercise.
 * 
 * author: Jonathan Sofri.
 * date: 15/6/20.
 */
using FlightControlAndroid.Util;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlightControlAndroid.Models
{
    public class Model : IModel
    {
        // different client for each request.
        private FlightGearClient _simulatorClient = null;
        private HttpImageClient _imageClient = null;

        /*
         * Ctor.
         */
        public Model() { }

        /*
         * Get a command object, use FlightGearClient to set data.
         * return status code (int).
         */
        public async Task<int> PostCommand(Command command)
        {
            Result res;

            if (_simulatorClient != null)
            {
                res = await _simulatorClient.Execute(command);
            }
            else
            {
                res = Result.ExternalServerError;
            }

            return (int)res;
        }

        /*
         * Use HttpClient to get image and return data.
         */
        public async Task<ActionResult> GetScreenshot()
        {
            ActionResult actionResult = null;

            if (_imageClient != null)
            {
                actionResult = await _imageClient.GetImage();
            }

            return actionResult;
        }

        public void SetImageClient(string host, string port)
        {
            if (_imageClient == null)
                _imageClient = new HttpImageClient(host, port);
        }

        public void SetTelnetClient(string host, string port)
        {
            if (_simulatorClient == null)
                _simulatorClient = new FlightGearClient(host, port);
        }
    }
}
