using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Text;

public class DBManager : MonoBehaviour
{
    Dictionary<string, PosetData> _posetData;
    public string posetFolderPath;
    private string logPath;

    private Action OnRefreshHandler;

    public void SetRefreshHandler(Action ac)
    {
        OnRefreshHandler -= ac;
        OnRefreshHandler += ac;
    }

    public PosetData GetPosetData(string name)
    {
        if (_posetData == null)
            return null;

        if (_posetData.TryGetValue(name, out PosetData temp))
            return temp;

        // 없으면 null
        return null;
    }

    public List<PosetData> GetAllPoset()
    {
        if (_posetData == null)
        {
            _posetData = new Dictionary<string, PosetData>();
        }
        return new List<PosetData>(_posetData.Values);
    }


    private void Awake()
    {
        _posetData = new Dictionary<string, PosetData>();
        string myDocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        posetFolderPath = Path.Combine(myDocs, "PosetSimulator");
        logPath = Path.Combine(posetFolderPath, "log.txt");

        RefreshDB();
    }

    void InitializeFolder()
    {
        string myDocs = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        posetFolderPath = Path.Combine(myDocs, "PosetSimulator");

        if (!Directory.Exists(posetFolderPath))
        {
            Directory.CreateDirectory(posetFolderPath);
            Debug.Log("폴더 생성됨: " + posetFolderPath);
            CreateTemplateFile();
        }
    }
    private void CreateTemplateFile()
    {
        // Resources/Template.txt 불러오기
        TextAsset templateAsset = Resources.Load<TextAsset>("Template");

        if (templateAsset == null)
        {
            Debug.LogError("Resources/Template.txt 를 찾을 수 없음!");
            return;
        }

        string templatePath = Path.Combine(posetFolderPath, "Template.txt");

        // 그냥 그대로 써주면 됨
        File.WriteAllText(templatePath, templateAsset.text, Encoding.UTF8);
         


        for (int i = 1; i < 5; i++)
        {
            PosetData temp = Resources.Load<PosetData>($"Posets/Temp{i}");
            SaveSOToFile(temp, Path.Combine(posetFolderPath, $"Temp{i}.txt"));
        }

    }


    public void OpenFolder()
    {
        InitializeFolder();
        System.Diagnostics.Process.Start("explorer.exe", posetFolderPath);
    }



    public void RefreshDB()
    {
        InitializeFolder();
        foreach (var file in Directory.GetFiles(posetFolderPath, "*.txt"))
        {
            string fileName = Path.GetFileName(file);

            // Template.txt는 스킵
            if (fileName.Equals("Template.txt", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (fileName.Equals("log.txt", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string text = File.ReadAllText(file);
            using var sr = new StringReader(text);

            PosetData data = TextToSO(sr);

            // 파싱 실패(null)도 체크
            if (data == null)
            {
                string msg = $"파일 로딩 실패: {fileName}";
                Debug.LogError(msg);
                WriteLog(msg);
                continue; 
            }

            string key = Path.GetFileNameWithoutExtension(file);

            data.name = key;

            _posetData[key] = data;
        }
        OnRefreshHandler?.Invoke();
    }

    //문자열을 SO로 변환해주는 함수
    //문자열 형식은 헤더 ->(엔터)-> 값 순으로 나열. 단, 엣지의 경우에만 헤더 이후 반복적으로 리스트 나열

    private PosetData TextToSO(StringReader sr)
    {
        PosetData data = ScriptableObject.CreateInstance<PosetData>();

        string? line;
        string currentHeader = string.Empty;
        List<string> tempEdges = new();

        int lineNumber = 0;

        // 숫자-숫자 정규식
        Regex edgeRegex = new Regex(@"^\d+\-\d+$");

        try
        {
            while ((line = sr.ReadLine()) != null)
            {
                lineNumber++;
                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("%"))
                    continue;


                if (line.StartsWith("#"))
                {
                    currentHeader = line.TrimStart('#').Trim().ToLower();
                    continue;
                }

                switch (currentHeader)
                {
                    case "nodecount":
                        if (!int.TryParse(line, out data.NodeCount))
                            throw new Exception($"NodeCount는 int여야 함. 값: {line}");
                        break;

                    case "edges":
                        // 정규식 검사
                        if (!edgeRegex.IsMatch(line))
                            throw new Exception($"Edges 형식 오류 (숫자-숫자만 가능). 값: {line}");
                        tempEdges.Add(line);
                        break;

                    default:
                        throw new Exception($"알 수 없는 헤더: '{currentHeader}' (줄 내용: {line})");
                }
            }

            data.edges = tempEdges;
            data.isGrid = false;
            return data;
        }
        catch (Exception ex)
        {
            string msg1 = $"[Poset Parser Error] Line {lineNumber}: {ex.Message}";

            Debug.LogError(msg1);

            WriteLog(msg1);

            return null;

        }
    }

    public void SaveSOToFile(PosetData data, string path)
    {
        string text = SOToText(data);
        File.WriteAllText(path, text, Encoding.UTF8);
    }


    public string SOToText(PosetData data)
    {
        StringBuilder sb = new StringBuilder();

        // NodeCount
        sb.AppendLine("# NodeCount");
        sb.AppendLine(data.NodeCount.ToString());
        sb.AppendLine();

        // edges
        sb.AppendLine("# edges");
        foreach (var e in data.edges)
        {
            sb.AppendLine(e);
        }

        return sb.ToString();
    }

    private void WriteLog(string msg)
    {
        try
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(logPath, $"[{time}] {msg}\n");
        }
        catch (Exception e)
        {
            Debug.LogError("로그 파일 작성 실패: " + e.Message);
        }
    }


}
