using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static GameManager _instance;

    public UIManager UI;
    public DBManager DB;    
    public GraphController graphController;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에 GameManager가 없으면 자동으로 생성
                GameObject obj = new GameObject("GameManager");
                _instance = obj.AddComponent<GameManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    // Awake에서 중복 방지
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

}
