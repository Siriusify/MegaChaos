namespace MegaChaos.Services.Chaos
{
    public interface IChaosOverlayEffect
    {
        bool HideProgressBar { get; }
        float? GetProgress01(float remainingTime, float totalDuration);
    }
}
