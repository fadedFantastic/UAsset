using System.IO;
using UnityEngine;

namespace xasset.editor
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
            var binaryPath = GetBinaryPath(pathOrURL);
            binaryBytes = File.ReadAllBytes(binaryPath);
            Finish();
        }
        
        protected override void OnComplete()
        {
            if (completed == null)
            {
                return;
            }

            var saved = completed;
            completed?.Invoke(pathOrURL, binaryBytes);
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