using UnityEngine;

public class ZoneInteractive : MonoBehaviour
{
    public string zoneName;
    private Material myMaterial;
    
    // 平时完全透明
    private Color hiddenColor = new Color(255f, 255f, 255f, 0f); 
    // 你的高亮颜色
    public Color highlightColor = new Color(0f, 0.8f, 1f, 0.5f); 

    void Start()
    {
        myMaterial = GetComponent<MeshRenderer>().material;
        
        // 游戏开始时，强制隐形
        if (myMaterial.HasProperty("_BaseColor"))
        {
            myMaterial.SetColor("_BaseColor", hiddenColor);
        }
    }

    void OnMouseEnter()
    {
        if (myMaterial.HasProperty("_BaseColor"))
        {
            myMaterial.SetColor("_BaseColor", highlightColor);
        }
    }

    void OnMouseExit()
    {
        if (myMaterial.HasProperty("_BaseColor"))
        {
            myMaterial.SetColor("_BaseColor", hiddenColor);
        }
    }

    void OnMouseDown()
    {
        string cleanName = zoneName.Split("_Part")[0]; 
        Debug.Log("点击了区域: " + cleanName);
    }
}