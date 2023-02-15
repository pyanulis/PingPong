using EasyNetQ.Management.Client.Model;
using Pyanulis.PingPong.Bus;
using Pyanulis.PingPong.DbWatch;

namespace Pyanulis.PingPong.ConsoleApp
{
    internal class Program
    {
        async static Task Main(string[] args)
        {
            Console.WriteLine("rabbit <host:port> <user> <password>");
            Console.WriteLine("rabbit_lg - connect to local rabbitmq with default user/password");

            while (true)
            {
                string? cmd = Console.ReadLine();

                if (cmd == "q")
                {
                    break;
                }

                if (cmd.IsCommand("rabbit_lg") || cmd.IsCommand("rl"))
                {
                    await ProcessLocalRabbit();
                    continue;
                }

                if (cmd.IsCommand("rabbit") || cmd.IsCommand("rc"))
                {
                    await ProcessRabbit(cmd.Params());
                    continue;
                }

                if (cmd.IsCommand("rel"))
                {
                    ListExchanges();
                    continue;
                }

                if (cmd.IsCommand("rex"))
                {
                    SelectExchange(cmd.TrimName());
                    continue;
                }

                if (cmd.IsCommand("rbl"))
                {
                    ListExchangeBindings();
                    continue;
                }

                if (cmd.IsCommand("rql"))
                {
                    ListExchangeQueues();
                    continue;
                }

                if (cmd.IsCommand("dbc"))
                {
                    CreateDbService(cmd.Params());
                    continue;
                }

                if (cmd.IsCommand("db-wi-a"))
                {
                    AddDbWatchItem(cmd.Params());
                    continue;
                }

                if (cmd.IsCommand("db-exec"))
                {
                    await ApplyDbWatch();
                    continue;
                }
            }

            Console.WriteLine("Closing.");
        }

        private static async Task ProcessLocalRabbit()
        {
            await ProcessRabbit(new string[] {"http://localhost:15672", "guest", "guest"});
        }

        private static async Task ProcessRabbit(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("rabbit/rc command parameters: <host:port> <user> <password-optional>");
                return;
            }

            string password = args.Length > 2 ? args[2] : "";

            try
            {
                await QueueRegistry.CreateNew(args[0], args[1], password);

                QueueRegistry.CurrentInstance.PropertyChanged += QueueRegistryPropertyChanged;
                Console.WriteLine($"Connected to {args[0]}");
                //Console.WriteLine(QueueRegistry.CurrentInstance.SelectedExchange.Name);
                //Console.WriteLine(QueueRegistry.CurrentInstance.SelectedBinding.RoutingKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void QueueRegistryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) 
        {
            switch (e.PropertyName)
            {
                case nameof(QueueRegistry.CurrentInstance.SelectedBinding):
                    Console.WriteLine($"Selected binding: {QueueRegistry.CurrentInstance.SelectedBinding?.GetName() ?? "no bindings"}");
                    break;
                case nameof(QueueRegistry.CurrentInstance.SelectedQueue):
                    Console.WriteLine($"Selected queue: {QueueRegistry.CurrentInstance.SelectedQueue?.Name ?? "no queues"}");
                    break;
            }
        }

        private static void ListExchanges()
        {
            foreach (Exchange exchange in QueueRegistry.CurrentInstance.Exchanges)
            {
                Console.WriteLine(exchange.Name);
            }
        }

        private static void SelectExchange(string name)
        {
            QueueRegistry.CurrentInstance.SelectedExchange = QueueRegistry.CurrentInstance.Exchanges.First(x => x.Name == name);
        }

        private static void ListExchangeBindings()
        {
            foreach (Binding b in QueueRegistry.CurrentInstance.ExchangeBindings)
            {
                Console.WriteLine(b.GetName());
            }
        }

        private static void ListExchangeQueues()
        {
            foreach (Queue q in QueueRegistry.CurrentInstance.ExchangeQueues)
            {
                Console.WriteLine(q.Name);
            }
        }

        private static void CreateDbService(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("dbc command parameters: <host> <user> <password-optional>.");
                return;
            }

            string password = args.Length > 2 ? args[2] : "";

            try
            {
                DbService.Create(args[0], args[1], password);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void AddDbWatchItem(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("db-wi-a command parameters: <database> <table>.");
                return;
            }

            if (DbService.Instance == null)
            {
                Console.WriteLine("Use dbc command first.");
                return;
            }

            try
            {
                DbService.Instance.AddWatchItem(args[0], args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task ApplyDbWatch()
        {
            if (DbService.Instance == null)
            {
                Console.WriteLine("Use dbc command first.");
                return;
            }

            try
            {
                await DbService.Instance.ApplyWatch();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}