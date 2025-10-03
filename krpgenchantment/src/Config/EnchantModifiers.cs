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
        /// Returns an int32 value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int GetInt(string key)
        {
            return Convert.ToInt32(this[key].ToString());
        }
        /// <summary>
        /// Returns a float value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public float GetFloat(string key)
        {
            return float.Parse(this[key].ToString());
        }
        /// <summary>
        /// Returns a double value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double GetDouble(string key) 
        {
            return double.Parse(this[key].ToString());
        }
        /// <summary>
        /// Returns a long value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long GetLong(string key)
        {
            return long.Parse(this[key].ToString());
        }
        /// <summary>
        /// Returns a string value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            return this[key].ToString();
        }
        /// <summary>
        /// Returns a bool value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool GetBool(string key)
        {
            return this[key].ToString().ToLower().Contains("true");
        }
    }
}
