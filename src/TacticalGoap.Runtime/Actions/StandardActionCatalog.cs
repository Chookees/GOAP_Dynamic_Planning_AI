using System;
using TacticalGoap.Abstractions.Results;
using TacticalGoap.Runtime.Planning;

namespace TacticalGoap.Runtime.Actions;

/// <summary>
/// Registers the standard GOAP action definitions, executors, and generators.
/// </summary>
public static class StandardActionCatalog
{
    /// <summary>
    /// Registers every built-in action into the supplied registry.
    /// </summary>
    /// <param name="registry">Destination registry.</param>
    /// <returns>Success or the first registration failure.</returns>
    public static OperationStatus RegisterAll(ActionDefinitionRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        OperationStatus status = RegisterMovement(registry);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterCombat(registry);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterSurvival(registry);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterInvestigation(registry);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterSquad(registry);
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterReadiness(registry);
    }

    private static OperationStatus RegisterOne(
        ActionDefinitionRegistry registry,
        ActionDefinition definition,
        IGoapActionExecutor executor)
    {
        return registry.Register(definition, executor);
    }

    private static OperationStatus RegisterMovement(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, MoveToNavigationNodeAction.CreateDefinition(), new MoveToNavigationNodeAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, MoveToTacticalPointAction.CreateDefinition(), new MoveToTacticalPointAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, MoveToCoverAction.CreateDefinition(), new MoveToCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, AdvanceToCoverAction.CreateDefinition(), new AdvanceToCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, RetreatToCoverAction.CreateDefinition(), new RetreatToCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, MoveToSearchSectorAction.CreateDefinition(), new MoveToSearchSectorAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterMovementContinued(registry);
    }

    private static OperationStatus RegisterMovementContinued(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, FollowFormationSlotAction.CreateDefinition(), new FollowFormationSlotAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, OpenDoorAction.CreateDefinition(), new OpenDoorAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, BreachDoorAction.CreateDefinition(), new BreachDoorAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, VaultObstacleAction.CreateDefinition(), new VaultObstacleAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, TraverseWindowAction.CreateDefinition(), new TraverseWindowAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterOne(registry, UseSmartObjectAction.CreateDefinition(), new UseSmartObjectAction());
    }

    private static OperationStatus RegisterCombat(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, AttackTargetAction.CreateDefinition(), new AttackTargetAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, AttackFromCoverAction.CreateDefinition(), new AttackFromCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, BlindFireFromCoverAction.CreateDefinition(), new BlindFireFromCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, SuppressTargetAreaAction.CreateDefinition(), new SuppressTargetAreaAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterCombatContinued(registry);
    }

    private static OperationStatus RegisterCombatContinued(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, ReloadWeaponAction.CreateDefinition(), new ReloadWeaponAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, SwitchWeaponAction.CreateDefinition(), new SwitchWeaponAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, MeleeTargetAction.CreateDefinition(), new MeleeTargetAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, ThrowGrenadeAction.CreateDefinition(), new ThrowGrenadeAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterOne(registry, HoldAimAction.CreateDefinition(), new HoldAimAction());
    }

    private static OperationStatus RegisterSurvival(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, DodgeShuffleAction.CreateDefinition(), new DodgeShuffleAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, DodgeRollAction.CreateDefinition(), new DodgeRollAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, EscapeGrenadeAction.CreateDefinition(), new EscapeGrenadeAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, FleeDangerAction.CreateDefinition(), new FleeDangerAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, CrouchInCoverAction.CreateDefinition(), new CrouchInCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterOne(registry, AbandonInvalidCoverAction.CreateDefinition(), new AbandonInvalidCoverAction());
    }

    private static OperationStatus RegisterInvestigation(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, InvestigateSoundAction.CreateDefinition(), new InvestigateSoundAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, InvestigateDamageDirectionAction.CreateDefinition(), new InvestigateDamageDirectionAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, MoveToLastKnownPositionAction.CreateDefinition(), new MoveToLastKnownPositionAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, InspectSearchSectorAction.CreateDefinition(), new InspectSearchSectorAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, LookAroundAction.CreateDefinition(), new LookAroundAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterOne(registry, HoldSearchPositionAction.CreateDefinition(), new HoldSearchPositionAction());
    }

    private static OperationStatus RegisterSquad(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, AcceptSquadOrderAction.CreateDefinition(), new AcceptSquadOrderAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, MoveToAssignedCoverAction.CreateDefinition(), new MoveToAssignedCoverAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, ProvideSuppressionAction.CreateDefinition(), new ProvideSuppressionAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, AdvanceUnderSuppressionAction.CreateDefinition(), new AdvanceUnderSuppressionAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, SearchAssignedSectorAction.CreateDefinition(), new SearchAssignedSectorAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, HoldFormationAction.CreateDefinition(), new HoldFormationAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterOne(registry, CompleteSquadOrderAction.CreateDefinition(), new CompleteSquadOrderAction());
    }

    private static OperationStatus RegisterReadiness(ActionDefinitionRegistry registry)
    {
        OperationStatus status = RegisterOne(registry, PatrolAction.CreateDefinition(), new PatrolAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        status = RegisterOne(registry, WaitAction.CreateDefinition(), new WaitAction());
        if (status != OperationStatus.Success)
        {
            return status;
        }

        return RegisterOne(registry, IdleAction.CreateDefinition(), new IdleAction());
    }
}
