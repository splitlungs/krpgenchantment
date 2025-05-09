using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class ModConfigDictionary : Dictionary<string, object>
    {
        public int ValueToInt(string key)
        {
            return (int)this[key];
        }
    }
}
