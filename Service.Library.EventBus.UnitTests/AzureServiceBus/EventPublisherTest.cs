using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Castle.Core.Internal;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Service.Library.EventBus.AzureServiceBus;
using Service.Library.EventBus.UnitTests.FakeEvents;

namespace Service.Library.EventBus.UnitTests.AzureServiceBus
{
    [TestClass]
    public class EventPublisherTest
    {
        private const string TopicName1 = "TopicName1";
        private const string TopicName2 = "TopicName2";

        private IEventPublisherConfiguration configuration;

        private IFixture fixture;

        private EventPublisher sut;

        private ITopicClientFactory topicClientFactory;

        [TestInitialize]
        public void TestInitialize()
        {
            fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            configuration = fixture.Freeze<IEventPublisherConfiguration>();
            topicClientFactory = fixture.Freeze<ITopicClientFactory>();

            var publisherInfos =
                new List<PublisherInfo>
                {
                    new PublisherInfo(typeof(FakeEvent1), TopicName1, RetryPolicyBase.DefaultRetry),
                    new PublisherInfo(typeof(FakeEvent2), TopicName2, RetryPolicyBase.DefaultRetry)
                };

            configuration.Publishers.Returns(publisherInfos);
            configuration.ServiceBusConnectionString.Returns(fixture.Create<string>());

            sut = fixture.Create<EventPublisher>();
        }

        [TestMethod]
        public void PublishEventsAsync_WithNullEvents_ThrowArgumentNullException()
        {
            // Arrange => Act
            Action act = () => sut.PublishEventsAsync(null).Wait();

            // Assert
            act.Should().Throw<ArgumentNullException>().Where(p => p.ParamName.Equals("eventsToPublish"));
        }

        [TestMethod]
        public void PublishEventsAsync_WithNoTopicMapped_ThrowInvalidOperationException()
        {
            // Arrange
            var integrationEvents = fixture.CreateMany<FakeEvent3>();

            // Act
            Action act = () => sut.PublishEventsAsync(integrationEvents).Wait();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [TestMethod]
        public void PublishEventsAsync_WhenDisposed_ThrowObjectDisposedException()
        {
            // Arrange  
            var fakeEvents = fixture.CreateMany<FakeEvent1>();
            sut.Dispose();

            // Act
            Action act = () => sut.PublishEventsAsync(fakeEvents).Wait();

            // Assert
            act.Should().Throw<ObjectDisposedException>()
                .WithMessage("Cannot access a disposed object.\r\nObject name: 'topicClients'.");
        }

        [TestMethod]
        public void PublishEventsAsync_WithValidInput_CallsTopicClientFactory()
        {
            // Arrange
            var event1 = fixture.Create<FakeEvent1>();
            var event2 = fixture.Create<FakeEvent2>();
            var events = new List<IntegrationEvent> {event1, event2};

            // Act
            sut.PublishEventsAsync(events).Wait();

            // Assert
            topicClientFactory.Received(2).Create(configuration.ServiceBusConnectionString, Arg.Any<string>(),
                RetryPolicyBase.DefaultRetry);
        }

        [TestMethod]
        public void PublishEventsAsync_WithValidInput_SendMessages()
        {
            // Arrange
            var events = fixture.CreateMany<FakeEvent1>();
            var topicClient = fixture.Create<ITopicClient>();

            topicClientFactory.Create(
                configuration.ServiceBusConnectionString,
                TopicName1, RetryPolicyBase.DefaultRetry).Returns(topicClient);

            // Act
            sut.PublishEventsAsync(events).Wait();

            // Assert
            topicClient.Received(3).SendAsync(Arg.Any<Message>());
        }

        [TestMethod]
        public void PublishEventsAsync_WithValidInput_CreateValidMessage()
        {
            // Arrange
            var events = fixture.CreateMany<FakeEvent1>(1);
            var topicClient = fixture.Freeze<ITopicClient>();

            topicClientFactory
                .Create(configuration.ServiceBusConnectionString, Arg.Any<string>(), RetryPolicyBase.DefaultRetry)
                .Returns(topicClient);

            var jsonMessage = JsonConvert.SerializeObject(events.First());
            var messageBody = Encoding.UTF8.GetBytes(jsonMessage);

            // Act
            sut.PublishEventsAsync(events).Wait();

            // Assert
            topicClient.Received(1).SendAsync(
                Arg.Is<Message>(
                    message => message.MessageId.IsNullOrEmpty().Equals(false)
                               && message.Label.Equals(nameof(FakeEvent1))
                               && message.Body.Any(x => messageBody.Contains(x))));
        }

        [TestMethod]
        public void Dispose_WhenHaventAlreadyBeenDisposed_CloseAllTopicClients()
        {
            // Arrange
            var event1 = fixture.Create<FakeEvent1>();
            var event2 = fixture.Create<FakeEvent2>();
            var event3 = fixture.Create<FakeEvent1>();
            var events = new List<IntegrationEvent> {event1, event2, event3};

            var topicClient1 = fixture.Create<ITopicClient>();
            var topicClient2 = fixture.Create<ITopicClient>();


            topicClientFactory.Create(configuration.ServiceBusConnectionString, TopicName1,
                RetryPolicyBase.DefaultRetry).Returns(topicClient1);
            topicClientFactory.Create(configuration.ServiceBusConnectionString, TopicName2,
                RetryPolicyBase.DefaultRetry).Returns(topicClient2);

            sut.PublishEventsAsync(events).Wait();

            // Act
            sut.Dispose();

            // Assert
            topicClient1.Received(1).CloseAsync();
            topicClient2.Received(1).CloseAsync();
        }

        [TestMethod]
        public void Dispose_WhenAlreadyHasBeenDisposed_CloseAllTopicClientsOnlyOnce()
        {
            // Arrange
            var event1 = fixture.Create<FakeEvent1>();
            var event2 = fixture.Create<FakeEvent2>();
            var event3 = fixture.Create<FakeEvent1>();
            var events = new List<IntegrationEvent> {event1, event2, event3};

            var topicClient1 = fixture.Create<ITopicClient>();
            var topicClient2 = fixture.Create<ITopicClient>();

            topicClientFactory.Create(configuration.ServiceBusConnectionString, TopicName1,
                RetryPolicyBase.DefaultRetry).Returns(topicClient1);
            topicClientFactory.Create(configuration.ServiceBusConnectionString, TopicName2,
                RetryPolicyBase.DefaultRetry).Returns(topicClient2);

            sut.PublishEventsAsync(events).Wait();

            // Act
            sut.Dispose();

#pragma warning disable S3966 // Objects should not be disposed more than once
            sut.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once

            // Assert
            topicClient1.Received(1).CloseAsync();
            topicClient2.Received(1).CloseAsync();
        }

        [TestMethod]
        public void Finalizer_WhenNotDisposibleCall_CloseAllTopicClients()
        {
            // Arrange
            var event1 = fixture.Create<FakeEvent1>();
            var events = new List<IntegrationEvent> {event1};

            var topicClient1 = fixture.Create<ITopicClient>();

            topicClientFactory
                .Create(configuration.ServiceBusConnectionString, TopicName1, RetryPolicyBase.DefaultRetry)
                .Returns(topicClient1);
            sut.PublishEventsAsync(events).Wait();

            // Act
            sut = null;

#pragma warning disable S1215 // "GC.Collect" should not be called
            GC.Collect();
#pragma warning restore S1215 // "GC.Collect" should not be called

            GC.WaitForPendingFinalizers();

            // Assert
            topicClient1.Received(1).CloseAsync();
        }
    }
}