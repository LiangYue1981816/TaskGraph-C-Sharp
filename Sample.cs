using System;


class Data
{
    private Random mRandom = new Random();
    private float[] datas = new float[500*500];

    public void Step0(int first, int step)
    {
        for (int loop = 0; loop < 100; loop++)
        {
            for (int index = first; index < datas.Length; index += step)
            {
                datas[index] = (float)Math.Pow(mRandom.Next(-1000, 1000) / 1000.0f, 3.5f);
            }
        }   
    }

    public void Step1(int first, int step)
    {
        for (int loop = 0; loop < 100; loop++)
        {
            for (int index = first; index < datas.Length; index += step)
            {
                datas[index] = (float)Math.Pow(5 * datas[index] * datas[index] * datas[index] - 2 * datas[index] * datas[index] + 10 * datas[index] + 103, 0.24f);
            }
        }
    }

    public void Step2(int first, int step)
    {
        for (int loop = 0; loop < 100; loop++)
        {
            for (int index = first; index < datas.Length; index += step)
            {
                datas[index] = (float)Math.Sqrt(datas[index]);
            }
        }   
    }
}

class TaskStep0 : TaskBase
{
    private Data mData;
    private int mFirst;
    private int mStep;

    public TaskStep0(Data data, int first, int step)
    {
        mData = data;
        mFirst = first;
        mStep = step;
    }

    public override void TaskFunc()
    {
        Console.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + " Step0");
        mData.Step0(mFirst, mStep);
    }
}

class TaskStep1 : TaskBase
{
    private Data mData;
    private int mFirst;
    private int mStep;

    public TaskStep1(Data data, int first, int step)
    {
        mData = data;
        mFirst = first;
        mStep = step;
    }

    public override void TaskFunc()
    {
        Console.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + " Step1");
        mData.Step1(mFirst, mStep);
    }
}

class TaskStep2 : TaskBase
{
    private Data mData;
    private int mFirst;
    private int mStep;

    public TaskStep2(Data data, int first, int step)
    {
        mData = data;
        mFirst = first;
        mStep = step;
    }

    public override void TaskFunc()
    {
        Console.WriteLine("Thread: " + Thread.CurrentThread.ManagedThreadId + " Step2");
        mData.Step2(mFirst, mStep);
    }
}



class Program
{
    static void Main(string[] args)
    {
        int count = 10;
        Data data = new Data();
        TaskStep0[] step0 = new TaskStep0[count];
        TaskStep1[] step1 = new TaskStep1[count];
        TaskStep2[] step2 = new TaskStep2[count];

        for (int index = 0; index < count; index++)
        {
            step0[index] = new TaskStep0(data, index, count);
            step1[index] = new TaskStep1(data, index, count);
            step2[index] = new TaskStep2(data, index, count);
        }

        TaskEvent event1 = new TaskEvent();
        TaskEvent event2 = new TaskEvent();

        TaskGraph taskGraph = new TaskGraph();
        taskGraph.Create(8);

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey();
            if (key.Key != ConsoleKey.Escape)
            {
                Console.WriteLine("");

                for (int index = 0; index < count; index++)
                {
                    taskGraph.Task(step0[index], event1, null);
                    taskGraph.Task(step1[index], event2, event1);
                    taskGraph.Task(step2[index], null, event2);
                }

                taskGraph.Dispatch();
                taskGraph.Wait();

                continue;
            }

            break;
        }
        taskGraph.Destroy();
    }
}
