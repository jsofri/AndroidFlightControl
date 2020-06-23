/*
 * Controller for posting commands.
 * 
 * author: Jonathan Sofri.
 * date: 15/6/20.
 */
using FlightControlAndroid.Models;
using FlightControlAndroid.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FlightControlAndroid.Controllers
{
    [Route("/api/[controller]")]
    [ApiController]
    public class CommandController : ControllerBase
    {
        private IModel _model;

        /*
         * Ctor.
         */
        public CommandController(IModel model, IConfiguration config)
        {
            var port = config.GetSection("SimulatorTelnetPort").Value;
            var host = config.GetSection("SimulatorTelnetHost").Value;
            _model = model;
            _model.SetTelnetClient(host, port);
        }

        // POST: api/Command
        [HttpPost]
        public async Task<int> Post([FromBody] Command command)
        {
            var answer = await _model.PostCommand(command);

            return (int)answer;
        }
    }
}
