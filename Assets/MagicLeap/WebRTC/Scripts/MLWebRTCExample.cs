// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

namespace MagicLeap
{
    using MagicLeap.Core;
    using SimpleJson;
    using System;
    using System.Text;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using UnityEngine.XR.MagicLeap;

    public class MLWebRTCExample : MonoBehaviour
    {
        public MLWebRTCVideoSinkBehavior localVideoSinkBehavior;
        public MLWebRTCVideoSinkBehavior remoteVideoSinkBehavior;
        public MLWebRTCAudioSinkBehavior remoteAudioSinkBehavior;

        public GameObject keyboardUI;
        public GameObject messageUI;
        public GameObject disconnectUI;

        public Text localStatusText;
        public Text remoteStatusText;
        public Text dataChannelText;

        public Toggle localVideoToggle;
        public Toggle remoteVideoToggle;

        public Toggle localAudioToggle;
        public Toggle remoteAudioToggle;

        public Dropdown localVideoSourceDropdown;

        public Slider audioCacheSizeSlider;
        public Text audioCacheSliderValue;

        private bool waitingForAnswer = false;
        private bool waitingForAnswerGetRequest = false;
        private bool shouldDisconnect = false;

        private string serverAddress = "";
        private string serverURI = "";
        private string localId = "";
        private string remoteId = "";
        private MLWebRTC.PeerConnection connection = null;
        private MLWebRTC.DataChannel dataChannel = null;

        private MLWebRTC.MediaStream localMediaStream = null;
        private MLWebRTC.MediaStream remoteMediaStream = null;

        private MLWebRTC.MediaStream.Track localVideoTrack = null;

        // The sample server can only handle concurrent requests. Maintain a queue to send only 1 request at a time.
        private Queue<UnityWebRequest> pendingWebRequests = new Queue<UnityWebRequest>();
        private Dictionary<UnityWebRequest, Action<AsyncOperation>> webRequestsToOnCompletedEvent = new Dictionary<UnityWebRequest, Action<AsyncOperation>>();
        private UnityWebRequest lastWebRequest = null;
        private UnityWebRequestAsyncOperation lastWebRequestAsyncOp = null;
        private bool lastWebRequestCompleted = true;

        void Start()
        {
#if PLATFORM_LUMIN
            MLResult result = MLPrivileges.RequestPrivileges(MLPrivileges.Id.Internet, MLPrivileges.Id.LocalAreaNetwork, MLPrivileges.Id.CameraCapture, MLPrivileges.Id.AudioCaptureMic);

            if (result.Result != MLResult.Code.PrivilegeGranted)
            {
                Debug.LogError("MLPrivileges failed to grant all needed privileges.");
                enabled = false;
            }
#endif
            disconnectUI.SetActive(false);
            messageUI.SetActive(false);
        }

        // Subscribed to keyboard event within the inspector
        public void Connect(string address)
        {
#if PLATFORM_LUMIN
            serverAddress = address;
            serverURI = CreateServerURI(serverAddress);
            remoteStatusText.text = "Creating connection...";
            keyboardUI.SetActive(false);
            Login();
#endif
        }

        public void Login()
        {
#if PLATFORM_LUMIN
            HttpPost(serverURI + "/login", string.Empty, (AsyncOperation asyncOp) =>
            {
                UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                if (webRequenstAsyncOp.webRequest.result != UnityWebRequest.Result.Success || string.IsNullOrEmpty(webRequenstAsyncOp.webRequest.downloadHandler.text))
                {
                    remoteStatusText.text = "";
                    keyboardUI.SetActive(true);
                    return;
                }

                localId = webRequenstAsyncOp.webRequest.downloadHandler.text;
                // Marshals the iceServers array and sets up the connection callbacks.
                connection = MLWebRTC.PeerConnection.CreateRemote(CreateIceServers(), out MLResult result);
                if (!result.IsOk)
                {
                    Debug.LogFormat("MLWebRTCExample.Login failed to create a connection. Reason: {0}.", MLResult.CodeToString(result.Result));
                    return;
                }

                disconnectUI.SetActive(true);

                SubscribeToConnection(connection);
                CreateLocalMediaStream();
                QueryOffers();
            });
#endif
        }

