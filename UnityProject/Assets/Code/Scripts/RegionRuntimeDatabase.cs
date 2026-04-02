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
}

[System.Serializable]
public class ResourceInfo
{
    public int food;
    public int wood;
    public int iron;
}

/*
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class RegionRuntimeDatabase : MonoBehaviour
{
    [Header("数据源")]
    public TextAsset generatedCellsJson;
    public TextAsset cellValuesJson;
    // 如果你暂时没有 groups.json，这里可以留空
    public TextAsset groupsJson; 

    // 内存字典
    public Dictionary<int, GeneratedCellInfo> cellsById = new Dictionary<int, GeneratedCellInfo>();
    public Dictionary<int, CellValueInfo> valuesById = new Dictionary<int, CellValueInfo>();

    private void Awake()
    {
        LoadGeneratedCells();
        LoadCellValues();
    }

    void LoadGeneratedCells()
    {
        if (generatedCellsJson == null) return;
        var root = JsonConvert.DeserializeObject<GeneratedCellRoot>(generatedCellsJson.text);
        cellsById.Clear();
        foreach (var c in root.cells)
            cellsById[c.id] = c;
    }

    void LoadCellValues()
    {
        if (cellValuesJson == null) return;
        var root = JsonConvert.DeserializeObject<CellValueRoot>(cellValuesJson.text);
        valuesById.Clear();
        foreach (var c in root.cells)
            valuesById[c.id] = c;
    }

    // 核心查询接口
    public bool TryGetCell(int cellId, out GeneratedCellInfo gen, out CellValueInfo value)
    {
        bool hasGen = cellsById.TryGetValue(cellId, out gen);
        bool hasVal = valuesById.TryGetValue(cellId, out value);
        return hasGen || hasVal;
    }
}

// --- 数据结构定义 ---
[System.Serializable]
public class GeneratedCellRoot { public List<GeneratedCellInfo> cells; }

[System.Serializable]
public class GeneratedCellInfo
{
    public int id;
    public string name;
    public int[] color;
}

[System.Serializable]
public class CellValueRoot { public List<CellValueInfo> cells; }

[System.Serializable]
public class CellValueInfo
{
    public int id;
    public string displayName;
    public string owner;
    public ResourceInfo resources;
}

[System.Serializable]
public class ResourceInfo { public int food; public int wood; public int iron; }
*/