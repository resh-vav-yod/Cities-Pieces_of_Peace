using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using EarcutNet;

public class ZoneMeshBuilder : MonoBehaviour
{
    [Header("Data")]
    public TextAsset geoJsonFile;

    [Header("Earth")]
    [Tooltip("拖你的地球 Renderer 进来。若为空，则使用 fallbackEarthDiameter。")]
    public Renderer earthRenderer;
    [Tooltip("你的地球在 Unity 里是 999x999x999，所以默认直径填 999。")]
    public float fallbackEarthDiameter = 999f;

    [Header("Zone Surface")]
    [Tooltip("分区面抬高高度。999直径地球下建议 0.15 ~ 0.5。")]
    public float zoneHeight = 0.25f;
    public Material defaultZoneMaterial;

    [Header("Collider")]
    public bool addMeshCollider = true;

    private void Start()
    {
        if (geoJsonFile == null || defaultZoneMaterial == null)
        {
            Debug.LogWarning("[ZoneMeshBuilder] geoJsonFile 或 defaultZoneMaterial 未设置。");
            return;
        }

        ParseAndBuildMesh(geoJsonFile.text);
    }

    private void ParseAndBuildMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        float radius = GetSurfaceRadius() + zoneHeight;

        foreach (JToken feature in features)
        {
            int zoneNumber = feature["properties"]?["cq_zone_number"] != null
                ? (int)feature["properties"]["cq_zone_number"]
                : -1;

            string geomType = (string)feature["geometry"]?["type"];
            JToken coordinates = feature["geometry"]?["coordinates"];
            if (coordinates == null) continue;

            if (geomType == "Polygon")
            {
                BuildPolygonMesh((JArray)coordinates, $"Zone_{zoneNumber}", radius);
            }
            else if (geomType == "MultiPolygon")
            {
                int partIndex = 0;
                foreach (JToken polygon in (JArray)coordinates)
                {
                    BuildPolygonMesh((JArray)polygon, $"Zone_{zoneNumber}_Part{partIndex}", radius);
                    partIndex++;
                }
            }
        }
    }

    private void BuildPolygonMesh(JArray polygonRings, string objName, float radius)
    {
        if (polygonRings == null || polygonRings.Count == 0) return;

        // 读取 outer ring
        List<Vector2> outerRaw = ReadRing2D((JArray)polygonRings[0]);
        if (outerRaw.Count < 3) return;

        List<Vector2> outerRing = UnwrapRingLongitudes(outerRaw);
        float outerAvgLon = GetAverageLon(outerRing);

        List<Vector3> vertices3D = new List<Vector3>(outerRing.Count * 2);
        List<double> flatData2D = new List<double>(outerRing.Count * 4);
        List<int> holeIndices = new List<int>();

        // outer
        AppendRing(outerRing, radius, vertices3D, flatData2D);

        // holes
        for (int ringIdx = 1; ringIdx < polygonRings.Count; ringIdx++)
        {
            List<Vector2> holeRaw = ReadRing2D((JArray)polygonRings[ringIdx]);
            if (holeRaw.Count < 3) continue;

            List<Vector2> holeRing = UnwrapRingLongitudes(holeRaw);

            // 把 hole 的经度整体平移到接近 outer，避免 +360/-360 错位
            float holeAvgLon = GetAverageLon(holeRing);
            float shift = Mathf.Round((outerAvgLon - holeAvgLon) / 360f) * 360f;
            ShiftRingLongitudes(holeRing, shift);

            holeIndices.Add(vertices3D.Count);
            AppendRing(holeRing, radius, vertices3D, flatData2D);
        }

        List<int> triangles = Earcut.Tessellate(flatData2D, holeIndices);
        if (triangles == null || triangles.Count < 3) return;

        EnsureTrianglesFaceOutward(vertices3D, triangles);

        Mesh mesh = new Mesh
        {
            name = objName,
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        mesh.SetVertices(vertices3D);
        mesh.SetTriangles(triangles, 0, calculateBounds: false);

        // 球面法线直接用径向法线，最稳
        Vector3[] normals = new Vector3[vertices3D.Count];
        for (int i = 0; i < vertices3D.Count; i++)
        {
            normals[i] = vertices3D[i].normalized;
        }

        mesh.normals = normals;
        mesh.RecalculateBounds();

        GameObject zoneObj = new GameObject(objName);
        zoneObj.transform.SetParent(transform, false);

        MeshFilter mf = zoneObj.AddComponent<MeshFilter>();
        MeshRenderer mr = zoneObj.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.material = new Material(defaultZoneMaterial);

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        if (addMeshCollider)
        {
            MeshCollider mc = zoneObj.AddComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = mesh;
        }

        ZoneInteractive interactiveScript = zoneObj.AddComponent<ZoneInteractive>();
        interactiveScript.zoneName = objName;
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

    private static void AppendRing(
        List<Vector2> ringLonLat,
        float radius,
        List<Vector3> vertices3D,
        List<double> flatData2D)
    {
        for (int i = 0; i < ringLonLat.Count; i++)
        {
            float lon = ringLonLat[i].x;
            float lat = ringLonLat[i].y;

            vertices3D.Add(LonLatToVector3(lon, lat, radius));
            flatData2D.Add(lon);
            flatData2D.Add(lat);
        }
    }

    private static List<Vector2> ReadRing2D(JArray ringCoords)
    {
        List<Vector2> points = new List<Vector2>();
        if (ringCoords == null || ringCoords.Count == 0) return points;

        int count = ringCoords.Count;

        // GeoJSON 通常首尾重复，去掉最后一个重复点
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

    private static float GetAverageLon(List<Vector2> ring)
    {
        if (ring == null || ring.Count == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < ring.Count; i++)
        {
            sum += ring[i].x;
        }
        return sum / ring.Count;
    }

    private static void ShiftRingLongitudes(List<Vector2> ring, float shift)
    {
        if (Mathf.Abs(shift) < 0.0001f) return;

        for (int i = 0; i < ring.Count; i++)
        {
            ring[i] = new Vector2(ring[i].x + shift, ring[i].y);
        }
    }

    private static bool IsSamePoint(JArray a, JArray b)
    {
        return Mathf.Abs((float)a[0] - (float)b[0]) < 0.000001f &&
               Mathf.Abs((float)a[1] - (float)b[1]) < 0.000001f;
    }

    private static void EnsureTrianglesFaceOutward(List<Vector3> vertices, List<int> triangles)
    {
        float sum = 0f;

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            Vector3 n = Vector3.Cross(b - a, c - a);
            Vector3 centerDir = ((a + b + c) / 3f).normalized;
            sum += Vector3.Dot(n, centerDir);
        }

        // 如果整体朝里，则翻转所有三角形
        if (sum < 0f)
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int tmp = triangles[i + 1];
                triangles[i + 1] = triangles[i + 2];
                triangles[i + 2] = tmp;
            }
        }
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
 * 这个脚本负责从 GeoJSON 数据中解析出分区的多边形坐标，并构建对应的 Mesh。
 * 核心功能包括：
 * 1. 解析 GeoJSON 文件，提取分区编号、名称和几何数据。
 * 2. 使用 Earcut 算法进行三角剖分，生成适合 Unity 渲染的 Mesh。
 * 3. 创建 GameObject，添加 MeshFilter、MeshRenderer 和 MeshCollider 组件。
 * 4. 将生成的 Mesh 应用到 GameObject 上，并设置材质。
 * 5. 添加交互脚本 ZoneInteractive，传递分区名称以供后续使用。
 * 
 * 注意事项：
 * - 确保 GeoJSON 数据格式正确，特别是坐标数组的结构。
 * - Earcut 算法要求输入的坐标是扁平化的二维数组（X,Y,X,Y,...）。
 * - Mesh 的顶点需要转换为三维坐标，以适应地球模型的球面结构。
 * - 材质和交互脚本需要根据项目需求进行调整和优化。
 
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using EarcutNet;

public class ZoneMeshBuilder : MonoBehaviour
{
    public TextAsset geoJsonFile;
    public float earthRadius = 1000f; // 保持 1000，和地球真实尺寸对应
    public Material defaultZoneMaterial;

    void Start()
    {
        if (geoJsonFile == null || defaultZoneMaterial == null) return;
        ParseAndBuildMesh(geoJsonFile.text);
    }

    void ParseAndBuildMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        foreach (JToken feature in features)
        {
            int zoneNumber = (int)feature["properties"]["cq_zone_number"];
            string geomType = (string)feature["geometry"]["type"];
            JArray coordinates = (JArray)feature["geometry"]["coordinates"];

            if (geomType == "Polygon")
            {
                BuildSingleMesh(coordinates[0], $"Zone_{zoneNumber}");
            }
            else if (geomType == "MultiPolygon")
            {
                int partIndex = 0;
                foreach (JToken polygon in coordinates)
                {
                    BuildSingleMesh(polygon[0], $"Zone_{zoneNumber}_Part{partIndex}");
                    partIndex++;
                }
            }
        }
    }

    void BuildSingleMesh(JToken ringCoords, string objName)
    {
        GameObject zoneObj = new GameObject(objName);
        zoneObj.transform.SetParent(this.transform, false); // 绝对不乱自身旋转，继承父级

        MeshFilter mf = zoneObj.AddComponent<MeshFilter>();
        MeshRenderer mr = zoneObj.AddComponent<MeshRenderer>();
        MeshCollider mc = zoneObj.AddComponent<MeshCollider>();

        List<Vector3> vertices3D = new List<Vector3>();
        List<double> flatData2D = new List<double>();
        
        JArray currentRing = (JArray)ringCoords;
        int ringPointsCount = currentRing.Count;

        // 🛡️ 终极防御：连续化经度，防止 180 度撕裂
        float currentContinuousLon = 0f;
        float previousRawLon = 0f;

        for (int i = 0; i < ringPointsCount; i++)
        {
            JArray pt = (JArray)currentRing[i];
            float rawLon = (float)pt[0];
            float lat = (float)pt[1];

            if (i == 0)
            {
                currentContinuousLon = rawLon;
            }
            else
            {
                // 计算实际跳跃距离
                float delta = rawLon - previousRawLon;
                if (delta > 180f) delta -= 360f;
                else if (delta < -180f) delta += 360f;
                
                currentContinuousLon += delta; // 强制连续
            }
            previousRawLon = rawLon;

            // 用连续化后的经度生成 3D 点
            Vector3 pos3D = LatLonToVector3(lat, currentContinuousLon, earthRadius);
            vertices3D.Add(pos3D);

            // 用连续化后的经度喂给 Earcut（2D展开图再也不会被撕裂）
            flatData2D.Add((double)currentContinuousLon);
            flatData2D.Add((double)lat);
        }

        List<int> triangles = Earcut.Tessellate(flatData2D, new List<int>());
        triangles.Reverse(); // 翻转法线朝向宇宙

        Mesh mesh = new Mesh();
        mesh.vertices = vertices3D.ToArray();
        mesh.triangles = triangles.ToArray();

        mf.mesh = mesh;
        mc.sharedMesh = mesh; // 碰撞体也会完美贴合表面
        mr.material = new Material(defaultZoneMaterial);
        
        ZoneInteractive interactiveScript = zoneObj.AddComponent<ZoneInteractive>();
        interactiveScript.zoneName = objName;
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
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EarcutNet; 
// 如果 Earcut 在不同命名空间，请使用 using 引入，通常是 using Mapbox.Earcut; (视你下载的代码而定)

public class ZoneMeshBuilder : MonoBehaviour
{
    [Header("配置参数")]
    public float earthRadius = 100f; 
    public TextAsset geoJsonFile; // 拖入 cq-zones.geojson
    public Material defaultZoneMaterial; // 给地块准备一个基础材质，比如半透明的白色

    void Start()
    {
        if (geoJsonFile != null)
        {
            ParseAndBuildMesh(geoJsonFile.text);
        }
    }

    /*
    void ParseAndBuildMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        foreach (JToken feature in features)
        {
            // 读取你数据里的分区编号和名字
            int zoneNumber = (int)feature["properties"]["cq_zone_number"];
            string zoneName = (string)feature["properties"]["cq_zone_name"];
            string geomType = (string)feature["geometry"]["type"];
            JArray coordinates = (JArray)feature["geometry"]["coordinates"];

            if (geomType == "Polygon")
            {
                // Polygon的结构是 [ [外环], [内环1(如果有的话)] ]
                BuildSingleMesh(coordinates[0], $"Zone_{zoneNumber}");
            }
            else if (geomType == "MultiPolygon")
            {
                // 像俄罗斯这种跨度极大的分区可能是多个独立多边形
                int partIndex = 0;
                foreach (JToken polygon in coordinates)
                {
                    BuildSingleMesh(polygon[0], $"Zone_{zoneNumber}_Part{partIndex}");
                    partIndex++;
                }
            }
        }
    }

    // 核心管线：构建单个 Mesh
    
    void BuildSingleMesh(JToken ringCoords, string objName)
    {
        // 1. 准备 Earcut 需要的 2D 扁平数据 (X,Y,X,Y...)
        List<double> flatData2D = new List<double>();
        
        // 2. 准备 Unity 需要的 3D 顶点数据
        List<Vector3> vertices3D = new List<Vector3>();

        foreach (JToken coord in ringCoords)
        {
            float lon = (float)coord[0];
            float lat = (float)coord[1];
            
            // 存入 2D 数据给 Earcut 算三角形 (作为平面处理)
            flatData2D.Add(lon);
            flatData2D.Add(lat);

            // 存入 3D 数据给 Unity 渲染 (转为球面坐标)
            // 这里复用了你之前的核心公式，加一点点半径偏移防止和地球模型穿模
            Vector3 pos3D = LatLonToVector3(lat, lon, earthRadius * 1.002f);
            vertices3D.Add(pos3D);
        }

        // 3. 执行 Earcut 三角剖分！它会返回一个顶点索引列表
        // 参数 2 表示我们的 flatData2D 是 2 维的 (XY)
        List<int> triangles = Earcut.Tessellate(flatData2D, new List<int>()); 
        triangles.Reverse(); // <--- 加上这一行，强制翻转模型的正反面！

        // 4. 构建 Unity Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices3D.ToArray();
        mesh.triangles = triangles.ToArray();

        // 计算法线，对于标准的球体，法线就是顶点位置的归一化方向
        Vector3[] normals = new Vector3[vertices3D.Count];
        for (int i = 0; i < vertices3D.Count; i++)
        {
            normals[i] = vertices3D[i].normalized;
        }
        mesh.normals = normals;

        // 5. 生成实体游戏对象
        GameObject zoneObj = new GameObject(objName);
        zoneObj.transform.SetParent(this.transform);
        zoneObj.transform.localPosition = Vector3.zero;

        // 添加网格渲染组件
        MeshFilter mf = zoneObj.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        
        MeshRenderer mr = zoneObj.AddComponent<MeshRenderer>();
        // 实例化一个独立材质，方便我们后续给它单独改颜色高亮
        mr.material = new Material(defaultZoneMaterial); 

        // 6. 添加碰撞体组件！有了它，物理射线就能点中它了
        MeshCollider mc = zoneObj.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        // 7. 挂载我们下一步写的交互脚本
        ZoneInteractive interactiveScript = zoneObj.AddComponent<ZoneInteractive>();
        interactiveScript.zoneName = objName;
    }
    ////////////

    void ParseAndBuildMesh(string jsonText)
    {
        JObject geoData = JObject.Parse(jsonText);
        JArray features = (JArray)geoData["features"];

        foreach (JToken feature in features)
        {
            // 读取你数据里的分区编号和名字
            int zoneNumber = (int)feature["properties"]["cq_zone_number"];
            // string zoneName = (string)feature["properties"]["cq_zone_name"];
            string geomType = (string)feature["geometry"]["type"];
            JArray coordinates = (JArray)feature["geometry"]["coordinates"];

            if (geomType == "Polygon")
            {
                // Polygon的结构是 [ [外环], [内环1(如果有的话)] ]
                // 👇 核心修复：在这里补上缺少的第 3 个参数 earthRadius
                BuildSingleMesh(coordinates[0], $"Zone_{zoneNumber}", earthRadius);
            }
            else if (geomType == "MultiPolygon")
            {
                // 像俄罗斯这种跨度极大的分区可能是多个独立多边形
                int partIndex = 0;
                foreach (JToken polygon in coordinates)
                {
                    // 👇 核心修复：在这里也补上缺少的第 3 个参数 earthRadius
                    BuildSingleMesh(polygon[0], $"Zone_{zoneNumber}_Part{partIndex}", earthRadius);
                    partIndex++;
                }
            }
        }
    }

    // 🛡️ 升级版：防止跨海缝合线的 BuildSingleMesh
    void BuildSingleMesh(JToken ringCoords, string objName, float radius)
    {
        // 1. 创建网格物体壳子
        GameObject zoneObj = new GameObject(objName);
        // 👇 核心修复：加上 false 参数！
        // 这会让网格完美继承地球的旋转和缩放，绝对不会再发生 90 度的错位！
        zoneObj.transform.SetParent(this.transform, false);

        /*
        zoneObj.transform.SetParent(transform); // 挂在地球下
        zoneObj.transform.localPosition = Vector3.zero;
        zoneObj.transform.localRotation = Quaternion.identity;
        /////////////

        MeshFilter mf = zoneObj.AddComponent<MeshFilter>();
        MeshRenderer mr = zoneObj.AddComponent<MeshRenderer>();
        MeshCollider mc = zoneObj.AddComponent<MeshCollider>();

        List<Vector3> vertices3D = new List<Vector3>();
        List<double> flatData2D = new List<double>();
        List<int> earcutTriangleIndices = new List<int>(); // 存放 Earcut 的临时三角形

        // 2. 🛡️ 核心修复：把一个完整的环，拆分成不相连的 Part（如果数据混乱的话）
        // 这一步是为了应对不完美的 JSON 数据，防止数据里强行把主大陆和岛屿连在一起。
        // 原理：Earcut 默认认为数据是闭合且不相连的。我们手动确保它。
        
        // 解析当前环的所有顶点
        JArray currentRing = (JArray)ringCoords;
        int ringPointsCount = currentRing.Count;

        // 用于记录上一个点的经度，以检测是否跨越了 180 度边界
        float previousLon = 0f; 

        for (int i = 0; i < ringPointsCount; i++)
        {
            JArray pt = (JArray)currentRing[i];
            float lon = (float)pt[0];
            float lat = (float)pt[1];

            // 🛡️ 核心修复：180度经线防撕裂补偿
            if (i > 0)
            {
                // 如果当前经度与上一个经度跳跃超过了 180 度（比如从 179 跳到 -179）
                if (lon - previousLon > 180f)
                {
                    lon -= 360f; // 强行把它拉回来（变成连贯的 -181）
                }
                else if (lon - previousLon < -180f)
                {
                    lon += 360f; // 强行把它拉回来（变成连贯的 181）
                }
            }
            previousLon = lon; // 记录当前状态给下一次循环对比

            // 转换 3D 坐标
            Vector3 pos3D = LatLonToVector3(lat, lon, radius);
            vertices3D.Add(pos3D);

            // 存 2D 坐标给 Earcut
            flatData2D.Add((double)lon);
            flatData2D.Add((double)lat);
        }
        
        // 3. 执行三角剖分（这一步 Earcut 会独立算每一Part，不会拉跨海桥）
        // holeIndices 填一个空的 List<int>()，骗过算法检查
        earcutTriangleIndices = EarcutNet.Earcut.Tessellate(flatData2D, new List<int>());

        // 4. 构建网格
        Mesh mesh = new Mesh();
        mesh.vertices = vertices3D.ToArray();
        mesh.triangles = earcutTriangleIndices.ToArray(); 

        // 如果上下南北颠倒，可以顺手 Reverse 一下，这里暂时注释掉，看效果决定
        // mesh.triangles = mesh.triangles.Reverse().ToArray(); 

        mf.mesh = mesh;
        mc.sharedMesh = mesh; // 赋予物理碰撞体

        // 5. 设置材质
        mr.material = new Material(defaultZoneMaterial);
        
        // 6. 挂载交互脚本并传递名字
        ZoneInteractive interactiveScript = zoneObj.AddComponent<ZoneInteractive>();
        // interactiveScript.zoneName = objName; // 传递 Part 名字
        
        // 如果需要，这里可以去掉 Part 名字（但建议保留，因为 Part 是独立的物理物体）
        interactiveScript.zoneName = objName.Split("_Part")[0]; 
    }

    // 你原有的核心数学：经纬度转三维坐标
    Vector3 LatLonToVector3(float lat, float lon, float radius)
    {
        // 1. 解决镜像翻转：将经度直接取反 (East 变成 West)
        lon = -lon; 

        // 2. 解决旋转错位：如果你说贴图转了90度，在这里补偿
        // 注意：因为上面经度取反了，这里的方向可能需要试一下是 +90 还是 -90
        lon += 0f;

        float latRad = lat * Mathf.Deg2Rad;
        float lonRad = lon * Mathf.Deg2Rad;
        float x = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
        float y = radius * Mathf.Sin(latRad);
        float z = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);
        return new Vector3(x, y, z);
    }
}
*/