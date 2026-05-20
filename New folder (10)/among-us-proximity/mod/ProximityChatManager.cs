using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Newtonsoft.Json;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// ה-Manager הראשי - רץ כ-MonoBehaviour בסצנה.
    /// מנהל את חיבור ה-WebSocket לשרת ואת לוגיקת ה-proximity.
    /// </summary>
    public class ProximityChatManager : MonoBehaviour
    {
        public static ProximityChatManager Instance = null!;

        // ---- הגדרות ----
        private const string SERVER_URL = "ws://localhost:3000";
        private const float POSITION_SEND_INTERVAL = 0.1f; // שניות בין שליחת מיקום

        // ---- מצב ----
        private ClientWebSocket _ws = null!;
        private string _myId = "";
        private bool _inMeeting = false;
        private bool _connected = false;

        // מיפוי playerId -> AudioSource לשליטה בווליום
        private readonly Dictionary<string, AudioSource> _playerAudio = new();

        // תור הודעות נכנסות (thread-safe)
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _messageQueue = new();

        private float _positionTimer = 0f;

        public ProximityChatManager(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Plugin.Log.LogInfo("ProximityChatManager initialized");
        }

        private void Start()
        {
            StartCoroutine(ConnectToServer().WrapToIl2Cpp());
        }

        private void Update()
        {
            // עבד הודעות נכנסות מה-WebSocket (חייב להיות ב-main thread)
            while (_messageQueue.TryDequeue(out var msg))
            {
                HandleMessage(msg);
            }

            // שלח מיקום בתדירות קבועה
            if (_connected && PlayerControl.LocalPlayer != null)
            {
                _positionTimer += Time.deltaTime;
                if (_positionTimer >= POSITION_SEND_INTERVAL)
                {
                    _positionTimer = 0f;
                    SendPosition();
                }
            }
        }

        // ---- WebSocket ----

        private IEnumerator ConnectToServer()
        {
            yield return new WaitForSeconds(1f); // המתן שהמשחק יטען

            Plugin.Log.LogInfo($"Connecting to {SERVER_URL}...");

            _ = Task.Run(async () =>
            {
                try
                {
                    _ws = new ClientWebSocket();
                    await _ws.ConnectAsync(new Uri(SERVER_URL), CancellationToken.None);
                    Plugin.Log.LogInfo("Connected to proximity server!");
                    _connected = true;

                    // קרא הודעות ברקע
                    await ReceiveLoop();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"WebSocket error: {ex.Message}");
                }
            });
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            while (_ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _messageQueue.Enqueue(text);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Receive error: {ex.Message}");
                    break;
                }
            }
        }

        private void SendJson(object data)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            var json = JsonConvert.SerializeObject(data);
            var bytes = Encoding.UTF8.GetBytes(json);
            _ = _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // ---- שליחת מיקום ----

        private void SendPosition()
        {
            var local = PlayerControl.LocalPlayer;
            if (local == null) return;

            var pos = local.transform.position;
            SendJson(new
            {
                type = "position",
                x = pos.x,
                y = pos.y,
            });
        }

        // ---- הצטרפות לחדר ----

        public void JoinRoom(string roomCode, string playerName)
        {
            SendJson(new
            {
                type = "join",
                roomCode,
                name = playerName,
            });
            Plugin.Log.LogInfo($"Joined room: {roomCode} as {playerName}");
        }

        // ---- טיפול בהודעות נכנסות ----

        private void HandleMessage(string json)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (msg == null) return;

                var type = msg["type"].ToString();

                switch (type)
                {
                    case "connected":
                        _myId = msg["id"].ToString()!;
                        Plugin.Log.LogInfo($"My server ID: {_myId}");

                        // הצטרף לחדר אוטומטית לפי קוד הלובי
                        if (AmongUsClient.Instance != null && PlayerControl.LocalPlayer != null)
                        {
                            var roomCode = AmongUsClient.Instance.GameId.ToString();
                            var name = PlayerControl.LocalPlayer.Data?.PlayerName ?? "Unknown";
                            JoinRoom(roomCode, name);
                        }
                        break;

                    case "proximity_update":
                        HandleProximityUpdate(msg);
                        break;

                    case "meeting_start":
                        _inMeeting = true;
                        SetAllVolumes(1f); // בפגישה כולם שומעים את כולם
                        break;

                    case "meeting_end":
                        _inMeeting = false;
                        break;

                    case "player_left":
                        var leftId = msg["id"].ToString()!;
                        RemovePlayerAudio(leftId);
                        break;

                    // WebRTC signaling
                    case "offer":
                    case "answer":
                    case "ice_candidate":
                        WebRTCManager.Instance?.HandleSignaling(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"HandleMessage error: {ex.Message}");
            }
        }

        private void HandleProximityUpdate(Dictionary<string, object> msg)
        {
            if (_inMeeting) return;

            var targetId = msg["targetId"].ToString()!;
            var volume = Convert.ToSingle(msg["volume"]);

            // עדכן ווליום של ה-AudioSource של השחקן
            if (_playerAudio.TryGetValue(targetId, out var audioSource))
            {
                audioSource.volume = volume;
            }

            // עדכן WebRTC
            WebRTCManager.Instance?.SetPeerVolume(targetId, volume);
        }

        // ---- ניהול AudioSources ----

        public void RegisterPlayerAudio(string playerId, AudioSource source)
        {
            _playerAudio[playerId] = source;
        }

        private void RemovePlayerAudio(string playerId)
        {
            _playerAudio.Remove(playerId);
        }

        private void SetAllVolumes(float volume)
        {
            foreach (var source in _playerAudio.Values)
            {
                if (source != null) source.volume = volume;
            }
        }

        // ---- WebRTC Signaling helpers (נקראים מ-WebRTCManager) ----

        public void SendOffer(string targetId, string sdp)
        {
            SendJson(new { type = "offer", targetId, sdp });
        }

        public void SendAnswer(string targetId, string sdp)
        {
            SendJson(new { type = "answer", targetId, sdp });
        }

        public void SendIceCandidate(string targetId, object candidate)
        {
            SendJson(new { type = "ice_candidate", targetId, candidate });
        }

        public void NotifyDead()
        {
            SendJson(new { type = "player_dead" });
        }

        private void OnDestroy()
        {
            _ws?.Dispose();
        }
    }
}
