using Mirror;
using UnityEngine;

public class PlayerScript : NetworkBehaviour {
    private SceneScript sceneScript;

    // Weapon
    private Weapon activeWeapon;
    private float weaponCooldownTime;

    // 本地的玩家名字
    public TextMesh playerNameText;
    public GameObject floatingInfo;

    private Material playerMaterialClone;

    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    private int selectedWeaponLocal = 0;
    public GameObject[] weaponArray;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 0;

    void Awake() {
        // 1. 获取场景脚本引用 (保留一种可靠的写法即可)
        // 建议使用 Find 这种过桥方式，防止找不到
        var refObj = GameObject.Find("SceneReference");
        if (refObj != null)
            sceneScript = refObj.GetComponent<SceneReference>().sceneScript;
        
        // 2. 隐藏所有武器，防止穿帮
        if (weaponArray != null) {
            foreach (var item in weaponArray) {
                if (item != null) item.SetActive(false);
            }
        }
    }

    // 新增：Start 强制刷新一次武器显示
    void Start() {
        if (isLocalPlayer) {
            // 手动调用一次，让 0 号武器显示出来
            OnWeaponChanged(activeWeaponSynced, activeWeaponSynced);
        }
    }

    void OnWeaponChanged(int _Old, int _New) {
        // 🛡️ 防报错保护：如果数组有问题，直接退出
        if (weaponArray == null) return;

        // 1. 禁用旧武器
        if (0 <= _Old && _Old < weaponArray.Length && weaponArray[_Old] != null) {
            weaponArray[_Old].SetActive(false);
        }

        // 2. 启用新武器
        if (0 <= _New && _New < weaponArray.Length && weaponArray[_New] != null) {
            weaponArray[_New].SetActive(true);

            // 更新当前活动武器的引用
            activeWeapon = weaponArray[_New].GetComponent<Weapon>();
            
            // 只有本地玩家才需要更新 UI 弹药数
            if (isLocalPlayer && sceneScript != null && activeWeapon != null) {
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
            }
        }
    }

    [Command]
    public void CmdChangeActiveWeapon(int newIndex) {
        activeWeaponSynced = newIndex;
    }

    [Command]
    public void CmdSendPlayerMessage() {
        if (sceneScript)
            sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
    }

    [Command]
    public void CmdSetupPlayer(string _name, Color _col) {
        playerName = _name;
        playerColor = _col;
        if (sceneScript) sceneScript.statusText = $"{playerName} joined.";
    }

    void OnNameChanged(string _Old, string _New) {
        playerNameText.text = playerName;
    }

    void OnColorChanged(Color _Old, Color _New) {
        playerNameText.color = _New;
        if (GetComponent<Renderer>() != null) {
            playerMaterialClone = new Material(GetComponent<Renderer>().material);
            playerMaterialClone.color = _New;
            GetComponent<Renderer>().material = playerMaterialClone;
        }
    }

    public override void OnStartLocalPlayer() {
        sceneScript.playerScript = this;

        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 5, -10);

        floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
        floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + Random.Range(100, 999);
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);
    }

    void Update() {
        if (!isLocalPlayer) {
            floatingInfo.transform.LookAt(Camera.main.transform);
            return;
        }

        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * 110.0f;
        float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * 4f;

        transform.Rotate(0, moveX, 0);
        transform.Translate(0, 0, moveZ);

        // 切换武器
        if (Input.GetButtonDown("Fire2")) {
            selectedWeaponLocal += 1;
            if (selectedWeaponLocal >= weaponArray.Length)
                selectedWeaponLocal = 0;
            CmdChangeActiveWeapon(selectedWeaponLocal);
        }

        // 开火逻辑
        if (Input.GetButtonDown("Fire1")) {
            // 🌟 修复逻辑：必须枪存在、枪是显示的、且有子弹才能开火
            if (activeWeapon != null && activeWeapon.gameObject.activeInHierarchy && Time.time > weaponCooldownTime && activeWeapon.weaponAmmo > 0) {
                weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;
                activeWeapon.weaponAmmo -= 1;
                if (sceneScript != null) sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                CmdShootRay();
            }
        }
    }

    //修复：把这两个函数移到了 Update 外面
    [Command]
    void CmdShootRay() {
        RpcFireWeapon();
    }

    [ClientRpc]
    void RpcFireWeapon() {
        if (activeWeapon != null && activeWeapon.weaponBullet != null) {
            GameObject bullet = Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
            bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
            Destroy(bullet, activeWeapon.weaponLife);
        }
    }
}