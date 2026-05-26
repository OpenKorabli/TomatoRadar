using Newtonsoft.Json.Linq;
using System;

namespace TomatoRadar.Utils
{
    static internal class JsonUtils
    {
        //currently this class is just for throwing custom exception on json parsing error
        public static JObject Parse(string jsonString)
        {
            try
            {
                return JObject.Parse(jsonString);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("JsonStringNotValid", ex);
            }
        }
    }
}
