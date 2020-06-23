/*
 * Controller for getting screenshot.
 * 
 * author: Jonathan Sofri.
 * date: 15/6/20.
 */
using FlightControlAndroid.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FlightControlAndroid.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class ScreenshotController : ControllerBase
    {
        private IModel _model;
        private HttpImageClient _image_client;

        /*
         * Ctor.
         */
        public ScreenshotController(IModel model, IConfiguration config)
        {
            var port = config.GetSection("ImageHttpPort").Value;
            var host = config.GetSection("ImageHttpHost").Value;
            _model = model;
            _model.SetImageClient(host, port);
        }

        // GET: /Screenshot
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var image = await _model.GetScreenshot();

            return image;
        }
    }
}
