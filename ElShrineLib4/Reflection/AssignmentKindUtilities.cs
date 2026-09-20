namespace ElShrine.Reflection;

internal class AssignmentKindUtilities
{
    public static bool CheckAssignable(AssignmentKind kind, Type sourceType, Type targetType, out string msg)
    {
        var suc = false;
        var reason = string.Empty;
        switch (kind)
        {
            case AssignmentKind.Any:
                if (targetType.IsAssignableTo(sourceType) || sourceType.IsAssignableTo(targetType))
                    suc = true;
                else
                    reason = $"Expected {targetType.FullName} or {targetType.FullName}'s implement or subtype but received non-relative {sourceType.FullName}.";
                break;
            case AssignmentKind.Same:
                if (targetType == sourceType)
                    suc = true;
                else
                    reason = $"Expected {targetType.FullName} but received {sourceType.FullName}.";
                break;
            case AssignmentKind.AssignableTo:
                if (sourceType.IsAssignableTo(targetType))
                    suc = true;
                else
                    reason = $"Expected {targetType.FullName}'s implement or subtype but received {sourceType.FullName}.";
                break;
            case AssignmentKind.AssignableFrom:
                if (targetType.IsAssignableTo(sourceType))
                    suc = true;
                else
                    reason = $"Expected {targetType.FullName}'s base or supertype but received {sourceType.FullName}.";
                break;
        }
        msg = reason;
        return suc;
    }
}
