using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LightShaker_K : MonoBehaviour
{
    [SerializeField] BaffledFlask_K flask;

    [Header("Shaking Thresholds")]
    public float angularSpeedRequired = 1.0f;
    public float holdSecondsRequired  = 0.8f;

    float held;
    Rigidbody rb;
    bool sent;

    void Reset() {
        rb = GetComponent<Rigidbody>();
        // 같은 오브젝트나 부모에서 자동 탐색
        if (!flask) flask = GetComponent<BaffledFlask_K>();
        if (!flask) flask = GetComponentInParent<BaffledFlask_K>();
    }

    void Awake() {
        rb = GetComponent<Rigidbody>();
        if (!flask) flask = GetComponent<BaffledFlask_K>() ?? GetComponentInParent<BaffledFlask_K>();
    }

    void Update() {
        if (sent || !flask) return;
        float w = rb.angularVelocity.magnitude;
        if (w >= angularSpeedRequired) held += Time.deltaTime;
        else                           held = Mathf.Max(0f, held - Time.deltaTime * 0.5f);

        if (held >= holdSecondsRequired) {
            sent = true;
            flask.OnLightShakeDone();
        }
    }
}
