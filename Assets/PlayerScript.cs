using Mirror;
using UnityEngine;

public class PlayerScript : NetworkBehaviour{
    private SceneScript sceneScript;

    //本地的玩家名字
    public TextMesh playerNameText;
    public GameObject floatingInfo;

    private Material playerMaterialClone;
    //SynceVar属性修饰一个变量，该变量只能在服务端被修改；
    //修改后会将信息同步到所有客户端，hook是一个回调；
    //在信息被同步后在所有客户端被调用
    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;


    void Awake(){
        //所有的Player都会运行这段代码
        sceneScript = GameObject.FindObjectOfType<SceneScript>();
    }

    [Command]
    public void CmdSendPlayerMessage(){
        if (sceneScript) 
            sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
    }

    [Command]
    public void CmdSetupPlayer(string _name, Color _col){
        //Player发送给服务器，服务器更新被SyncVar修饰的字段,之后在所有客户端调用对应的hook函数
        //player info sent to server, then server updates sync vars which handles it on all clients
        playerName = _name;
        playerColor = _col;
        sceneScript.statusText = $"{playerName} joined.";
    }

    //服务器上的玩家名字被修改后，本地作何修改
    void OnNameChanged(string _Old, string _New){
        playerNameText.text = playerName;
    }
    //服务器上的颜色被修改后，本地作何修改
    void OnColorChanged(Color _Old, Color _New){
        playerNameText.color = _New;
        playerMaterialClone = new Material(GetComponent<Renderer>().material);
        playerMaterialClone.color = _New;
        GetComponent<Renderer>().material = playerMaterialClone;
    }
    //本地玩家开始时
    public override void OnStartLocalPlayer(){
        sceneScript.playerScript = this;

        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 5, -10);

        floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
        floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + Random.Range(100, 999);
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        //将本地玩家的名字和颜色同步到服务器端
        CmdSetupPlayer(name, color);
    }
    //Command属性修饰一个函数，被修饰的函数在客户端被触发，但是在服务端被执行
        void Update(){
        if (!isLocalPlayer){
            // make non-local players run this
            floatingInfo.transform.LookAt(Camera.main.transform);
            return;
        }

        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * 110.0f;
        float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * 4f;

        transform.Rotate(0, moveX, 0);
        transform.Translate(0, 0, moveZ);
    }
}