using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Battle 场景无线电塔生成管理器。
/// 为每个玩家生成一个本方无线电塔。
/// 5.20 临时版：按玩家出现顺序分配出生点，避免所有玩家刷在一起。
/// </summary>
public class BattleRadioTowerSpawnManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject radioTowerPrefab;

    [Header("出生区域")]
    public Transform[] spawnCenters;

    [Header("随机范围")]
    public float minRadius = 6f;
    public float maxRadius = 14f;
    public float navMeshSampleRadius = 6f;
    public int maxTryCount = 30;

    [Header("循环检查")]
    public float checkInterval = 1f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            if (NetworkServer.active)
            {
                SpawnTowerForPlayersWithoutTower();
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    [Server]
    private void SpawnTowerForPlayersWithoutTower()
    {
        PlayerBuilder[] players = FindObjectsByType<PlayerBuilder>(FindObjectsSortMode.None);

        foreach (PlayerBuilder player in players)
        {
            if (player == null)
                continue;

            if (HasTower(player.netId))
                continue;

            SpawnTowerForPlayer(player);
        }
    }

    [Server]
    private bool HasTower(uint playerNetId)
    {
        NetworkedRadioTower[] towers = FindObjectsByType<NetworkedRadioTower>(FindObjectsSortMode.None);

        foreach (NetworkedRadioTower tower in towers)
        {
            if (tower != null && tower.ownerPlayerNetId == playerNetId)
                return true;
        }

        return false;
    }

    [Server]
    private void SpawnTowerForPlayer(PlayerBuilder player)
    {
        if (radioTowerPrefab == null)
        {
            Debug.LogError("[BattleRadioTowerSpawnManager] radioTowerPrefab 未设置。");
            return;
        }

        Vector3 spawnPos = FindSpawnPosition(player);
        GameObject towerObj = Instantiate(radioTowerPrefab, spawnPos, Quaternion.identity);

        NetworkedRadioTower tower = towerObj.GetComponent<NetworkedRadioTower>();
        if (tower != null)
        {
            tower.ServerInit(player.myTeamColor, player.netId);
        }

        NetworkServer.Spawn(towerObj);

        Debug.Log($"[BattleRadioTowerSpawnManager] 为玩家 {player.netId} 生成无线电塔，位置：{spawnPos}");
    }

    [Server]
    private Vector3 FindSpawnPosition(PlayerBuilder player)
    {
        Vector3 center = GetSpawnCenterByExistingTowerCount();

        for (int i = 0; i < maxTryCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius);
            Vector3 candidate = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return center;
    }

    [Server]
    private Vector3 GetSpawnCenterByExistingTowerCount()
    {
        if (spawnCenters == null || spawnCenters.Length == 0)
            return Vector3.zero;

        NetworkedRadioTower[] existingTowers = FindObjectsByType<NetworkedRadioTower>(FindObjectsSortMode.None);

        int index = existingTowers.Length % spawnCenters.Length;

        if (spawnCenters[index] == null)
            return Vector3.zero;

        return spawnCenters[index].position;
    }
}