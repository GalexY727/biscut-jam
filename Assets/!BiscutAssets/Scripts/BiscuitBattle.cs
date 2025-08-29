using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BiscuitBattle : MonoBehaviour
{
    public enum State { Idle, EnemyClaiming, Battle }
    public State CurrentState { get; private set; } = State.Idle;

    [Header("UI")]
    [SerializeField] private Slider progressBar; // World-space slider above biscuit
    [SerializeField] private float clicksPerSecondForMax = 5f; // cps/5 => 1.0
    [SerializeField] private float winHoldSeconds = 2f;

    private readonly List<float> _clickTimes = new List<float>(32); // timestamps (Time.time) of clicks in last 1s
    private float _winTimer = 0f;

    // The enemy currently interacting with this biscuit.
    private EnemyCat _claimer;

    private void Start()
    {
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    } 

    /// <summary>Called by an enemy to begin claiming (uncontested countdown happens on the enemy).</summary>
    public void BeginEnemyClaim(EnemyCat enemy)
    {
        if (CurrentState != State.Idle) return;
        _claimer = enemy;
        CurrentState = State.EnemyClaiming;
        BiscuitFloat bf = GetComponent<BiscuitFloat>();
        bf.bobSpeed = 0f; // stop bobbing while being claimed
    }

    /// <summary>Called by the enemy if its uncontested 3s claim succeeds.</summary>
    public void EnemyConsumes()
    {
        Destroy(gameObject);
    }

    /// <summary>Player clicked this biscuit while enemy is claiming → enter battle mode.</summary>
    private void OnMouseDown()
    {
        if (CurrentState == State.EnemyClaiming)
        {
            EnterBattle();
        }
        else if (CurrentState == State.Battle)
        {
            RegisterClick();
        }
        // If Idle, this biscuit is free; your existing player-hold logic can live elsewhere.
    }

    private void OnMouseOver()
    {
        // During battle, allow rapid clicks using OnMouseOver + GetMouseButtonDown
        if (CurrentState == State.Battle && Input.GetMouseButtonDown(0))
        {
            RegisterClick();
        }
    }

    private void EnterBattle()
    {
        CurrentState = State.Battle;
        _clickTimes.Clear();
        _winTimer = 0f;
        if (progressBar != null)
        {
            progressBar.value = 0f;
            progressBar.gameObject.SetActive(true);
        }
        _claimer?.NotifyContested(this); // let enemy pause its 3s claim and wait for result
    }

    private void RegisterClick()
    {
        _clickTimes.Add(Time.time);
    }

    private void Update()
    {
        if (CurrentState != State.Battle) return;

        // Keep only last 1.0s of clicks
        float now = Time.time;
        for (int i = _clickTimes.Count - 1; i >= 0; i--)
        {
            if (now - _clickTimes[i] > 1f) _clickTimes.RemoveAt(i);
        }

        float cps = _clickTimes.Count; // clicks in last 1s
        float progress = Mathf.Clamp01(cps / Mathf.Max(1f, clicksPerSecondForMax));
        if (progressBar != null) progressBar.value = progress;

        if (progress >= 1f)
        {
            _winTimer += Time.deltaTime;
            if (_winTimer >= winHoldSeconds)
            {
                PlayerWinsBattle();
            }
        }
        else
        {
            _winTimer = 0f; // must be continuously at 100% for winHoldSeconds
        }
    }

    private void PlayerWinsBattle()
    {
        ScoreManager.Add(1);
        _claimer?.OnPlayerWonBattle();
        // Biscuit disappears on player win
        Destroy(gameObject);
    }

    /// <summary>Enemy ended interaction (e.g., moved on) — reset UI/state if needed.</summary>
    public void ResetIfEnemyGone(EnemyCat enemy)
    {
        if (_claimer != enemy) return;
        _claimer = null;
        if (CurrentState == State.Battle || CurrentState == State.EnemyClaiming)
        {
            CurrentState = State.Idle;
            if (progressBar != null) progressBar.gameObject.SetActive(false);
            _clickTimes.Clear();
            _winTimer = 0f;
        }
    }
}
