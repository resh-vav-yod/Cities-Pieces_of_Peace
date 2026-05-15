using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkedRadioTower : NetworkBehaviour
{
    [Header("生命值")]
    public float maxHp = 100f;

    [SyncVar]
    private float currentHp;

    [SyncVar]
    private bool destroyed;

    [Header("规则")]
    public float secondsBeforeReload = 8f;
    public bool disableManualControlWhenDestroyed = true;

    [Header("场景")]
    [Tooltip("留空则刷新当前场景。必须加入 Build Settings。")]
    public string sceneNameAfterCountdown = "";

    [Header("UI")]
    public BattleCountdownUI countdownUI;
    public string destroyedTitle = "无线电塔已被摧毁";

    [Header("Debug")]
    public bool allowDebugKillByKey = true;
    public KeyCode debugKillKey = KeyCode.K;

    public bool IsDestroyed => destroyed;

    [Header("碰撞")]
    public Collider towerCollider;

    private void Awake()
    {
        if (towerCollider == null)
            towerCollider = GetComponent<Collider>();
    }

    public Vector3 GetClosestPoint(Vector3 fromPosition)
    {
        if (towerCollider != null)
            return towerCollider.ClosestPoint(fromPosition);

        return transform.position;
    }

    public float GetDistanceTo(Vector3 fromPosition)
    {
        Vector3 closest = GetClosestPoint(fromPosition);
        return Vector3.Distance(fromPosition, closest);
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        currentHp = maxHp;
        destroyed = false;
    }

    private void Update()
    {
        if (!isServer)
            return;

        if (allowDebugKillByKey && Input.GetKeyDown(debugKillKey))
        {
            ServerTakeDamage(maxHp + 9999f);
        }
    }

    [Server]
    public void ServerTakeDamage(float damage)
    {
        if (destroyed)
            return;

        if (damage <= 0f)
            return;

        currentHp = Mathf.Max(0f, currentHp - damage);

        if (currentHp <= 0f)
        {
            ServerDestroyTower();
        }
    }

    [Server]
    private void ServerDestroyTower()
    {
        if (destroyed)
            return;

        destroyed = true;

        Debug.Log("[NetworkedRadioTower] Radio tower destroyed.");

        if (disableManualControlWhenDestroyed)
        {
            BattleCommandAuthority.SetManualControl(false);
        }

        RpcOnTowerDestroyed(secondsBeforeReload);

        StartCoroutine(ServerReloadAfterCountdown());
    }

    [ClientRpc]
    private void RpcOnTowerDestroyed(float countdownSeconds)
    {
        if (disableManualControlWhenDestroyed)
        {
            BattleCommandAuthority.SetManualControl(false);
        }

        if (countdownUI == null)
        {
            countdownUI = FindFirstObjectByType<BattleCountdownUI>();
        }

        if (countdownUI != null)
        {
            countdownUI.Show(destroyedTitle, countdownSeconds);
            StartCoroutine(ClientCountdown(countdownSeconds));
        }
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
        {
            targetScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.ServerChangeScene(targetScene);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }
}