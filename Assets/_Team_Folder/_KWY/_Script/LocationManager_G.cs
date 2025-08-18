using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class LocationEntry
{
    public string ID;
    public Transform TargetTransform;
}

public class LocationManager_G : MonoBehaviour
{
    [SerializeField] private List<LocationEntry> locations = new List<LocationEntry>();

    private Dictionary<string, Transform> locationDictionary = new Dictionary<string, Transform>();

    private void Awake()
    {
        foreach (var entry in locations)
        {
            if (!locationDictionary.ContainsKey(entry.ID))
            {
                locationDictionary.Add(entry.ID, entry.TargetTransform);
            }
            else
            {
                Debug.LogWarning($"[LocationManager] 중복된 ID가 존재합니다: {entry.ID}");
            }
        }
    }

    public Transform GetLocation(string id)
    {
        if (locationDictionary.ContainsKey(id))
        {
            return locationDictionary[id];
        }

        Debug.LogError($"[LocationManager] ID '{id}'에 해당하는 위치를 찾을 수 없습니다!");
        return null;
    }
}
