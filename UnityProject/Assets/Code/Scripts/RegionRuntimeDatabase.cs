using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class RegionRuntimeDatabase : MonoBehaviour
{
    [Header("数据源")]
    public TextAsset generatedCellsJson;
    public TextAsset cellValuesJson;

    public Dictionary<int, GeneratedCellInfo> cellsById = new Dictionary<int, GeneratedCellInfo>();
    public Dictionary<int, CellValueInfo> valuesById = new Dictionary<int, CellValueInfo>();

    private void Awake()
    {
        LoadGeneratedCells();
        LoadCellValues();

        // Earth 场景加载后，把 BattleContext 里记录的战斗结果回写到运行时数据库。
        ApplyAllBattleResultsFromContext();
    }

    void LoadGeneratedCells()
    {
        if (generatedCellsJson == null) return;

        var root = JsonConvert.DeserializeObject<GeneratedCellRoot>(generatedCellsJson.text);
        cellsById.Clear();

        if (root == null || root.cells == null) return;

        foreach (var c in root.cells)
            cellsById[c.id] = c;
    }

    void LoadCellValues()
    {
        if (cellValuesJson == null) return;

        var root = JsonConvert.DeserializeObject<CellValueRoot>(cellValuesJson.text);
        valuesById.Clear();

        if (root == null || root.cells == null) return;

        foreach (var c in root.cells)
            valuesById[c.id] = c;
    }

    public bool TryGetCell(int cellId, out GeneratedCellInfo gen, out CellValueInfo value)
    {
        bool hasGen = cellsById.TryGetValue(cellId, out gen);
        bool hasVal = valuesById.TryGetValue(cellId, out value);
        return hasGen || hasVal;
    }

    /// <summary>
    /// 从 BattleContext 回写所有已经产生战斗结果的区域。
    /// 当前 5.20 版本只需要运行时生效，不写回 JSON 文件。
    /// </summary>
    public void ApplyAllBattleResultsFromContext()
    {
        foreach (int cellId in cellsById.Keys)
        {
            if (BattleContext.TryGetOutcome(cellId, out BattleOutcome outcome))
            {
                ApplyBattleResult(cellId, outcome);
            }
        }
    }

    /// <summary>
    /// 把某个区域的战斗结果写入运行时数据库。
    /// </summary>
    public void ApplyBattleResult(int cellId, BattleOutcome outcome)
    {
        if (cellId <= 0)
            return;

        if (!valuesById.TryGetValue(cellId, out CellValueInfo value) || value == null)
        {
            value = new CellValueInfo();
            value.id = cellId;
            value.displayName = GetDisplayName(cellId);
            value.owner = "None";
            value.terrain = "";
            value.resources = null;
            value.population = 0;
            value.taxRate = 0f;
            value.canBuild = true;
            value.tags = new List<string>();

            valuesById[cellId] = value;
        }

        if (BattleContext.TryGetOwnerOverride(cellId, out string ownerOverride))
        {
            value.owner = ownerOverride;
        }

        if (BattleContext.TryGetStatus(cellId, out string status))
        {
            value.battleStatus = status;
        }

        value.lastBattleOutcome = outcome.ToString();

        Debug.Log($"[RegionRuntimeDatabase] 区域回写：cellId={cellId}, owner={value.owner}, status={value.battleStatus}");
    }

    public string GetDisplayName(int cellId)
    {
        TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        if (val != null && !string.IsNullOrEmpty(val.displayName))
            return val.displayName;

        if (gen != null && !string.IsNullOrEmpty(gen.name))
            return gen.name;

        return "Unknown Region";
    }

    public string GetOwner(int cellId)
    {
        TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        if (val != null && !string.IsNullOrEmpty(val.owner))
            return val.owner;

        return "None";
    }

    public string GetTerrain(int cellId)
    {
        TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        if (val != null && !string.IsNullOrEmpty(val.terrain))
            return val.terrain;

        return "";
    }

    public string GetBattleStatus(int cellId)
    {
        TryGetCell(cellId, out GeneratedCellInfo gen, out CellValueInfo val);

        if (val != null && !string.IsNullOrEmpty(val.battleStatus))
            return val.battleStatus;

        return "No Battle";
    }
}

[System.Serializable]
public class GeneratedCellRoot
{
    public int version;
    public string projection;
    public int width;
    public int height;
    public List<GeneratedCellInfo> cells;
}

[System.Serializable]
public class GeneratedCellInfo
{
    public int id;
    public string name;
    public int[] color;
    public int sourceFeatureIndex;
}

[System.Serializable]
public class CellValueRoot
{
    public int version;
    public List<CellValueInfo> cells;
}

[System.Serializable]
public class CellValueInfo
{
    public int id;
    public string displayName;
    public string owner;
    public string terrain;
    public ResourceInfo resources;
    public int population;
    public float taxRate;
    public bool canBuild;
    public List<string> tags;

    [Header("运行时战斗状态")]
    public string battleStatus;
    public string lastBattleOutcome;
}

[System.Serializable]
public class ResourceInfo
{
    public int food;
    public int wood;
    public int iron;
}