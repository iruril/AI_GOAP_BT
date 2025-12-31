using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class PlayerSettingManager : MonoBehaviour
{
    Player.Input.InputRecorder inputMap;

    private const int DEF_MOUSE_SENS = 10;
    private const int DEF_GAMEPAD_SENS = 25;
    private const string SECTION = "Section";
    private const string INPUT_SENSITIVITY = "InputSensitivity";

    #region fileSavePath
    private string settingFolderPath = "\\Settings";
    private string settingFileName = "\\InputSettings.ini";
    #endregion

    private void Awake()
    {
#if UNITY_EDITOR
        settingFolderPath = settingFolderPath.Replace("\\", "/");
        settingFileName = settingFileName.Replace("\\", "/");
#endif
        inputMap = GetComponent<Player.Input.InputRecorder>();

        bool fileExist = TryLoadFile(out var data);

        if (fileExist && data.Count >= 2) // File이 없거나, ini일부가 깨졌을 경우
        {
            inputMap.SensitivityOnMouse = data[0];
            inputMap.SensitivityOnGamepad = data[1];
        }
        else
        {
            inputMap.SensitivityOnMouse = DEF_MOUSE_SENS;
            inputMap.SensitivityOnGamepad = DEF_GAMEPAD_SENS;
        }
    }

    private void OnApplicationQuit()
    {
        SaveFile();
    }

    #region ini Import
    [System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern long WritePrivateProfileString(
        string Section, string Key, string Value, string FilePath);
    [System.Runtime.InteropServices.DllImport("kernel32", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int GetPrivateProfileString(
        string Section, string Key, string Default, System.Text.StringBuilder RetVal, int Size, string FilePath);
    #endregion

    private bool TryLoadFile(out List<float> result)
    {
        string folderPath = Application.persistentDataPath + settingFolderPath;
        string iniFilePath = folderPath + settingFileName;

        result = new();

        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        if (!System.IO.File.Exists(iniFilePath))
        {
            var file = System.IO.File.CreateText(iniFilePath);
            file.Close();
            return false;
        }

        string raw = ReadIni(iniFilePath, SECTION, INPUT_SENSITIVITY);
        if (raw == "Error")
            return false;

        var iniFile = raw.Split(",");

        foreach (var item in iniFile)
        {
            if (float.TryParse(item, 
                NumberStyles.Float, 
                CultureInfo.InvariantCulture, 
                out var output))
            {
                result.Add(output);
            }
            else
            {
                Debug.LogWarning($"This String cannot be parsed into Float Type!!! : {item}");
            }
        }

        return true;
    }

    public void SaveFile()
    {
        string folderPath = Application.persistentDataPath + settingFolderPath;
        string iniFilePath = folderPath + settingFileName;

        if (!System.IO.Directory.Exists(folderPath))//폴더 확인
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        string value =
            $"{inputMap.SensitivityOnMouse.ToString(CultureInfo.InvariantCulture)}," +
            $"{inputMap.SensitivityOnGamepad.ToString(CultureInfo.InvariantCulture)}";

        WriteIni(iniFilePath, SECTION, INPUT_SENSITIVITY, value);
    }


    /// <summary>
    /// ini 파일 쓰기
    /// </summary>
    /// <param name="filePath">폴더경로 및 파일(확장자 포함)</param>
    /// <param name="section">ini 파일 내부 섹션</param>
    /// <param name="key">섹션 내부 key</param>
    /// <param name="value">저장 값</param>
    public void WriteIni(string filePath, string section, string key, string value)
    {
        WritePrivateProfileString(section, key, value, filePath);
    }

    /// <summary>
    /// ini 파일 읽기
    /// </summary>
    /// <param name="filePath">폴더경로 및 파일(확장자 포함)</param>
    /// <param name="section">ini 파일 내부 섹션</param>
    /// <param name="key">섹션 내부 key</param>
    /// <returns></returns>
    public string ReadIni(string filePath, string section, string key)
    {
        var value = new System.Text.StringBuilder(255);
        GetPrivateProfileString(section, key, "Error", value, 255, filePath);
        return value.ToString();
    }
}
