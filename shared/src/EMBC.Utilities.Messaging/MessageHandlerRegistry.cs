﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EMBC.Utilities.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EMBC.Utilities.Messaging
{
    internal class MessageHandlerRegistry
    {
        private readonly Dictionary<Type, MethodInfo> registry = new Dictionary<Type, MethodInfo>();
        private readonly ITelemetryReporter logger;

        public MessageHandlerRegistry(ITelemetryProvider telemetryProvider, IOptions<MessageHandlerRegistryOptions> options)
        {
            this.logger = telemetryProvider.Get<MessageHandlerRegistry>();
            foreach (var type in options.Value.RegisteredHandlers)
            {
                Register(type);
            }
        }

        public MethodInfo Resolve(Type requestType)
        {
            var type = registry.Keys.SingleOrDefault(k => k.Equals(requestType));
            if (type == null) throw new ArgumentException($"Request type '{requestType.AssemblyQualifiedName}' has no registered handlers");
            var method = registry[type] ?? null!;

            logger.LogDebug("Resolved handler {0} for type {1}", $"{method.DeclaringType?.FullName}.{method.Name}", type);

            return method;
        }

        public void Register<THandlerService>() => Register(typeof(THandlerService));

        public void Register(Type handlerType)
        {
            var methods = handlerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                Register(method);
            }
        }

        private void Register(MethodInfo method)
        {
            var methodName = $"{method.DeclaringType?.FullName ?? null!}.{method.Name}";
            var requestType = method.GetParameters().Single().ParameterType ?? null!;
            var parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                throw new InvalidOperationException($"Handler {methodName} can only have a single parameter");
            }

            if (!registry.TryAdd(requestType, method))
            {
                throw new InvalidOperationException($"Type '{requestType.AssemblyQualifiedName}' already has a registered handler: " +
                    $"{registry[requestType].DeclaringType?.FullName}.{registry[requestType].Name}, cannot register {methodName})");
            }
            logger.LogDebug("Registered {0} to handle type '{1}'", methodName, requestType.AssemblyQualifiedName);
        }
    }

    public class MessageHandlerRegistryOptions
    {
        private readonly List<Type> handlerTypes = new List<Type>();
        public IEnumerable<Type> RegisteredHandlers => handlerTypes;

        public void Add(Type handlerType) => handlerTypes.Add(handlerType);
    }
}
