using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Client.Tools.Debug.ServerPaths;

namespace Top.Client.Tests.Tools.Debug.ServerPaths;

public class ServerPathRecorderTests
{
    private static readonly Vector2[] FirstStep = [new Vector2(10f, 20f), new Vector2(12f, 20f), new Vector2(12f, 21f)];

    private static readonly Vector2[] SecondStep = [new Vector2(12f, 21f), new Vector2(12f, 23f)];

    private static readonly Vector2[] ThirdStep = [new Vector2(12f, 23f), new Vector2(12f, 25f)];

    private FakeServerConnection _connection;

    private FakeTimeProvider _time;

    private ServerPathRecorder _recorder;

    [SetUp]
    public void CreateRecorder()
    {
        _connection = new FakeServerConnection();
        _time = new FakeTimeProvider();
        _recorder = new ServerPathRecorder(_connection, _time);
    }

    [Test]
    public void A_started_recorder_keeps_the_steps_of_each_character_in_order()
    {
        _recorder.Start();

        _connection.Publish(new CharacterMovedEvent(7, FirstStep, MoveStates.None, null));
        _connection.Publish(new CharacterMovedEvent(7, SecondStep, MoveStates.None, null));

        Assert.That(_recorder.Trails[7].Select(step => step.Points), Is.EqualTo(new[] { FirstStep, SecondStep }));
    }

    [Test]
    public void A_new_step_drops_the_steps_older_than_the_trail_duration()
    {
        _recorder.Start();
        _connection.Publish(new CharacterMovedEvent(7, FirstStep, MoveStates.None, null));
        _time.Advance(ServerPathRecorder.TrailDuration * 0.75);
        _connection.Publish(new CharacterMovedEvent(7, SecondStep, MoveStates.None, null));
        _time.Advance(ServerPathRecorder.TrailDuration * 0.5);

        _connection.Publish(new CharacterMovedEvent(7, ThirdStep, MoveStates.None, null));

        Assert.That(_recorder.Trails[7].Select(step => step.Points), Is.EqualTo(new[] { SecondStep, ThirdStep }));
    }

    [Test]
    public void The_age_of_a_step_is_the_time_since_it_arrived()
    {
        _recorder.Start();
        _connection.Publish(new CharacterMovedEvent(7, FirstStep, MoveStates.None, null));

        _time.Advance(TimeSpan.FromSeconds(3));

        Assert.That(_recorder.MeasureAge(_recorder.Trails[7][0]), Is.EqualTo(TimeSpan.FromSeconds(3)));
    }

    [Test]
    public void A_character_that_leaves_view_loses_its_trail()
    {
        _recorder.Start();
        _connection.Publish(new CharacterMovedEvent(7, FirstStep, MoveStates.None, null));

        _connection.Publish(new CharacterDisappearedEvent(7, SeeType.New));

        Assert.That(_recorder.Trails, Is.Empty);
    }

    [Test]
    public void A_stopped_recorder_forgets_its_trails_and_releases_the_connection()
    {
        _recorder.Start();
        _connection.Publish(new CharacterMovedEvent(7, FirstStep, MoveStates.None, null));

        _recorder.Stop();

        Assert.That(_recorder.Trails, Is.Empty);
        Assert.That(_connection.SubscriptionCount, Is.Zero);
    }

    [Test]
    public void A_recorder_started_twice_records_until_it_is_stopped_twice()
    {
        _recorder.Start();
        _recorder.Start();
        _recorder.Stop();

        _connection.Publish(new CharacterMovedEvent(7, FirstStep, MoveStates.None, null));

        Assert.That(_recorder.Trails[7].Select(step => step.Points), Is.EqualTo(new[] { FirstStep }));

        _recorder.Stop();

        Assert.That(_recorder.Trails, Is.Empty);
        Assert.That(_connection.SubscriptionCount, Is.Zero);
    }
}
