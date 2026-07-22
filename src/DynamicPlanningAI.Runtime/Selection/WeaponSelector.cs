using System;
using DynamicPlanningAI.Abstractions.Attributes;
using DynamicPlanningAI.Abstractions.Diagnostics;
using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Geometry;
using DynamicPlanningAI.Abstractions.Hosting;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Limits;
using DynamicPlanningAI.Abstractions.Results;
using DynamicPlanningAI.Abstractions.Ticks;

namespace DynamicPlanningAI.Runtime.Selection;

/// <summary>
/// Deterministic weapon selector over a bounded candidate span.
/// </summary>
public sealed class WeaponSelector
{
    private int _selectCount;

    /// <summary>
    /// Gets the number of selections performed by this instance.
    /// </summary>
    public int SelectCount => _selectCount;

    /// <summary>
    /// Selects a weapon from a caller-owned candidate span.
    /// </summary>
    /// <param name="candidates">Weapon candidates for this tick.</param>
    /// <param name="request">Selection inputs including hysteresis.</param>
    /// <param name="trace">Optional trace sink.</param>
    /// <returns>Selection result.</returns>
    [FrozenRuntimePath]
    public WeaponSelectionResult Select(
        ReadOnlySpan<WeaponCandidate> candidates,
        in WeaponSelectionRequest request,
        IRuntimeTraceSink? trace)
    {
        _selectCount = checked(_selectCount + 1);
        int count = candidates.Length;
        if (count > AiHardLimits.MaximumActionCandidates)
        {
            count = AiHardLimits.MaximumActionCandidates;
        }

        WeaponId best = WeaponId.Invalid;
        int bestScore = int.MinValue;

        for (int i = 0; i < count; i++)
        {
            ref readonly WeaponCandidate candidate = ref candidates[i];
            if (!candidate.Weapon.IsValid)
            {
                continue;
            }

            int score = ScoreWeapon(candidate, request);
            if (IsBetter(score, candidate.Weapon, bestScore, best))
            {
                bestScore = score;
                best = candidate.Weapon;
            }
        }

        bool changed = best != request.CurrentWeapon;
        if (changed && trace is not null)
        {
            WriteTrace(request, best, bestScore, trace);
        }

        return new WeaponSelectionResult(best, best.IsValid ? bestScore : 0, changed);
    }

    [FrozenRuntimePath]
    private static int ScoreWeapon(in WeaponCandidate candidate, in WeaponSelectionRequest request)
    {
        int score = 0;
        score = checked(score + ScoreAmmo(candidate));
        score = checked(score + ScoreRange(candidate, request.TargetRange));
        score = checked(score + ScoreReload(candidate));
        score = checked(score + ScoreRole(candidate, request));
        score = checked(score + ScoreSwitch(candidate, request));
        score = checked(score + ScoreHysteresis(candidate, request));
        return score;
    }

    [FrozenRuntimePath]
    private static int ScoreAmmo(in WeaponCandidate candidate)
    {
        if (candidate.IsMelee)
        {
            return 100;
        }

        if (candidate.Ammunition <= 0)
        {
            return -500;
        }

        int ammo = candidate.Ammunition;
        if (ammo > 100)
        {
            ammo = 100;
        }

        return ammo;
    }

    [FrozenRuntimePath]
    private static int ScoreRange(in WeaponCandidate candidate, DistanceCategory targetRange)
    {
        if (candidate.PreferredRange == targetRange)
        {
            return 300;
        }

        int delta = (int)candidate.PreferredRange - (int)targetRange;
        if (delta < 0)
        {
            delta = -delta;
        }

        return 150 - checked(delta * 75);
    }

    [FrozenRuntimePath]
    private static int ScoreReload(in WeaponCandidate candidate)
    {
        return candidate.RequiresReload ? -200 : 50;
    }

    [FrozenRuntimePath]
    private static int ScoreRole(in WeaponCandidate candidate, in WeaponSelectionRequest request)
    {
        int score = 0;
        if (request.PreferSuppression && candidate.SuppressionSuitable)
        {
            score = checked(score + 200);
        }

        if (request.PreferMelee && candidate.IsMelee)
        {
            score = checked(score + 250);
        }

        if (request.PreferGrenade && candidate.IsGrenade)
        {
            score = checked(score + 250);
        }

        if (!request.PreferMelee && candidate.IsMelee && request.TargetRange != DistanceCategory.Near)
        {
            score = checked(score - 150);
        }

        if (!request.PreferGrenade && candidate.IsGrenade)
        {
            score = checked(score - 100);
        }

        return score;
    }

    [FrozenRuntimePath]
    private static int ScoreSwitch(in WeaponCandidate candidate, in WeaponSelectionRequest request)
    {
        if (!request.CurrentWeapon.IsValid || candidate.Weapon == request.CurrentWeapon)
        {
            return 0;
        }

        int cost = candidate.SwitchCost;
        return cost > 0 ? -cost : -25;
    }

    [FrozenRuntimePath]
    private static int ScoreHysteresis(in WeaponCandidate candidate, in WeaponSelectionRequest request)
    {
        if (!request.CurrentWeapon.IsValid || candidate.Weapon != request.CurrentWeapon)
        {
            return 0;
        }

        int bonus = request.HysteresisBonus;
        return bonus > 0 ? bonus : 100;
    }

    [FrozenRuntimePath]
    private static bool IsBetter(int score, WeaponId weapon, int bestScore, WeaponId best)
    {
        if (!best.IsValid)
        {
            return true;
        }

        if (score > bestScore)
        {
            return true;
        }

        if (score < bestScore)
        {
            return false;
        }

        return weapon.CompareTo(best) < 0;
    }

    [FrozenRuntimePath]
    private static void WriteTrace(
        in WeaponSelectionRequest request,
        WeaponId selected,
        int score,
        IRuntimeTraceSink trace)
    {
        AiTick tick = new AiTick(request.CurrentTick, 1);
        trace.Write(
            new TraceRecord(
                tick,
                request.Agent,
                request.Squad,
                DiagnosticSubsystem.WeaponSelection,
                (int)TraceEventCode.WeaponSelected,
                selected.IsValid ? selected.Value : -1,
                request.CurrentWeapon.IsValid ? request.CurrentWeapon.Value : -1,
                score,
                0,
                (int)OperationStatus.Success));
    }
}
