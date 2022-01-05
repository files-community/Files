﻿namespace Files.Models.JsonSettings
{
    public interface IJsonSettingsSerializer
    {

        string SerializeToJson(object obj);

        T DeserializeFromJson<T>(string json);
    }
}
