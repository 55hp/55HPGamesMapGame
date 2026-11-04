using System;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.UI;
using UnityEngine;

namespace hp55games.Mobile.Core.Haptics
{
    /// <summary>
    /// Implementazione di default di IHapticsService.
    /// Android: VibrationEffect a durata/ampiezza diverse per intensità (API 26+), altrimenti vibrate(ms).
    /// iOS: senza plugin nativo esiste solo Handheld.Vibrate (forte): si usa per Heavy, Light/Medium sono ignorati.
    /// Editor/altro: nessun effetto.
    /// </summary>
    public sealed class HapticsService : IHapticsService
    {
        // Durate (ms) e ampiezze (1..255) per intensità. Valori di presentazione, non regole di gioco.
        private static readonly long[] DurationsMs = { 15, 30, 60 };
        private static readonly int[] Amplitudes = { 70, 150, 255 };

        private IUIOptionsService _options;

        public bool Enabled
        {
            get
            {
                if (_options == null) ServiceRegistry.TryResolve(out _options);
                return _options == null || _options.Haptics;
            }
        }

        public void Play(HapticStrength strength)
        {
            if (!Enabled) return;
            int i = (int)strength;
#if UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(DurationsMs[i], Amplitudes[i]);
#elif UNITY_IOS && !UNITY_EDITOR
            if (strength == HapticStrength.Heavy) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private int _sdk = -1;

        private void PlayAndroid(long ms, int amplitude)
        {
            try
            {
                if (_vibrator == null)
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        _sdk = version.GetStatic<int>("SDK_INT");
                }
                if (_vibrator == null) return;
                if (_sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude))
                        _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", ms);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HapticsService] Vibrazione Android non riuscita: {ex.Message}");
                Handheld.Vibrate(); // fa anche aggiungere a Unity il permesso VIBRATE nel manifest
            }
        }
#endif
    }
}
