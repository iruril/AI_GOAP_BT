using UnityEngine;
using System.Collections;
using Mirror;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance = null;

    public bool GameReady = false;

    [SerializeField] private float totalTeamScore = 1000f;
    [SerializeField] private float captureWeightPerSec = 1.0f;
    [SerializeField] private float killWeight = 1.0f;

    [SyncVar(hook = nameof(BlueScoreChanged))] public float CurrentBlueScore;
    [SyncVar(hook = nameof(RedScoreChanged))] public float CurrentRedScore;

    public override void OnStartServer()
    {
        Instance = this;
        StartCoroutine(Initialize());
        CurrentBlueScore = totalTeamScore;
        CurrentRedScore = totalTeamScore;
    }

    public override void OnStopServer()
    {
        Instance = null;
    }

    private IEnumerator Initialize()
    {
        yield return new WaitUntil(()=> GameManager.GetInstance().MyPlayer != null);
        yield return new WaitUntil(() => BotSpawner.Instance.BotSpawned);
        //추후에 플래그 넣을 것들 여기에 추가할 것.
        //로딩 후 시작 대기 타이머 (네트워크 객체)

        GameReady = true;
    }

    private void FixedUpdate()
    {
        if (!isServer) return;
        if (!GameReady) return;

        ApplyScoreByCap();
        CheckWinCondition();
    }

    [Server]
    private void ApplyScoreByCap()
    {
        int totalCapScore = WorldManager.Instance.GetTotalCaptureScore();
        float delta = Mathf.Abs(totalCapScore) * captureWeightPerSec * Time.fixedDeltaTime;

        if (totalCapScore > 0)
            CurrentRedScore -= delta;
        else if (totalCapScore < 0)
            CurrentBlueScore -= delta;

        CurrentBlueScore = Mathf.Max(0f, CurrentBlueScore);
        CurrentRedScore = Mathf.Max(0f, CurrentRedScore);
    }

    [Server]
    private void CheckWinCondition()
    {
        if (CurrentBlueScore <= 0)
            EndGame(Team.Red);
        else if (CurrentRedScore <= 0)
            EndGame(Team.Blue);
    }

    [Server]
    public void EndGame(Team winningTeam)
    {
        //게임 종료 처리
        Debug.Log($"Game Over! {winningTeam} Team Wins!");
    }

    [Server]
    public void ApplyKillScore(Team killer, Team victim)
    {
        if (killer == victim) return;

        if (killer == Team.Blue)
        {
            CurrentRedScore -= killWeight;
        }
        else if (killer == Team.Red)
        {
            CurrentBlueScore -= killWeight;
        }
    }

    private void GameReadyChanged(bool oldVar, bool newVar)
    {

    }

    private void BlueScoreChanged(float oldVar, float newVar)
    {

    }

    private void RedScoreChanged(float oldVar, float newVar)
    {

    }
}