        private void CreateLocalMediaStream()
        {
            localVideoSinkBehavior.gameObject.SetActive(true);
            localStatusText.text = "";

            string id = "local";

            // Use factory methods to create a new media stream.
            if(localMediaStream == null)
            {
                switch (localVideoSourceDropdown.value)
                {
                    // MLCamera defined source.
                    case 0:
                    {
#if PLATFORM_LUMIN
                        localMediaStream = MLWebRTC.MediaStream.CreateWithAppDefinedVideoTrack(id, MLCameraVideoSource.CreateLocal(new MLCamera.CaptureSettings(), out MLResult result), MLWebRTC.MediaStream.Track.AudioType.Microphone);
#endif
                        break;
                    }

                    // MLMRCamera defined source.
                    case 1:
                    {
                        localMediaStream = MLWebRTC.MediaStream.CreateWithAppDefinedVideoTrack(id, MLMRCameraVideoSource.CreateLocal(MLMRCamera.InputContext.Create(), out MLResult result), MLWebRTC.MediaStream.Track.AudioType.Microphone);
                        break;
                    }
                }
            }
            else // Replace the local video track with another.
            {
                // Determine which local video source to use
                switch (localVideoSourceDropdown.value)
                {
                    // MLCamera defined source.
                    case 0:
                    {
                        if (!(localVideoTrack is MLCameraVideoSource))
                        {
#if PLATFORM_LUMIN
                            localVideoTrack = MLCameraVideoSource.CreateLocal(new MLCamera.CaptureSettings(), out MLResult result);
#endif
                        }
                        break;
                    }

                    // MLMRCamera defined source.
                    case 1:
                    {
                        if (!(localVideoTrack is MLMRCameraVideoSource))
                        {
                            localVideoTrack = MLMRCameraVideoSource.CreateLocal(MLMRCamera.InputContext.Create(), out MLResult result);
                        }
                        break;
                    }
                }

                localMediaStream.AddLocalTrack(localVideoTrack);
                localMediaStream.SelectTrack(localVideoTrack);
            }

            foreach (MLWebRTC.MediaStream.Track track in localMediaStream.Tracks)
            {
#if PLATFORM_LUMIN
                // TODO : in case we're recycling the connection / sources, the track might already have been added.
                connection.AddLocalTrack(track);
#endif
            }

            localVideoSinkBehavior.VideoSink.SetStream(localMediaStream);
            localVideoSinkBehavior.ResetFrameFit();

            if (localVideoSourceDropdown.value == 1 || localVideoSourceDropdown.value == 3)
            {
                // Turn off the local video track behavior when in MLMRCamera ("screen share") mode.
                localVideoSinkBehavior.gameObject.SetActive(false);
                localStatusText.text = "Screen Sharing";
            }

        }

        void Update()
        {
            UpdateWebRequests();

            if (waitingForAnswer && !waitingForAnswerGetRequest)
            {
                // Reads the answer to the offer
                waitingForAnswerGetRequest = true;
                HttpGet(serverURI + "/answer/" + localId, (AsyncOperation asyncOp) =>
                {
                    UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                    waitingForAnswerGetRequest = false;
                    string response = webRequenstAsyncOp.webRequest.downloadHandler.text;
                    if (ParseAnswer(response, out remoteId, out string remoteAnswer))
                    {
                        waitingForAnswer = false;
#if PLATFORM_LUMIN
                        connection.SetRemoteAnswer(remoteAnswer);
#endif
                        // We've received a remoteId. Try to consume ices.
                        ConsumeIces();
                    }
                });
            }

            if (shouldDisconnect)
            {
                shouldDisconnect = false;
                Disconnect();
            }
        }

        void OnDestroy()
        {
#if PLATFORM_LUMIN
            Disconnect(true);
#endif
        }

        public void SendMessageOnDataChannel(string message)
        {
#if PLATFORM_LUMIN
            MLResult? result = this.dataChannel?.SendMessage(message);
            if (result.HasValue)
            {
                if (result.Value.IsOk)
                {
                    dataChannelText.text = "Sent: " + message;
                }
                else
                {
                    Debug.LogError($"MLWebRTC.DataChannel.SendMessage() failed with error {result}");
                }
            }
#endif
        }

        public void SendBinaryMessageOnDataChannel()
        {
#if PLATFORM_LUMIN
            // generate an array of 5 random integers to be sent via the data channel
            System.Random rand = new System.Random();
            int[] randomIntegers = new int[5];
            for (int i = 0; i < randomIntegers.Length; ++i)
            {
                randomIntegers[i] = rand.Next(0, 101);
            }

            MLResult? result = this.dataChannel?.SendMessage<int>(randomIntegers);
            if (result.HasValue)
            {
                if (result.Value.IsOk)
                {
                    dataChannelText.text = $"Sent: {string.Join(", ", randomIntegers)}";
                }
                else
                {
                    Debug.LogError($"MLWebRTC.DataChannel.SendMessage() failed with error {result}");
                }
            }
#endif
        }

