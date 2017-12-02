using System;
using System.Threading;
using System.Collections.Generic;

public class TaskEvent
{
    private int mCount = 0;
    private ManualResetEvent mEvent = null;

    public TaskEvent(bool bSet = true)
    {
        mCount = bSet ? 0 : 1;
        mEvent = new ManualResetEvent(bSet);
    }

    public void Reset()
    {
        mCount = 1;
        mEvent.Reset();
    }

    public void Signal()
    {
        if (mCount > 0)
        {
            mCount--;
        }

        if (mCount == 0)
        {
            mEvent.Set();
        }
    }

    public void Unsignal()
    {
        if (mCount == 0)
        {
            mEvent.Reset();
        }

        mCount++;
    }

    public bool Wait(int timeout = int.MaxValue)
    {
        return mEvent.WaitOne(timeout);
    }
}

public class TaskBase
{
    public TaskBase Next = null;
    private TaskEvent mTaskEvent = null;

    public void SetEvent(TaskEvent taskEvent)
    {
        mTaskEvent = taskEvent;

        if (mTaskEvent != null)
        {
            mTaskEvent.Unsignal();
        }
    }

    public void SetEventSignal()
    {
        if (mTaskEvent != null)
        {
            mTaskEvent.Signal();
        }
    }

    public virtual void TaskFunc()
    {
        // Overwrite
    }
}

public class TaskGraph
{
    private Thread[] mThreads = null;

    private TaskEvent mEventRun = null;
    private TaskEvent mEventReady = null;
    private TaskEvent mEventFinish = null;
    private TaskEvent mEventExit = null;
    private TaskEvent mEventDispatch = null;

    private Mutex mMutex = new Mutex();
    private Dictionary<TaskEvent, TaskBase> mTaskListHeads = new Dictionary<TaskEvent, TaskBase>();
    private Dictionary<TaskEvent, TaskEvent> mTaskListDependence = new Dictionary<TaskEvent, TaskEvent>();

    public void Create(int numThreads)
    {
        mThreads = new Thread[numThreads];

        mEventRun = new TaskEvent(true);
        mEventReady = new TaskEvent(true);
        mEventFinish = new TaskEvent(true);
        mEventExit = new TaskEvent(false);
        mEventDispatch = new TaskEvent(false);

        for (int index = 0; index < numThreads; index++)
        {
            mThreads[index] = new Thread(() => WorkThread(this));
            mThreads[index].Start();
        }
    }

    public void Destroy()
    {
        mEventExit.Signal();
        mEventDispatch.Signal();

        for (int index = 0; index < mThreads.Length; index++)
        {
            mThreads[index].Join();
            mThreads[index] = null;
        }

        mThreads = null;

        mEventRun = null;
        mEventExit = null;
        mEventFinish = null;
        mEventDispatch = null;

        mTaskListHeads.Clear();
        mTaskListDependence.Clear();
    }

    public void Task(TaskBase task, TaskEvent taskEventSignal, TaskEvent taskEventWait)
    {
        task.Next = null;
        task.SetEvent(taskEventSignal);

        mMutex.WaitOne();
        {
            if (taskEventWait == null)
            {
                taskEventWait = mEventRun;
            }

            if (mTaskListHeads.ContainsKey(taskEventWait))
            {
                task.Next = mTaskListHeads[taskEventWait];
            }

            mTaskListHeads[taskEventWait] = task;
            mTaskListDependence[taskEventWait] = taskEventSignal;
        }
        mMutex.ReleaseMutex();
    }

    public void Dispatch()
    {
        for (int index = 0; index < mThreads.Length; index++)
        {
            mEventReady.Unsignal();
            mEventFinish.Unsignal();
        }

        mEventDispatch.Signal();
    }

    public void Wait()
    {
        mEventFinish.Wait();

        mTaskListHeads.Clear();
        mTaskListDependence.Clear();
    }

    static void WorkThread(TaskGraph taskGraph)
    {
        while (true)
        {
            taskGraph.mEventDispatch.Wait();
            {
                if (taskGraph.mEventExit.Wait(0))
                {
                    break;
                }

                taskGraph.mEventReady.Signal();
                taskGraph.mEventReady.Wait();

                if (taskGraph.mTaskListHeads.Count > 0 && taskGraph.mTaskListDependence.Count > 0)
                {
                    TaskEvent taskEvent = taskGraph.mEventRun;
                    do
                    {
                        taskEvent.Wait();

                        while (true)
                        {
                            bool bFinish = false;
                            TaskBase task = null;

                            taskGraph.mMutex.WaitOne();
                            {
                                if (taskGraph.mTaskListHeads.ContainsKey(taskEvent) && taskGraph.mTaskListHeads[taskEvent] != null)
                                {
                                    task = taskGraph.mTaskListHeads[taskEvent];
                                    taskGraph.mTaskListHeads[taskEvent] = taskGraph.mTaskListHeads[taskEvent].Next;
                                }
                                else
                                {
                                    bFinish = true;
                                }
                            }
                            taskGraph.mMutex.ReleaseMutex();

                            if (task != null)
                            {
                                task.TaskFunc();
                                task.SetEventSignal();
                            }

                            if (bFinish)
                            {
                                break;
                            }
                        }

                        taskEvent = taskGraph.mTaskListDependence.Count > 0 ? taskGraph.mTaskListDependence[taskEvent] : null;
                    } while (taskEvent != null);
                }
            }
            taskGraph.mEventDispatch.Reset();
            taskGraph.mEventFinish.Signal();
        }
    }
}
