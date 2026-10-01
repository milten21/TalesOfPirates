using System;
using System.Collections.Generic;
using Godot;

namespace Top.Client.Features.Characters.Movement;

public class Pathfinder(Walkability walkability)
{
    private const float CellSize = 0.5f;

    private const int StraightStepCost = 10;

    private const int DiagonalStepCost = 14;

    private const float CrossingTolerance = 1e-5f;

    private const sbyte UncheckedCell = 0;

    private const sbyte WalkableCell = 1;

    private const sbyte BlockedCell = 2;

    private static readonly int[] StepsX = [0, 1, 0, -1, 1, 1, -1, -1];

    private static readonly int[] StepsY = [-1, 0, 1, 0, -1, 1, 1, -1];

    private readonly PriorityQueue<int, int> _frontier = new PriorityQueue<int, int>();

    private int[] _costs = [];

    private int[] _parents = [];

    private bool[] _isVisited = [];

    private sbyte[] _walkableStates = [];

    private int _windowSize;

    private Vector2I _windowOrigin;

    public int WindowSize { get; set; } = 128;

    public int MaximumPointCount { get; set; } = 32;

    public bool FallsBackToStraightLine { get; set; } = true;

    public Vector2[] FindPath(Vector2 start, Vector2 target)
    {
        OpenWindow(start);

        var startIndex = ToIndex(ToLocalCell(start));

        if (!TrySearch(startIndex, ToLocalCell(target), out var reachedIndex))
        {
            return FallsBackToStraightLine ? ClipStraightLine(start, target) : [start];
        }

        return Smooth(start, TraceCells(reachedIndex));
    }

    public Vector2[] FindStraightLine(Vector2 start, Vector2 target)
    {
        OpenWindow(start);

        return ClipStraightLine(start, target);
    }

    private static Vector2I ToCell(Vector2 mapPoint)
    {
        return new Vector2I(Mathf.FloorToInt(mapPoint.X / CellSize), Mathf.FloorToInt(mapPoint.Y / CellSize));
    }

    private static int MeasureSquaredDistance(int offsetX, int offsetY)
    {
        return (offsetX * offsetX) + (offsetY * offsetY);
    }

    private static int EstimateCost(int offsetX, int offsetY)
    {
        offsetX = Math.Abs(offsetX);
        offsetY = Math.Abs(offsetY);

        return (StraightStepCost * Math.Abs(offsetX - offsetY)) + (DiagonalStepCost * Math.Min(offsetX, offsetY));
    }

    private static float MeasureCrossingStep(float direction)
    {
        return direction != 0f ? 1f / Math.Abs(direction) : float.PositiveInfinity;
    }

    private static float MeasureFirstCrossing(float start, int cell, float direction)
    {
        return direction switch
        {
            > 0f => (cell + 1 - start) / direction,
            < 0f => (start - cell) / -direction,
            _ => float.PositiveInfinity
        };
    }

    private void OpenWindow(Vector2 start)
    {
        _windowSize = WindowSize;
        _windowOrigin = ToCell(start) - (Vector2I.One * (_windowSize / 2));

        PrepareBuffers();

        _walkableStates[ToIndex(ToLocalCell(start))] = WalkableCell;
    }

    private void PrepareBuffers()
    {
        var cellCount = _windowSize * _windowSize;

        if (_walkableStates.Length != cellCount)
        {
            _costs = new int[cellCount];
            _parents = new int[cellCount];
            _isVisited = new bool[cellCount];
            _walkableStates = new sbyte[cellCount];
        }

        Array.Fill(_costs, int.MaxValue);
        Array.Clear(_isVisited);
        Array.Clear(_walkableStates);

        _frontier.Clear();
    }

    private bool TrySearch(int startIndex, Vector2I target, out int reachedIndex)
    {
        _costs[startIndex] = 0;
        _parents[startIndex] = startIndex;
        _frontier.Enqueue(startIndex, 0);

        var visitedCount = 0;
        var bestIndex = startIndex;
        var bestDistance = int.MaxValue;

        while (_frontier.TryDequeue(out var index, out _))
        {
            if (_isVisited[index])
            {
                continue;
            }

            _isVisited[index] = true;
            visitedCount++;

            var cellX = index % _windowSize;
            var cellY = index / _windowSize;
            var distance = MeasureSquaredDistance(target.X - cellX, target.Y - cellY);

            if (distance < bestDistance || (distance == bestDistance && _costs[index] < _costs[bestIndex]))
            {
                bestIndex = index;
                bestDistance = distance;
            }

            if (distance == 0)
            {
                break;
            }

            Expand(cellX, cellY, index, target);
        }

        reachedIndex = bestIndex;

        return visitedCount > 1 || bestDistance == 0;
    }

