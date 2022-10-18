using System.IO;
using UnityEngine;

namespace UAsset.Editor
{
    public class EditorRawAsset : RawAsset
    {
        protected override void OnLoad()
        {
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading) return;

            FinishLoad();
        }

        private void FinishLoad()
        {
            var path = PathManager.GetBinaryAssetPath(pathOrURL);
            _binaryBytes = File.ReadAllBytes(path);
            Finish();
        }
        
        protected override void OnComplete()
        {
            if (completed == null)
            {
                return;
            }

            var saved = completed;
            completed?.Invoke(this);
            completed -= saved;
        }

        public override void LoadImmediate()
        {
            if (isDone) return;

            FinishLoad();
        }

        internal static EditorRawAsset Create(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);

            return new EditorRawAsset
            {
                pathOrURL = path,
            };
        }
    }
}