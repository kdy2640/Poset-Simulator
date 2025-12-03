using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PosetData", menuName = "Poset/PosetData")]
public class PosetData : ScriptableObject
{
    [Header("격자 프리셋 여부")]
    public bool isGrid;

    [Header("isGrid==True일때")]
    public int Width;
    public int Height;

    [Header("노드 개수: 맨 아래가 1, 맨 위에가 NodeCount")]
    public int NodeCount;

    [Header("엣지 정보 (문자열 리스트)")]
    [Tooltip("각 항목은 'from,to' 형식. 예: \"1-2\", \"1-3\" ...")]
    public List<string> edges = new List<string>();


    /// <summary>
    /// 엣지를 (from,to) 튜플로 변환
    /// </summary>
    public List<Vector2Int> GetParsedEdges()
    {
        var parsed = new List<Vector2Int>();
        foreach (var edge in edges)
        {
            var tokens = edge.Split('-');
            if (tokens.Length == 2
                && int.TryParse(tokens[0].Trim(), out int from)
                && int.TryParse(tokens[1].Trim(), out int to))
            {
                parsed.Add(new Vector2Int(from, to));
            }
        }
        return parsed;
    }

    public static PosetData GetGridPosetData(int n, int m)
    {
        // ScriptableObject 생성
        PosetData poset = ScriptableObject.CreateInstance<PosetData>();
        poset.isGrid = true;

        poset.edges = new List<string>();
        poset.Width = n;
        poset.Height = m;
        poset.NodeCount = n * m;

        // 격자 노드 번호는 (x,y) 기준으로
        // x는 가로 1~n, y는 세로 1~m
        // 아래 행이 y=1, 위 행이 y=m
        // 노드 번호 = (y-1)*n + x

        for (int y = 1; y <= m; y++)
        {
            for (int x = 1; x <= n; x++)
            {
                int current = (y - 1) * n + x;

                // 오른쪽으로 엣지 (가로 연결)
                if (x < n)
                {
                    int right = (y - 1) * n + (x + 1);
                    poset.edges.Add($"{current}-{right}");
                }

                // 위쪽으로 엣지 (세로 연결)
                if (y < m)
                {
                    int up = y * n + x;
                    poset.edges.Add($"{current}-{up}");
                }
            }
        }

        return poset;
    }

}

