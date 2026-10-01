using System;
using System.Collections.Generic;
using Godot;
using Top.Client.Messages;

namespace Top.Client.UI;

public partial class Screen : Control
{
    private readonly List<IDisposable> _subscriptions = [];

    [Signal]
    public delegate void ClosedEventHandler();

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            ReleaseSubscriptions();
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!NavigationInput.IsNavigation(inputEvent) || GetViewport().GuiGetFocusOwner() != null || NavigationStart is not { } control)
        {
            return;
        }

        control.GrabFocus();

        GetViewport().SetInputAsHandled();
    }

    protected virtual Control? NavigationStart => null;

    protected void Close()
    {
        EmitSignal(SignalName.Closed);
    }

    protected static void ChainFocus(params Control[] order)
    {
        for (int index = 0; index < order.Length - 1; index++)
        {
            Control current = order[index];
            Control next = order[index + 1];

            current.FocusNext = current.GetPathTo(next);
            next.FocusPrevious = next.GetPathTo(current);
        }
    }

    protected static void StackFocus(params Control[] order)
    {
        ChainFocus(order);

        for (int index = 0; index < order.Length - 1; index++)
        {
            Control upper = order[index];
            Control lower = order[index + 1];

            upper.FocusNeighborBottom = upper.GetPathTo(lower);
            lower.FocusNeighborTop = lower.GetPathTo(upper);
        }
    }

    protected static void LinkVertically(Control control, Control? upper, Control? lower)
    {
        control.FocusNeighborTop = upper is null ? new NodePath() : control.GetPathTo(upper);
        control.FocusNeighborBottom = lower is null ? new NodePath() : control.GetPathTo(lower);
    }

    protected static void LinkHorizontally(Control left, Control right)
    {
        left.FocusNeighborRight = left.GetPathTo(right);
        right.FocusNeighborLeft = right.GetPathTo(left);
    }

    protected void Subscribe<TEvent>(IServerConnection connection, Action<TEvent> handler)
        where TEvent : IServerEvent
    {
        _subscriptions.Add(connection.Subscribe(handler));
    }

    private void ReleaseSubscriptions()
    {
        foreach (IDisposable subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }
}
