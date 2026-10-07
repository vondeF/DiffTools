using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sapphire_diffmaker.Entities
{
    public class InvalidLaunchParametersException : Exception
    {
        public string RusMessage { get; set; }
        public InvalidLaunchParametersException(string message, string rusMessage) : base (message) 
        {
            RusMessage = rusMessage;
        }
    }
}
