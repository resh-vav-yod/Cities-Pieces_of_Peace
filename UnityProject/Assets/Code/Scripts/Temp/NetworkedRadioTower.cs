using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// 网络无线电塔。
/// 负责阵营颜色、生命值死亡响应、摧毁奖励、通信中断、返回地球。
/// </summary>
public class NetworkedRadioTower : NetworkBehaviour
{
    [Header("阵营")]
    [SyncVar(hook = nameof(OnTeamColorChanged))]
    public Color teamColor = Color.white;

    [SyncVar]
    public uint ownerPlayerNetId;

    [Header("组件")]
    public NetworkHealth health;
    public NetworkVisionSource visionSource;
    public Renderer towerRenderer;
    public Collider towerCollider;

    [Header("经济")]
    public int destroyReward = 150;

    [Header("规则")]
    public float secondsBeforeReload = 8f;
    public float regionReentryLockSeconds = 30f;
    public bool disableManualControlWhenDestroyed = true;

    [Header("场景")]
    public string sceneNameAfterCountdown = "Earth_v1.0";

    [Header("UI")]
    public BattleCountdownUI countdownUI;
    public string destroyedTitle = "无线电塔已被摧毁";

    [Header("Debug")]
    public bool allowDebugKillByKey = true;
    public KeyCode debugKillKey = KeyCode.K;

    [SyncVar]
    private bool destroyed;

    public bool IsDestroyed => destroyed;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<NetworkHealth>();

        if (visionSource == null)
            visionSource = GetComponent<NetworkVisionSource>();

        if (towerRenderer == null)
            towerRenderer = GetComponentInChildren<Renderer>();

        if (towerCollider == null)
            towerCollider = GetComponent<Collider>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        destroyed = false;

        if (health != null)
            health.ServerDied += HandleHealthDied;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        if (health != null)
            health.ServerDied -= HandleHealthDied;
    }

    private void Update()
    {
        if (!isServer)
            return;

        if (allowDebugKillByKey && Input.GetKeyDown(debugKillKey))
        {
            ServerTakeDamage(99999f, null);
        }
    }

    [Server]
    public void ServerInit(Color ownerColor, uint ownerNetId)
    {
        teamColor = ownerColor;
        ownerPlayerNetId = ownerNetId;

        if (visionSource != null)
            visionSource.ServerSetTeam(ownerColor);

        RpcApplyColor(ownerColor);
    }

    [Server]
    public void ServerTakeDamage(float damage, GameObject source = null)
    {
        if (destroyed)
            return;

        if (health != null)
            health.ServerTakeDamage(damage, source);
    }

    [Server]
    private void HandleHealthDied(NetworkHealth deadHealth)
    {
        GiveRewardToAttacker(deadHealth.LastDamageSource);
        ServerDestroyTower();
    }

    [Server]
    private void GiveRewardToAttacker(GameObject source)
    {
        if (source == null)
            return;

        UnitAI attackerUnit = source.GetComponent<UnitAI>();

        if (attackerUnit == null)
            return;

        if (UnitAI.IsSameTeam(attackerUnit.teamColor, teamColor))
            return;

        PlayerBuilder[] players = FindObjectsByType<PlayerBuilder>(FindObjectsSortMode.None);

        foreach (PlayerBuilder player in players)
        {
            if (player == null)
                continue;

            if (UnitAI.IsSameTeam(player.myTeamColor, attackerUnit.teamColor))
            {
                player.ServerAddCredits(destroyReward);
                Debug.Log($"[NetworkedRadioTower] 摧毁无线电塔奖励：玩家 {player.netId} +{destroyReward}");
                return;
            }
        }
    }

    [Server]
    private void ServerDestroyTower()
    {
        if (destroyed)
            return;

        destroyed = true;

        BattleContext.ReportCurrentBattleResult(BattleOutcome.RadioLost, regionReentryLockSeconds);

        Debug.Log("[NetworkedRadioTower] Radio tower destroyed.");

        if (disableManualControlWhenDestroyed)
            BattleCommandAuthority.SetManualControl(false);

        RpcOnTowerDestroyed(secondsBeforeReload);

        StartCoroutine(ServerReloadAfterCountdown());
    }

    [ClientRpc]
    private void RpcOnTowerDestroyed(float countdownSeconds)
    {
        if (disableManualControlWhenDestroyed)
            BattleCommandAuthority.SetManualControl(false);

        if (countdownUI == null)
            countdownUI = FindCountdownUI();

        if (countdownUI != null)
        {
            countdownUI.Show(destroyedTitle, countdownSeconds);
            StartCoroutine(ClientCountdown(countdownSeconds));
        }
        else
        {
            Debug.LogWarning("[NetworkedRadioTower] 找不到 BattleCountdownUI。");
        }
    }

    private BattleCountdownUI FindCountdownUI()
    {
        BattleCountdownUI[] uis = FindObjectsByType<BattleCountdownUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (uis != null && uis.Length > 0)
            return uis[0];

        return null;
    }

    private IEnumerator ClientCountdown(float seconds)
    {
        float remaining = seconds;

        while (remaining > 0f)
        {
            if (countdownUI != null)
                countdownUI.SetRemaining(remaining);

            remaining -= Time.deltaTime;
            yield return null;
        }

        if (countdownUI != null)
            countdownUI.SetRemaining(0f);
    }

    [Server]
    private IEnumerator ServerReloadAfterCountdown()
    {
        yield return new WaitForSeconds(secondsBeforeReload);

        string targetScene = sceneNameAfterCountdown;

        if (string.IsNullOrWhiteSpace(targetScene))
            targetScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (NetworkManager.singleton != null)
            NetworkManager.singleton.ServerChangeScene(targetScene);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }

    private void OnTeamColorChanged(Color oldColor, Color newColor)
    {
        ApplyColor(newColor);
    }

    [ClientRpc]
    private void RpcApplyColor(Color color)
    {
        ApplyColor(color);
    }

    private void ApplyColor(Color color)
    {
        if (towerRenderer != null)
            towerRenderer.material.color = color;
    }

    public Vector3 GetClosestPoint(Vector3 fromPosition)
    {
        if (towerCollider != null)
            return towerCollider.ClosestPoint(fromPosition);

        return transform.position;
    }

    public float GetDistanceTo(Vector3 fromPosition)
    {
        return Vector3.Distance(fromPosition, GetClosestPoint(fromPosition));
    }
}