        private void QueryOffers()
        {
            // GET request to check the server for any awaiting remote offers.
            HttpGet(serverURI + "/offers", (AsyncOperation asyncOp) =>
            {
#if PLATFORM_LUMIN
                UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                string offers = webRequenstAsyncOp.webRequest.downloadHandler.text;
                if (ParseOffers(offers, out remoteId, out string sdp))
                {
                    // We've received the offers and thus the remoteId. Next, try to consume ices. It'll only proceed if ice gathering is already compelete.
                    ConsumeIces();
                    connection.SetRemoteOffer(sdp);
                }
                else // If there are no offers available then create our own local data channel on the connection.
                {
                    messageUI.SetActive(true);
                    this.dataChannel = MLWebRTC.DataChannel.CreateLocal(connection, out MLResult result);
                    SubscribeToDataChannel(this.dataChannel);
                    connection.CreateOffer();
                }
#endif
            });
        }

        private void OnConnectionLocalOfferCreated(MLWebRTC.PeerConnection connection, string sendSdp)
        {
            remoteStatusText.text = "Sending offer...";
            HttpPost(serverURI + "/post_offer/" + localId, FormatSdpOffer("offer", sendSdp), (AsyncOperation ao) =>
            {
                remoteStatusText.text = "Waiting for answer...";
                waitingForAnswer = true;
            });
        }

        private void OnConnectionLocalAnswerCreated(MLWebRTC.PeerConnection connection, string sendAnswer)
        {
            remoteStatusText.text = "Sending answer to an offer...";
            HttpPost(serverURI + "/post_answer/" + localId + "/" + remoteId, FormatSdpOffer("answer", sendAnswer));
        }

        private void OnConnectionLocalIceCandidateFound(MLWebRTC.PeerConnection connection, MLWebRTC.IceCandidate iceCandidate)
        {
            remoteStatusText.text = "Sending ice candidate...";
            HttpPost(serverURI + "/post_ice/" + localId, FormatIceCandidate(iceCandidate));
        }

        private void OnConnectionIceGatheringCompleted(MLWebRTC.PeerConnection connection)
        {
            remoteStatusText.text = "On ice gathering completed...";
        }

        private void ConsumeIces()
        {
            if (!string.IsNullOrEmpty(remoteId))
            {
                // Queries for all the ices to test
                HttpPost(serverURI + "/consume_ices/" + remoteId, "", (AsyncOperation asyncOp) =>
                {

                    UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                    string iceCandidates = webRequenstAsyncOp.webRequest.downloadHandler.text;
                    // Parses all the ice candidates
                    JsonObject jsonObjects = (JsonObject)SimpleJson.DeserializeObject(iceCandidates);

                    JsonArray jsonArray = (JsonArray)jsonObjects[0];

                    for (int i = 0; i < jsonArray.Count; ++i)
                    {
                        JsonObject jsonObj = (JsonObject)jsonArray[i];
                        MLWebRTC.IceCandidate iceCandidate = MLWebRTC.IceCandidate.Create((string)jsonObj["candidate"], (string)jsonObj["sdpMid"], Convert.ToInt32(jsonObj["sdpMLineIndex"]));

#if PLATFORM_LUMIN
                        MLResult result = connection.AddRemoteIceCandidate(iceCandidate);
#endif
                        remoteStatusText.text = "";
                    }
                });
            }
        }

        private void OnConnectionConnected(MLWebRTC.PeerConnection connection)
        {
            remoteStatusText.text = "";
        }

        private void OnConnectionTrackAdded(List<MLWebRTC.MediaStream> mediaStream, MLWebRTC.MediaStream.Track addedTrack)
        {
            remoteStatusText.text = $"Adding {addedTrack.TrackType} track.";
            if (remoteMediaStream == null)
            {
                remoteMediaStream = mediaStream[0];
            }

            switch (addedTrack.TrackType)
            {
                // if the incoming track is audio, set the audio sink to this track.
                case MLWebRTC.MediaStream.Track.Type.Audio:
                {
                    remoteAudioSinkBehavior.AudioSink.SetStream(remoteMediaStream);
                    remoteAudioSinkBehavior.gameObject.SetActive(true);
                    remoteAudioSinkBehavior.AudioSink.SetCacheSize((uint)audioCacheSizeSlider.value);
                    break;
                }

                // if the incoming track is video, set the video sink to this track.
                case MLWebRTC.MediaStream.Track.Type.Video:
                {
                    remoteVideoSinkBehavior.VideoSink.SetStream(remoteMediaStream);
                    remoteVideoSinkBehavior.ResetFrameFit();
                    remoteVideoSinkBehavior.gameObject.SetActive(true);
                    break;
                }
            }
        }

