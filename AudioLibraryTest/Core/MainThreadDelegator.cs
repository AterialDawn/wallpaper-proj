using System;
using System.Collections.Concurrent;

namespace player.Core
{
    static class MainThreadDelegator
    {
        private static ConcurrentQueue<Action> beforeRenderDelegates = new ConcurrentQueue<Action>();
        private static ConcurrentQueue<Action> afterRenderDelegates = new ConcurrentQueue<Action>();

        public static void InvokeOn(InvocationTarget timeToExecute, Action actionToInvoke)
        {
            ConcurrentQueue<Action> targetCollection = null;
            switch (timeToExecute)
            {
                case InvocationTarget.BeforeRender: targetCollection = beforeRenderDelegates; break;
                case InvocationTarget.AfterRender: targetCollection = afterRenderDelegates; break;
            }

            targetCollection.Enqueue(actionToInvoke);
        }

        public static void ExecuteDelegates(InvocationTarget executionTargets)
        {
            ConcurrentQueue<Action> targetCollection = null;
            switch (executionTargets)
            {
                case InvocationTarget.BeforeRender: targetCollection = beforeRenderDelegates; break;
                case InvocationTarget.AfterRender: targetCollection = afterRenderDelegates; break;
            }

            for (; ; )
            {
                if (!targetCollection.TryDequeue(out Action toExecute))
                {
                    break;
                }
                toExecute();
            }
        }
    }
    enum InvocationTarget
    {
        BeforeRender,
        AfterRender
    }
}
