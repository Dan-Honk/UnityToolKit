using UnityEngine;
using System.IO;
using UnityEditor;

public class TimelineEffectFinder : MonoBehaviour
{
    private static string targetPath = "Assets/m_MyMesh/";

    [MenuItem("Assets/Effect/找回预制内丢失的物体")]
    public static void ReadPrefab()
    {
        if (Selection.activeGameObject != null)
        {
            string relativePath = AssetDatabase.GetAssetPath(Selection.activeGameObject);
            if (!string.IsNullOrEmpty(relativePath))
            {
                string path = GetFullPath(relativePath);

                ParsePrefab(path);
            }
        }
    }

    public static string GetFullPath(string relativePath)
    {
        int index = relativePath.IndexOf(Path.DirectorySeparatorChar);
        if (index == -1)
        {
            index = relativePath.IndexOf(Path.AltDirectorySeparatorChar);
        }
        string path = Application.dataPath + relativePath.Substring(index);
        return path;
    }

    public static void ParsePrefab(string path)
    {
        string[] lines = File.ReadAllLines(path);
        string rootID = "400000";

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.StartsWith("  m_CorrespondingSourceObject: {fileID: "))
            {
                int endIndex = line.IndexOf(',');
                if (endIndex == -1)
                    continue;
                string fileID = line.Substring(40, endIndex - 40);
                int guidEndIndex = line.IndexOf(',', endIndex + 1);
                int guidStartIndex = endIndex + 8;
                string guid = line.Substring(guidStartIndex, guidEndIndex - guidStartIndex);
                if (fileID != "0" && CheckIsLost(fileID, guid))
                {
                    //取剩余部分拼接回去
                    string restString = line.Substring(endIndex, line.Length - endIndex);
                    Debug.LogError("not exist,parent = " + fileID);
                    lines[i] = "  m_CorrespondingSourceObject: {fileID: " + rootID + restString;
                }
            }
        }
        //重写预设文件
        File.WriteAllLines(path, lines);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(Selection.activeGameObject));
        Debug.Log("find over");
    }

    public static bool CheckIsLost(string fileID, string guid)
    {
        bool isLost = false;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (path != "" && path.StartsWith(targetPath))
        {
            //解析meta文件
            string metaPath = GetFullPath(path) + ".meta";
            string[] metaLines = File.ReadAllLines(metaPath);
            string childNameInMeta = "";
            for (int i = 0; i < metaLines.Length; i++)
            {
                string line = metaLines[i];
                if (line.StartsWith("    " + fileID))
                {
                    int frontPartCount = 4 + fileID.Length + 2;
                    childNameInMeta = line.Substring(frontPartCount, line.Length - frontPartCount);
                }
            }
            if (childNameInMeta.Contains("RootNode"))
            {
                return isLost;
            }
            if (childNameInMeta != "")
            {
                Transform rootTrans = Selection.activeGameObject.transform;
                string perfabName = GetPerfabNameByPath(path);
                //先找到对应的嵌套预设节点
                Transform perfabTrans = FindInChild(rootTrans, perfabName);
                Transform childTrans = FindInChild(perfabTrans, childNameInMeta);
                if (childTrans == null)
                    isLost = true;
            }
        }
        return isLost;
    }

    public static Transform FindInChild(Transform rootTrans, string name)
    {
        if (rootTrans.name == name)
        {
            return rootTrans;
        }
        if (rootTrans.childCount < 1)
        {
            return null;
        }
        Transform target = null;
        for (int i = 0; i < rootTrans.childCount; i++)
        {
            Transform t = rootTrans.GetChild(i).transform;
            target = FindInChild(t, name);
            if (target != null)
            {
                break;
            }
        }
        return target;
    }

    public static string GetPerfabNameByPath(string path)
    {
        int startIndex = path.LastIndexOf(Path.AltDirectorySeparatorChar) + 1;
        string allName = path.Substring(startIndex, path.Length - startIndex);
        string fileName = allName.Substring(0, allName.Length - 4);
        return fileName;
    }
}
