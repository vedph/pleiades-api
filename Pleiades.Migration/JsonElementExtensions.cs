using System;
using System.Text.Json;

namespace Pleiades.Migration
{
    /// <summary>
    /// Extensions to JsonElement.
    /// </summary>
    static public class JsonElementExtensions
    {
        // https://stackoverflow.com/questions/61553962/getting-nested-properties-with-system-text-json

        public static JsonElement GetDescendant(this JsonElement element,
            string path)
        {
            if (element.ValueKind == JsonValueKind.Null ||
                element.ValueKind == JsonValueKind.Undefined)
            {
                return default;
            }

            string[] segments =
                path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int n = 0; n < segments.Length; n++)
            {
                element = element.TryGetProperty(
                    segments[n], out JsonElement value) ? value : default;

                if (element.ValueKind == JsonValueKind.Null ||
                    element.ValueKind == JsonValueKind.Undefined)
                {
                    return default;
                }
            }

            return element;
        }

        public static string GetStringPropertyValue(this JsonElement element,
            string name)
        {
            JsonElement child = element.TryGetProperty(
                name, out JsonElement value) ? value : default;

            return (child.ValueKind == JsonValueKind.Null ||
                child.ValueKind == JsonValueKind.Undefined) ?
                null : child.ToString();
        }

        public static int GetIntPropertyValue(this JsonElement element,
            string name)
        {
            JsonElement child = element.TryGetProperty(
                name, out JsonElement value) ? value : default;

            return (child.ValueKind == JsonValueKind.Null ||
                child.ValueKind == JsonValueKind.Undefined) ?
                0 : child.GetInt32();
        }

        public static double GetDoublePropertyValue(this JsonElement element,
            string name)
        {
            JsonElement child = element.TryGetProperty(
                name, out JsonElement value) ? value : default;

            return (child.ValueKind == JsonValueKind.Null ||
                child.ValueKind == JsonValueKind.Undefined) ?
                0 : child.GetDouble();
        }

        public static DateTime GetDatePropertyValue(this JsonElement element,
            string name)
        {
            JsonElement child = element.TryGetProperty(
                name, out JsonElement value) ? value : default;

            return (child.ValueKind == JsonValueKind.Null ||
                child.ValueKind == JsonValueKind.Undefined) ?
                default : child.GetDateTime();
        }

        public static string[] GetStringArrayValue(this JsonElement element,
            string name)
        {
            JsonElement child = element.TryGetProperty(
                name, out JsonElement value) ? value : default;

            if (child.ValueKind == JsonValueKind.Null ||
                child.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            string[] items = new string[child.GetArrayLength()];
            for (int i = 0; i < items.Length; i++)
                items[i] = child[i].ToString();

            return items;
        }

        public static double[] GetDoubleArrayValue(this JsonElement element,
            string name)
        {
            JsonElement child = element.TryGetProperty(
                name, out JsonElement value) ? value : default;

            if (child.ValueKind == JsonValueKind.Null ||
                child.ValueKind == JsonValueKind.Undefined)
            {
                return null;
            }

            double[] items = new double[child.GetArrayLength()];
            for (int i = 0; i < items.Length; i++)
                items[i] = child[i].GetDouble();

            return items;
        }
    }
}
