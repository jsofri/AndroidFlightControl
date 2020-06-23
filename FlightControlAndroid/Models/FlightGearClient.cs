/*
 * class for telnet client that send set requests to flightgear simulator.
 * has inner class and enum to represent commands and status code.
 * 
 * author: Jonathan Sofri.
 * date: 15/6/20.
 */
using FlightControlAndroid.Util;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlightControlAndroid.Models
{
    // Enum to represent status code of REST convention.
    public enum Result
    {
        Ok = 200, InvalidValue = 302, NotModified = 304, InvalidCommand = 400, InternalServerError = 500,
        ExternalServerError = 503
    }

    /*
     * class for handling command in a queue of 2 threads.
     * Based on pavel solution in class 11.
     */
    public class AsyncCommand
    {
        public Command Command { get; private set; }
        public Task<Result> Task { get => Completion.Task; }
        public TaskCompletionSource<Result> Completion { get; private set; }

        public AsyncCommand(Command input)
        {
            Command = input;
            Completion = new TaskCompletionSource<Result>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    public class FlightGearClient
    {
        // Commands to send to FlightGear simulator.
        static readonly string getData = "get /controls/flight/aileron\n"
                                + "get /controls/engines/current-engine/throttle\n"
                                + "get /controls/flight/rudder\nget /controls/flight/elevator\n";
        private readonly byte[] _getDataBytes;
        static readonly string setAileron = "set /controls/flight/aileron ";
        static readonly string setRudder = "set /controls/flight/rudder ";
        static readonly string setThrottle = "set /controls/engines/current-engine/throttle ";
        static readonly string setElevator = "set /controls/flight/elevator ";
        static readonly string initString = "data\n";
        static readonly double epsilon = 0.0005;

        // Data members to address simulator and handle command queue.
        private readonly BlockingCollection<AsyncCommand> _queue;
        private readonly TcpClient _client;
        private readonly int _port;
        private readonly IPAddress _host;
        private bool _connected = false;


        /*
         * Ctor.
         */
        public FlightGearClient(string host, string port)
        {
            _queue = new BlockingCollection<AsyncCommand>();
            _client = new TcpClient();
            _getDataBytes = Encoding.ASCII.GetBytes(getData);
            _port = Int32.Parse(port);
            _host = IPAddress.Parse(host);
            start();
        }

        /*
         * Called by the WebApi Controller, await on the returned Task<>.
         */
        public Task<Result> Execute(Command cmd)
        {
            // if TcpClient is connected to a server.
            if (_connected)
            {
                var asyncCommand = new AsyncCommand(cmd);
                _queue.Add(asyncCommand);
                return asyncCommand.Task;
            }
            else
            {
                return GetTaskErrorCode();
            }
        }

        /*
         * Start new thread of loop for sending requests to simulator.
         */
        public void start()
        {
            Task.Factory.StartNew(ProcessCommands);
        }

        /*
         * Initialize connection to telnet client.
         * In a loop, send set request and verify modification in simulator.
         */
        public void ProcessCommands()
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(initString);
            byte[] recvBuffer = new byte[1024];
            Result res;
            int nRead;

            TryConnectionLoop();
            NetworkStream stream = _client.GetStream();
            stream.Write(sendBuffer, 0, sendBuffer.Length);

            foreach (AsyncCommand command in _queue.GetConsumingEnumerable())
            {
                try
                {
                    sendBuffer = ConvertCommandToByteArray(command);
                    stream.Write(sendBuffer, 0, sendBuffer.Length);

                    // Deliberately wait for 1 MS to allow simulator process command.
                    Thread.Sleep(1);
                    stream.Write(_getDataBytes, 0, _getDataBytes.Length);
                    nRead = stream.Read(recvBuffer, 0, recvBuffer.Length);
                    res = bytesAnswerToResult(recvBuffer, nRead, command);
                    command.Completion.SetResult(res);
                }
                catch (Exception)
                {
                    command.Completion.SetResult(Result.ExternalServerError);
                }

            }
        }

        /*
         * return a Task<Result> with the value of external server error status code.
         */
        private async Task<Result> GetTaskErrorCode()
        {
            return await Task.FromResult(Result.ExternalServerError);
        }

        /*
         * Try connecting to TCP server. If successed, change data member _connected to true.
         */
        private void TryConnectionLoop()
        {
            while (!_connected)
            {
                try
                {
                    _client.Connect(_host, _port);
                    _connected = true;
                }
                catch (Exception)
                { }
            }
        }

        /*
         * Take a Command object with data and make appropriate set request.
         */
        private byte[] ConvertCommandToByteArray(AsyncCommand command)
        {
            Command cmd = command.Command;
            byte[] arr = new byte[1024];
            string stringCommand;
            stringCommand = $"{setAileron}{cmd.Aileron}\n";
            stringCommand += $"{setThrottle}{cmd.Throttle}\n";
            stringCommand += $"{setRudder}{cmd.Rudder}\n";
            stringCommand += $"{setElevator}{cmd.Elevator}\n";

            return Encoding.ASCII.GetBytes(stringCommand);
        }

        /*
         * Convert bytes to a result object (status code enum).
         */
        private Result bytesAnswerToResult(byte[] recvBuffer, int nRead, AsyncCommand command)
        {
            var result = Result.ExternalServerError;
            string fromServer = Encoding.ASCII.GetString(recvBuffer);
            int i = fromServer.IndexOf('\0');
            fromServer = (i >= 0) ? fromServer.Substring(0, i) : fromServer;
            string[] tokens = fromServer.Split('\n', StringSplitOptions.None);

            if (tokens.Length >= 4)
            {
                result = GetResult(tokens, command.Command);
            }
            return result;
        }

        /*
         * Get an array of tokens, each is Command origin property value.
         * Check if server has modified correctly the values and return answer.
         */
        private Result GetResult(string[] tokens, Command origin)
        {
            Result answer = Result.Ok;

            try
            {
                if (!VerifyAileron(double.Parse(tokens[0]), origin.Aileron))
                {
                    answer = GetErrorResult(double.Parse(tokens[0]), origin.Aileron);
                }
                else if (!VerifyParamPlusMinusOne(double.Parse(tokens[1]), origin.Throttle))
                {
                    answer = GetErrorResult(double.Parse(tokens[1]), origin.Throttle);
                }
                else if (!VerifyParamPlusMinusOne(double.Parse(tokens[2]), origin.Rudder))
                {
                    answer = GetErrorResult(double.Parse(tokens[2]), origin.Rudder);
                }
                else if (!VerifyParamPlusMinusOne(double.Parse(tokens[3]), origin.Elevator))
                {
                    answer = GetErrorResult(double.Parse(tokens[3]), origin.Elevator);
                }
            }
            catch (Exception)
            {
                answer = Result.InvalidValue;
            }

            return answer;
        }

        /*
         * Is called when it's known that an error occured.
         * Method return the specific reason.
         */
        private Result GetErrorResult(double fromServer, double fromClient)
        {
            Result answer = Result.InternalServerError;
            double diff = fromServer - fromClient;

            if (diff > epsilon || diff < -epsilon)
            {
                answer = Result.NotModified;
            }

            return answer;
        }

        // verify similarity and value between range [0.0, 1.0].
        private bool VerifyAileron(double fromServer, double fromClient)
        {
            return VerifySimilarityAndRange(fromServer, fromClient, 0.0);
        }

        // verify similarity and value between range [-1.0, 1.0].
        private bool VerifyParamPlusMinusOne(double fromServer,
                                             double fromClient)
        {
            return VerifySimilarityAndRange(fromServer, fromClient, -1.0);
        }

        private bool VerifySimilarityAndRange(double fromServer,
                                              double fromClient, double min)
        {
            bool result;
            double diff = fromServer - fromClient;

            if (fromServer < min || fromServer > 1)
            {
                result = false;
            }
            else if (fromServer == 1)
            {
                result = fromServer <= fromClient;
            }
            else if (fromServer == min)
            {
                result = fromServer >= fromClient;
            }
            else
            {
                result = (diff < epsilon && diff > -epsilon && fromClient > min && fromClient <= 1);
            }

            return result;
        }
    }
}