        private void OnConnectionTrackRemoved(List<MLWebRTC.MediaStream> mediaStream, MLWebRTC.MediaStream.Track removedTrack)
        {
            remoteStatusText.text = $"Removed {removedTrack.TrackType} track.";

            switch (removedTrack.TrackType)
            {
                case MLWebRTC.MediaStream.Track.Type.Audio:
                {
                    remoteAudioSinkBehavior.AudioSink.SetStream(null);
                    remoteAudioSinkBehavior.gameObject.SetActive(false);
                    break;
                }

                case MLWebRTC.MediaStream.Track.Type.Video:
                {
                    remoteVideoSinkBehavior.VideoSink.SetStream(null);
                    remoteVideoSinkBehavior.gameObject.SetActive(false);
                    break;
                }
            }
        }

        private void OnConnectionDataChannelReceived(MLWebRTC.PeerConnection connection, MLWebRTC.DataChannel dataChannel)
        {
            messageUI.SetActive(true);

            if (this.dataChannel != null)
            {
                UnsubscribeFromDataChannel(this.dataChannel);
            }

            this.dataChannel = dataChannel;
            SubscribeToDataChannel(this.dataChannel);
            dataChannelText.text = "Data Channel";
        }

        private void SubscribeToConnection(MLWebRTC.PeerConnection connection)
        {
            connection.OnError += OnConnectionError;
            connection.OnConnected += OnConnectionConnected;
            connection.OnDisconnected += OnConnectionDisconnected;
            connection.OnTrackAddedMultipleStreams += OnConnectionTrackAdded;
            connection.OnTrackRemovedMultipleStreams += OnConnectionTrackRemoved;
            connection.OnDataChannelReceived += OnConnectionDataChannelReceived;
            connection.OnLocalOfferCreated += OnConnectionLocalOfferCreated;
            connection.OnLocalAnswerCreated += OnConnectionLocalAnswerCreated;
            connection.OnLocalIceCandidateFound += OnConnectionLocalIceCandidateFound;
            connection.OnIceGatheringCompleted += OnConnectionIceGatheringCompleted;
        }

        private void UnsubscribeFromConnection(MLWebRTC.PeerConnection connection)
        {
            connection.OnError -= OnConnectionError;
            connection.OnConnected -= OnConnectionConnected;
            connection.OnDisconnected -= OnConnectionDisconnected;
            connection.OnTrackAddedMultipleStreams -= OnConnectionTrackAdded;
            connection.OnTrackRemovedMultipleStreams -= OnConnectionTrackRemoved;
            connection.OnDataChannelReceived -= OnConnectionDataChannelReceived;
            connection.OnLocalOfferCreated -= OnConnectionLocalOfferCreated;
            connection.OnLocalAnswerCreated -= OnConnectionLocalAnswerCreated;
            connection.OnLocalIceCandidateFound -= OnConnectionLocalIceCandidateFound;
            connection.OnIceGatheringCompleted -= OnConnectionIceGatheringCompleted;
        }

        private void SubscribeToDataChannel(MLWebRTC.DataChannel dataChannel)
        {
            dataChannel.OnClosed += OnDataChannelClosed;
            dataChannel.OnOpened += OnDataChannelOpened;
            dataChannel.OnMessageText += OnDataChannelTextMessage;
            dataChannel.OnMessageBinary += OnDataChannelBinaryMessage;
        }

        private void UnsubscribeFromDataChannel(MLWebRTC.DataChannel dataChannel)
        {
            dataChannel.OnClosed -= OnDataChannelClosed;
            dataChannel.OnOpened -= OnDataChannelOpened;
            dataChannel.OnMessageText -= OnDataChannelTextMessage;
            dataChannel.OnMessageBinary -= OnDataChannelBinaryMessage;
        }

        private void OnDataChannelOpened(MLWebRTC.DataChannel dataChannel)
        {
            dataChannelText.text = "Data Channel";
        }

