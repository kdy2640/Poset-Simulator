using System;
using System.Collections;
using System.Collections.Generic;  
using Unity.Mathematics; 
using UnityEngine;
using UnityEngine.UI;

public class GraphController : MonoBehaviour
{
    [SerializeField] private GameObject NodePrefab;
    [SerializeField] private GameObject EdgePrefab;
    [SerializeField] float ButtonSize = 100f;
    [SerializeField] float ySpacing = 100f;
    [SerializeField] float xSpacing = 100f;
    private GameManager manager;
    private Vector2 startPos = new Vector2(0,-200); // 맨 아래 맨 왼쪽 노드 위치 기준

    // id → NodeBehaviour 매핑
    private Dictionary<int, NodeBehaviour> nodeMap = new Dictionary<int, NodeBehaviour>();
    private List<int> NowSelected = new List<int>();
    private PosetData Pdata;

    private Action<int> OnNodeClickedAction;
    private bool[,] AdjacencyMatrix;

    private void Start()
    {
        manager = GameManager.Instance;
    }


    ///             외부 인터페이스
    public void ShowGraph(PosetData data)
    {
        ClearGraph();
        Pdata = data;
        LoadGraph(data);
        FillAdjacency(data);
        if (!data.isGrid) AdjustPresetPoset();
        OnNodeClickedAction?.Invoke(-1);
    }
    public void SetNodeClickAction(Action<int> ac)
    {
        OnNodeClickedAction -= ac;
        OnNodeClickedAction += ac;
    }

    
    ///             내부 구현용
    //클릭 이벤트
    private void OnNodeClicked(int id)
    {
        NodeBehaviour nowNode = nodeMap[id];
        int index = NowSelected.IndexOf(id);

        if (index == -1)
        {
            // 아직 선택 안 됨 → 추가
            NowSelected.Add(id);
        }
        else
        {
            // 선택 된 상태 -> 이후 링크 모두 끊음
            List<int> toRemove = NowSelected.GetRange(index, NowSelected.Count - index);
            toRemove.ForEach(nid =>
            {
                nodeMap[nid].OnLabeled(-1);
                nodeMap[nid].SetAvailable();
                nodeMap[nid].ToList.ForEach(x => x.SetAvailable());
            });
            NowSelected.RemoveRange(index, NowSelected.Count - index);
        }
        RefreshGraph();
        OnNodeClickedAction.Invoke(MaxDist);
    }
    //그래프 초기화
    private void ClearGraph()
    {
        foreach (Transform child in transform.GetChild(0))
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in transform.GetChild(1))
        {
            Destroy(child.gameObject);
        }

