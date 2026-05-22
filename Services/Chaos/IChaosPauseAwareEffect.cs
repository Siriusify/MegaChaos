namespace MegaChaos.Services.Chaos
{
    public interface IChaosPauseAwareEffect
    {
        void OnPauseState(bool isTimePaused, bool isMenuOpen);
    }
}
