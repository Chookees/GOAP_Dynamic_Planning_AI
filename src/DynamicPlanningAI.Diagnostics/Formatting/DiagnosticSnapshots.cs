using DynamicPlanningAI.Abstractions.Enums;
using DynamicPlanningAI.Abstractions.Identifiers;
using DynamicPlanningAI.Abstractions.Results;

namespace DynamicPlanningAI.Diagnostics.Formatting;

/// <summary>
/// One planner explanation row for on-demand formatting.
/// </summary>
/// <param name="StepIndex">Zero-based step index within the explanation.</param>
/// <param name="ActionId">Action considered or selected at this step.</param>
/// <param name="Cost">Accumulated or incremental cost.</param>
/// <param name="Status">Planner status associated with the step.</param>
/// <param name="NoteCode">Host-defined integer note; formatting maps codes offline.</param>
public readonly record struct PlannerExplanationRow(
    int StepIndex,
    ActionId ActionId,
    int Cost,
    PlannerStatus Status,
    int NoteCode);

/// <summary>
/// One goal-priority table row for on-demand formatting.
/// </summary>
/// <param name="GoalId">Evaluated goal.</param>
/// <param name="Priority">Computed priority score.</param>
/// <param name="IsRelevant">Whether the goal reported relevance.</param>
/// <param name="IsSelected">Whether the goal was selected as active.</param>
public readonly record struct GoalPriorityRow(
    GoalId GoalId,
    int Priority,
    bool IsRelevant,
    bool IsSelected);

/// <summary>
/// Compact squad state snapshot for on-demand formatting.
/// </summary>
/// <param name="SquadId">Squad identifier.</param>
/// <param name="IsActive">Whether the squad is live.</param>
/// <param name="BehaviorCode">Host-mapped behavior enum ordinal.</param>
/// <param name="MemberCount">Occupied member slots.</param>
/// <param name="ActiveOrderCount">Live orders for the squad.</param>
/// <param name="FocusX">Focus cell X.</param>
/// <param name="FocusY">Focus cell Y.</param>
public readonly record struct SquadStateRow(
    SquadId SquadId,
    bool IsActive,
    int BehaviorCode,
    int MemberCount,
    int ActiveOrderCount,
    int FocusX,
    int FocusY);

/// <summary>
/// Compact memory dump row for on-demand formatting.
/// </summary>
/// <param name="RecordId">Memory record identifier.</param>
/// <param name="Type">Memory type ordinal.</param>
/// <param name="SourceEntity">Source entity raw id.</param>
/// <param name="RelatedEntity">Related entity raw id.</param>
/// <param name="PositionX">Believed cell X.</param>
/// <param name="PositionY">Believed cell Y.</param>
/// <param name="Confidence">Confidence in 0..1000.</param>
/// <param name="Flags">Memory flag bits.</param>
/// <param name="UpdateTick">Last update tick sequence.</param>
public readonly record struct MemoryDumpRow(
    MemoryRecordId RecordId,
    MemoryType Type,
    int SourceEntity,
    int RelatedEntity,
    int PositionX,
    int PositionY,
    int Confidence,
    int Flags,
    long UpdateTick);
