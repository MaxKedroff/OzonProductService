using Application.Ports.Output;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.Messaging
{
    public class KafkaMessageBus : IMessageBus, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly IConsumer<string, string> _consumer;
        private readonly KafkaSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public KafkaMessageBus(IOptions<KafkaSettings> settings)
        {
            _settings = settings.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                ClientId = _settings.ClientId
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        }

        public void Dispose()
        {
            _producer?.Dispose();
            _consumer?.Dispose();
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            var topic = GetTopicName<T>();
            var key = Guid.NewGuid().ToString();
            var value = JsonSerializer.Serialize(message, _jsonOptions);
            await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = value
            }, cancellationToken);
        }

        public async Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class
            where TH : IEventHandler<T>
        {
            var topic = GetTopicName<T>();
            _consumer.Subscribe(topic);
            _handlers[typeof(T)] = async (string messageJson) =>
            {
                var message = JsonSerializer.Deserialize<T>(messageJson, _jsonOptions);
                if (message != null)
                {
                    var handler = Activator.CreateInstance<TH>();
                    await handler.HandleAsync(message, cancellationToken);
                }
            };
            _ = Task.Run(() => ConsumeLoop(cancellationToken), cancellationToken);
        }

        private async Task ConsumeLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(cancellationToken);
                    if (_handlers.TryGetValue(result.Message.Value.GetType(), out var handler))
                    {
                        await ((Func<string, Task>)handler)(result.Message.Value);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            } 
        }

        private static string GetTopicName<T>() => typeof(T).Name.Replace("Event", "").ToLower();
    }

    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string ClientId { get; set; } = "product-service";
        public string GroupId { get; set; } = "product-service-group";
    }

}
