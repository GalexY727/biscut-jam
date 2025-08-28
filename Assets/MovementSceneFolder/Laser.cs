using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;  // new input
#endif

public class Laser : MonoBehaviour
{
    [Header("References")]
    public Transform muzzle;
    public LineRenderer beam;
    public EnemyHealth enemy;

    [Header("Laser Settings")]
    public float maxDistance = 30f;
    public float width = 0.08f;
    public LayerMask hitMask = ~0;

    [Header("Firing")]
    public bool holdToFire = true;
    public float beamOnTime = 0.05f;
    public float fireRate = 10f;
    public float damagePerSecond = 25f;

    // Private
    float _nextShotTime;

    void Awake()
    {
        if (!beam) beam = GetComponentInChildren<LineRenderer>();
        if (beam)
        {
            beam.enabled = false;
            beam.positionCount = 2;
            beam.startWidth = width;
            beam.endWidth = width;
            beam.alignment = LineAlignment.View;
            beam.textureMode = LineTextureMode.Stretch;
        }
    }

    void Update()
    {
        Vector3 mouseWorld = GetMouseWorld();
        Vector2 dir = ((Vector2)(mouseWorld - muzzle.position)).normalized;

        if (holdToFire)
        {
            if (IsFireHeld()) FireContinuous(dir);
            else if (beam) beam.enabled = false;
        }
        else
        {
            if (FirePressedThisFrame() && Time.time >= _nextShotTime)
            {
                _nextShotTime = Time.time + 1f / Mathf.Max(1f, fireRate);
                StartCoroutine(FirePulse(dir));
            }
        }
    }

    // ------- Input helpers (work with either system) -------
    bool IsFireHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    bool FirePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    Vector3 GetMouseWorld()
    {
        Vector3 screen;
#if ENABLE_INPUT_SYSTEM
        Vector2 p = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        screen = new Vector3(p.x, p.y, 0f);
#else
        screen = Input.mousePosition;
#endif
        var cam = Camera.main;
        var world = cam.ScreenToWorldPoint(screen);
        world.z = 0f; 
        return world;
    }
    // ------------------------------------------------------

    void FireContinuous(Vector2 dir) => DoRaycastAndRender(dir, Time.deltaTime);

    System.Collections.IEnumerator FirePulse(Vector2 dir)
    {
        float t = 0f;
        while (t < beamOnTime)
        {
            DoRaycastAndRender(dir, Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }
        if (beam) beam.enabled = false;
    }

    void DoRaycastAndRender(Vector2 dir, float damageScale)
    {
        Vector3 start = muzzle.position;
        RaycastHit2D hit = Physics2D.Raycast(start, dir, maxDistance, hitMask);
        Vector3 end = hit ? (Vector3)hit.point : start + (Vector3)(dir * maxDistance);

        if (beam)
        {
            beam.enabled = true;
            beam.startWidth = width;
            beam.endWidth = width;
            beam.SetPosition(0, start);
            beam.SetPosition(1, end);
        }

        if (hit)
        {
            var dmg = hit.collider.GetComponent<IDamageable>();
            if (dmg != null) dmg.Damage(damagePerSecond * damageScale);
        }
    }
}

public interface IDamageable { void Damage(float amount); }

