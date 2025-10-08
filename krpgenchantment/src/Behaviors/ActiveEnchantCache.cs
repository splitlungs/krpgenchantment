using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class ActiveEnchantCache
    {
        public Dictionary<string, int> Enchantments = new Dictionary<string, int>();
        public long LastCheckTime = 0;
        public int ItemId = 0;
    }
}
