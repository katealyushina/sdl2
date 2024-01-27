using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

class Post_DB 
{
    string interval;
    string user;
    string pass;
    string logPath;
    string connectionString;

    public Post_DB(string interval, string user, string pass, string logPath)
    {
        this.interval = interval;
        this.user = user;
        this.pass = pass;
        this.logPath = logPath;
        
        using (StreamReader sr = new StreamReader("sdl1.conf"))
        {
            var connectionStringFromConf = sr.ReadLine();
            NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder(connectionStringFromConf);
            csb.Username = user;
            csb.Password = pass;
            connectionString = csb.ToString();
        }
    }
    void Pinger(string connectionString, string logPath)
    {
        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand("SELECT version()", connection))
                {
                    string version = (string)command.ExecuteScalar();

                    LogMessage(logPath, $"PostgreSQL version: {version}");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage(logPath, $"Error: {ex.Message}");
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
    void LogMessage(string logPath, string message)
    {
        using (StreamWriter logWriter = new StreamWriter(logPath, true))
        {
            logWriter.WriteLine($"{DateTime.Now}: {message}");
        }

        Console.WriteLine($"{DateTime.Now}: {message}");
    }
    public async Task Watch_sdlfile()
    {
        var tasklist = new List<Task>();
        LogMessage(logPath, "Task is started");
        while (true)
        {
            lock (tasklist)
            {
                tasklist.RemoveAll(x => x.IsCompleted || x.IsFaulted);
                if (tasklist.Count > 0)
                {
                    using (StreamWriter logWriter = new StreamWriter(logPath, true))
                    {
                        logWriter.WriteLine("Task is paused");
                    }
                    Console.WriteLine("Task is paused");
                }
                tasklist.Clear();

                var task = Task.Run(() =>
                {
                    Pinger(connectionString, logPath);
                });

                tasklist.Add(task);
            }

            await Task.Delay(TimeSpan.FromSeconds(double.Parse(interval)));
        }
    }
}
class Program
{
    static async Task Main()
    {
        string interval = Environment.GetEnvironmentVariable("INTERVAL");
        string user = Environment.GetEnvironmentVariable("USERNAME");
        string pass = Environment.GetEnvironmentVariable("PASSWORD");
        string logPath = Environment.GetEnvironmentVariable("LOG_FILE_PATH");

        Post_DB post1 = new Post_DB(interval, user, pass, logPath);
        await post1.Watch_sdlfile();
    }
}





