using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Microsoft.MixedReality.WebRTC.Unity
{
    [AddComponentMenu("MixedReality-WebRTC/Whep Signaler")]
    public class WhepSignaler : Signaler
    {
        public bool AutoLogErrors = true;

        public string LocalPeerId;

        [Header("Server")]
        public string HttpServerAddress;

        private string RemotePeerId;

        #region ISignaler interface

        private string lastMessage = "";

        public override Task SendMessageAsync(SdpMessage message)
        {
            if (message.Content == lastMessage)
            {
                Debug.Log($"Not sending the same message...");
                return null;
            }
            lastMessage = message.Content;
            Debug.Log($"Sending message of type {message.Type} to remote peer.");
            return SendMessageImplAsync(message.Content);
        }

        public override Task SendMessageAsync(IceCandidate candidate)
        {
            if (RemotePeerId == null || RemotePeerId.Length == 0)
            {
                return null;
            }
            Debug.Log($"Sending ICE candidate to remote peer {RemotePeerId}");
            return SendMessageImplAsync(candidate.Content);
        }

        #endregion

        private Task SendMessageImplAsync(String message)
        {
            var tcs = new TaskCompletionSource<bool>();
            _mainThreadWorkQueue.Enqueue(() => StartCoroutine(PostToServerAndWait(message, tcs)));
            return tcs.Task;
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(HttpServerAddress))
            {
                throw new ArgumentNullException("HttpServerAddress");
            }

            if (string.IsNullOrEmpty(LocalPeerId))
            {
                LocalPeerId = SystemInfo.deviceName;
            }

            PeerConnection.OnInitialized.AddListener(StartConnection);
        }

        private void StartConnection()
        {
            Debug.Log($"Local peer ID is {LocalPeerId}");
            PeerConnection.StartConnection();
        }

        private IEnumerator PostToServer(String msg)
        {
            if (RemotePeerId == null || RemotePeerId.Length == 0)
            {
                Debug.Log($"Sending OPTIONS to {HttpServerAddress}");

                var yyy = new UnityWebRequest($"{HttpServerAddress}", UnityWebRequest.kHttpVerbPOST);
                yyy.method = "OPTIONS";
                yield return yyy.SendWebRequest();

                if (AutoLogErrors && (yyy.result == UnityWebRequest.Result.ProtocolError))
                {
                    Debug.Log($"Failed to send message to remote peer {RemotePeerId}: {yyy.error}");
                    yield break;
                }

                Debug.Log($"Sending POST to {HttpServerAddress}");

                var www = new UnityWebRequest($"{HttpServerAddress}", UnityWebRequest.kHttpVerbPOST);
                www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(msg));
                www.SetRequestHeader("Content-Type", "application/sdp");
                www.downloadHandler = new DownloadHandlerBuffer();

                yield return www.SendWebRequest();

                if (www.GetResponseHeaders().ContainsKey("Location"))
                {
                    string[] parts = www.GetResponseHeaders()["Location"].Split('/');
                    RemotePeerId = parts[parts.Length - 1];
                    Debug.Log($"Remote peer ID is {RemotePeerId}");
                }

                if (AutoLogErrors && (www.result == UnityWebRequest.Result.ProtocolError))
                {
                    Debug.Log($"Failed to send message to remote peer {RemotePeerId}: {www.error}");
                    yield break;
                }

                Debug.Log($"Working with the server SDP answer");

                var sdpAnswer = new WebRTC.SdpMessage { Type = SdpMessageType.Answer, Content = www.downloadHandler.text };
                _ = PeerConnection.HandleConnectionMessageAsync(sdpAnswer);
            }
            else
            {
                Debug.Log($"Sending PATCH to {HttpServerAddress}/{RemotePeerId}");

                var www = new UnityWebRequest($"{HttpServerAddress}/{RemotePeerId}", UnityWebRequest.kHttpVerbPOST);
                www.method = "PATCH";
                www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(msg));
                www.SetRequestHeader("Content-Type", "application/trickle-ice-sdpfrag");

                yield return www.SendWebRequest();

                if (AutoLogErrors && (www.result == UnityWebRequest.Result.ProtocolError))
                {
                    Debug.Log($"Failed to send message to remote peer {RemotePeerId}: {www.error}");
                }
            }
        }

        private IEnumerator PostToServerAndWait(String message, TaskCompletionSource<bool> tcs)
        {
            yield return StartCoroutine(PostToServer(message));
            const bool flag = true;
            tcs.SetResult(flag);
        }

        protected override void Update()
        {
            base.Update();
        }
    }
}
