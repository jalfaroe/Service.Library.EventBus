using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Service.Library.EventBus.AzureServiceBus;
using Service.Library.EventBus.UnitTests.FakeEventHandlers;
using Service.Library.EventBus.UnitTests.FakeEvents;

namespace Service.Library.EventBus.UnitTests.AzureServiceBus
{
    [TestClass]
    public class EventSubscriberTest
    {
        private const string TopicName1 = "TopicName1";

        private const string TopicName2 = "TopicName2";

        private const string Subscriptioname1 = "SubscriptioName1";

        private const string Subscriptioname2 = "SubscriptioName2";

        private IEventSubscriberConfiguration configuration;

        private IFixture fixture;

        private IServiceProvider inversionOfControlContainer;

        private ILogger logger;

        private ISubscriptionClientFactory subscriptionClientFactory;

        private EventSubscriber sut;

        [TestInitialize]
        public void TestInitialize()
        {
            fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            inversionOfControlContainer = fixture.Freeze<IServiceProvider>();
            configuration = fixture.Freeze<IEventSubscriberConfiguration>();
            subscriptionClientFactory = fixture.Freeze<ISubscriptionClientFactory>();
            logger = fixture.Freeze<ILogger>();

            IList<SubscriptionInfo> subscriptionsConfig = new List<SubscriptionInfo>
            {
                new SubscriptionInfo(
                    typeof(FakeEvent1),
                    typeof(FakeEvent1Handler),
                    TopicName1,
                    Subscriptioname1,
                    RetryPolicyBase.DefaultRetry),
                new SubscriptionInfo(
                    typeof(FakeEvent2),
                    typeof(FakeEvent2Handler),
                    TopicName2,
                    Subscriptioname2,
                    RetryPolicyBase.DefaultRetry)
            };

            configuration.ServiceBusConnectionString.Returns(fixture.Create<string>());
            configuration.Subscriptions.Returns(subscriptionsConfig);

            sut = fixture.Create<EventSubscriber>();
        }

        [TestMethod]
        public void StartReceivingEvents_WhenDisposed_ThrowObjectDisposedException()
        {
            // Arrange
            sut.Dispose();

            // Act
            Action act = () => sut.StartReceivingEvents();

            // Assert
            act.Should().Throw<ObjectDisposedException>().WithMessage(
                "Cannot access a disposed object.\r\nObject name: 'subscriptionClients'.");
        }

        [TestMethod]
        public void StartReceivingEvents_WithValidConfig_CallsTopicClientFactory()
        {
            // Arrange => Act
            sut.StartReceivingEvents();

            // Assert
            subscriptionClientFactory.Received(2).Create(
                configuration.ServiceBusConnectionString,
                Arg.Any<string>(),
                Arg.Any<string>(),
                RetryPolicyBase.DefaultRetry);
        }

        [TestMethod]
        public void Dispose_WhenHaventAlreadyBeenDisposed_CloseAllSubscriptionClients()
        {
            // Arrange
            var subscriptionClient1 = fixture.Create<ISubscriptionClient>();
            var subscriptionClient2 = fixture.Create<ISubscriptionClient>();

            subscriptionClientFactory.Create(configuration.ServiceBusConnectionString, TopicName1, Subscriptioname1,
                    RetryPolicyBase.DefaultRetry)
                .Returns(subscriptionClient1);
            subscriptionClientFactory.Create(configuration.ServiceBusConnectionString, TopicName2, Subscriptioname2,
                    RetryPolicyBase.DefaultRetry)
                .Returns(subscriptionClient2);

            sut.StartReceivingEvents();

            // Act
            sut.Dispose();

            // Assert
            subscriptionClient1.Received(1).CloseAsync();
            subscriptionClient2.Received(1).CloseAsync();
        }

        [TestMethod]
        public void Dispose_WhenAlreadyHasBeenDisposed_CloseAllSubscriptionClientsOnlyOnce()
        {
            // Arrange
            var subscriptionClient1 = fixture.Create<ISubscriptionClient>();
            var subscriptionClient2 = fixture.Create<ISubscriptionClient>();

            subscriptionClientFactory.Create(configuration.ServiceBusConnectionString, TopicName1, Subscriptioname1,
                    RetryPolicyBase.DefaultRetry)
                .Returns(subscriptionClient1);
            subscriptionClientFactory.Create(configuration.ServiceBusConnectionString, TopicName2, Subscriptioname2,
                    RetryPolicyBase.DefaultRetry)
                .Returns(subscriptionClient2);

            sut.StartReceivingEvents();

            // Act
            sut.Dispose();

#pragma warning disable S3966 // Objects should not be disposed more than once
            sut.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once

            // Assert
            subscriptionClient1.Received(1).CloseAsync();
            subscriptionClient2.Received(1).CloseAsync();
        }
    }
}