        nodeMap.Clear();
        NowSelected.Clear();
        MaxDist = -1;
        EndId = (0, 0);
    }
    //그래프 갱신
    private void RefreshGraph()
    {
        for (int i = 0; i < NowSelected.Count; i++)
        {
            int id = NowSelected[i];
            NodeBehaviour nowNode = nodeMap[id];
            nowNode.OnLabeled(i + 1);
            nowNode.SetAvailable();
            foreach (NodeBehaviour item in nowNode.ToList)
            {
                item.SetAvailable();
            }
        }
        MaxDist = FindMax();
    }
    //그래프 작성
    private void LoadGraph(PosetData data)
    {
        nodeMap.Clear();

        // 노드 생성
        for (int id = 1; id <= data.NodeCount; id++)
        {
            GameObject go = Instantiate(NodePrefab, transform.GetChild(1));
            go.name = $"Node_{id}";
            NodeBehaviour nb = go.GetComponent<NodeBehaviour>();
            nb.Init(id);
            nb.SetSize(ButtonSize);
            nb.SetClickAction(OnNodeClicked);
            nb.OnLabeled(-1);
             
            // 위치 계산
            Vector2 pos;
            if (data.isGrid)
            {
                int x = (id - 1) % data.Width;
                int y = (id - 1) / data.Width;

                // 다이아몬드(45도 기울인 격자)
                float step = xSpacing * 0.7071f; // 1/√2 ≈ 0.7071
                float rotatedX = (x - y) * step;
                float rotatedY = (x + y) * step;

                pos = startPos + new Vector2(rotatedX, rotatedY);
            }

            else
            {
                // 일반 poset: 일렬로 배치
                pos = startPos + new Vector2((id - 1) * xSpacing, 0);
            }

            go.transform.localPosition = pos;
            nodeMap[id] = nb;
        } 

        // 엣지 생성
        var parsedEdges = data.GetParsedEdges();
        int edgeIndex = 0;
        foreach (var edge in parsedEdges)
        {
            int from = edge.x;
            int to = edge.y;

            if (!nodeMap.ContainsKey(from) || !nodeMap.ContainsKey(to))
            {
                Debug.LogWarning($"엣지 연결 실패: {from} -> {to}");
                continue;
            }

            NodeBehaviour fromNode = nodeMap[from];
            NodeBehaviour toNode = nodeMap[to];

            fromNode.ToList.Add(toNode);
            toNode.FromList.Add(fromNode);
            if(data.isGrid)
            {
                LoadEdge($"Edge_{edgeIndex++}",
                    fromNode.transform.position,
                    toNode.transform.position,
                    Color.yellow);
            }
        }

        // 초기 상태 세팅
        foreach (var kv in nodeMap)
        {
            kv.Value.SetAvailable();
        }
    }

    private void AdjustPresetPoset()
    {
        // 1. 각 노드의 indegree 계산
        Dictionary<int, int> indegree = new Dictionary<int, int>();
        foreach (var kv in nodeMap)
            indegree[kv.Key] = 0;

        foreach (var kv in nodeMap)
        {
            foreach (var toNode in kv.Value.ToList)
                indegree[toNode.id]++;
        }

        // 2. 위상정렬 + 레벨 분리
        List<List<int>> layers = new List<List<int>>();
        Queue<int> q = new Queue<int>();

        // indegree 0 → 첫 레벨
        foreach (var kv in indegree)
            if (kv.Value == 0) q.Enqueue(kv.Key);

        Dictionary<int, int> level = new Dictionary<int, int>();
        int currentLevel = 0;

        while (q.Count > 0)
        {
            int size = q.Count;
            List<int> layer = new List<int>();

            for (int i = 0; i < size; i++)
            {
                int node = q.Dequeue();
                layer.Add(node);
                level[node] = currentLevel;

                foreach (var next in nodeMap[node].ToList)
                {
                    indegree[next.id]--;
                    if (indegree[next.id] == 0)
                        q.Enqueue(next.id);
                }
            }

            layers.Add(layer);
            currentLevel++;
        }

        // 3. 위치 배치 (위쪽이 indegree 0, 아래로 갈수록 깊은 노드)
        float startY = startPos.y;
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            float rowWidth = (layer.Count - 1) * xSpacing;
            float startX = -rowWidth / 2f;

            for (int j = 0; j < layer.Count; j++)
            {
                int id = layer[j];
                Vector2 pos = new Vector2(startX + j * xSpacing, startY + i * ySpacing);
                nodeMap[id].transform.localPosition = pos;
            }
        }
        // 4. 엣지 재조정

        // 엣지 생성
        var parsedEdges = Pdata.GetParsedEdges();
        int edgeIndex = 0;
        foreach (var edge in parsedEdges)
        {
            int from = edge.x;
            int to = edge.y;

            if (!nodeMap.ContainsKey(from) || !nodeMap.ContainsKey(to))
            {
                Debug.LogWarning($"엣지 연결 실패: {from} -> {to}");
                continue;
            }

            NodeBehaviour fromNode = nodeMap[from];
            NodeBehaviour toNode = nodeMap[to];

            fromNode.ToList.Add(toNode);
            toNode.FromList.Add(fromNode);

            LoadEdge($"Edge_{edgeIndex++}",
                fromNode.transform.position,
                toNode.transform.position,
                Color.yellow);
        }
    }


    GameObject RedEdge; 
    private int MaxDist = 0;
    private  (int, int) EndId = (0, 0);

    //DFS로 내부순환
    private void FillAdjacency(PosetData data)
    {
        AdjacencyMatrix = new bool[data.NodeCount+1, data.NodeCount+1];
        Stack<int> dfs = new Stack<int>();
        for (int nowID = 1; nowID <= data.NodeCount; nowID++)
        {
            dfs = new Stack<int>();
            dfs.Push(nowID);
            bool[] visited = new bool[data.NodeCount+1];
            while(dfs.Count > 0)
            {
                int nowNode = dfs.Pop();
                if (visited[nowNode]) continue;
                visited[nowNode] = true;
                NodeBehaviour nowBeh = nodeMap[nowNode];
                for (int i = 0; i < nowBeh.ToList.Count; i++)
                {
                    int nextNode = nowBeh.ToList[i].id;
                    if (visited[nextNode]) continue;
                    dfs.Push(nextNode);
                }
            }
            for (int i = 0; i <= data.NodeCount; i++)
            {
                AdjacencyMatrix[nowID, i] = visited[i];
            }
        }
    }
    private int FindMax()
    {
        int max = -1;
        for (int i = 0; i < NowSelected.Count; i++)
        {
            int nowID = NowSelected[i];
            for (int j = i + 1; j < NowSelected.Count; j++)
            {
                int nextID = NowSelected[j];
                if (AdjacencyMatrix[nowID, nextID]) continue;
                if(max < j-i)
                {
                    max = j - i;
                    EndId = (nowID, nextID);
                }
            }
        }
        if (RedEdge != null)
        {
            Destroy(RedEdge);
        }
        if (max > 0)
        {
            RedEdge = LoadEdge("Edge_Max", nodeMap[EndId.Item1].transform.position, nodeMap[EndId.Item2].transform.position, Color.red);
        }
        return max;
    }

    [SerializeField] float thickness = 10f;

    private GameObject LoadEdge(string name, Vector3 startWorld, Vector3 endWorld, Color color)
    {
        // 엣지 생성
        GameObject go = Instantiate(EdgePrefab, transform.GetChild(0));
        go.name = name;
        go.GetComponent<Image>().color = color;

        RectTransform edgeRect = go.GetComponent<RectTransform>();
        RectTransform parent = transform.GetChild(0).GetComponent<RectTransform>();

        // 월드 → 로컬 변환
        Vector2 start = parent.InverseTransformPoint(startWorld);
        Vector2 end = parent.InverseTransformPoint(endWorld);

        // 배치
        edgeRect.localPosition = start;

        // 방향
        Vector2 dir = end - start;

        // 각도 (UI는 z축 회전 그대로 가능)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        edgeRect.localRotation = Quaternion.Euler(0, 0, angle);

        // 길이는 그냥 로컬거리 그대로 쓰면 됨
        float distance = dir.magnitude;
        edgeRect.sizeDelta = new Vector2(thickness, distance);

        return go;
    }



    // 기타
    private int GetId(int x, int y)
    {
        return y * Pdata.Width + x + 1;
    }
    private (int, int) GetIndex(int id)
    {
        int x = (id-1) % Pdata.Width;
        int y = (id-1)/ Pdata.Width;
        return (x, y);
    }

}
