using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NSpeedTest.Client.Jitbit.Utils;
using NSpeedTest.Models;

namespace NSpeedTest.Client
{
    class Program
    {
        private static SpeedTestClient client;
        private static Settings settings;
        private const string DefaultCountry = "Australia";
        private static bool IsAbort = false;
        private static double SleepTimer = 1;

        static void Main()
        {
            //Console.WriteLine("Getting speedtest.net settings and server list...");
        
            //Console.WriteLine("Testing speed...");
            var csvExport = new CsvExport();

            string sleepTimerIn = string.Empty;
            do
            {
                Console.WriteLine("1) Enter how many minutes before next check ");
                sleepTimerIn = Console.ReadLine();
            } while (!double.TryParse(sleepTimerIn, out SleepTimer));

            var sleepAmt = TimeSpan.FromMinutes(SleepTimer).TotalMilliseconds;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CTRL + C will exit the app back to console.");
            Console.ResetColor();

            client = new SpeedTestClient();
            settings = client.GetSettings();
            var servers = SelectServers();

            while (!IsAbort)
            {
                var bestServer = SelectBestServer(servers);
                var downloadSpeed = client.TestDownloadSpeed(bestServer, settings.Download.ThreadsPerUrl);
                var uploadSpeed = client.TestUploadSpeed(bestServer, settings.Upload.ThreadsPerUrl);
                PrintSpeed("Download", downloadSpeed);
                PrintSpeed("Upload", uploadSpeed);

                csvExport.AddRow();
                csvExport["Date"] = DateTime.UtcNow;
                csvExport["UP"] = PrintSpeed(uploadSpeed);
                csvExport["DOWN"] = PrintSpeed(downloadSpeed);
                csvExport["UPRaw"] = uploadSpeed;
                csvExport["DOWNRaw"] = downloadSpeed;
                csvExport["Server"] = bestServer.Name;
                csvExport["Host"] = bestServer.Host;
                csvExport["Latency"] = bestServer.Latency;
                csvExport["Distance"] = bestServer.Distance;
                csvExport.ExportToFile("results.csv");
                Thread.Sleep((int) sleepAmt);
            }

            Console.WriteLine("Press a key to exit.");
            Console.ReadKey();
        }

        private static Server SelectBestServer(IEnumerable<Server> servers)
        {
            Console.WriteLine();
            Console.WriteLine("Best server by latency:");
            var bestServer = servers.OrderBy(x => x.Latency).First();
            PrintServerDetails(bestServer);
            Console.WriteLine();
            return bestServer;
        }

        private static IEnumerable<Server> SelectServers()
        {
            Console.WriteLine();
            Console.WriteLine("Selecting best server by distance...");
            var servers = settings.Servers.Where(s => s.Country.Equals(DefaultCountry)).Take(10).ToList();

            foreach (var server in servers)
            {
                server.Latency = client.TestServerLatency(server);
                PrintServerDetails(server);
            }
            return servers;
        }

        private static void PrintServerDetails(Server server)
        {
            Console.WriteLine("Hosted by {0} ({1}/{2}), distance: {3}km, latency: {4}ms", server.Sponsor, server.Name,
                server.Country, (int) server.Distance/1000, server.Latency);
        }

        private static void PrintSpeed(string type, double speed)
        {
            if (speed > 1024)
            {
                Console.WriteLine("{0} speed: {1} Mbps", type, Math.Round(speed / 1024, 2));
            }
            else
            {
                Console.WriteLine("{0} speed: {1} Kbps", type, Math.Round(speed, 2));
            }
        }

        private static string PrintSpeed(double speed)
        {
            if (speed > 1024)
            {
                return string.Format("{0} Mbps", Math.Round(speed / 1024, 2));
            }
            else
            {
                return string.Format("{0} Kbps", Math.Round(speed, 2));
            }
        }
    }
}
