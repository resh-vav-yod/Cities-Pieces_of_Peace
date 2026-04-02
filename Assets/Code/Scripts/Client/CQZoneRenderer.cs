/*
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CQMergedZoneRenderer : MonoBehaviour
{
    [Header("Data")]
    public TextAsset geoJsonFile;

    [Header("Earth")]
    [Tooltip("拖你的地球 Renderer 进来。若为空，则使用 fallbackEarthDiameter。")]
    public Renderer earthRenderer;
    [Tooltip("你的地球在 Unity 里是 999x999x999，所以默认直径填 999。")]
    public float fallbackEarthDiameter = 999f;

    [Header("Line")]
    [Tooltip("边界线抬高高度，单位和地球尺寸一致。999直径地球下，0.5~1.2 比较合适。")]
    public float lineHeight = 0.9f;
    public Material lineMaterial;

    private void Start()
    {
        if (geoJsonFile == null || lineMaterial == null)
        {
            Debug.LogWarning("[CQMergedZoneRenderer] geoJsonFile 或 lineMaterial 未设置。");
            return;
        }

        ParseAndBuildMergedMesh(geoJsonFile.text);
    }

    private void ParseAndBuildMergedMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        float radius = GetSurfaceRadius() + lineHeight;

        List<Vector3> allVertices = new List<Vector3>(65536);
        List<int> lineIndices = new List<int>(131072);
        int currentIndex = 0;

        foreach (JToken feature in features)
        {
            string geomType = (string)feature["geometry"]?["type"];
            JToken coordinates = feature["geometry"]?["coordinates"];

            if (coordinates == null) continue;

            if (geomType == "Polygon")
            {
                AddPolygonRings((JArray)coordinates, radius, allVertices, lineIndices, ref currentIndex);
            }
            else if (geomType == "MultiPolygon")
            {
                foreach (JToken polygon in (JArray)coordinates)
                {
                    AddPolygonRings((JArray)polygon, radius, allVertices, lineIndices, ref currentIndex);
                }
            }
        }

        Mesh mesh = new Mesh
        {
            name = "CQ_Merged_BorderLines",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        mesh.SetVertices(allVertices);
        mesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0, calculateBounds: false);
        mesh.RecalculateBounds();

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterial = lineMaterial;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private void AddPolygonRings(
        JArray polygonRings,
        float radius,
        List<Vector3> verts,
        List<int> indices,
        ref int startIndex)
    {
        if (polygonRings == null || polygonRings.Count == 0) return;

        for (int ringIdx = 0; ringIdx < polygonRings.Count; ringIdx++)
        {
            JArray ringCoords = (JArray)polygonRings[ringIdx];
            List<Vector2> rawRing = ReadRing2D(ringCoords);
            if (rawRing.Count < 2) continue;

            List<Vector2> unwrappedRing = UnwrapRingLongitudes(rawRing);
            AddRing(unwrappedRing, radius, verts, indices, ref startIndex);
        }
    }

    private void AddRing(
        List<Vector2> ringLonLat,
        float radius,
        List<Vector3> verts,
        List<int> indices,
        ref int startIndex)
    {
        int count = ringLonLat.Count;
        if (count < 2) return;

        for (int i = 0; i < count; i++)
        {
            float lon = ringLonLat[i].x;
            float lat = ringLonLat[i].y;
            verts.Add(LonLatToVector3(lon, lat, radius));
        }

        for (int i = 0; i < count - 1; i++)
        {
            indices.Add(startIndex + i);
            indices.Add(startIndex + i + 1);
        }

        indices.Add(startIndex + count - 1);
        indices.Add(startIndex);

        startIndex += count;
    }

    private float GetSurfaceRadius()
    {
        if (earthRenderer != null)
        {
            Vector3 e = earthRenderer.bounds.extents;
            return Mathf.Max(e.x, Mathf.Max(e.y, e.z));
        }

        return fallbackEarthDiameter * 0.5f; // 999 -> 499.5
    }

    private static List<Vector2> ReadRing2D(JArray ringCoords)
    {
        List<Vector2> points = new List<Vector2>();
        if (ringCoords == null || ringCoords.Count == 0) return points;

        int count = ringCoords.Count;

        // GeoJSON 通常首尾闭合重复，去掉最后一个重复点
        if (count > 1 && IsSamePoint((JArray)ringCoords[0], (JArray)ringCoords[count - 1]))
        {
            count--;
        }

        for (int i = 0; i < count; i++)
        {
            JArray pt = (JArray)ringCoords[i];
            float lon = (float)pt[0];
            float lat = (float)pt[1];
            points.Add(new Vector2(lon, lat));
        }

        return points;
    }

    private static List<Vector2> UnwrapRingLongitudes(List<Vector2> rawPoints)
    {
        List<Vector2> result = new List<Vector2>(rawPoints.Count);
        if (rawPoints.Count == 0) return result;

        float currentLon = rawPoints[0].x;
        float prevRawLon = rawPoints[0].x;
        result.Add(new Vector2(currentLon, rawPoints[0].y));

        for (int i = 1; i < rawPoints.Count; i++)
        {
            float rawLon = rawPoints[i].x;
            float lat = rawPoints[i].y;

            float delta = rawLon - prevRawLon;
            if (delta > 180f) delta -= 360f;
            else if (delta < -180f) delta += 360f;

            currentLon += delta;
            result.Add(new Vector2(currentLon, lat));

            prevRawLon = rawLon;
        }

        return result;
    }

    private static bool IsSamePoint(JArray a, JArray b)
    {
        return Mathf.Abs((float)a[0] - (float)b[0]) < 0.000001f &&
               Mathf.Abs((float)a[1] - (float)b[1]) < 0.000001f;
    }

    private static Vector3 LonLatToVector3(float lon, float lat, float radius)
    {
        // 保持你原来的镜像修正逻辑
        lon = -lon;

        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;

        float x = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
        float y = radius * Mathf.Sin(latRad);
        float z = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);

        return new Vector3(x, y, z);
    }
}

/*
 * 这个脚本是 CQ Zone 边界的合并渲染版本，使用一个 Mesh 来绘制所有边界线，性能更好。
 * 注意：需要配合一个专门的 Unlit Shader（ZTest Always）来避免和地形模型的 Z-Fighting 问题。
 * 使用方法：
 * 1. 将这个脚本挂在地球模型上（确保有 MeshFilter 和 MeshRenderer 组件）。
 * 2. 在 Inspector 中赋值 geoJsonFile（cq-zones.geojson）和 lineMaterial（使用 ZTest Always 的 Unlit 材质）。
 * 3. 调整 earthRadius 以确保边界线正确贴合地球表面。
    * 4. 运行游戏，你应该能看到所有 CQ Zone 的边界线被渲染出来了。
 * 5. 这个版本不包含交互功能，如果需要交互，可以在基础上添加碰撞体和交互脚本，或者使用 ZoneInteractive 脚本单独处理交互。
 */
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CQMergedZoneRenderer : MonoBehaviour
{
    public TextAsset geoJsonFile;
    public float earthRadius = 998f; // 比 1000 小，埋在地下，用 Shader 透视
    public Material lineMaterial;

    void Start()
    {
        if (geoJsonFile == null || lineMaterial == null) return;
        ParseAndBuildMergedMesh(geoJsonFile.text);
    }

    void ParseAndBuildMergedMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        List<Vector3> allVertices = new List<Vector3>();
        List<int> lineIndices = new List<int>();
        int currentIndex = 0;

        foreach (JToken feature in features)
        {
            string geomType = (string)feature["geometry"]["type"];
            JArray coordinates = (JArray)feature["geometry"]["coordinates"];

            if (geomType == "Polygon")
            {
                currentIndex = AddRing((JArray)coordinates[0], allVertices, lineIndices, currentIndex);
            }
            else if (geomType == "MultiPolygon")
            {
                foreach (JToken polygon in coordinates)
                {
                    currentIndex = AddRing((JArray)polygon[0], allVertices, lineIndices, currentIndex);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 允许极多顶点
        mesh.vertices = allVertices.ToArray();
        // 🛡️ 终极防御：使用离散线段，取代一笔画
        mesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = lineMaterial;
    }

    int AddRing(JArray ringCoords, List<Vector3> verts, List<int> indices, int startIndex)
    {
        int count = ringCoords.Count;
        for (int i = 0; i < count; i++)
        {
            float lon = (float)ringCoords[i][0];
            float lat = (float)ringCoords[i][1];

            verts.Add(LatLonToVector3(lat, lon, earthRadius));

            if (i > 0)
            {
                float prevLon = (float)ringCoords[i - 1][0];
                
                // 🔪 智能抬笔：如果两点距离跳跃超过 180 度，不生成线段（斩断蜘蛛网）
                if (Mathf.Abs(lon - prevLon) < 180f)
                {
                    indices.Add(startIndex + i - 1);
                    indices.Add(startIndex + i);
                }
            }
        }

        // 检查多边形首尾闭合时的撕裂
        float firstLon = (float)ringCoords[0][0];
        float lastLon = (float)ringCoords[count - 1][0];
        if (Mathf.Abs(firstLon - lastLon) < 180f)
        {
            indices.Add(startIndex + count - 1);
            indices.Add(startIndex);
        }

        return startIndex + count;
    }

    Vector3 LatLonToVector3(float lat, float lon, float radius)
    {
        lon = -lon; // 解决镜像
        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;
        return new Vector3(
            radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad),
            radius * Mathf.Sin(latRad),
            radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad)
        );
    }
}

