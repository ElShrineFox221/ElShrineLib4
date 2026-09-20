using System.Reflection;

namespace ElShrine.Reflection;

public static class ValidationAttributeExtensions
{
    public static bool IsAggregate(this ValidationResult result)
        => result.Type == typeof(object);
    public static IReadOnlyList<ValidationResult> Flatten(this ValidationResult source, bool onlyFailure = false)
    {
        if (onlyFailure && source.IsSuccess)
            return [];
        if (!source.IsAggregate())
            return [source];
        var list = new List<ValidationResult>();
        foreach (var item in source.NestedResults)
            list.AddRange(item.Flatten(onlyFailure));
        return list;
    }
    public static ValidationResult Validate(this Type type)
    {
        var attrs = type.GetCustomAttributes<ValidationBaseAttribute>();
        if (!attrs.Any())
            return ValidationResult.Success(type);
        var results = new List<ValidationResult>();
        foreach (var attr in attrs)
        {
            // validate type
            var typeData = new ValidationData(type);
            results.Add(attr.Validate(typeData));
            // validate members
            var members = type.GetMembers().Where(static m => m.IsDefined(typeof(ValidationBaseAttribute)));
            foreach (var member in members)
            {
                var memberData = new ValidationData(member);
                results.Add(attr.Validate(memberData));
            }
        }
        return ValidationResult.Aggregate([.. results]);
    }
    public static ValidationResult Validate(this Assembly assembly)
    {
        var types = assembly.GetAssemblyTypes().Where(static t => t.IsDefined(typeof(ValidationBaseAttribute), inherit: true));
        var results = new List<ValidationResult>();
        foreach (var t in types)
        {
            try
            {
                results.Add(t.Validate());
            }
            catch (Exception e)
            {
                results.Add(ValidationResult.Failed(t, $"{e.GetType().Name}: {e.Message}"));
            }
        }
        var result = ValidationResult.Aggregate([.. results]);
        return result;
    }
}
