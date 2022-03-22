using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string resultVerify = verifyStringToJson(json);
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(resultVerify);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    public static string verifyStringToJson(string json)
    {

        string result = json;
        if (json.StartsWith("["))
        {
            result = $"{{\"Items\": {json} }}";
        }

        return result;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}