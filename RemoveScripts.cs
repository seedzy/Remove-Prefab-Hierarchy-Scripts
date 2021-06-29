using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public class RemoveScripts 
{
    [MenuItem("GameTools/遍历Hierarchy")]
    static void GetAllSceneObjectsWithInactive()
    {
        var allGos = Resources.FindObjectsOfTypeAll(typeof(GameObject));
        var previousSelection = Selection.objects;
        Selection.objects = allGos;
        var selectedTransforms = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);
        Selection.objects = previousSelection;
        foreach(var trans in selectedTransforms)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(trans.gameObject);
            var nav = trans.GetComponent<NavMeshAgent>();
            if (nav != null)
            {
                Undo.DestroyObjectImmediate(nav);
            }
        }
    }
    
    [MenuItem("GameTools/遍历project")]
    static void CheckMissingScripts()
    {
        List<string> listString = new List<string>();

        CollectFiles(@"D:\Unity\project\BossGame\Assets", listString);

        for (int i = 0; i < listString.Count; i++)
        {
            string Path = listString[i];

            float progressBar = (float)i / listString.Count;

            EditorUtility.DisplayProgressBar("Check Missing Scripts", "The progress of ： " + ((int)(progressBar * 100)).ToString() + "%", progressBar);

            if (!Path.EndsWith(".prefab"))//只处理prefab文件
            {
                continue;
            }

            //Path = ChangeFilePath(Path);
            Path = Path.Replace(@"D:\Unity\project\BossGame\", "");
            Debug.LogWarning(Path);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            
            if (prefab == null)
            {
                Debug.LogError("空的预设 ： " + Path);
            
                continue;
            }
            
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>();
            //获取所有的子节点;
            
            for (int j = 0; j < transforms.Length; j++)
            {
                GameObject obj = transforms[j].gameObject;
            
                // var components = obj.GetComponents<Component>();
                // //获取对象所有的Component组件
                // //所有继承MonoBehaviour的脚本都继承Component
                //
                // for (int k = 0; k < components.Length; k++)
                // {
                //     if (components[k] == null)
                //     {
                //         Debug.LogError("这个预制中有空的脚本 ：" + tmpAssetImport.assetPath + " 挂在对象 : " + obj.name + " 上");
                //     }
                // }
                Debug.LogError("空的预设 ： ");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            }
        }
        EditorUtility.ClearProgressBar();
    }

    //改变路径  
    //这种格式的路径 "C:/Users/XX/Desktop/aaa/New Unity Project/Assets\\a.prefab" 改变成 "Assets/a.prefab"
    static string ChangeFilePath(string path)
    {
        path = path.Replace("\\\\", "/");
        path = path.Replace(Application.dataPath + "/", "");
        path = "Assets/" + path;

        return path;
    }

    //迭代获取文件路径;
    static void CollectFiles(string directory, List<string> outfiles)
    {
        string[] files = Directory.GetFiles(directory);

        outfiles.AddRange(files);

        string[] childDirectories = Directory.GetDirectories(directory);

        if (childDirectories != null && childDirectories.Length > 0)
        {
            for (int i = 0; i < childDirectories.Length; i++)
            {
                string dir = childDirectories[i];
                if (string.IsNullOrEmpty(dir)) continue;
                CollectFiles(dir, outfiles);
            }
        }
    }
    // static void delete () 
    // {
    //     GameObject prefab = AssetDatabase.l<GameObject>("Assets/GameObject.prefab");
    //     
    //     
    //
    //     // //删除MeshCollider
    //     // MeshCollider [] meshColliders = prefab.GetComponentsInChildren<MeshCollider>(true);
    //     // foreach(MeshCollider meshCollider in meshColliders){
    //     //
    //     //     GameObject.DestroyImmediate(meshCollider,true);
    //     // }
    //     //
    //     // //删除空的Animation组件
    //     // Animation [] animations = prefab.GetComponentsInChildren<Animation>(true);
    //     // foreach(Animation animation in animations){
    //     //     if(	animation.clip == null){
    //     //         GameObject.DestroyImmediate(animation,true);
    //     //     }
    //     //
    //     // }
    //     //
    //     // //删除missing的脚本组件
    //     // MonoBehaviour [] monoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
    //     // foreach(MonoBehaviour monoBehaviour in monoBehaviours){
    //     //
    //     //
    //     //     if(monoBehaviour == null){
    //     //         Debug.Log("有个missing的脚本");
    //     //         //GameObject.DestroyImmediate(monoBehaviour,true);
    //     //
    //     //     }
    //     // }
    //     //
    //     // //遍历Transform的名子， 并且给某个游戏对象添加一个脚本
    //     // Transform [] transforms = prefab.GetComponentsInChildren<Transform>(true);
    //     // foreach(Transform transfomr in transforms){
    //     //     if(transfomr.name == "GameObject (1)"){
    //     //         Debug.Log(transfomr.parent.name);
    //     //         transfomr.gameObject.AddComponent<BoxCollider>();
    //     //         return;
    //     //     }
    //     //
    //     // }
    //     // //遍历Transform的名子， 删除某个GameObject节点
    //     // foreach(Transform transfomr in transforms){
    //     //     if(transfomr.name == "GameObject (2)"){
    //     //         GameObject.DestroyImmediate(transfomr.gameObject,true);
    //     //         return;
    //     //     }
    //     //
    //     // }
    //     // EditorUtility.SetDirty(prefab);
    // }
    [MenuItem("GameTools/Remove Missing Scripts Recursively Visit Prefabs")]
     private static void FindAndRemoveMissingInSelected()
     {
         // EditorUtility.CollectDeepHierarchy does not include inactive children
         var deeperSelection = Selection.gameObjects.SelectMany(go => go.GetComponentsInChildren<Transform>(true))
             .Select(t => t.gameObject);
         var prefabs = new HashSet<Object>();
         int compCount = 0;
         int goCount = 0;
         foreach (var go in deeperSelection)
         {
             int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
             if (count > 0)
             {
                 if (PrefabUtility.IsPartOfAnyPrefab(go))
                 {
                     RecursivePrefabSource(go, prefabs, ref compCount, ref goCount);
                     count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                     // if count == 0 the missing scripts has been removed from prefabs
                     if (count == 0)
                         continue;
                     // if not the missing scripts must be prefab overrides on this instance
                 }
 
                 Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
                 GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                 
                 compCount += count;
                 goCount++;
             }
         }
 
         Debug.Log($"Found and removed {compCount} missing scripts from {goCount} GameObjects");
     }
 
     // Prefabs can both be nested or variants, so best way to clean all is to go through them all
     // rather than jumping straight to the original prefab source.
     private static void RecursivePrefabSource(GameObject instance, HashSet<Object> prefabs, ref int compCount,
         ref int goCount)
     {
         var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
         // Only visit if source is valid, and hasn't been visited before
         if (source == null || !prefabs.Add(source))
             return;
 
         // go deep before removing, to differantiate local overrides from missing in source
         RecursivePrefabSource(source, prefabs, ref compCount, ref goCount);
 
         int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(source);
         if (count > 0)
         {
             Undo.RegisterCompleteObjectUndo(source, "Remove missing scripts");
             GameObjectUtility.RemoveMonoBehavioursWithMissingScript(source);
             compCount += count;
             goCount++;
         }
     }

     [MenuItem("GameTools/Remove Missing Scripts prefabs")]
     public static void DeleteProjectPanelMissScript()
     {
         Debug.LogWarning("Start");

         var targets = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets"})
             .Select(AssetDatabase.GUIDToAssetPath);;
         int count = 0;
         foreach (var variable in targets)
         {
             bool isMiss = false;
             GameObject targetGo = PrefabUtility.LoadPrefabContents(variable);
             var gos = targetGo.GetComponentsInChildren<Transform>(true);
             foreach (var go in gos)
             {
                 if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject) > 0)
                 {
                     count+=GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                     isMiss = true;
                     Debug.LogWarning($"{go.gameObject.name}身上的Miss被我删除了#24");//如果不需要记录可以删除
                 }
                 ///////////////////////////////
                 var nav = go.GetComponent<NavMeshAgent>();
                 if (nav != null)
                 {
                     Undo.DestroyObjectImmediate(nav);
                     Debug.LogWarning("删");
                     isMiss = true;
                 }

                 if (nav)
                 {
                     Debug.LogWarning("你怎么不删呢");
                 }
                 
                 var navo = go.GetComponent<NavMeshObstacle>();
                 if (navo != null)
                 {
                     Undo.DestroyObjectImmediate(navo);
                     Debug.LogWarning("删");
                     isMiss = true;
                 }
                 if (navo)
                 {
                     Debug.LogWarning("你怎么不删呢");
                 }
                 ///////////////////////////////
             }
             if (isMiss) PrefabUtility.SaveAsPrefabAsset(targetGo, variable);
             PrefabUtility.UnloadPrefabContents(targetGo);
         }
         if (count!=0)
             Debug.LogWarning($"Project中删除了{count}个miss组件");
         AssetDatabase.Refresh();
         Debug.LogWarning("End");
     }
     
     [MenuItem("GameTools/Remove Missing Scripts Hierarchy")]
     public static void DeleteHierarchyPanelMissScript()
     {
         Debug.LogWarning("Start");
         
         var sceneCount = EditorSceneManager.sceneCount;
         var count = 0;
         while (sceneCount > 0)
         {
             sceneCount--;
             var scene = EditorSceneManager.GetSceneAt(sceneCount);
             var rootGameObjects = scene.GetRootGameObjects();
             foreach (var rootGameObject in rootGameObjects)
             {
                 var gos = rootGameObject.GetComponentsInChildren<Transform>(true);
                 foreach (var go in gos)
                 {
                     if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go.gameObject) > 0)
                     {
                         count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go.gameObject);
                         Debug.LogWarning($"{go.gameObject.name}身上的Miss被我删除了#24");//如果不需要记录可以删除
                     }
                     ///////////////////////////////
                     var nav = go.GetComponent<NavMeshAgent>();
                     if (nav != null)
                     {
                         Undo.DestroyObjectImmediate(nav);
                     }
                 
                     var navo = go.GetComponent<NavMeshObstacle>();
                     if (navo != null)
                     {
                         Undo.DestroyObjectImmediate(navo);
                     }
                     ///////////////////////////////
                 }
             }
             EditorSceneManager.SaveScene(scene);
         }

         if (count!=0)
             Debug.LogWarning($"Hierarchy中删除了{count}个miss组件");
         AssetDatabase.Refresh();
         Debug.LogWarning("End");
     }
    
}