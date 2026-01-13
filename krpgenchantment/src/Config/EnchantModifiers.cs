using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KRPGLib.Enchantment
{
    /// <summary>
    /// Dictionary of objects with string keys, used to serialize JSON object strings to/from Enchantments. Be sure to use the typed Get methods, instead of getting directly from object.
    /// </summary>
    public class EnchantModifiers : Dictionary<string, object>
    {
        /// <summary>
        /// Returns an int32 value or 0 if a value cannot be found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetInt(string key)
        {
            if (!this.ContainsKey(key)) return 0;
            return Convert.ToInt32(this[key].ToString());
        }
        /// <summary>
        /// Returns a float value or 0 if a value cannot be found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public float GetFloat(string key)
        {
            if (!this.ContainsKey(key)) return 0;
            return float.Parse(this[key].ToString());
        }
        /// <summary>
        /// Returns a double value or 0 if a value cannot be found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double GetDouble(string key) 
        {
            if (!this.ContainsKey(key)) return 0;
            return double.Parse(this[key].ToString());
        }
        /// <summary>
        /// Returns a long value or 0 if a value cannot be found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetLong(string key)
        {
            if (!this.ContainsKey(key)) return 0;
            return long.Parse(this[key].ToString());
        }
        /// <summary>
        /// Returns a string value or null if a value cannot be found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            if (!this.ContainsKey(key)) return null;
            return this[key].ToString();
        }
        /// <summary>
        /// Returns a bool value or false if a value cannot be found.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool GetBool(string key)
        {
            if (!this.ContainsKey(key)) return false;
            return this[key].ToString().ToLower().Equals("true");
        }
    }
}
