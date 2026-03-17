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