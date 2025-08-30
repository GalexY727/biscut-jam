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
    public float reaction = 3f;
    public bool triggerAnimation = true;
    public string animTrigger = "Laser";
    public bool changeColor = true;
    public Color reactedColor = Color.red;
    public bool revertColor = false;

    [Header("Firing")]
    public bool holdToFire = true;
    public float beamOnTime = 0.05f;
    public float fireRate = 10f;
    public float damagePerSecond = 25f;

    // Private
    float _nextShotTime;

    Collider2D _currentHit;
    float _heldTime;
    SpriteRenderer _currentSR;
    Color _originalColor;
    bool _reacted;

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

    /// <summary>
    /// Aims the laser towards the mouse, decide whether to fire continuously or fire a short pulse
    /// </summary>
    void Update()
    {
        // Gets the mouse position in world space
        // GetMouseWorld uses the Camera.ScreenToWorldPoint
        Vector3 mouseWorld = GetMouseWorld();

        // Creates a unit direction vector from the muzzle to the mouse
        // Normalizing ensures raycasting and math behave consistently
        Vector2 dir = ((Vector2)(mouseWorld - muzzle.position)).normalized;

        if (holdToFire)
        {
            if (IsFireHeld()) FireContinuous(dir);
            else if (beam) beam.enabled = false;
        }
        else
        {
            // This only acts for pulse mode which is the frame the button was pressed
            // and only if our cooldown has expired
            if (FirePressedThisFrame() && Time.time >= _nextShotTime)
            {

                // This checks the next time we are allowed to fire a pulse
                // Mathf.Max prevents division by zero if fireRate is set to 0 accidentally
                _nextShotTime = Time.time + 1f / Mathf.Max(1f, fireRate);

                // Uses a coroutine that keeps the laser visible and dealing damage for beamOnTime seconds then turns off
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

    // Fires a continuous beam
    // We pass in Time.deltaTime so the damage is applied as damage per second
    void FireContinuous(Vector2 dir) => DoRaycastAndRender(dir, Time.deltaTime);

    /// <summary>
    /// Fires a short burst that stays on for the beamOnTime seconds
    /// then turns off. This is when holdToFire is off
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    System.Collections.IEnumerator FirePulse(Vector2 dir)
    {
        float t = 0f;

        // Keeps the beam active for the duration of beamOnTime
        while (t < beamOnTime)
        {
            // Complies the work of raycasting, drawing, and damage)
            // We pass in deltaTime so damage scales over time. 
            DoRaycastAndRender(dir, Time.deltaTime);

            // Increase the timer by one 
            t += Time.deltaTime;

            // Wait until the next frame
            yield return null;
        }

        // Hide the beam when the pulse ends
        if (beam) beam.enabled = false;
    }

    /// <summary>
    /// The Raycast from the muzzle uses the "dir" to find out what we are hitting.
    /// We draw the "LineRenderer" from the start to the end (hit point or max distance.
    /// If we hti something, that implements the IDamageable, then we apply damage. 
    /// </summary>
    /// <param name="dir">Normalized direction the laser travels</param>
    /// <param name="damageScale">Amount the damage scales by the frame of Time.deltaTime
    /// so in short, damagePerSecond * deltaTime = consistent damageperSecond or DPS</param>
    void DoRaycastAndRender(Vector2 dir, float damageScale)
    {
        // Where the laser starts
        Vector3 start = muzzle.position;

        // Physics raycast in 2D to see what we are hitting
        // The hitMask restricts which layers can be hit
        RaycastHit2D hit = Physics2D.Raycast(start, dir, maxDistance, hitMask);

        // End point of the beam
        // If we hit something, we use the impact point.
        // Otherwise, we draw the laser to the max range in the direction of the mouse
        Vector3 end = hit ? (Vector3)hit.point : start + (Vector3)(dir * maxDistance);

        // Draws the update the LineRenderer so we can see the laser
        if (beam)
        {
            beam.enabled = true;
            beam.startWidth = width;
            beam.endWidth = width;
            beam.SetPosition(0, start);
            beam.SetPosition(1, end);
        }

        // Applies the damage if we hit something that can take damage
        if (hit)
        {
            if (hit)
            {
                if (_currentHit != hit.collider)
                {
                    ResetHoldState();
                    _currentHit = hit.collider;
                }

                _heldTime += damageScale;

                if (!_reacted && _heldTime >= reaction)
                {
                    if (triggerAnimation && !string.IsNullOrEmpty(animTrigger))
                    {
                        var anim = hit.collider.GetComponentInParent<Animator>();
                        if (anim)
                        {
                            anim.SetTrigger(animTrigger);
                        }
                    }

                    if (changeColor)
                    {
                        if (_currentSR == null)
                        {
                            _currentSR = hit.collider.GetComponentInParent<SpriteRenderer>();
                            if (_currentSR != null) _originalColor = _currentSR.color;
                        }
                        if (_currentSR != null) _currentSR.color = reactedColor;
                    }

                    _reacted = true;
                }

                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null)
                {
                    dmg.Damage(damagePerSecond * damageScale);
                }
            }
            else
            {
                ResetHoldState();
            }

            /// <summary>
            /// Clear the "held on target" state. If 'revertColor' is true, restore original color.
            /// Called when the target changes, beam stops hitting, or firing stops.
            /// </summary>
            void ResetHoldState()
            {
                _currentHit = null;
                _heldTime = 0f;
                _reacted = false;

                if (revertColor && _currentSR != null)
                {
                    _currentSR.color = _originalColor;
                }
                _currentSR = null;
            }
            //// Looks for a component that implements IDamageable on the hit objects
            //var dmg = hit.collider.GetComponent<IDamageable>();
            //if (dmg != null)
            //{
            //    // damagePerSecond is scaled by damageScale
            //    dmg.Damage(damagePerSecond * damageScale);
            //}
        }
    }
}

public interface IDamageable { void Damage(float amount); }

