
namespace AutoParkLibrary
{
    public class Car
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int InspectionYear { get; set; }

        public Car(int id, string name, int inspectionYear)
        {
            Id = id;
            Name = name;
            InspectionYear = inspectionYear;
        }

        public void Deconstruct(out int id, out string name, out int inspectionYear)
        {
            id = Id;
            name = Name;
            inspectionYear = InspectionYear;
        }

        public override string ToString() =>
            $"Car{{Id={Id}, Name={Name}, InspectionYear={InspectionYear}}}";
    }

    public class StreamService<T>
    {
        private readonly object _sync = new object();

        public async Task WriteToStreamAsync(Stream stream, IEnumerable<T> data, IProgress<string> progress)
        {
            lock (_sync)
            {
                progress.Report($"[Thread {Thread.CurrentThread.ManagedThreadId}]] Начало записи");
            }

            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                int total = data.Count();
                int written = 0;
                foreach (var item in data)
                {
                    string line = System.Text.Json.JsonSerializer.Serialize(item);
                    await writer.WriteLineAsync(line);
                    await writer.FlushAsync();
                    await Task.Delay(3000/total);
                    
                    written++;
                    lock (_sync)
                    {
                        int percent = written*100/total;
                        progress.Report($"[Thread {Thread.CurrentThread.ManagedThreadId}] Запись: {percent}%");
                    }
                }
            }

            lock (_sync)
            {
                progress.Report($"[Thread {Thread.CurrentThread.ManagedThreadId}] Конец Записи");
            }
        }

        public async Task CopyFromStreamAsync(Stream stream, string fileName, IProgress<string> progress)
        {
            lock (_sync)
            {
                progress.Report($"[Thread {Thread.CurrentThread.ManagedThreadId}] Начало чтения");
            }
            stream.Position = 0;
            using (var reader = new StreamReader(stream, leaveOpen: true)) 
            using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(file))
            {
                var allText = await reader.ReadToEndAsync();
                var lines = allText.Split(new[]{Environment.NewLine}, StringSplitOptions.None);
                int total = lines.Length;
                for (int i = 0; i < total; i++)
                {
                    await writer.WriteLineAsync(lines[i]);
                    await writer.FlushAsync();
                    
                    int percent = (i+1)*100/total;
                    lock (_sync)
                    {
                        progress.Report($"[Thread {Thread.CurrentThread.ManagedThreadId}] Чтение: {percent}%");
                    }
                }
            }

            lock (_sync)
            {
                progress.Report($"[Thread {Thread.CurrentThread.ManagedThreadId}] Конец чтения");
            }
        }

        public async Task<int> GetStatisticsAsync(string fileName, Func<T, bool> filter)
        {
            int count = 0;
            using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(file))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Пропускаем пустые или пробельные строки
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    try
                    {
                        var item = System.Text.Json.JsonSerializer.Deserialize<T>(line);
                        if (item != null && filter(item))
                            count++;
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Если строка не является валидным JSON, пропускаем
                        continue;
                    }
                }
            }
            return count;
        }
    }
}