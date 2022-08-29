
using System.Collections.Generic;
using UnityEngine;

namespace UAsset.Editor
{
    public abstract class BuildTaskJob
    {
        protected readonly BuildTask _task;

        protected readonly List<string> changes = new List<string>();
        public string error;

        protected BuildTaskJob(BuildTask task)
        {
            _task = task;
        }

        public void Run()
        {
            Debug.Log($"开始执行{GetType().Name}任务");
            var startTime = System.DateTime.Now;
            
            DoTask();

            var timeSpan = System.DateTime.Now.Subtract(startTime);
            Debug.Log($"结束执行{GetType().Name}任务, 耗时为{timeSpan.TotalSeconds}s");
        }

        protected abstract void DoTask();

        protected string GetBuildPath(string filename)
        {
            return _task.GetBuildPath(filename);
        }

        protected void TreatError(string e)
        {
            error = e;
            Debug.LogError(error);
        }
    }
}