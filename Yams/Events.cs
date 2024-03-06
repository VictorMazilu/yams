using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YamsEvents
{
    public class Events
    {
        //Events
        public event EventHandler<DiceStoppedEventArgs>? DiceStopped;

        public void OnDiceStopped(int pips)
        {
            // Check if there are any subscribers to the event
            DiceStoppedEventArgs args = new DiceStoppedEventArgs();
            args.Pips = pips;
            DiceStopped?.Invoke(this,args);
        }
    }

    public class DiceStoppedEventArgs : EventArgs
    {
        public int Pips { get; set; }
    }
}