    private void Expand(int cellX, int cellY, int index, Vector2I target)
    {
        for (var step = 0; step < StepsX.Length; step++)
        {
            var neighborX = cellX + StepsX[step];
            var neighborY = cellY + StepsY[step];
            var isDiagonal = StepsX[step] != 0 && StepsY[step] != 0;

            if (!IsOpen(neighborX, neighborY)
                || (isDiagonal && (!IsOpen(neighborX, cellY) || !IsOpen(cellX, neighborY))))
            {
                continue;
            }

            var neighborIndex = (neighborY * _windowSize) + neighborX;
            var cost = _costs[index] + (isDiagonal ? DiagonalStepCost : StraightStepCost);

            if (cost >= _costs[neighborIndex])
            {
                continue;
            }

            _costs[neighborIndex] = cost;
            _parents[neighborIndex] = index;
            _frontier.Enqueue(neighborIndex, cost + EstimateCost(target.X - neighborX, target.Y - neighborY));
        }
    }

    private List<int> TraceCells(int reachedIndex)
    {
        var cells = new List<int>();
        var index = reachedIndex;

        while (true)
        {
            cells.Add(index);

            if (_parents[index] == index)
            {
                break;
            }

            index = _parents[index];
        }

        cells.Reverse();

        return cells;
    }

    private Vector2[] Smooth(Vector2 start, List<int> cells)
    {
        var points = new List<Vector2> { start };
        var anchor = start;
        var anchorCell = 0;

        while (anchorCell < cells.Count - 1 && points.Count < MaximumPointCount)
        {
            var nextCell = anchorCell + 1;

            while (nextCell < cells.Count - 1 && HasLineOfSight(anchor, ToCenter(cells[nextCell + 1])))
            {
                nextCell++;
            }

            anchor = ToCenter(cells[nextCell]);
            anchorCell = nextCell;

            points.Add(anchor);
        }

        return points.ToArray();
    }

    private Vector2[] ClipStraightLine(Vector2 start, Vector2 target)
    {
        var lastOpenCell = TraceLine(start, target);

        return lastOpenCell == ToLocalCell(start) ? [start] : [start, ToCenter(lastOpenCell)];
    }

    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        return TraceLine(from, to) == ToLocalCell(to);
    }

    private Vector2I TraceLine(Vector2 from, Vector2 to)
    {
        var start = (from / CellSize) - _windowOrigin;
        var end = (to / CellSize) - _windowOrigin;
        var cell = ToLocalCell(from);
        var endCell = ToLocalCell(to);
        var direction = end - start;
        var step = new Vector2I(Math.Sign(direction.X), Math.Sign(direction.Y));
        var crossingStep = new Vector2(MeasureCrossingStep(direction.X), MeasureCrossingStep(direction.Y));
        var nextCrossing = new Vector2(
            MeasureFirstCrossing(start.X, cell.X, direction.X),
            MeasureFirstCrossing(start.Y, cell.Y, direction.Y));
        var stepBudget = Math.Abs(endCell.X - cell.X) + Math.Abs(endCell.Y - cell.Y);

        while (cell != endCell && stepBudget-- > 0)
        {
            var nextCell = cell;

            if (Math.Abs(nextCrossing.X - nextCrossing.Y) <= CrossingTolerance)
            {
                if (!IsOpen(cell.X + step.X, cell.Y) || !IsOpen(cell.X, cell.Y + step.Y))
                {
                    return cell;
                }

                nextCell += step;
                nextCrossing += crossingStep;
                stepBudget--;
            }
            else if (nextCrossing.X < nextCrossing.Y)
            {
                nextCell.X += step.X;
                nextCrossing.X += crossingStep.X;
            }
            else
            {
                nextCell.Y += step.Y;
                nextCrossing.Y += crossingStep.Y;
            }

            if (!IsOpen(nextCell.X, nextCell.Y))
            {
                return cell;
            }

            cell = nextCell;
        }

        return cell;
    }

    private bool IsOpen(int cellX, int cellY)
    {
        if (cellX < 0 || cellY < 0 || cellX >= _windowSize || cellY >= _windowSize)
        {
            return false;
        }

        var index = (cellY * _windowSize) + cellX;

        if (_walkableStates[index] == UncheckedCell)
        {
            _walkableStates[index] = walkability.IsWalkable(_windowOrigin.X + cellX, _windowOrigin.Y + cellY)
                ? WalkableCell
                : BlockedCell;
        }

        return _walkableStates[index] == WalkableCell;
    }

    private int ToIndex(Vector2I cell)
    {
        return (cell.Y * _windowSize) + cell.X;
    }

    private Vector2I ToLocalCell(Vector2 mapPoint)
    {
        return ToCell(mapPoint) - _windowOrigin;
    }

    private Vector2I ToLocalCell(int index)
    {
        return new Vector2I(index % _windowSize, index / _windowSize);
    }

    private Vector2 ToCenter(int index)
    {
        return ToCenter(ToLocalCell(index));
    }

    private Vector2 ToCenter(Vector2I localCell)
    {
        var cell = _windowOrigin + localCell;

        return (new Vector2(cell.X, cell.Y) + (Vector2.One * 0.5f)) * CellSize;
    }
}
