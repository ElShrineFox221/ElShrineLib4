namespace ElShrine.Reflection;

[Flags]
public enum AssignmentKind
{
    Any = 0,
    AssignableTo = 1,
    AssignableFrom = 2,

    Same = AssignableTo | AssignableFrom,
}
