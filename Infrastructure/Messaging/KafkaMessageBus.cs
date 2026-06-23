using Application.Ports.Output;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly Dictionary<string, Func<string, Task>> _handlers = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaMessageBus> _logger;
        private bool _isConsuming = false;

        public KafkaMessageBus(
            IOptions<KafkaSettings> settings,
            IServiceProvider serviceProvider,
            ILogger<KafkaMessageBus> logger)
        {
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                ClientId = _settings.ClientId,
                EnableIdempotence = false,
                MessageTimeoutMs = 5000,
                SocketTimeoutMs = 5000
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                AllowAutoCreateTopics = true
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

            _logger.LogInformation("KafkaMessageBus initialized with bootstrap servers: {BootstrapServers}",
                _settings.BootstrapServers);
        }

        public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var topic = GetTopicName<T>();
                var key = Guid.NewGuid().ToString();
                var value = JsonSerializer.Serialize(message, _jsonOptions);

                _logger.LogDebug("Publishing message to topic {Topic}: {Message}", topic, value);

                var result = await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = value
                }, cancellationToken);

                _logger.LogInformation("Message published to topic {Topic}, partition {Partition}, offset {Offset}",
                    result.Topic, result.Partition, result.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to Kafka");
                throw;
            }
        }

        public Task SubscribeAsync<T, TH>(CancellationToken cancellationToken = default)
            where T : class
            where TH : IEventHandler<T>
        {
            var topic = GetTopicName<T>();

            _logger.LogInformation("Subscribing to topic: {Topic} with handler: {Handler}",
                topic, typeof(TH).Name);

            _handlers[topic] = async (messageJson) =>
            {
                try
                {
                    _logger.LogDebug("Processing message from topic {Topic}: {Message}", topic, messageJson);

                    var message = JsonSerializer.Deserialize<T>(messageJson, _jsonOptions);
                    if (message != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<TH>();
                        await handler.HandleAsync(message, cancellationToken);

                        _logger.LogInformation("Successfully processed message from topic {Topic}", topic);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from topic {Topic}", topic);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                    throw;
                }
            };

            if (!_isConsuming)
            {
                _isConsuming = true;
                _ = Task.Run(() => ConsumeLoop(cancellationToken), cancellationToken);
            }

            return Task.CompletedTask;
        }

        private async Task ConsumeLoop(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(_handlers.Keys);

            _logger.LogInformation("Started consuming from topics: {Topics}", string.Join(", ", _handlers.Keys));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromSeconds(1));
                    if (result == null) continue;

                    _logger.LogDebug("Received message from topic {Topic}, partition {Partition}, offset {Offset}",
                        result.Topic, result.Partition, result.Offset);

                    if (_handlers.TryGetValue(result.Topic, out var handler))
                    {
                        await handler(result.Message.Value);
                        _consumer.Commit(result);
                        _logger.LogDebug("Message from topic {Topic} committed", result.Topic);
                    }
                    else
                    {
                        _logger.LogWarning("No handler registered for topic: {Topic}", result.Topic);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Consume loop cancelled");
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error from topic");
                    await Task.Delay(1000, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in consume loop");
                    await Task.Delay(1000, cancellationToken);
                }
            }

            _consumer.Close();
            _logger.LogInformation("Consume loop stopped");
        }

        private static string GetTopicName<T>()
        {
            var typeName = typeof(T).Name;
            // OrderCancelledEvent -> ordercancelled
            var topicName = typeName.Replace("Event", "").ToLowerInvariant();
            return topicName;
        }

        public void Dispose()
        {
            _producer?.Dispose();
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "161.104.19.132:9092";
        public string ClientId { get; set; } = "product-service";
        public string GroupId { get; set; } = "product-service-group";
    }
}