using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pyanulis.PingPong.Bus
{
    public sealed class QueueRegistry : IDisposable, INotifyPropertyChanged
    {
        private readonly ManagementClient m_client;
        private static QueueRegistry m_instance;
        private List<Exchange> m_exchanges;
        private List<Binding> m_bindings;
        private List<Queue> m_queues;

        private Exchange m_selectedExchange;
        private Binding m_selectedBinding;

        private Queue m_selectedQueue;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static QueueRegistry CurrentInstance => m_instance;

        private QueueRegistry(string hostPort, string user, string password)
        {
            Uri uri = new Uri(hostPort);
            m_client = new(uri, user, password);
        }


        public Queue SelectedQueue
        {
            get { return m_selectedQueue; }
            set
            {
                if (m_selectedQueue == value)
                {
                    return;
                }

                m_selectedQueue = value;
                OnPropertyChanged();
            }
        }

        public Binding SelectedBinding
        {
            get { return m_selectedBinding; }
            set 
            {
                if (m_selectedBinding == value)
                {
                    return;
                }

                m_selectedBinding = value;
                OnPropertyChanged();

                LoadQueues();
            }
        }

        public Exchange SelectedExchange
        {
            get { return m_selectedExchange; }
            set 
            {
                if (m_selectedExchange == value)
                {
                    return;
                }

                m_selectedExchange = value;
                OnPropertyChanged();

                LoadBindings();
            }
        }

        public IReadOnlyList<Exchange> Exchanges => m_exchanges;
        public IReadOnlyList<Binding> ExchangeBindings => m_bindings;
        public IReadOnlyList<Queue> ExchangeQueues => m_queues;

        public static async Task CreateNew(string hostPort, string user, string password)
        {
            m_instance?.Dispose();
            m_instance = null;

            m_instance = new QueueRegistry(hostPort, user, password);

            await m_instance.LoadExchanges();
            //await m_instance.LoadBindings();
            //await m_instance.LoadQueues();
        }

        public async Task LoadExchanges()
        {
            m_exchanges = new (await m_client.GetExchangesAsync());

            SelectedExchange = m_exchanges.FirstOrDefault();
        }

        public async Task LoadBindings()
        {
            if (SelectedExchange == null)
            {
                SelectedBinding = null;
                return;
            }

            m_bindings = new(await m_client.GetBindingsWithSourceAsync(SelectedExchange));

            SelectedBinding = m_bindings.FirstOrDefault();
        }

        public async Task LoadQueues()
        {
            if (SelectedExchange == null)
            {
                SelectedBinding = null;
                return;
            }

            m_queues = new((await m_client.GetQueuesAsync(SelectedExchange.Vhost)).Where(q=> m_bindings.Select(b=>b.Destination).Contains(q.Name)));

            SelectedQueue = m_queues.FirstOrDefault();
        }

        public void Dispose()
        {
            m_client?.Dispose();
        }

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChangedEventHandler? handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
