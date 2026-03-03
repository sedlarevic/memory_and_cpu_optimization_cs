using Domain;

namespace Generator;

public class StateMachine
{
    public State CurrentState { get; private set; }
    public Seed Seed { get; }
    private readonly Random _rng;

    public StateMachine(Seed seed)
    {
        Seed = seed;
        _rng = new Random(seed.Value);
        CurrentState = State.Idle;
    }

    public ValueTuple<State,State> Step()
    {
        State from =  CurrentState;
        State to = DecideNextState(CurrentState);
        CurrentState = to;
        return (from, to);
    }

    private State DecideNextState(State state)
    {
        int roll = _rng.Next(100);
        switch (state)
        {
            case State.Idle:
                if (roll < 85)
                {
                    return State.RequestReceived;
                }
                else
                {
                    return State.Error;
                }
            case State.RequestReceived:
                if (roll < 80)
                {
                    return State.Processing;
                }
                else
                {
                    return State.Error;
                }
            case State.Processing:
                if (roll < 75)
                {
                    return State.Completed;
                }else if (roll < 95)
                {
                    return State.Error;
                }
                else
                {
                    return State.Retry;
                }
            case State.Completed:
                if (roll < 60)
                {
                    return State.Idle;
                }
                else
                {
                    return State.RequestReceived;
                }
            case State.Error:
                if (roll < 70)
                {
                    return State.Retry;
                }
                else
                {
                    return State.Processing;
                }
            case State.Retry:
                if (roll < 60)
                {
                    return State.Processing;
                }
                else
                {
                    return State.Error;
                }
            default:
                throw new InvalidOperationException("Invalid state");
        }
    }

}