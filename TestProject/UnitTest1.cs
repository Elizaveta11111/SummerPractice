using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class MessageBrokerTests
{
    [TestMethod]
    public void ShouldCreateExchangeOnceMessageSent()
    {
        // Arrange
        var connectionMock = new Mock<IMessageBrokerConnectionGeneric<Mock>>();// new FakeBrokerConnection();
        var channelMock = new Mock<IMessageBrokerChannel>(); // FakeBrokerChannel();

        // Act
        var sut = new MessageBrokerGeneric<Mock>(connectionMock.Object, channelMock.Object);
        sut.Send("ABCDE", "asd", new byte[] { 1, 2, 3, 4 });

        // Assert
        channelMock.Verify(x => x.CreateExchange("ABCDE", "topic"));
    }

    [TestMethod]
    public void ShouldCallBrokerPublishOnceMessageSent()
    {
        var connectionMock = new Mock<IMessageBrokerConnectionGeneric<Mock>>();
        var channelMock = new Mock<IMessageBrokerChannel>();

        var sut = new MessageBrokerGeneric<Mock>(connectionMock.Object, channelMock.Object);
        sut.Send("ABCDE", "asd", new byte[] { 1, 2, 3, 4 });

        channelMock.Verify(x => x.Publish("ABCDE", "asd", new byte[] { 1, 2, 3, 4 }));
    }

    [TestMethod]
    public void ShouldDisposeConnectionOnceBrockerDispose()
    {
        var connectionMock = new Mock<IMessageBrokerConnectionGeneric<Mock>>();
        var channelMock = new Mock<IMessageBrokerChannel>();

        var sut = new MessageBrokerGeneric<Mock>(connectionMock.Object, channelMock.Object);
        sut.Dispose();

        connectionMock.Verify(x => x.Dispose());
    }

    [TestMethod]
    public void ShouldDisposeChannelOnceBrockerDispose()
    {
        var connectionMock = new Mock<IMessageBrokerConnectionGeneric<Mock>>();
        var channelMock = new Mock<IMessageBrokerChannel>();

        var sut = new MessageBrokerGeneric<Mock>(connectionMock.Object, channelMock.Object);
        sut.Dispose();

        channelMock.Verify(x => x.Dispose());
    }

    [TestMethod]
    public void ShouldCallDelayOnceMessageReceived()
    {
        var connectionMock = new Mock<IMessageBrokerConnectionGeneric<Mock>>();
        var channelMock = new Mock<IMessageBrokerChannel>();
        var delayProvider = new Mock<IDelayProvider>();

        var sut = new MessageBrokerGeneric<Mock>(connectionMock.Object, channelMock.Object);
        sut.ReceiveNoWait("aaa", 5, delayProvider.Object);

        delayProvider.Verify(x => x.Delay(5));
    }

    [TestMethod]
    public void ShouldDeclareQueueOnceMessageReceived()
    {
        var connectionMock = new Mock<IMessageBrokerConnectionGeneric<Mock>>();
        var channelMock = new Mock<IMessageBrokerChannel>();

        var sut = new MessageBrokerGeneric<Mock>(connectionMock.Object, channelMock.Object);
        var delayProvider = new Mock<IDelayProvider>();
        sut.ReceiveNoWait("aaa", 5, delayProvider.Object);

        channelMock.Verify(x => x.QueueDeclarePassive("aaa"));
    }
}