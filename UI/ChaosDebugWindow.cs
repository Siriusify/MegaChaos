using UnityEngine;
using MegaChaos.Services.Chaos;

namespace MegaChaos.UI
{
    internal sealed class ChaosDebugWindow
    {
        private bool _visible = false;
        private Rect _windowRect = new Rect(20, 20, 450, 450);
        private Vector2 _scrollPosition;
        private float _customDuration = 30f;

        public void Update()
        {
            // F9 ile aç/kapa
            if (Input.GetKeyDown(KeyCode.F9))
            {
                _visible = !_visible;
            }
        }

        public void OnGUI()
        {
            if (!_visible) return;

            if (Event.current.type == EventType.ScrollWheel && _windowRect.Contains(Event.current.mousePosition))
            {
                _scrollPosition.y += Event.current.delta.y * 20f;
                _scrollPosition.y = Mathf.Max(0, _scrollPosition.y);
            }

            GUI.depth = -10001; // RewardScheduler -10000 kullanıyor, ondan daha üstte olsun
            var oldColor = GUI.color;

            // Arka plan
            GUI.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
            GUI.DrawTexture(_windowRect, Texture2D.whiteTexture);
            GUI.color = oldColor;

            // Başlık
            GUI.Label(new Rect(_windowRect.x + 10, _windowRect.y + 10, 400, 30), "<color=#08f5d7><b>CHAOS MODE DEBUG (F9 to Close)</b></color>");

            var engine = ChaosEngine.Instance;
            
            // Engine Durumu
            float topY = _windowRect.y + 50;
            GUI.Label(new Rect(_windowRect.x + 10, topY, 150, 25), "Chaos Engine Status:");
            string statusTxt = "<color=#00FF00>[ACTIVE VIA RULE]</color>";
            GUI.Label(new Rect(_windowRect.x + 160, topY, 150, 25), statusTxt);
            
            if (GUI.Button(new Rect(_windowRect.x + 350, topY, 80, 25), "Clean"))
            {
                engine.ClearAllEffects();
            }

            topY += 50;
            GUI.Label(new Rect(_windowRect.x + 10, topY, 280, 25), "Custom Duration for Next Trigger (secs):");
            
            if (GUI.Button(new Rect(_windowRect.x + 300, topY, 30, 25), "-"))
            {
                _customDuration = Mathf.Max(0, _customDuration - 5);
            }
            
            GUI.Label(new Rect(_windowRect.x + 335, topY, 40, 25), _customDuration.ToString());
            
            if (GUI.Button(new Rect(_windowRect.x + 380, topY, 30, 25), "+"))
            {
                _customDuration += 5;
            }

            topY += 40;
            GUI.Label(new Rect(_windowRect.x + 10, topY, 400, 25), "<b>Available Effects:</b>");
            topY += 30;

            if (engine.AvailableEffects != null)
            {
                float listY = topY - _scrollPosition.y;
                foreach (var effect in engine.AvailableEffects)
                {
                    if (listY < topY - 60) 
                    { 
                        listY += 60; 
                        continue; 
                    }
                    if (listY > _windowRect.y + _windowRect.height - 50) 
                    {
                        break; 
                    }

                    GUI.color = new Color(0.2f, 0.2f, 0.25f, 1f);
                    GUI.DrawTexture(new Rect(_windowRect.x + 10, listY, 430, 50), Texture2D.whiteTexture);
                    GUI.color = oldColor;

                    GUI.Label(new Rect(_windowRect.x + 20, listY + 5, 300, 20), $"<b>{effect.Name}</b>");
                    GUI.Label(new Rect(_windowRect.x + 20, listY + 25, 300, 20), $"<color=#AAAAAA>{effect.Description}</color>");

                    if (GUI.Button(new Rect(_windowRect.x + 340, listY + 5, 90, 40), "Trigger"))
                    {
                        engine.TriggerEffect(effect, _customDuration);
                    }
                    
                    listY += 60;
                }
                
                // Max scroll limit
                float maxScroll = Mathf.Max(0, (engine.AvailableEffects.Count * 60) - (_windowRect.height - topY));
                _scrollPosition.y = Mathf.Min(_scrollPosition.y, maxScroll);
            }
        }
    }
}
