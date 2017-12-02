TaskGraph的C#版本实现

一. 使用说明
1. 开发者需要从TaskBase基类派生自己的任务类并实现其中的执行函数
2. 在使用时将任务类添加到TaskGraph中
3. 触发TaskGraph的执行
4. 等待TaskGraph执行结束（不是必须）

二. 使用建议
1. TaskGraph设计目标是将那些大量琐碎且运算量较低的任务充分并行化，比如游戏中的各种逻辑Update等。而单项任务计算时间过长且不可拆解的任务建议使用独立线程单独计算，比如下载和寻路等。
2. 并行执行任务之间如果有依赖关系，则在其添加到TaskGraph可通过TaskEvent设置好其依赖关系，TaskGraph内部会自行调度，并尽可能并行执行。

三. 实例代码

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