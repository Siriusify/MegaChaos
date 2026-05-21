using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services.Chaos.Effects
{
    public class VirusEffect : IChaosEffect
    {
        public string Id => "effect_virus";
        public string Name => "Computer Virus";
        public string Description => "Bilgisayarına virüs girmiş gibi sürekli hata pencereleri çıkar!";
        public float DefaultDuration => 30f;

        private class ErrorWindow
        {
            public Rect Rect;
            public int Id;
            public string Title;
            public string Message;
            public float TimeLeft;
            public GUIStyle WindowStyle;
        }

        private List<ErrorWindow> _windows;
        private float _spawnTimer;
        private int _nextId;
        private GUIStyle _boxStyle;

        private readonly string[] _titles = {
            "System Error", "Fatal Exception 0x00000008", 
            "Trojan.Win32 Detected", "Memory Leak", 
            "Blue Screen Imminent", "Critical Failure",
            "Task Manager", "Windows Defender", "ERROR"
        };

        private readonly string[] _messages = {
            "C:\\Windows\\System32 siliniyor...",
            "Bellek yetersiz. Lütfen bazı uygulamaları kapatın.",
            "Kritik bir donanım arızası tespit edildi.",
            "Bilinmeyen bir yazılım ekran kartına erişmeye çalışıyor.",
            "Sistem çökmek üzere. Kurtarmak için 10 saniyeniz var.",
            "Uygulama yanıt vermiyor.",
            "x0000007b adresindeki yönerge, x00000000 bellek adresine başvurdu."
        };

        public void OnStart()
        {
            _windows = new List<ErrorWindow>();
            _spawnTimer = 0f;
            _nextId = 1000;
            _boxStyle = null; // initialized in OnGUI

            NotificationService.Show("VIRUS DETECTED!", null, NotificationService.NotificationType.Warning);
        }

        public void OnUpdate(float dt)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f && _windows.Count < 25)
            {
                SpawnWindow();
                _spawnTimer = UnityEngine.Random.Range(0.2f, 1.5f);
            }

            for (int i = _windows.Count - 1; i >= 0; i--)
            {
                _windows[i].TimeLeft -= dt;
                if (_windows[i].TimeLeft <= 0)
                {
                    _windows.RemoveAt(i);
                }
            }
        }

        private void SpawnWindow()
        {
            float w = UnityEngine.Random.Range(250f, 400f);
            float h = UnityEngine.Random.Range(100f, 180f);
            
            // Ekranı tamamen kapatmaması için ortada bir "güvenli alan" bırakabiliriz veya rastgele serpiştirebiliriz.
            // Kullanıcı %100 opacity istiyor ve ekranı tamamen kaplamamasını istiyor.
            // Max 25 pencere dedik, bu yüzden çok fazla kaplamaz, yine de spawn alanını ayarlayalım.
            float sw = Screen.width;
            float sh = Screen.height;

            float x = UnityEngine.Random.Range(0, sw - w);
            float y = UnityEngine.Random.Range(0, sh - h);

            var win = new ErrorWindow
            {
                Rect = new Rect(x, y, w, h),
                Id = _nextId++,
                Title = _titles[UnityEngine.Random.Range(0, _titles.Length)],
                Message = _messages[UnityEngine.Random.Range(0, _messages.Length)],
                TimeLeft = UnityEngine.Random.Range(3f, 8f) // her pencere 3-8 saniye kalsın
            };
            _windows.Add(win);
        }

        public void OnGUI()
        {
            if (_windows == null || _windows.Count == 0) return;

            // Opacity %100
            var oldColor = GUI.color;
            GUI.color = Color.white;

            // Style initialization
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.window);
                _boxStyle.normal.textColor = Color.white;
                _boxStyle.fontStyle = FontStyle.Bold;
                _boxStyle.fontSize = 14;
            }

            foreach (var win in _windows)
            {
                // GUI.Window in IL2CPP throws delegate MissingMethodException
                // Draw a simple box mimicking a window
                GUI.Box(win.Rect, win.Title, _boxStyle);
                DrawWindowContents(win);
            }

            GUI.color = oldColor;
        }

        private void DrawWindowContents(ErrorWindow target)
        {
            if (target != null)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                style.normal.textColor = Color.white;
                
                GUI.Label(new Rect(target.Rect.x + 10, target.Rect.y + 30, target.Rect.width - 20, target.Rect.height - 70), target.Message, style);

                if (GUI.Button(new Rect(target.Rect.x + target.Rect.width / 2 - 40, target.Rect.y + target.Rect.height - 35, 80, 25), "OK"))
                {
                    target.TimeLeft = 0; 
                }
            }
        }

        public void OnEnd()
        {
            if (_windows != null) _windows.Clear();
            NotificationService.Show("Virus Removed.", null, NotificationService.NotificationType.Reward);
        }
    }
}
