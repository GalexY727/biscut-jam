using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static event Action<int> OnScoreChanged;
    public static int Score { get; private set; }

    public static void Add(int delta)
    {
        Score += delta;
        OnScoreChanged?.Invoke(Score);
    }

    // Optional: call this when a new round starts
    public static void ResetScore()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}
