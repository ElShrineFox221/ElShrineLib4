using ElShrine.Modules.Log;
using System.Reflection;
using System.Runtime.Loader;

namespace ElShrine.Reflection;

public static class ReflectionExtensions
{
    #region MemberInfo: Property & Field
    public static void SetMemberValue(this Type ownerClass, object? owner, object? value, string memberName)
    {
        var member = ((MemberInfo?)ownerClass.GetProperty(memberName) ?? ownerClass.GetField(memberName)) 
            ?? throw new MissingMemberException(ownerClass.Name, memberName);
        member.SetMemberValue(owner, value);
    }
    public static void SetMemberValue(this MemberInfo member, object? owner, object? value)
    {
        switch (member)
        {
            case PropertyInfo p: 
                p.SetValue(owner, value); 
                break;
            case FieldInfo f: 
                f.SetValue(owner, value); 
                break;
            default: 
                throw new InvalidOperationException("Member must be Property or Field.");
        }
    }
    public static object? GetMemberValue(this MemberInfo member, object? owner) 
        => member switch
    {
        PropertyInfo p => p.GetValue(owner),
        FieldInfo f => f.GetValue(owner),
        _ => throw new InvalidOperationException("Member must be Property or Field.")
    };
    #endregion

    #region Type

    #region predicts
    public static bool IsBaseOrInterfaceOf(this Type baseType, Type targetType)
    {
        if (!baseType.IsGenericTypeDefinition) 
            return baseType.IsAssignableFrom(targetType);
        if (baseType.IsInterface)
        {
            var suc = targetType.IsGenericType && targetType.GetGenericTypeDefinition() == baseType;
            if (suc) 
                return true;
            else 
                return targetType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType);
        }

        var current = targetType;
        while (current is not null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == baseType) 
                return true;
            current = current.BaseType;
        }
        return false;
    }
    public static bool IsImplementOf(this Type subType, Type baseType) 
        => baseType.IsBaseOrInterfaceOf(subType);
    public static bool IsStaticClass(this Type type) 
        => type.IsSealed && type.IsAbstract;
    #endregion

    #endregion

    #region Assembly
    public static Type[] GetAssemblyTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch
        {
            return [];
        }
    }
    public static IEnumerable<Type> GetImplements(this AssemblyLoadContext? alc, Type baseTypeOrInterface)
    {
        alc ??= AssemblyLoadContext.Default;
        var targetRange = alc.Assemblies;
        foreach (var asb in targetRange)
        {
            var types = asb.GetAssemblyTypes();
            foreach (var type in types)
            {
                if (baseTypeOrInterface.IsBaseOrInterfaceOf(type))
                    yield return type;
            }
        }
        yield break;
    }
    public static IEnumerable<KeyValuePair<Type, IReadOnlyList<TAttr>>> GetTypesByAttribute<TAttr>(this AssemblyLoadContext? alc, bool inherit)
        where TAttr : Attribute
    {
        alc ??= AssemblyLoadContext.Default;
        var targetRange = alc.Assemblies;
        foreach (var asb in targetRange)
        {
            var types = asb.GetAssemblyTypes();
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes<TAttr>(inherit).ToList();
                if (attrs.Count > 0)
                    yield return new(type, attrs);
            }
        }
    }
    #endregion

    #region Assembly Context
    private static string GetAssembliesText(int count)
        => nameof(Assembly).GetPuralWithNum(count).ToLower();
    public static Assembly? LoadAssembly(this AssemblyLoadContext alc, AssemblyName name, out bool isLoaded, ILogger? logger)
    {
        isLoaded = true;
        bool predict(Assembly asm)
            => AssemblyName.ReferenceMatchesDefinition(asm.GetName(), name);
        if (AssemblyLoadContext.Default.Assemblies.FirstOrDefault(predict) is Assembly asm_default)
            return asm_default;
        if (alc != AssemblyLoadContext.Default && alc.Assemblies.FirstOrDefault(predict) is Assembly asm)
            return asm;
        isLoaded = false;
        // do load
        try
        {
            asm = alc.LoadFromAssemblyName(name);
            logger?.Info(LogItems.Success, $" Loaded assembly {asm.Location}.".Normal());
            return asm;
        }
        catch (Exception e)
        {
            logger?.Error(e.ToLogItems());
            return null;
        }
    }
    public static bool LoadAllReferencesAssemblies(this AssemblyLoadContext alc, IEnumerable<AssemblyName> roots, ILogger? logger)
    {
        using var sc = logger?.BeginScope("Loading references...");
        var suc = true;
        foreach (var name in roots)
        {
            var asm = alc.LoadAssembly(name, out var isLoaded, sc);
            if (isLoaded)
                continue;
            if (asm is null)
            {
                suc = false;
                break;
            }
            if (!LoadAllReferenceAssembliesInternal(alc, asm))
            {
                suc = false; 
                break;
            }
        }
        bool LoadAllReferenceAssembliesInternal(AssemblyLoadContext ctx, Assembly assembly)
        {
            var refs = assembly.GetReferencedAssemblies();
            var suc1 = true;
            foreach (AssemblyName refAssembly in refs)
            {
                if (ctx.Assemblies.Any(a => a.GetName() == refAssembly))
                    continue;
                try
                {
                    var newLoadedAssembly = ctx.LoadAssembly(refAssembly, out var isLoaded, sc);
                    if (isLoaded)
                        continue;
                    if (newLoadedAssembly is null)
                    {
                        suc1 = false;
                        break;
                    }
                    if (!LoadAllReferenceAssembliesInternal(ctx, newLoadedAssembly))
                    {
                        suc1 = false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    sc?.Error(ex.ToLogItems());
                    suc1 = false;
                    break;
                }
            }
            return suc1;
        }
        if (suc)
            sc?.Info($"Loaded {GetAssembliesText(alc.Assemblies.Count())}.");
        else
            sc?.Error("Failed to load all references assemblies.");
        return suc;
    }
    #endregion
}
