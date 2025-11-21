using TriggerCalculator;

Storage storage=new Storage();
while (!storage.IsEnd)
{
    storage.Print();
    var cmd=Console.ReadLine()!;
    storage.Execute(cmd);
}
storage.Print();
Console.ReadKey();