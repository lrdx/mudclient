using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Adan.Client.Common.Messages;
using CSLib.Net.Diagnostics;

namespace Adan.Client.Plugins.Timers
{
    public class TimerMessage : Message
    {

        public TimerMessage(int number)
        {
            Number = number;
        }

        public int Number
        {
            get;
        }

        public override int MessageType
        {
            get
            {
                return Constants.TimerMessage;
            }
        }
    }
}
