using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProximityChat
{
    /// <summary>
    /// מנהל חיבורי WebRTC peer-to-peer לאודיו.
    /// כל שחקן קרוב מקבל RTCPeerConnection נפרד.
    /// 
    /// NOTE: WebRTC ב-Unity דורש plugin חיצוני.
    /// מומלץ: Unity WebRTC package (com.unity.webrtc)
    /// כאן מוגדר ה-interface - יש להחליף את ה-stub calls ב-API האמיתי.
    /// </summary>
    public class WebRTCManager : MonoBehaviour
    {
        public static WebRTCManager Instance = null!;

        // playerId -> peer connection wrapper
        private readonly Dictionary<string, PeerConnection> _peers = new();

        public WebRTCManager(IntPtr ptr) : base(ptr) { }

        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// יצור חיבור חדש לשחקן (כ-initiator)
        /// </summary>
        public void CreatePeer(string peerId, bool isInitiator)
        {
            if (_peers.ContainsKey(peerId)) return;

            Plugin.Log.LogInfo($"Creating peer connection to {peerId}, initiator={isInitiator}");

            var peer = new PeerConnection(peerId, isInitiator);
            _peers[peerId] = peer;

            peer.OnIceCandidate += (candidate) =>
            {
                ProximityChatManager.Instance?.SendIceCandidate(peerId, candidate);
            };

            peer.OnOffer += (sdp) =>
            {
                ProximityChatManager.Instance?.SendOffer(peerId, sdp);
            };

            peer.OnAnswer += (sdp) =>
            {
                ProximityChatManager.Instance?.SendAnswer(peerId, sdp);
            };

            if (isInitiator)
            {
                peer.CreateOffer();
            }
        }

        /// <summary>
        /// טפל בהודעות signaling מהשרת
        /// </summary>
        public void HandleSignaling(Dictionary<string, object> msg)
        {
            var type = msg["type"].ToString()!;
            var fromId = msg["fromId"].ToString()!;

            switch (type)
            {
                case "offer":
                    if (!_peers.ContainsKey(fromId))
                        CreatePeer(fromId, false);
                    _peers[fromId].SetRemoteOffer(msg["sdp"].ToString()!);
                    break;

                case "answer":
                    if (_peers.TryGetValue(fromId, out var peer))
                        peer.SetRemoteAnswer(msg["sdp"].ToString()!);
                    break;

                case "ice_candidate":
                    if (_peers.TryGetValue(fromId, out var p))
                        p.AddIceCandidate(msg["candidate"]);
                    break;
            }
        }

        /// <summary>
        /// עדכן ווליום של שחקן ספציפי
        /// </summary>
        public void SetPeerVolume(string peerId, float volume)
        {
            if (_peers.TryGetValue(peerId, out var peer))
            {
                peer.SetVolume(volume);
            }
        }

        public void RemovePeer(string peerId)
        {
            if (_peers.TryGetValue(peerId, out var peer))
            {
                peer.Close();
                _peers.Remove(peerId);
            }
        }
    }

    /// <summary>
    /// Wrapper ל-RTCPeerConnection אחד.
    /// מחליף את ה-stub methods ב-Unity WebRTC API האמיתי.
    /// </summary>
    public class PeerConnection
    {
        public string PeerId { get; }
        public bool IsInitiator { get; }

        public event Action<object>? OnIceCandidate;
        public event Action<string>? OnOffer;
        public event Action<string>? OnAnswer;

        private AudioSource? _remoteAudio;

        // --- Unity WebRTC stubs ---
        // החלף את אלה ב-RTCPeerConnection האמיתי מ-com.unity.webrtc

        public PeerConnection(string peerId, bool isInitiator)
        {
            PeerId = peerId;
            IsInitiator = isInitiator;

            // TODO: RTCPeerConnection pc = new RTCPeerConnection(config);
            // pc.OnIceCandidate = (candidate) => OnIceCandidate?.Invoke(candidate);
            // pc.OnTrack = (e) => { /* הוסף audio track */ };
        }

        public void CreateOffer()
        {
            // TODO:
            // var offer = await pc.CreateOffer();
            // await pc.SetLocalDescription(ref offer);
            // OnOffer?.Invoke(offer.sdp);
            Plugin.Log.LogInfo($"[WebRTC] Creating offer for {PeerId}");
        }

        public void SetRemoteOffer(string sdp)
        {
            // TODO:
            // var desc = new RTCSessionDescription { type = RTCSdpType.Offer, sdp = sdp };
            // await pc.SetRemoteDescription(ref desc);
            // var answer = await pc.CreateAnswer();
            // await pc.SetLocalDescription(ref answer);
            // OnAnswer?.Invoke(answer.sdp);
            Plugin.Log.LogInfo($"[WebRTC] Received offer from {PeerId}");
        }

        public void SetRemoteAnswer(string sdp)
        {
            // TODO:
            // var desc = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = sdp };
            // await pc.SetRemoteDescription(ref desc);
            Plugin.Log.LogInfo($"[WebRTC] Received answer from {PeerId}");
        }

        public void AddIceCandidate(object candidate)
        {
            // TODO:
            // pc.AddIceCandidate(candidate as RTCIceCandidate);
        }

        public void SetVolume(float volume)
        {
            if (_remoteAudio != null)
                _remoteAudio.volume = volume;
        }

        public void Close()
        {
            // TODO: pc.Close();
            Plugin.Log.LogInfo($"[WebRTC] Closed connection to {PeerId}");
        }
    }
}
