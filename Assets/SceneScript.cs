using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class SceneScript : NetworkBehaviour{
    //本地显示的欢迎语
    public Text canvasStatusText;
    public PlayerScript playerScript;

    //服务器端的欢迎语
    [SyncVar(hook = nameof(OnStatusTextChanged))]
    public string statusText;
    //当服务器上的statusText改变时，调用所有客户端
    void OnStatusTextChanged(string _Old, string _New)        {
        //called from sync var hook, to update info on screen for all players
        canvasStatusText.text = _New;
    }

    public void ButtonSendMessage()        {
        if (playerScript != null)  
            playerScript.CmdSendPlayerMessage();
    }
}