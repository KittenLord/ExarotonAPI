using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Exaroton
{
    internal class JsonConverterObjectToString : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // lmfao
            JToken token = JToken.Load(reader);
            return token.ToString();
            // if (token.Type == JTokenType.Object || token.Type == JTokenType.Array || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            // {
            //     return token.ToString();
            // }
            // return null;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // //serializer.Serialize(writer, value);

            // //serialize as actual JSON and not string data
            // var token = JToken.Parse(value.ToString());
            // writer.WriteToken(token.CreateReader());
        }
    }
}
        