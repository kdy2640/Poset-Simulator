using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField xText;
    [SerializeField] private TMP_InputField yText;
    [SerializeField] private TextMeshProUGUI MaxDist;
    [SerializeField] private Button ChangeModeButton;
    [SerializeField] private TMP_Dropdown Preset;
    [SerializeField] private Button DataOpenButton;
    [SerializeField] private Button DataRefreshButton;

    string GridMessage = "Click to change \n Preset Mode";
    string PresetMessage = "Click to change \n Grid Mode";
    string ToolMessage = "Click to change \n Back Main";

    enum GameMode
    {
        Grid, Preset, Tool
    }
    GameMode nowEnum = GameMode.Grid;
    public Canvas canvas;

    private GameManager manager;
    private DBManager db;
    List<PosetData> assets;
    // Start is called before the first frame update
    void Start()
    {
        manager = GameManager.Instance;
        db = manager.DB;
        db.SetRefreshHandler(OnDBRefresh);
        db.RefreshDB();

        ChangeMode(GameMode.Grid);
    }

    void OnDBRefresh()
    {
        assets = db.GetAllPoset();
        Preset.ClearOptions();
        for (int i = 0; i < assets.Count; i++)
        {
            Preset.options.Add(new TMP_Dropdown.OptionData(assets[i].name));
        }
    }
    public void InitializeUI()
    {
        int n; int m;
        switch(nowEnum)
        {
            case GameMode.Grid:
                {
                    bool A = int.TryParse(xText.text.Trim(), out n);
                    bool B = int.TryParse(yText.text.Trim(), out m);
                    if (A && B)
                    {
                        PosetData grid = PosetData.GetGridPosetData(n, m);
                        manager.graphController.SetNodeClickAction(SetDist);
                        manager.graphController.ShowGraph(grid);
                    }
                    break;
                }
            case GameMode.Preset:
                {
                    string nowSelect = Preset.options[Preset.value].text;
                    PosetData pose = db.GetPosetData(nowSelect);
                    manager.graphController.SetNodeClickAction(SetDist);
                    manager.graphController.ShowGraph(pose);
                    break;
                }
            case GameMode.Tool:
                {
                    break;
                }
        }
    }
    public void OnClickChageMode()
    {
        switch (nowEnum)
        {
            case GameMode.Grid:
                {
                    ChangeMode(GameMode.Preset);
                    ChangeModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = PresetMessage;

                    break;
                }
            case GameMode.Preset:
                {
                    ChangeMode(GameMode.Grid);
                    ChangeModeButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = GridMessage;

                    break;
                }
            case GameMode.Tool:
                {
                    break;
                }
        }
    }
    public void SetDist(int value)
    {
        MaxDist.text = "Now Dist:" + value.ToString();
    }

    public void OnClickExitButton()
    {
        Application.Quit();
    }
    private void ChangeMode(GameMode mode)
    { 
        nowEnum = mode;
        switch (nowEnum)
        {
            case GameMode.Grid:
            {
                xText.gameObject.SetActive(true);
                yText.gameObject.SetActive(true);
                Preset.gameObject.SetActive(false);
                DataOpenButton.gameObject.SetActive(false);
                DataRefreshButton.gameObject.SetActive(false);
                    break;
            }
            case GameMode.Preset:
            {
                xText.gameObject.SetActive(false);
                yText.gameObject.SetActive(false);
                Preset.gameObject.SetActive(true);
                DataOpenButton.gameObject.SetActive(true);
                DataRefreshButton.gameObject.SetActive(true);
                db.RefreshDB();
                break;
            }
            case GameMode.Tool:
            {
                break;
            }
            default:
                break;
        }
    }


}
