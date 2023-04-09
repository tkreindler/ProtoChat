using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal interface IRequestHandler
    {
        /// <summary>
        /// Handles a line of input text and does the corresponding action
        /// </summary>
        /// <param name="inputLine">The text input from the console or other</param>
        Task HandleInput(string inputLine);
    }
}