        private void OnDataChannelClosed(MLWebRTC.DataChannel dataChannel)
        {
            dataChannelText.text = "";
            UnsubscribeFromDataChannel(dataChannel);
        }

        private void OnDataChannelTextMessage(MLWebRTC.DataChannel dataChannel, string message)
        {
            dataChannelText.text = "Received: \n" + message;
        }

        private void OnDataChannelBinaryMessage(MLWebRTC.DataChannel dataChannel, byte[] message)
        {
            // example is built only to expect integer data in the binary message.
            if (message.Length % sizeof(int) != 0)
            {
                dataChannelText.text = "Received: \n" + message.Length + " bytes were received.";
            }
            else
            {
                int numIntegers = message.Length / sizeof(int);
                int[] intMessage = new int[numIntegers];
                for (int i = 0; i < numIntegers; ++i)
                {
                    intMessage[i] = BitConverter.ToInt32(message, i * sizeof(int));
                }

                dataChannelText.text = $"Received: \n {string.Join(", ", intMessage)}";
            }
        }

        private void OnConnectionDisconnected(MLWebRTC.PeerConnection connection)
        {
            // Don't call Disconnect() here because that attempts to destroy the connection object
            // while being inside its callback and results in a deadlock.
            shouldDisconnect = true;
        }

        private void OnConnectionError(MLWebRTC.PeerConnection connection, string errorMessage)
        {
            remoteStatusText.text = "Error: " + errorMessage;
            dataChannelText.text = "";
            keyboardUI.SetActive(true);
            messageUI.SetActive(false);
        }

        private bool ParseOffers(string data, out string remoteId, out string sdp)
        {
            bool result = false;
            sdp = "";
            remoteId = "";

            if (data == "{}" || data == string.Empty)
            {
                return result;
            }

            SimpleJson.TryDeserializeObject(data, out object obj);
            JsonObject jsonObj = (JsonObject)obj;
            foreach (KeyValuePair<string, object> pair in jsonObj)
            {
                remoteId = pair.Key;
                JsonObject offerObj = (JsonObject)pair.Value;
                sdp = (string)offerObj["sdp"];
                result = true;
            }

            return result;
        }

        private bool ParseAnswer(string data, out string remoteId, out string sdp)
        {
            bool result = false;
            sdp = "";
            remoteId = "";

            if (data == "{}" || data == string.Empty)
            {
                return result;
            }

            SimpleJson.TryDeserializeObject(data, out object obj);
            if(obj == null)
            {
                return false;
            }

            JsonObject jsonObj = (JsonObject)obj;
            if (jsonObj.ContainsKey("id") && jsonObj.ContainsKey("answer"))
            {
                remoteId = ((Int64)jsonObj["id"]).ToString();
                JsonObject answerObj = (JsonObject)jsonObj["answer"];
                sdp = (string)answerObj["sdp"];
                result = true;
            }

            return result;
        }

        public MLWebRTC.IceServer[] CreateIceServers()
        {
            string stunServer1Uri = "stun:stun.l.google.com:19302";
            string stunServer2Uri = "stun:" + serverAddress + ":3478";
            string turnServerUri = "turn:" + serverAddress + ":3478";
            string userName = "foo";
            string password = "bar";

            MLWebRTC.IceServer[] iceServers = new MLWebRTC.IceServer[3];

            // Stun server 1
            iceServers[0] = MLWebRTC.IceServer.Create(stunServer1Uri);

            // Stun server 2
            iceServers[1] = MLWebRTC.IceServer.Create(stunServer2Uri);

            // Turn server
            iceServers[2] = MLWebRTC.IceServer.Create(turnServerUri, userName, password);

            return iceServers;
        }

        public string CreateServerURI(string serverAddress)
        {
            return "http://" + serverAddress + ":8080";
        }

        public static string FormatSdpOffer(string offer, string sdp)
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj["sdp"] = sdp;
            jsonObj["type"] = offer;
            return jsonObj.ToString();
        }

