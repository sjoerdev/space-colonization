using System;

static class Repeater
{
    static bool repeating;

    public static void ToggleRepeating(Action action, float time)
    {
        if (!repeating) StartRepeating(action, time);
        else StopRepeating();
    }

    public static void StartRepeating(Action action, float time)
    {
        if (repeating) return;
        repeating = true;
        Loop(action, time);
    }

    public static void StopRepeating()
    {
        if (!repeating) return;
        repeating = false;
    }

    async static void Loop(Action action, float time)
    {
        if (!repeating) return;
        action.Invoke();
        await Task.Delay((int)(time * 1000f));
        Loop(action, time);
    }
}