using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeBehaviour : MonoBehaviour
{
    public int id { get; private set; }
    public List<NodeBehaviour> FromList = new List<NodeBehaviour>();
    public List<NodeBehaviour> ToList = new List<NodeBehaviour>();
    private Action<int> OnButtonClickedAction;
    private TextMeshProUGUI text;
    Button button;

    GameManager manager;
    public void SetClickAction(Action<int> action)
    {
        OnButtonClickedAction -= action;
        OnButtonClickedAction += action;
    }

    public void SetSize(float size)
    {
        GetComponent<RectTransform>().sizeDelta = size * Vector2.one;
    }
    public void OnLabeled(int label)
    {
        if(label == -1)
        {
            text.text = "";
        }
        else
        {
            text.text = label.ToString();
        }
    }
    public void SetAvailable()
    {
        if(IsAvailable())
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }
    public bool IsAvailable()
    {
        foreach (NodeBehaviour item in FromList)
        {
            if (item.text.text == "") return false;
            if (item.button.interactable == false) return false;
        }
        return true;
    }

    public void Init(int newId)
    {
        id = newId;
        FromList.Clear();
        ToList.Clear();


        manager = GameManager.Instance;
        text = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        button = GetComponent<Button>();

        button.onClick.RemoveAllListeners(); // 혹시 모를 중복 방지
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        OnButtonClickedAction?.Invoke(id);
    }

}