        public static string FormatIceCandidate(MLWebRTC.IceCandidate iceCandidate)
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj["candidate"] = iceCandidate.Candidate;
            jsonObj["sdpMLineIndex"] = iceCandidate.SdpMLineIndex;
            jsonObj["sdpMid"] = iceCandidate.SdpMid;
            return jsonObj.ToString();
        }

        public void ToggleLocalAudio(bool on)
        {
#if PLATFORM_LUMIN
            localMediaStream.ActiveAudioTrack.SetEnabled(on);
#endif
        }

        public void ToggleRemoteAudio(bool on)
        {
#if PLATFORM_LUMIN
            remoteAudioSinkBehavior.AudioSink.Stream.ActiveAudioTrack.SetEnabled(on);
#endif
        }

        public void ToggleLocalVideo(bool on)
        {
#if PLATFORM_LUMIN
            localVideoSinkBehavior.VideoSink.Stream.ActiveVideoTrack.SetEnabled(on);
#endif
        }

        public void ToggleRemoteVideo(bool on)
        {
#if PLATFORM_LUMIN
            remoteVideoSinkBehavior.VideoSink.Stream.ActiveVideoTrack.SetEnabled(on);
#endif
        }


        public void OnAudioCacheSizeSliderValueChanged()
        {
            if (audioCacheSliderValue != null)
            {
                audioCacheSliderValue.text = $"{audioCacheSizeSlider.value} ms";
            }

            if (remoteAudioSinkBehavior.AudioSink != null)
            {
                remoteAudioSinkBehavior.AudioSink.SetCacheSize((uint)audioCacheSizeSlider.value);
            }
        }

        public void Disconnect(bool onDestroy = false)
        {
            if (connection == null)
            {
                return;
            }

            HttpPost(serverURI + "/logout/" + localId, string.Empty);

            if (dataChannel != null)
            {
                dataChannel.OnClosed -= OnDataChannelClosed;
                dataChannel.OnOpened -= OnDataChannelOpened;
                dataChannel.OnMessageText -= OnDataChannelTextMessage;
                dataChannel.OnMessageBinary -= OnDataChannelBinaryMessage;
                dataChannel = null;
            }

            UnsubscribeFromConnection(connection);
#if PLATFORM_LUMIN
            connection.Destroy();
#endif
            connection = null;

            remoteMediaStream = null;
            waitingForAnswer = false;
            waitingForAnswerGetRequest = false;

            localMediaStream.DestroyLocal();
            localMediaStream = null;

            if (!onDestroy)
            {
                keyboardUI.SetActive(true);
                localVideoSinkBehavior.gameObject.SetActive(false);
                remoteStatusText.text = "Disconnected";
                localStatusText.text = "";
                remoteVideoSinkBehavior.VideoSink.SetStream(null);
                remoteAudioSinkBehavior.gameObject.SetActive(false);
                remoteAudioSinkBehavior.gameObject.SetActive(false);
                remoteVideoSinkBehavior.gameObject.SetActive(false);
                disconnectUI.SetActive(false);
                messageUI.SetActive(false);
                dataChannelText.text = "";
            }

            remoteId = "";
            localId = "";
        }

        private void HttpPost(string url, string data, Action<AsyncOperation> onCompleted = null)
        {
            UnityWebRequest request;
            if (data != string.Empty)
            {
                request = new UnityWebRequest(url);
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.method = UnityWebRequest.kHttpVerbPOST;
            }
            else
            {
                request = UnityWebRequest.Post(url, data);
            }

            pendingWebRequests.Enqueue(request);
            webRequestsToOnCompletedEvent.Add(request, onCompleted);
        }

        private void HttpGet(string url, Action<AsyncOperation> onCompleted = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            pendingWebRequests.Enqueue(request);
            webRequestsToOnCompletedEvent.Add(request, onCompleted);
        }

        private void UpdateWebRequests()
        {
            // Use lastWebRequestCompleted instead of lastWebRequest.isDone because the latter can
            // cause race conditions resulting in the "completed" callback never being fired.
            if (pendingWebRequests.Count > 0 && lastWebRequestCompleted)
            {
                lastWebRequestCompleted = false;
                lastWebRequest = pendingWebRequests.Dequeue();
                lastWebRequestAsyncOp = lastWebRequest.SendWebRequest();
                lastWebRequestAsyncOp.completed += (AsyncOperation asyncOp) =>
                {
                    UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                    if (webRequenstAsyncOp.webRequest.result != UnityWebRequest.Result.Success)
                    {
#if PLATFORM_LUMIN
                        MLPluginLog.ErrorFormat($"MLWebRTCExample.Http{webRequenstAsyncOp.webRequest.method}({webRequenstAsyncOp.webRequest.url}) failed, Reason : {webRequenstAsyncOp.webRequest.error}");
#endif
                    }
                    webRequestsToOnCompletedEvent[lastWebRequest]?.Invoke(asyncOp);
                    lastWebRequestCompleted = true;
                };
            }
        }
    }
}


