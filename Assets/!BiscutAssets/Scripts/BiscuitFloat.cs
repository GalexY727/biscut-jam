using UnityEngine;

public class BiscuitFloat : MonoBehaviour
{
    [Header("Bob")]
    public float bobAmplitude = 0.2f;   // height of the bob
    public float bobSpeed = 1.4f;       // cycles per second

    [Header("Spin / Wobble (2D)")]
    public float spinDegreesPerSec = 30f; // Z-rotation
    public float scalePulse = 0.04f;      // subtle squash/stretch

    Vector3 basePos;
    Vector3 baseScale;
    float t;

    void Awake()
    {
        basePos = transform.localPosition;
        baseScale = transform.localScale;
        // slight randomization so multiple biscuits don't sync
        t = Random.value * 10f;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
    }

    void Update()
    {
        t += Time.deltaTime;

        // vertical bob
        float y = Mathf.Sin(t * Mathf.PI * 2f * bobSpeed) * bobAmplitude;
        transform.localPosition = basePos + new Vector3(0f, y, 0f);

        // gentle spin
        transform.Rotate(0f, 0f, spinDegreesPerSec * Time.deltaTime);

        // tiny scale pulse to sell the "coin" feel
        float s = 1f + Mathf.Sin(t * Mathf.PI * 2f * bobSpeed) * scalePulse;
        transform.localScale = baseScale * s;
    }
}
