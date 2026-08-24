using System.Reflection;

namespace ElShrine.DependencyInjection;

public static class ActivatorUtilities
{
    #region InstanceBuilder
    private static readonly NullabilityInfoContext _nullabilityCtx = new();
    // resolveParameter: parameterType, allowNullable, resolvedParam
    private static object CreateInstance(Type instanceType, Func<Type, bool, object?> resolveParameter)
    {
        // validate implement is instantiatable
        if (instanceType.IsInterface || instanceType.IsAbstract)
            throw new InvalidOperationException($"Cannot instantiate abstract type or interface '{instanceType.Name}'.");
        // construct
        var constructors = instanceType.GetConstructors().OrderByDescending(c => c.GetParameters().Length);
        var errorList = new List<Exception>();
        foreach (var ctor in constructors)
        {
            try
            {
                var parameters = ctor.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    // default value
                    if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                        continue;
                    }
                    //
                    var paramType = param.ParameterType;
                    var nullable = _nullabilityCtx.Create(param).ReadState == NullabilityState.Nullable;
                    var isStr = paramType == typeof(string);
                    // value serviceType parameter & string parameter
                    if (isStr || paramType.IsValueType)
                    {
                        if (nullable)
                            args[i] = null;
                        else if (isStr) 
                            args[i] = string.Empty;
                        else
                            args[i] = Activator.CreateInstance(paramType);
                        continue;
                    }
                    // normal
                    try
                    {
                        args[i] = resolveParameter(paramType, nullable);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Failed to resolve parameter (type: {paramType.Name}).", innerException: e);
                    }
                }
                var instance = Activator.CreateInstance(instanceType, args)!;
                return instance;
            }
            catch (Exception e)
            {
                errorList.Add(e);
            }
        }
        throw new InvalidOperationException($"No suitable constructor found for {instanceType.Name}",
            innerException: new AggregateException(errorList));
    }
    // queryFromCache: requiredType, allowNullable, queryResult
    private static object Resolver(
        Type type,
        Func<Type, bool, object?> queryFromCache,
        Func<Type, Type> getInstanceType,
        Action<Type, object> onInstanceCreated)
    {
        var resolvingStack = new Stack<Type>();
        return Resolve(type, false)!;
        
        object? Resolve(Type type, bool allowNullable)
        {
            var r = queryFromCache(type, allowNullable);
            if (allowNullable || r is not null)
                return r;
            // validate circular dependency
            if (resolvingStack.Contains(type))
                throw new InvalidOperationException($"Circular dependency detected for type '{type.Name}'.");
            // begin resolve with stack
            resolvingStack.Push(type);
            var implementType = getInstanceType(type);
            try
            {
                var instance = CreateInstance(implementType, Resolve);
                onInstanceCreated(type, instance);
                return instance;
            }
            finally
            {
                resolvingStack.Pop();
            }
        }
    }

    #region Service
    public static object ServiceConstructorInvoker(
        Type serviceType,
        Func<Type, object?> additionalResolve,
        IDictionary<Type, object> singletons,
        IDictionary<Type, object> scopeds,
        IReadOnlyDictionary<Type, ServiceInfo> serviceInfos)
    {
        var temporaryCache = new Dictionary<Type, object>();
        lock (singletons)
        {
            lock (scopeds)
            {
                var r = Resolver(serviceType, queryFromCache, getInstanceType, onInstanceCreated);
                return r;
            }
        }

        object? queryFromCache(Type type, bool allowNullable)
        {
            var r = additionalResolve(type);
            if (r is not null) 
                return r;
            // query instance if exists
            if (!serviceInfos.TryGetValue(type, out var info))
            {
                if (allowNullable)
                    return null;
                throw new InvalidOperationException($"Service type {type.Name} unregistered.");
            }
            if (temporaryCache.TryGetValue(type, out var temp))
                return temp;
            switch (info.Lifetime)
            {
                case ServiceLifetime.Scoped:
                    if (scopeds.TryGetValue(type, out var scoped))
                    {
                        temporaryCache[type] = scoped;
                        return scoped;
                    }
                    break;
                case ServiceLifetime.Singleton:
                    if (singletons.TryGetValue(type, out var singleton))
                    {
                        temporaryCache[type] = singleton;
                        return singleton;
                    }
                    break;
            }
            return null;
        }
        Type getInstanceType(Type type)
            => serviceInfos[type].ImplementType;
        void onInstanceCreated(Type type, object instance)
        {
            var info = serviceInfos[type];
            temporaryCache[type] = instance;

            if (info.Lifetime == ServiceLifetime.Scoped)
                scopeds[type] = instance;
            else if (info.Lifetime == ServiceLifetime.Singleton)
                singletons[type] = instance;
        }
    }

    public static TService ServiceConstructorInvoker<TService>(
        Func<Type, object?> additionalResolve,
        IDictionary<Type, object> singletons,
        IDictionary<Type, object> scopeds,
        IReadOnlyDictionary<Type, ServiceInfo> serviceInfos)
        => (TService)ServiceConstructorInvoker(typeof(TService), additionalResolve, singletons, scopeds, serviceInfos);
    #endregion

    #region Instance
    public static object InstanceConstructorInvoker(
         Type instanceType,
         IDictionary<Type, object> cachedInstances,
         IReadOnlyDictionary<Type, Type> typeMap,
         bool cacheUnmappeds = false)
    {
        var temporaryCache = new Dictionary<Type, object>();
        lock (cachedInstances)
        {
            var r = Resolver(instanceType, queryFromCache, getInstanceType, onInstanceCreated);
            return r;
        }

        object? queryFromCache(Type type, bool allowNullable)
        {
            // query instance if exists
            if (temporaryCache.TryGetValue(type, out var temporaryCached))
                return temporaryCached;
            if (cachedInstances.TryGetValue(type, out var cached))
            {
                temporaryCache[type] = cached;
                return cached;
            }
            return null;
        }
        Type getInstanceType(Type type)
            => typeMap.TryGetValue(type, out var mappedType) ? mappedType : type;
        void onInstanceCreated(Type type, object instance)
        {
            temporaryCache[type] = instance;

            if (cacheUnmappeds || typeMap.ContainsKey(type))
                cachedInstances[type] = instance;
        }
    }
    public static TInstance InstanceConstructorInvoker<TInstance>(
        IDictionary<Type, object> cachedInstances,
        IReadOnlyDictionary<Type, Type> typeMap,
        bool cacheUnmappeds = false)
        => (TInstance)InstanceConstructorInvoker(typeof(TInstance), cachedInstances, typeMap, cacheUnmappeds);
    #endregion

    #endregion
}
