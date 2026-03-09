using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// [구현 원리 요약]
/// - JSON 입출력의 "기술"만 담당(읽기/쓰기/삭제/텍스트).
/// - 정책(어떤 파일을 우선할지)은 JsonManager2D가 결정.
/// </summary>
public static class JsonIO2D
{
    public static string GetPersistentPath(string fileName)
        => Path.Combine(Application.persistentDataPath, fileName);

    public static bool TryLoadTextFromPersistent(string fileName, out string text, out string error)
    {
        text = null;
        error = null;

        try
        {
            string path = GetPersistentPath(fileName);
            if (!File.Exists(path))
            {
                error = "파일이 없습니다.";
                return false;
            }

            text = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "파일 내용이 비어있습니다.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TrySaveTextToPersistent(string fileName, string text, out string error)
    {
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "저장할 텍스트가 비어있습니다.";
                return false;
            }

            string path = GetPersistentPath(fileName);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, text, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryLoadFromPersistent<T>(string fileName, out T data, out string error)
    {
        data = default;
        error = null;

        try
        {
            string path = GetPersistentPath(fileName);
            if (!File.Exists(path))
            {
                error = "파일이 없습니다.";
                return false;
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            data = JsonUtility.FromJson<T>(json);

            if (data == null)
            {
                error = "JSON 파싱 결과가 null입니다.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TrySaveToPersistent<T>(string fileName, T data, bool prettyPrint, out string error)
    {
        error = null;

        try
        {
            string path = GetPersistentPath(fileName);
            string json = JsonUtility.ToJson(data, prettyPrint);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json, Encoding.UTF8);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryDeletePersistent(string fileName, out string error)
    {
        error = null;

        try
        {
            string path = GetPersistentPath(fileName);
            if (File.Exists(path))
                File.Delete(path);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}