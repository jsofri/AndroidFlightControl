/*
 * Interface for model class of ex3 AP2.
 * 
 * author: Jonathan Sofri.
 * date: 15/6/20.
 */
using FlightControlAndroid.Util;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlightControlAndroid.Models
{
    public interface IModel
    {
        public Task<int> PostCommand(Command command);
        public Task<ActionResult> GetScreenshot();
        public void SetImageClient(string host, string port);
        public void SetTelnetClient(string host, string port);

    }
}
