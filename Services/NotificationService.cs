using System.Collections.Generic;
using UnityEngine;

namespace MegaChaos.Services;

internal static class NotificationService
{
    public enum NotificationType
    {
        Reward,
        Unlucky,
        Warning
    }

    private class Notification
    {
        public string Text;
        public float StartTime;
        public float ExpireTime;
        public Texture2D Icon;
        public NotificationType Type;
        public bool ForcedDismiss;
    }

    private static readonly List<Notification> _notifications = new();
    private static GUIStyle _style;
    private static Texture2D _bgTexture;

    public static void Show(string text, string iconName = null, NotificationType type = NotificationType.Reward)
    {
        var icon = iconName != null ? ItemIconService.GetIcon(iconName)?.Texture as Texture2D : null;
        _notifications.Add(new Notification
        {
            Text = text,
            StartTime = Time.unscaledTime,
            ExpireTime = Time.unscaledTime + 3.5f,
            Icon = icon,
            Type = type,
            ForcedDismiss = false
        });

        int activeCount = 0;
        for (int i = _notifications.Count - 1; i >= 0; i--)
        {
            if (!_notifications[i].ForcedDismiss)
            {
                activeCount++;
                if (activeCount > 2)
                {
                    _notifications[i].ForcedDismiss = true;
                    _notifications[i].ExpireTime = Mathf.Min(_notifications[i].ExpireTime, Time.unscaledTime + 0.3f);
                }
            }
        }
    }

    public static void Draw()
    {
        if (_notifications.Count == 0) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            
            var padding = new RectOffset();
            padding.left = 10;
            padding.right = 10;
            padding.top = 10;
            padding.bottom = 10;
            _style.padding = padding;
            
            _bgTexture = new Texture2D(1, 1);
            _bgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.85f));
            _bgTexture.Apply();
            _style.normal.background = _bgTexture;
        }

        float now = Time.unscaledTime;
        _notifications.RemoveAll(n => now >= n.ExpireTime);

        float y = Screen.height - 30f;
        foreach (var n in _notifications)
        {
            float timeRemaining = n.ExpireTime - now;
            float alpha = Mathf.Clamp01(timeRemaining / 0.3f);
            
            float timeAlive = now - n.StartTime;
            float slideProgress = Mathf.Clamp01(timeAlive / 0.3f);
            slideProgress = slideProgress * (2f - slideProgress);

            var oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            
            Color textColor = Color.white;
            if (n.Type == NotificationType.Reward) textColor = new Color(0.3f, 0.9f, 0.4f);
            else if (n.Type == NotificationType.Unlucky) textColor = new Color(0.9f, 0.3f, 0.3f);
            else if (n.Type == NotificationType.Warning) textColor = new Color(0.9f, 0.8f, 0.2f);

            _style.normal.textColor = textColor;
            
            var textContent = new GUIContent(n.Text);
            var size = _style.CalcSize(textContent);
            
            float width = size.x + (n.Icon != null ? 50f : 0f);
            float height = Mathf.Max(size.y, n.Icon != null ? 40f : 0f);
            
            float targetX = Screen.width - width - 20f;
            float startX = Screen.width + 20f;
            float currentX = Mathf.Lerp(startX, targetX, slideProgress);
            
            float slideDownY = 0f;
            float reservedHeight = height + 10f;

            if (n.ForcedDismiss || timeRemaining < 0.3f)
            {
                float dismissProgress = 1f - alpha;
                slideDownY = dismissProgress * 50f;
                reservedHeight *= alpha;
            }

            y -= reservedHeight;
            var rect = new Rect(currentX, y + 10f + slideDownY, width, height);
            
            GUI.Box(rect, string.Empty, _style);
            
            float contentX = rect.x + 10f;
            if (n.Icon != null)
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(contentX, rect.y + (height - 30f) / 2, 30f, 30f), n.Icon);
                contentX += 40f;
            }
            
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(contentX, rect.y, size.x, height), textContent, _style);
            
            GUI.color = oldColor;
        }
    }
}
