using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class EnemyCat : MonoBehaviour
{
    private enum State { Searching, Claiming, WaitingBattle, Stunned }
    [Header("Movement/Targeting")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float claimRange = 1000f; // cat can laser from anywhere; keep huge if you don't want to move
    [SerializeField] private float retargetDelay = 0.5f;

    [Header("Claim")]
    [SerializeField] private float claimSecondsUncontested = 3f;

    [Header("Visuals")]
    [SerializeField] private Transform eyeOrigin; // optional; if null, uses transform.position
    [SerializeField] private SpriteRenderer normalSprite;
    [SerializeField] private SpriteRenderer stunnedSprite; // assign an alt sprite for stun
    [SerializeField] private float stunnedAlpha = 0.35f;
    [SerializeField] private float fadeToOpaqueSeconds = 0.5f;

    private BiscuitBattle _target;
    private State _state = State.Searching;
    private LineRenderer _laser;
    private Coroutine _claimRoutine;

    private void Awake()
    {
        _laser = GetComponent<LineRenderer>();
        _laser.enabled = false;
        if (stunnedSprite != null) stunnedSprite.enabled = false;
    }

    private void Start()
    {
        StartCoroutine(StateLoop());
    }

    private IEnumerator StateLoop()
    {
        while (_state != State.Stunned)
        {
            switch (_state)
            {
                case State.Searching:
                    AcquireTarget();
                    break;
                case State.Claiming:
                    UpdateLaser();
                    // optional: move closer for flavor
                    MoveTowardsTarget(0.02f);
                    break;
                case State.WaitingBattle:
                    UpdateLaser();
                    break;
            }
            yield return null;
        }
    }

    private void AcquireTarget()
    {
        // Find nearest Biscuit in scene
        var biscuits = FindObjectsByType<BiscuitBattle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (biscuits.Length == 0)
        {
            // No biscuits; wait and try again
            StartCoroutine(DelayRetarget());
            return;
        }
        _target = biscuits
            .Where(b => b != null && b.CurrentState == BiscuitBattle.State.Idle)
            .OrderBy(b => (b.transform.position - transform.position).sqrMagnitude)
            .FirstOrDefault();

        if (_target == null)
        {
            StartCoroutine(DelayRetarget());
            return;
        }

        // Begin claiming
        _state = State.Claiming;
        _target.BeginEnemyClaim(this);
        _laser.enabled = true;
        _claimRoutine = StartCoroutine(ClaimCountdown());
    }

    private IEnumerator DelayRetarget()
    {
        yield return new WaitForSeconds(retargetDelay);
        _state = State.Searching;
    }

    private void MoveTowardsTarget(float dtScale)
    {
        if (_target == null) return;
        Vector3 dir = (_target.transform.position - transform.position);
        float dist = dir.magnitude;
        if (dist > 0.1f && dist > claimRange)
        {
            transform.position += dir.normalized * moveSpeed * Time.deltaTime * dtScale;
        }
    }

    private void UpdateLaser()
    {
        if (!_laser.enabled || _target == null) return;
        Vector3 start = eyeOrigin ? eyeOrigin.position : transform.position;
        Vector3 end = _target.transform.position;
        _laser.positionCount = 2;
        _laser.SetPosition(0, start);
        _laser.SetPosition(1, end);
    }

    private IEnumerator ClaimCountdown()
    {
        float t = 0f;
        while (t < claimSecondsUncontested)
        {
            if (_state != State.Claiming) yield break; // interrupted (e.g., contested)
            t += Time.deltaTime;
            yield return null;
        }

        // Uncontested success: consume biscuit, retarget
        if (_target != null)
        {
            _target.EnemyConsumes();
            _target = null;
        }
        _laser.enabled = false;
        _state = State.Searching;
        yield return null;
    }

    /// <summary>Called by the Biscuit when the player clicks it during claim.</summary>
    public void NotifyContested(BiscuitBattle b)
    {
        if (_state != State.Claiming || _target != b) return;

        // Stop the uncontested timer; continue to hold laser while waiting battle outcome
        if (_claimRoutine != null) StopCoroutine(_claimRoutine);
        _state = State.WaitingBattle;
    }

    public void OnPlayerWonBattle()
    {
        // Player gets the biscuit; cat is stunned in place for 10s, then fade alpha→1 and destroy
        StartCoroutine(StunAndDie());
    }

    private IEnumerator StunAndDie()
    {
        _state = State.Stunned;
        _laser.enabled = false;

        // Swap to stunned visuals and lower alpha
        if (normalSprite != null) normalSprite.enabled = false;
        if (stunnedSprite != null)
        {
            stunnedSprite.enabled = true;
            var c = stunnedSprite.color;
            c.a = Mathf.Clamp01(stunnedAlpha);
            stunnedSprite.color = c;
        }

        // Freeze cat in place for 10s
        yield return new WaitForSeconds(10f);

        // Fade transparency to 1 (opaque) over fadeToOpaqueSeconds, then destroy
        if (stunnedSprite != null)
        {
            float t = 0f;
            Color c = stunnedSprite.color;
            float startA = c.a;
            while (t < fadeToOpaqueSeconds)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(startA, 1f, Mathf.Clamp01(t / fadeToOpaqueSeconds));
                c.a = a;
                stunnedSprite.color = c;
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Let the biscuit reset if we disappear mid-interaction
        if (_target != null)
        {
            _target.ResetIfEnemyGone(this);
        }
    }
}
