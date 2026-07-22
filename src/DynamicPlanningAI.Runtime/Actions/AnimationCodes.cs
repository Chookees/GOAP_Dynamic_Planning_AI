namespace DynamicPlanningAI.Runtime.Actions;

/// <summary>
/// Host animation codes used by standard action executors.
/// </summary>
public static class AnimationCodes
{
    /// <summary>Idle/readiness animation.</summary>
    public const int Idle = 1;

    /// <summary>Traversal/vault animation.</summary>
    public const int Traverse = 2;

    /// <summary>Door breach animation.</summary>
    public const int Breach = 3;

    /// <summary>Weapon switch animation.</summary>
    public const int SwitchWeapon = 4;

    /// <summary>Grenade throw animation.</summary>
    public const int ThrowGrenade = 5;

    /// <summary>Dodge/escape animation.</summary>
    public const int Dodge = 6;
}
