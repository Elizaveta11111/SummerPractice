using System;
using System.Linq;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using RabbitMQ.Client.Events;
using System.IO;
using RabbitMQ.Client.Exceptions;

namespace WebApplication
{
    struct Message
    {
        public string routingKey;
        public byte[] messageBody;
        public Message(string routingKey, byte[] messageBody)
        {
            this.routingKey = routingKey;
            this.messageBody = messageBody;
        }
    }

    interface IMessageBroker
    {
        void Send(string exchange, string routingKey, byte[] messageBody);
        IList<Message> ReceiveNoWait(string queueName, int timeOutSeconds);
    }

    class TopicExchange : IMessageBroker
    {
        public void Send(string exchangeName, string routingKey, byte[] messageBody)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: exchangeName,
                                        type: "topic");
                channel.BasicPublish(exchange: exchangeName,
                                     routingKey: routingKey,
                                     basicProperties: null,
                                     body: messageBody);
            }
        }
        public IList<Message> ReceiveNoWait(string queueName, int timeOutSeconds)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                IList<Message> messages = new List<Message>();
                try
                {
                    channel.QueueDeclarePassive(queueName);
                }
                catch (OperationInterruptedException e)
                {
                    messages.Add(new Message("error", Encoding.UTF8.GetBytes(e.Message)));
                    return messages;
                }
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    messages.Add(new Message(ea.RoutingKey, ea.Body.ToArray()));
                };
                channel.BasicConsume(queue: queueName,
                     autoAck: true,
                     consumer: consumer);
                Thread.Sleep(timeOutSeconds * 1000);
                return messages;
            }
        }
    }
}
