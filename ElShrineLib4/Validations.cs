using ElShrine.Reflection;
using System.Reflection;

namespace ElShrine;

public readonly struct ValidationData
{
    public ValidationData(Type type)
    {
        DefinedType = type;
        DefinedMember = null;
    }
    public ValidationData(MemberInfo memberInfo)
    {
        if (memberInfo.DeclaringType is null)
            throw new InvalidValidationDataException(memberInfo);
        DefinedType = memberInfo.DeclaringType;
        DefinedMember = memberInfo;
    }
    /// <summary>
    /// Class, struct, enum, interface or delegate if attributed to a certain type. 
    /// </summary>
    public readonly Type DefinedType;
    /// <summary>
    /// Null if attributed to a type.
    /// </summary>
    public readonly MemberInfo? DefinedMember;
}
// Result
public sealed class ValidationResult
{
    private readonly List<ValidationResult> _nestedResults = [];
    public readonly bool IsSuccess;
    public readonly Type Type;
    public readonly string FailedReason;
    public IReadOnlyList<ValidationResult> NestedResults => _nestedResults.AsReadOnly();
    private ValidationResult(bool isSuccess, Type type, string failedReason)
    {
        IsSuccess = isSuccess;
        Type = type;
        FailedReason = failedReason;
    }

    public static ValidationResult Success(Type type)
        => new(true, type, string.Empty);
    public static ValidationResult Failed(Type type, string reason)
        => new(false, type, reason);
    public static ValidationResult Aggregate(params ValidationResult[] results)
    {
        var failedCount = results.Count(x => !x.IsSuccess);
        var r = new ValidationResult(failedCount == 0, typeof(object), failedCount == 0 ? string.Empty : $"{failedCount} validation(s) failed.");
        r._nestedResults.AddRange(results);
        return r;
    }

    #region implicit
    public static implicit operator bool(ValidationResult v) => v.IsSuccess;
    #endregion
}

// Exception
public sealed class InvalidValidationDataException(MemberInfo memberInfo) :
    Exception($"Invalid validation data: {memberInfo.Name} has no valid declaring type.")
{
    public readonly MemberInfo MemberInfo = memberInfo;
}

#region Validation bases

[AttributeUsage(AllValidationTargets)]
public abstract class ValidationBaseAttribute() : Attribute()
{
    public const AttributeTargets TypeValidationTargets = 
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate;
    public const AttributeTargets MemberValidationTargets =
        AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Event;
    public const AttributeTargets AllValidationTargets = TypeValidationTargets | MemberValidationTargets;
    public abstract ValidationResult Validate(ValidationData data);
}
[AttributeUsage(TypeValidationTargets)]
public abstract class TypeValidationBaseAttribute() : ValidationBaseAttribute()
{
    public sealed override ValidationResult Validate(ValidationData data)
    {
        if (data.DefinedMember is not null)
            return ValidationResult.Failed(data.DefinedType,
                "Invalid validation data: TypeValidationBaseAttribute expects validation data without member info.");
        return Validate(data.DefinedType);
    }
    protected abstract ValidationResult Validate(Type type);
}
public class AssignableValidation<T> : TypeValidationBaseAttribute
{
    public AssignmentKind Kind { get; init; } = AssignmentKind.AssignableTo;

    protected AssignableValidation(AssignmentKind kind)
    {
        Kind = kind;
    }

    protected override ValidationResult Validate(Type type)
    {
        var suc = AssignmentKindUtilities.CheckAssignable(Kind, type, typeof(T), out var reason);
        return suc ? ValidationResult.Success(type) : ValidationResult.Failed(type, reason);
    }
}
#endregion