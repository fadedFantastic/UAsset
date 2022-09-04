using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    /// <summary>
    /// 资源过滤规则接口
    /// </summary>
    public interface IFilterRule
    {
        /// <summary>
        /// 是否为收集资源
        /// </summary>
        /// <returns>如果收集该资源返回TRUE</returns>
        bool IsCollectAsset(string path);
    }
    
    /// <summary>
    /// 收集所有资源
    /// </summary>
    public class CollectAll : IFilterRule
    {
        public bool IsCollectAsset(string path)
        {
            return true;
        }
    }

    /// <summary>
    /// 只收集场景
    /// </summary>
    public class CollectScene : IFilterRule
    {
        public bool IsCollectAsset(string path)
        {
            return Path.GetExtension(path) == ".unity";
        }
    }

    /// <summary>
    /// 只收集预制体
    /// </summary>
    public class CollectPrefab : IFilterRule
    {
        public bool IsCollectAsset(string path)
        {
            return Path.GetExtension(path) == ".prefab";
        }
    }

    /// <summary>
    /// 只收集精灵类型的资源
    /// </summary>
    public class CollectSprite : IFilterRule
    {
        public bool IsCollectAsset(string path)
        {
            var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (mainAssetType == typeof(Texture2D))
            {
                var texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                return texImporter != null && texImporter.textureType == TextureImporterType.Sprite;
            }
            return false;
        }
    }
    
    /// <summary>
    /// 文件过滤规则
    /// </summary>
    public static class FileFilterRule
    {
        private static readonly Dictionary<string, Type> _cacheFilterRuleTypes = new Dictionary<string, Type>();
        private static readonly Dictionary<string, IFilterRule> _cacheFilterRuleInstance = new Dictionary<string, IFilterRule>();

        // 获取所有类型
        private static List<Type> _types = new List<Type>(10)
        {
            typeof(CollectAll),
            typeof(CollectScene),
            typeof(CollectPrefab),
            typeof(CollectSprite)
        };

        static FileFilterRule()
        {
            foreach (var t in _types)
            {
                _cacheFilterRuleTypes.Add(t.Name, t);
            }
        }
        
        /// <summary>
        /// 获取过滤规则名
        /// </summary>
        /// <returns></returns>
        public static string[] GetFilterRuleNames()
        {
            var names = new List<string>();
            foreach (var pair in _cacheFilterRuleTypes)
            {
                names.Add(pair.Key);
            }
            return names.ToArray();
        }
        
        /// <summary>
        /// 获取过滤规则实例
        /// </summary>
        /// <param name="ruleName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IFilterRule GetFilterRuleInstance(string ruleName)
        {
            if (_cacheFilterRuleInstance.TryGetValue(ruleName, out IFilterRule instance))
                return instance;

            // 如果不存在创建类的实例
            if (_cacheFilterRuleTypes.TryGetValue(ruleName, out var type))
            {
                instance = (IFilterRule)Activator.CreateInstance(type);
                _cacheFilterRuleInstance.Add(ruleName, instance);
                return instance;
            }
            
            throw new Exception($"{nameof(IFilterRule)}类型无效：{ruleName}");
        }
    }
}