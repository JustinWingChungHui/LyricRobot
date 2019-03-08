using LyricRobotCommon;
using System;
using System.Threading.Tasks;

namespace TestDataCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {                      
            Console.WriteLine("Getting songs out of db");

            await TrainingData.Create();
        }
    }
}
