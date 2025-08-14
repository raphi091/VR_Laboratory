// Assets/Editor/PSBoundsTools.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PSBoundsTools
{
    [MenuItem("Tools/Particles/Apply Custom Bounds To Selected")]
    public static void ApplyCustomBoundsToSelected()
    {
        var renderers = Selection.GetFiltered<ParticleSystemRenderer>(
            SelectionMode.Editable | SelectionMode.Deep);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("선택한 오브젝트에 ParticleSystemRenderer가 없습니다.");
            return;
        }

        foreach (var r in renderers)
        {
            var so = new SerializedObject(r);

            // Use Custom Bounds = true
            var useProp = so.FindProperty("m_UseCustomBounds");
            if (useProp == null) { Debug.LogError("m_UseCustomBounds 를 찾을 수 없습니다."); continue; }
            useProp.boolValue = true;

            // Bounds 설정 (Center/Extent) — Extent는 Size의 절반
            var boundsProp = so.FindProperty("m_CustomBounds") ?? so.FindProperty("m_LocalBounds");
            if (boundsProp == null) { Debug.LogError("Bounds property not found."); continue; }

            boundsProp.FindPropertyRelative("m_Center").vector3Value = new Vector3(0f, -0.05f, 0.1f);
            boundsProp.FindPropertyRelative("m_Extent").vector3Value = new Vector3(0.2f, 0.3f, 0.3f); // Size 0.4,0.6,0.6

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(r);
            Debug.Log($"Applied custom bounds to: {r.gameObject.name}");
        }
    }
}
#endif
