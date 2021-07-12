using System;
using System.Linq;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

public class Message
{
    public string routingKey;
    public byte[] messageBody;
    public Message(string routingKey, byte[] messageBody)
    {
        this.routingKey = routingKey;
        this.messageBody = messageBody;
    }
}

public interface IDelayProvider
{
    void Delay(int timeOutSeconds);
}

public class ThreadDelayProvider : IDelayProvider
{
    public void Delay(int timeOutSeconds)
    {
        Thread.Sleep(timeOutSeconds * 1000);
    }
}

public interface IMessageBroker
{
    void Send(string exchange, string routingKey, byte[] messageBody);
    IList<Message> ReceiveNoWait(string queueName, int timeOutSeconds, IDelayProvider delayProvider);
}

public interface IMessageBrokerChannel : IDisposable
{
    void CreateExchange(string exchange, string exchangeType);
    void Publish(string exchange, string routingKey, byte[] messageBody);
    void QueueDeclarePassive(string queueName);
    void Receive(string queueName, Action<Message> onMessageReceived);
}

public interface IMessageBrokerConnectionGeneric<T> : IDisposable where T : class
{
    T CreateChannel();
}

public class RabbitMqMessageBrokerConnectionGeneric : IMessageBrokerConnectionGeneric<IModel>
{
    private readonly IConnection rabbitMqConnection;

    public RabbitMqMessageBrokerConnectionGeneric(IConnection rabbitMqConnection) => 
        this.rabbitMqConnection = rabbitMqConnection;

    public IModel CreateChannel() => this.rabbitMqConnection.CreateModel();

    public void Dispose() => rabbitMqConnection.Dispose();
}

public class RabbitMQMessageBrokerChannel : IMessageBrokerChannel
{
    private readonly IModel rabbitMqChannel;

    public RabbitMQMessageBrokerChannel(IMessageBrokerConnectionGeneric<IModel> messageBrokerConnection)
    {
        this.rabbitMqChannel = messageBrokerConnection.CreateChannel();
    }

    public void CreateExchange(string exchange, string exchangeType)
    {
        rabbitMqChannel.ExchangeDeclare(exchange: exchange, type: exchangeType);
    }

    public void Dispose()
    {
        rabbitMqChannel.Dispose();
    }

    public void Publish(string exchange, string routingKey, byte[] messageBody)
    {
        rabbitMqChannel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: null, body: messageBody);
    }

    public void QueueDeclarePassive(string queueName)
    {
        rabbitMqChannel.QueueDeclarePassive(queueName);
    }

    public void Receive(string queueName, Action<Message> onMessageReceived)
    {
        var consumer = new EventingBasicConsumer(rabbitMqChannel);
        consumer.Received += (model, ea) =>
        {
            onMessageReceived(new Message(ea.RoutingKey, ea.Body.ToArray()));
        };
        rabbitMqChannel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
    }
}

public class MessageBrokerGeneric<ChannelType> : IMessageBroker, IDisposable where ChannelType : class
{
    private readonly IMessageBrokerChannel channel;
    private readonly IMessageBrokerConnectionGeneric<ChannelType> connection;

    public MessageBrokerGeneric(IMessageBrokerConnectionGeneric<ChannelType> connection, IMessageBrokerChannel channel)
    {
        this.channel = channel;
        this.connection = connection;
    }

    public void Dispose()
    {
        this.channel.Dispose();
        this.connection.Dispose();
    }

    public IList<Message> ReceiveNoWait(string queueName, int timeOutSeconds, IDelayProvider delayProvider)
    {
        var listOfMessages = new List<Message>();

        channel.QueueDeclarePassive(queueName);

        channel.Receive(queueName, (Message obj) =>
        {
            listOfMessages.Add(obj);
        });

        delayProvider.Delay(timeOutSeconds);

        return listOfMessages;
    }

    public void Send(string exchange, string routingKey, byte[] messageBody)
    {
        channel.CreateExchange(exchange, "topic");
        channel.Publish(exchange, routingKey, messageBody);
    }
}