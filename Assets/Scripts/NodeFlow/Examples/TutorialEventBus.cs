using System;

public static class TutorialEventBus
{
    public static event Action<string> OnEventRaised;

    public static void Raise(string eventId)
    {
        OnEventRaised?.Invoke(eventId);
    }
}