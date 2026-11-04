namespace hp55games.Mobile.Core.Haptics
{
    public enum HapticStrength { Light, Medium, Heavy }

    /// <summary>
    /// Vibrazione del dispositivo. Rispetta l'opzione "Vibrazione" (IUIOptionsService.Haptics):
    /// se è spenta, Play non fa nulla. Nell'Editor e sulle piattaforme senza vibrazione è un no-op.
    /// </summary>
    public interface IHapticsService
    {
        bool Enabled { get; }
        void Play(HapticStrength strength);
    }
}