/*
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CQMergedZoneRenderer : MonoBehaviour
{
    [Header("数据文件 (geojson改成.json)")]
    public TextAsset geoJsonFile;

    [Header("地球半径 (和地形咬合)")]
    public float earthRadius = 996f; // 比面高亮小，物理上埋在地表，视觉上用 ZTest Always 

    [Header("画线材质 (必须是用 Shader Graph 连的 ZTest Always 的 Unlit Shader)")]
    public Material lineMaterial;

    [Header("线宽 (单位：米)")]
    public float lineWidth = 0.5f; // 注意：Merged Mesh Lines 的线宽在 Shader 里设置更高效，这里是物理宽度

    void Start()
    {
        if (geoJsonFile == null || lineMaterial == null)
        {
            Debug.LogError("请确保已赋值 GeoJsonFile 和 LineMaterial");
            return;
        }

        // 彻底清理地球上的 LineRenderer 组件尸体（以防万一）
        LineRenderer[] lrs = GetComponentsInChildren<LineRenderer>();
        foreach(LineRenderer lr in lrs) { Destroy(lr); }

        ParseAndBuildMergedMesh(geoJsonFile.text);
    }

    void ParseAndBuildMergedMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        List<Vector3> allVertices = new List<Vector3>();
        List<int> lineIndices = new List<int>();

        int currentIndex = 0;

        foreach (JToken feature in features)
        {
            string geomType = (string)feature["geometry"]["type"];
            JArray coordinates = (JArray)feature["geometry"]["coordinates"];

            if (geomType == "Polygon")
            {
                // coordinates[0] 是外环
                currentIndex = AddRingToMergedMesh((JArray)coordinates[0], allVertices, lineIndices, currentIndex);
            }
            else if (geomType == "MultiPolygon")
            {
                foreach (JToken polygon in coordinates)
                {
                    // polygon[0] 是多多边形中的一个多边形的外环
                    currentIndex = AddRingToMergedMesh((JArray)polygon[0], allVertices, lineIndices, currentIndex);
                }
            }
        }

        // 构建合并网格
        Mesh mesh = new Mesh();
        mesh.vertices = allVertices.ToArray();
        // 🛡️ 核心黑科技：不使用默认的三角剖分Topology.Lines（性能高）
        mesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);

        // 设置到地球本体上
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = lineMaterial;
    }

    int AddRingToMergedMesh(JArray ringCoords, List<Vector3> verticesList, List<int> indicesList, int startIndex)
    {
        int pointsCount = ringCoords.Count;
        for (int i = 0; i < pointsCount; i++)
        {
            JArray pt = (JArray)ringCoords[i];
            float lon = (float)pt[0];
            float lat = (float)pt[1];

            // 转换 3D 坐标
            Vector3 pos3D = LatLonToVector3(lat, lon, earthRadius);
            verticesList.Add(pos3D);

            // 存索引 (画线顺序)
            if (i > 0)
            {
                indicesList.Add(startIndex + i - 1); // 上一个点
                indicesList.Add(startIndex + i);     // 当前点
            }
        }
        // 为了确保多边形闭合，需要把最后一个点和第一个点连起来
        indicesList.Add(startIndex + pointsCount - 1);
        indicesList.Add(startIndex);

        // 返回新的起始索引给下一个 Feature 使用
        return startIndex + pointsCount;
    }

    Vector3 LatLonToVector3(float lat, float lon, float radius)
    {
        // 🛡️ 确保数学同步 (解决镜像翻转)
        lon = -lon; 

        // 转换为弧度
        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;

        // 计算 XYZ
        float x = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
        float y = radius * Mathf.Sin(latRad);
        float z = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);

        return new Vector3(x, y, z);
    }
}
*/

