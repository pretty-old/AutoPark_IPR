
using AutoParkLibrary;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Запуск приложения");
        
        var rnd = new Random();
        var cars = new List<Car>(1000);
        int currentYear = DateTime.Now.Year;
        for (int i = 1; i <= 1000; i++)
        {
            cars.Add(new Car(i, $"Model {i}", rnd.Next(currentYear - 40, currentYear)));
        }

        var service = new StreamService<Car>();
        var progress = new Progress<string>(msg => Console.WriteLine(msg));
        using var memStream = new MemoryStream();
        
        var writeTask = service.WriteToStreamAsync(memStream, cars, progress);
        await Task.Delay(rnd.Next(100, 200));
        var tempFile = "cars.txt";
        var copyTask = service.CopyFromStreamAsync(memStream, tempFile, progress);

        Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Запущены оба потока");
        
        await Task.WhenAll(writeTask, copyTask);
        
        int count = await service.GetStatisticsAsync(tempFile, c => c.InspectionYear != currentYear);
        
        Console.WriteLine($"Машин для техосмотра в {currentYear}: {count}");
    }
}