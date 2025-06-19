using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    public class EnchantModifiers : Dictionary<string, object>
    {
        public int GetInt(string key)
        {
            return Convert.ToInt32(this[key].ToString());
        }
        public float GetFloat(string key)
        {
            return float.Parse(this[key].ToString());
        }
        public double GetDouble(string key) 
        {
            return double.Parse(this[key].ToString());
        }
        public long GetLong(string key)
        {
            return long.Parse(this[key].ToString());
        }
        public string GetString(string key)
        {
            return this[key].ToString();
        }
    }
}
