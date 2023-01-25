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
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
        