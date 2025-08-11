using UnityEngine;
using UnityEditor;
using System.IO;

public class LiquidDataImporter_L
{
    [MenuItem("Tools/Biology Simulator/Generate Liquids from CSV")]
    public static void GenerateLiquids()
    {
        string filePath = Path.Combine(Application.dataPath, "_Team_Folder", "_LES", "3.Script", "Bio", "liquids_database.csv");

        if (!File.Exists(filePath))
        {
            EditorUtility.DisplayDialog("오류", $"지정된 경로에 파일이 없습니다:\n{filePath}", "확인");
            return;
        }

        string[] lines = File.ReadAllLines(filePath);

        string folderPath = "Assets/Resources/LiquidData";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');

            LiquidData_L data = ScriptableObject.CreateInstance<LiquidData_L>();

            data.liquidName = values[0];
            data.formula = values[1];
            data.description = values[2];
            data.colorName = values[3];
            
            if (ColorUtility.TryParseHtmlString(values[4], out Color newColor))
            {
                data.color = newColor;
            }

            // --- 여기를 수정했습니다 ---
            // 파일 이름에 포함될 수 없는 문자들을 안전한 문자로 교체합니다.
            string safeFileName = data.liquidName
                .Replace(" (", "_")
                .Replace(")", "")
                .Replace("/", "_"); // 슬래시(/)를 언더스코어(_)로 변경하는 코드 추가

            string assetPath = $"{folderPath}/{safeFileName}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료", $"{lines.Length - 1}개의 용액 데이터 에셋 생성을 완료했습니다.", "확인");
    }
}
