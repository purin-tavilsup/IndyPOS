﻿namespace IndyPOS.Application.Common.Interfaces;

public interface IJsonService
{
    Task SaveToFileAsync<TValue>(TValue value, string filePath);

    Task<TValue> ReadFromFileAsync<TValue>(string filePath);

    void SaveToFile<TValue>(TValue value, string filePath);

    TValue ReadFromFile<TValue>(string filePath);

    string Serialize<TValue>(TValue value);

    TValue? Deserialize<TValue>(string json);
}