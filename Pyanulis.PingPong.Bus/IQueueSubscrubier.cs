using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pyanulis.PingPong.Bus
{
    public interface IQueueSubscrubier
    {
        public void RegisterProducing(string exchange, string routKey);

        public void SubscribeConsumingType(string queueName, Type message);
    }
}
