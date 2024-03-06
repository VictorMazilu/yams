using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YamsForm
{
    static class Settings
    {
        public static int currentPlayer { get; set; } = 0;
        public static int noOfPlayers { get; set; }
        public static Dictionary<int,string>? PlayerNames { get; set; }

        public static void InitializePlayerNames() { 
            PlayerNames = new Dictionary<int,string>();
            for (int i = 0; i < PlayerNames.Count; i++) {
                PlayerNames.Add(i, "");
            }
        }
    }
}