/*
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq; // 引入 Newtonsoft.Json

public class ZoneRenderer : MonoBehaviour
{
    [Header("配置参数")]
    public float earthRadius = 100f; // 你的地球模型半径（请根据实际模型调整）
    public TextAsset geoJsonFile;    // 将你的 cq-zones.geojson 拖到这里
    public Material lineMaterial;    // 边界线的材质
    public float lineWidth = 0.5f;   // 边界线宽度

    void Start()
    {
        if (geoJsonFile != null)
        {
            ParseAndDrawGeoJSON(geoJsonFile.text);
        }
    }

    void ParseAndDrawGeoJSON(string jsonText)
    {
        // 解析整个 JSON
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        foreach (JToken feature in features)
        {
            // 获取分区信息，例如 CQ Zone 编号
            int zoneNumber = (int)feature["properties"]["cq_zone_number"];
            string geomType = (string)feature["geometry"]["type"];
            JArray coordinates = (JArray)feature["geometry"]["coordinates"];

            // GeoJSON 中通常是 Polygon 或 MultiPolygon
            if (geomType == "MultiPolygon")
            {
                // MultiPolygon 结构: [ [ [ [lon, lat], ... ] ] ]
                foreach (JToken polygon in coordinates)
                {
                    foreach (JToken ring in polygon)
                    {
                        DrawRing(ring, $"Zone_{zoneNumber}");
                    }
                }
            }
            else if (geomType == "Polygon")
            {
                foreach (JToken ring in coordinates)
                {
                    DrawRing(ring, $"Zone_{zoneNumber}");
                }
            }
        }
    }

    // 绘制一个闭合的经纬度环
    void DrawRing(JToken ringCoords, string zoneName)
    {
        List<Vector3> points = new List<Vector3>();

        foreach (JToken coord in ringCoords)
        {
            float lon = (float)coord[0]; // GeoJSON 标准是先经度(X)
            float lat = (float)coord[1]; // 后纬度(Y)
            
            // 将经纬度转为 Unity 的球面三维坐标
            Vector3 pos = LatLonToVector3(lat, lon, earthRadius);
            points.Add(pos);
        }

        // 创建 LineRenderer 游戏物体
        GameObject lineObj = new GameObject(zoneName + "_Border");
        lineObj.transform.SetParent(this.transform); // 设置为地球的子物体
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = false; // 跟随地球旋转
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }

    // 核心数学：经纬度转三维坐标
    Vector3 LatLonToVector3(float lat, float lon, float radius)
    {
        lon = -lon; 

        // 转换为弧度
        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;

        // 注意：这里的公式假设你的地球极点在 Y 轴，赤道在 XZ 平面
        // 且本初子午线（经度0）对应 Z 轴正方向。
        // 如果画出来发现大陆是倒着的或者翻转的，请调整下面 XYZ 的对应关系或正负号
        float x = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
        float y = radius * Mathf.Sin(latRad);
        float z = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);

        // 为了防止线和地球模型重叠引发Z-Fighting（闪烁），可以在返回时让半径稍微加一点点，例如 radius * 1.001f
        return new Vector3(x, y, z) * 1.002f; 
    }
}
*/