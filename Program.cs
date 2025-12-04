using System.Linq;
using TriggerCalculator;

Storage storage = new Storage();
var multi=false;
Console.WriteLine("是否启用多人模式？(y/N)：");
var key=Console.ReadKey(true);
if(key.Key==ConsoleKey.Y)
    multi = true;
Interactive.Run(storage, multi);