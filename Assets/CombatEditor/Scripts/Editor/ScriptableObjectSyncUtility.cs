using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CombatEditor
{
    /// <summary>
    /// ScriptableObject文件名同步工具
    /// 用于同步AbilityScriptableObject的文件名与其AnimationClip名称
    /// </summary>
    public static class ScriptableObjectSyncUtility
    {
        /// <summary>
        /// 自动同步单个ScriptableObject的文件名与其AnimationClip名称
        /// </summary>
        /// <param name="scriptableObject">要同步的ScriptableObject</param>
        /// <param name="forceRename">是否强制重命名（即使名称相同）</param>
        /// <returns>是否成功同步</returns>
        public static bool SyncScriptableObjectName(AbilityScriptableObject scriptableObject, bool forceRename = false)
        {
            if (scriptableObject == null)
            {
                Debug.LogWarning("❌ ScriptableObject为空，无法同步");
                return false;
            }
            
            if (scriptableObject.Clip == null)
            {
                Debug.LogWarning($"❌ ScriptableObject '{scriptableObject.name}' 没有AnimationClip，无法同步");
                return false;
            }
            
            string clipName = scriptableObject.Clip.name;
            string currentName = scriptableObject.name;
            
            // 检查是否需要同步
            if (!forceRename && currentName == clipName)
            {
                Debug.Log($"✅ ScriptableObject '{currentName}' 名称已与Clip '{clipName}' 同步");
                return true;
            }
            
            // 获取当前文件路径
            string currentPath = AssetDatabase.GetAssetPath(scriptableObject);
            if (string.IsNullOrEmpty(currentPath))
            {
                Debug.LogWarning($"❌ 无法获取ScriptableObject '{currentName}' 的文件路径");
                return false;
            }
            
            // 生成新的文件路径
            string directory = Path.GetDirectoryName(currentPath);
            string extension = Path.GetExtension(currentPath);
            string newFileName = SanitizeFileName(clipName);
            string newPath = Path.Combine(directory, newFileName + extension);
            
            // 检查新路径是否已存在
            if (File.Exists(newPath) && newPath != currentPath)
            {
                // 如果存在同名文件，添加数字后缀
                int counter = 1;
                string baseNewPath = Path.Combine(directory, newFileName);
                while (File.Exists($"{baseNewPath}_{counter}{extension}"))
                {
                    counter++;
                }
                newPath = $"{baseNewPath}_{counter}{extension}";
                newFileName = $"{newFileName}_{counter}";
            }
            
            try
            {
                // 重命名文件
                AssetDatabase.MoveAsset(currentPath, newPath);
                
                // 更新ScriptableObject的name属性
                scriptableObject.name = newFileName;
                
                // 标记为dirty并保存
                EditorUtility.SetDirty(scriptableObject);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"✅ 成功同步: '{currentName}' → '{newFileName}' (Clip: {clipName})");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ 同步失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 自动同步CombatController中所有的ScriptableObject文件名
        /// </summary>
        /// <param name="combatController">目标CombatController</param>
        /// <param name="forceRename">是否强制重命名</param>
        /// <returns>同步成功的数量</returns>
        public static int SyncAllScriptableObjects(CombatController combatController, bool forceRename = false)
        {
            if (combatController?.CombatDatas == null)
            {
                Debug.LogWarning("❌ CombatController或CombatDatas为空");
                return 0;
            }
            
            int syncedCount = 0;
            int totalCount = 0;
            
            Debug.Log("=== 开始批量同步ScriptableObject文件名 ===");
            
            foreach (var group in combatController.CombatDatas)
            {
                if (group?.CombatObjs == null) continue;
                
                Debug.Log($"📁 处理组: {group.Label}");
                
                foreach (var scriptableObject in group.CombatObjs)
                {
                    if (scriptableObject == null) continue;
                    
                    totalCount++;
                    
                    if (SyncScriptableObjectName(scriptableObject, forceRename))
                    {
                        syncedCount++;
                    }
                }
            }
            
            Debug.Log($"=== 批量同步完成: {syncedCount}/{totalCount} 个文件成功同步 ===");
            return syncedCount;
        }
        
        /// <summary>
        /// 创建ScriptableObject时自动同步名称
        /// </summary>
        /// <param name="scriptableObject">新创建的ScriptableObject</param>
        /// <param name="animationClip">关联的AnimationClip</param>
        /// <returns>是否成功同步</returns>
        public static bool AutoSyncOnCreate(AbilityScriptableObject scriptableObject, AnimationClip animationClip)
        {
            if (scriptableObject == null || animationClip == null)
            {
                return false;
            }
            
            // 设置AnimationClip
            scriptableObject.Clip = animationClip;
            
            // 同步名称
            return SyncScriptableObjectName(scriptableObject, true);
        }
        
        /// <summary>
        /// 清理文件名，移除非法字符
        /// </summary>
        /// <param name="fileName">原始文件名</param>
        /// <returns>清理后的文件名</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unnamed";
            
            // 移除文件名中的非法字符
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;
            
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            
            // 移除多余的空格和点
            sanitized = sanitized.Trim().Trim('.');
            
            if (string.IsNullOrEmpty(sanitized))
                return "Unnamed";
            
            return sanitized;
        }
        
        /// <summary>
        /// 扫描项目中所有的AbilityScriptableObject
        /// </summary>
        /// <returns>所有找到的AbilityScriptableObject</returns>
        public static List<AbilityScriptableObject> FindAllAbilityScriptableObjects()
        {
            List<AbilityScriptableObject> results = new List<AbilityScriptableObject>();
            
            // 查找所有AbilityScriptableObject类型的资产
            string[] guids = AssetDatabase.FindAssets("t:AbilityScriptableObject");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AbilityScriptableObject obj = AssetDatabase.LoadAssetAtPath<AbilityScriptableObject>(path);
                
                if (obj != null)
                {
                    results.Add(obj);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// 扫描并同步项目中所有的AbilityScriptableObject
        /// </summary>
        /// <param name="forceRename">是否强制重命名</param>
        /// <returns>同步成功的数量</returns>
        public static int SyncAllInProject(bool forceRename = false)
        {
            var allObjects = FindAllAbilityScriptableObjects();
            int syncedCount = 0;
            
            Debug.Log($"=== 开始项目范围同步，找到 {allObjects.Count} 个AbilityScriptableObject ===");
            
            foreach (var obj in allObjects)
            {
                if (SyncScriptableObjectName(obj, forceRename))
                {
                    syncedCount++;
                }
            }
            
            Debug.Log($"=== 项目范围同步完成: {syncedCount}/{allObjects.Count} 个文件成功同步 ===");
            return syncedCount;
        }
        
        /// <summary>
        /// 获取同步状态报告
        /// </summary>
        /// <param name="combatController">目标CombatController</param>
        /// <returns>同步状态报告</returns>
        public static SyncStatusReport GetSyncStatusReport(CombatController combatController)
        {
            var report = new SyncStatusReport();
            
            if (combatController?.CombatDatas == null)
            {
                return report;
            }
            
            foreach (var group in combatController.CombatDatas)
            {
                if (group?.CombatObjs == null) continue;
                
                foreach (var obj in group.CombatObjs)
                {
                    if (obj == null) continue;
                    
                    report.TotalCount++;
                    
                    if (obj.Clip == null)
                    {
                        report.NoClipCount++;
                    }
                    else if (obj.name == obj.Clip.name)
                    {
                        report.SyncedCount++;
                    }
                    else
                    {
                        report.UnsyncedCount++;
                        report.UnsyncedObjects.Add(new UnsyncedObjectInfo
                        {
                            ScriptableObject = obj,
                            CurrentName = obj.name,
                            ClipName = obj.Clip.name,
                            GroupLabel = group.Label
                        });
                    }
                }
            }
            
            return report;
        }
    }
    
    /// <summary>
    /// 同步状态报告
    /// </summary>
    [System.Serializable]
    public class SyncStatusReport
    {
        public int TotalCount = 0;
        public int SyncedCount = 0;
        public int UnsyncedCount = 0;
        public int NoClipCount = 0;
        public List<UnsyncedObjectInfo> UnsyncedObjects = new List<UnsyncedObjectInfo>();
        
        public float SyncedPercentage => TotalCount > 0 ? (float)SyncedCount / TotalCount * 100f : 0f;
    }
    
    /// <summary>
    /// 未同步对象信息
    /// </summary>
    [System.Serializable]
    public class UnsyncedObjectInfo
    {
        public AbilityScriptableObject ScriptableObject;
        public string CurrentName;
        public string ClipName;
        public string GroupLabel;
    }
} 