using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using UnityEngine.Events;

namespace LethalLanFixFix;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        SceneManager.sceneLoaded += OnSceneLoaded;

        var harmonyInstance = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmonyInstance.PatchAll();
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "MainMenu":
                MenuManagerPatches.AddIPInputMenu();
                Logger.LogInfo("IP Input menu added");

                MenuManagerPatches.Patch_StartLAN();
                Logger.LogInfo("StartLAN button swapped");
                break;
        }
    }
}

public class MenuManagerPatches : MonoBehaviour
{
    public static void Patch_StartLAN()
    {
        var startLAN_button = GameObject.Find("Canvas/MenuContainer/MainButtons/StartLAN");
        Debug.Log(startLAN_button);

        startLAN_button.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
        startLAN_button.GetComponent<Button>().onClick
            .AddListener(() => OnStartLANClick());
    }

    private static void OnStartLANClick()
    {
        // Show IP Input Menu
        GameObject.Find("Canvas/MenuContainer/IPInputMenu").gameObject.SetActive(true);
    }

    public static void AddIPInputMenu()
    {
        var menu = IPInput.Create();
        Debug.Log(menu);
        var menuContainer = GameObject.Find("Canvas/MenuContainer");
        Debug.Log(menuContainer);
        menu.transform.SetParent(menuContainer.transform);
    }

    [HarmonyPatch(typeof(MenuManager), "LAN_HostSetAllowRemoteConnections")]
    public static class MenuManager_LAN_HostSetAllowRemoteConnections
    {
        [HarmonyPostfix]
        public static void Postfix(MenuManager __instance)
        {
            if (Traverse.Create(__instance).Field("startingAClient").Equals(false)) return;

            Debug.Log($"Listening to LAN server: {IPInput.IP_Address}");
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IPInput.IP_Address;
        }
    }
}

public class IPInput : MonoBehaviour
{
    public static string IP_Address = "192.168.20.4";

    public static GameObject Create() 
    {
        var menuContainer = GameObject.Find("Canvas/MenuContainer");
        var LANWarning = GameObject.Find("Canvas/MenuContainer/LANWarning");

        // prefab LANWarning
        GameObject menu = Instantiate(LANWarning, LANWarning.transform.position, LANWarning.transform.rotation, 
            menuContainer.transform);

        var menu_panel = menu.transform.Find("Panel");

        // Change default notification to simple header        
        GameObject headerText = menu_panel.Find("NotificationText").gameObject;

        // IP Field for inserting address 
        var inputField = Resources.FindObjectsOfTypeAll<TMP_InputField>()
            .Where(obj => obj.name == "ServerNameField")
            .ElementAt(0)
            .gameObject;
        GameObject ip_field = Instantiate(inputField, menu_panel.position+new Vector3(0,2,0), menu_panel.rotation,
            menu_panel);

        // LANWarning.Panel.Confirm
        GameObject connect_button = menu.transform.Find("Panel/Confirm").gameObject;
        
        // prefab LANWarning.Panel.Confirm
        GameObject cancel_button = Instantiate(connect_button, connect_button.transform.position+new Vector3(0,6,0), 
            connect_button.transform.rotation,
            menu_panel);
   
        menu.name = "IPInputMenu";

        headerText.name = "HeaderText";
        headerText.GetComponent<TextMeshProUGUI>().text = "Enter LAN Server IP Address";

        ip_field.name = "IPInputField";
        ip_field.GetComponent<TMP_InputField>().placeholder.GetComponent<TextMeshProUGUI>().text = "127.0.0.1";
        ip_field.GetComponent<TMP_InputField>().onValueChanged = new TMP_InputField.OnChangeEvent();

        connect_button.name = "Connect";
        connect_button.GetComponentInChildren<TextMeshProUGUI>().text = "[ Connect ]";
        // connect_button.GetComponent<Button>().onClick = new Button.ButtonClickedEvent() // it will close itself by default
        // set ip from input field and start client
        connect_button.GetComponent<Button>().onClick
            .AddListener(
                () => {
                    if (ip_field.GetComponent<TMP_InputField>().text != "")
                        IP_Address = ip_field.GetComponent<TMP_InputField>().text;
                    else 
                        IP_Address = "127.0.0.1";
                    GameObject.Find("MenuManager").GetComponent<MenuManager>().StartAClient();
                }
            );

        cancel_button.name = "Cancel";
        cancel_button.GetComponentInChildren<TextMeshProUGUI>().text = "[ Cancel ]";

        return menu;
    }
}