using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Text;
using RabbitMQ.Client;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MessageBrokerController : ControllerBase
    {
        private readonly Serilog.ILogger _logger;
        private readonly IMessageBroker _messageBroker;
        private readonly IDelayProvider _delayProvider;

        public MessageBrokerController(Serilog.ILogger logger, IMessageBroker messageBroker, IDelayProvider delayProvider)
        {
            _logger = logger;
            _messageBroker = messageBroker;
            _delayProvider = delayProvider;
        }

        public string SendLog(string key, string message)
        {
            return SendMessage("log", key, message);
        }
        private string SendMessage(string exchangeName, string key, string message)
        {
            _messageBroker.Send(exchangeName, key, Encoding.UTF8.GetBytes(message));
            return exchangeName + " " + key + " " + message;
        }
        public string RecieveLog(string key, int timeOutSeconds)
        {
            return RecieveMessages("log", key, timeOutSeconds);
        }
        private string RecieveMessages(string exchangeName, string key, int timeOutSeconds)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: exchangeName, type: "topic");
                var queueName = channel.QueueDeclare(exclusive: false).QueueName;

                channel.QueueBind(queue: queueName,
                                  exchange: exchangeName,
                                  routingKey: key);

                IList<Message> messages = _messageBroker.ReceiveNoWait(queueName, timeOutSeconds, _delayProvider);
                string recieved = "";
                foreach (Message message in messages)
                    recieved += message.routingKey + " " + Encoding.UTF8.GetString(message.messageBody) + "\n";
                return recieved;
            }
        }
    }
}