// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

namespace MagicLeap.Examples
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MagicLeap.Core;
    using SimpleJson;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using UnityEngine.XR.MagicLeap;

    public class MLWebRTCExample : MonoBehaviour
    {
        private static readonly string PlayerPrefs_ServerAddress_Key = "MLWebRTC_Example_Server";

        public MLWebRTC.MediaStream.Track.AudioType audioType = MLWebRTC.MediaStream.Track.AudioType.Microphone;
        public MLWebRTCLocalAppDefinedAudioSourceBehavior localAppDefinedAudioSourceBehavior;
        public MLWebRTCVideoSinkBehavior localVideoSinkBehavior;
        public MLWebRTCVideoSinkBehavior remoteVideoSinkBehavior;
        public MLWebRTCAudioSinkBehavior remoteAudioSinkBehavior;

        public InputField serverInput;
        public InputField messageInput;
        public GameObject messageUI;
        public GameObject disconnectUI;

        public Text localStatusText;
        public Text remoteStatusText;
        public Text dataChannelText;
        public Text connectionStatusText;

        public Text localAudioStatus;
        public Text localVideoStatus;
        public Text remoteAudioStatus;
        public Text remoteVideoStatus;

        public Dropdown localVideoSourceDropdown;
        public Dropdown localVideoSizeDropdownRGB;
        public Dropdown localVideoSizeDropdownMR;

        public Slider audioCacheSizeSlider;
        public Text audioCacheSliderValue;
        public Button connectButton;

        [SerializeField]
        private MLVirtualKeyboard virtualKeyboard;

        [SerializeField]
        private Renderer localVideoRenderer;

        [SerializeField]
        private bool useHWBuffers = true;

        private bool waitingForAnswer = false;
        private bool waitingForAnswerGetRequest = false;
        private bool shouldDisconnect = false;

        private string serverAddress = "";
        private string serverURI = "";
        private string localId = "";
        private string remoteId = "";
        private int captureWidth = 1920;
        private int captureHeight = 1080;
        private MLWebRTC.PeerConnection connection = null;
        private MLWebRTC.DataChannel dataChannel = null;
        private MLWebRTC.MLCameraVideoSource localVideoSource;
        private MLWebRTC.MediaStream localMediaStream = null;
        private MLWebRTC.MediaStream remoteMediaStream = null;

        // The sample server can only handle concurrent requests. Maintain a queue to send only 1 request at a time.
        private ConcurrentWebRequestManager webRequestManager = new ConcurrentWebRequestManager();

        private MLCamera mlCamera;
        private MLCamera.ConnectContext connectContext;

        private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();
        private static readonly string[] requiredPermissions = new string[] { MLPermission.Camera, MLPermission.RecordAudio };
        private readonly HashSet<string> grantedPermissions = new HashSet<string>();

        private VideoSize selectedVideoSize = VideoSize._1080p;
        private MLCamera.ConnectFlag selectedFlag = MLCamera.ConnectFlag.CamOnly;
        private MLCamera.MRQuality selectedMRQuality = MLCamera.MRQuality._1440x1080;

        enum VideoSize
        {
            _720p,
            _1080p,
            _1440p,
            _2160p
        }

        private void Awake()
        {
            permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        }

        void Start()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            // Camera and Microphone must be checked at runtime
            foreach (string permission in requiredPermissions)
            {
                MLPermissions.RequestPermission(permission, permissionCallbacks);
            }
#endif
        }

        private void StartAfterPermissions()
        {
            disconnectUI.SetActive(false);
            messageUI.SetActive(false);
            connectButton.gameObject.SetActive(false);

            var savedServer = PlayerPrefs.GetString(PlayerPrefs_ServerAddress_Key, "");
            if (!string.IsNullOrEmpty(savedServer))
            {
                serverInput.text = savedServer;
                connectButton.gameObject.SetActive(true);
            }

            virtualKeyboard.gameObject.SetActive(false);
        }

        public void TryToConnect()
        {
            Connect(serverInput.text);
        }

        // Subscribed to keyboard event within the inspector
        public void Connect(string address)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            serverAddress = address;
            serverURI = CreateServerURI(serverAddress);
            remoteStatusText.text = "Creating connection...";
            serverInput.gameObject.SetActive(false);
            connectButton.gameObject.SetActive(false);
            Login();
            PlayerPrefs.SetString(PlayerPrefs_ServerAddress_Key, address);
#endif
        }

        public void Login()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            try
            {
                webRequestManager.HttpPost(serverURI + "/login", string.Empty, 
                async (AsyncOperation asyncOp) =>
                {
                    UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                    connectionStatusText.text = webRequenstAsyncOp.webRequest.result.ToString();
                    if (webRequenstAsyncOp.webRequest.result != UnityWebRequest.Result.Success || string.IsNullOrEmpty(webRequenstAsyncOp.webRequest.downloadHandler.text))
                    {
                        remoteStatusText.text = "";
                        serverInput.gameObject.SetActive(true);
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
                    localVideoSourceDropdown.interactable = false;
                    localVideoSizeDropdownRGB.interactable = false;
                    localVideoSizeDropdownMR.interactable = false;

                    SubscribeToConnection(connection);
                    await CreateLocalMediaStream();
                    QueryOffers();
                });
            }
            catch (UriFormatException)
            {
                Debug.LogError($"Bad URI: hostname \"{serverURI}\" could not be parsed.");
            }
#endif
        }

        private async Task CreateLocalMediaStream()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            localVideoSinkBehavior.gameObject.SetActive(true);
            localStatusText.text = "";

            string id = "local";

            connectContext = new MLCamera.ConnectContext()
            {
                CamId = MLCamera.Identifier.Main,
                Flags = selectedFlag,
                EnableVideoStabilization = false,
                MixedRealityConnectInfo = new MLCamera.MRConnectInfo()
                {
                    MRBlendType = MLCamera.MRBlendType.Additive,
                    MRQuality = selectedMRQuality,
                    FrameRate = MLCamera.CaptureFrameRate._60FPS
                }
            };

            var captureFrameRate = MLCamera.CaptureFrameRate._60FPS;
            if (selectedVideoSize > VideoSize._1080p)
            {
                captureFrameRate = MLCamera.CaptureFrameRate._30FPS;
            }

            if (selectedFlag != MLCamera.ConnectFlag.CamOnly)
            {
                // these MR quality settings don't support 60 FPS, so lower framerate if one of them is being used
                if (selectedMRQuality == MLCamera.MRQuality._1944x2160 || selectedMRQuality == MLCamera.MRQuality._2880x2160)
                {
                    captureFrameRate = connectContext.MixedRealityConnectInfo.FrameRate = MLCamera.CaptureFrameRate._30FPS;
                }

                connectContext.MixedRealityConnectInfo.FrameRate = captureFrameRate;
            }

            mlCamera = await MLCamera.CreateAndConnectAsync(connectContext);
            if (mlCamera == null)
            {
                return;
            }

            MLCamera.StreamCapability[] streamCapabilities = useHWBuffers ? MLCamera.GetImageStreamCapabilitiesForCamera(mlCamera, MLCamera.CaptureType.Video, MLCamera.CaptureType.Preview) 
                                                                          : MLCamera.GetImageStreamCapabilitiesForCamera(mlCamera, MLCamera.CaptureType.Video);

            if (streamCapabilities.Length == 0)
            {
                Debug.LogError("Could not get stream capabilities of camera");
                return;
            }

            var outputFormat = MLCamera.OutputFormat.YUV_420_888;
            if (!useHWBuffers && connectContext.Flags != MLCamera.ConnectFlag.CamOnly)
            {
                outputFormat = MLCamera.OutputFormat.RGBA_8888;
            }

            // get stream configs based on available capabilities. Preview won't be available if capturing virtual content.
            var streamConfigs = new List<MLCamera.CaptureStreamConfig>();

            if (MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, captureWidth, captureHeight, MLCamera.CaptureType.Video,
                out var videoStreamCapability))
            {
                streamConfigs.Add(MLCamera.CaptureStreamConfig.Create(videoStreamCapability , outputFormat));
            }

            if (MLCamera.IsCaptureTypeSupported(mlCamera, MLCamera.CaptureType.Preview) && useHWBuffers)
            {
                if (MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, captureWidth, captureHeight,
                    MLCamera.CaptureType.Preview, out var previewStreamCapability))
                {
                    streamConfigs.Add(MLCamera.CaptureStreamConfig.Create(previewStreamCapability, outputFormat));
                }
            }

            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig()
            {
                CaptureFrameRate = captureFrameRate,
                StreamConfigs = streamConfigs.ToArray()
            };

            // Use factory methods to create a new media stream.
            if (localMediaStream == null)
            {
                localVideoSource =
                    MLWebRTC.MLCameraVideoSource.CreateLocal(mlCamera, captureConfig, out MLResult result, id, localVideoRenderer, useHWBuffers);

                var localDefinedAudioSource = (audioType == MLWebRTC.MediaStream.Track.AudioType.Defined) ? new DefinedAudioSourceExample(id) : null;

                localMediaStream = MLWebRTC.MediaStream.CreateWithAppDefinedVideoTrack(id, localVideoSource, audioType, "", localDefinedAudioSource);

                if (localAppDefinedAudioSourceBehavior != null)
                {
                    if (audioType == MLWebRTC.MediaStream.Track.AudioType.Defined)
                    {
                        localAppDefinedAudioSourceBehavior.gameObject.SetActive(true);
                        localAppDefinedAudioSourceBehavior.Init(localMediaStream.AudioTracks[0] as DefinedAudioSourceExample);
                    }
                }

                connection.AddLocalTrack(localMediaStream.ActiveVideoTrack);
                connection.AddLocalTrack(localMediaStream.ActiveAudioTrack);

                localVideoSinkBehavior.VideoSink.SetStream(localMediaStream);
            }
#endif
        }

        void Update()
        {
            webRequestManager.UpdateWebRequests();

            if (waitingForAnswer && !waitingForAnswerGetRequest)
            {
                // Reads the answer to the offer
                waitingForAnswerGetRequest = true;
                webRequestManager.HttpGet(serverURI + "/answer/" + localId, (AsyncOperation asyncOp) =>
                {
                    UnityWebRequestAsyncOperation webRequenstAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                    waitingForAnswerGetRequest = false;
                    string response = webRequenstAsyncOp.webRequest.downloadHandler.text;
                    if (ParseAnswer(response, out remoteId, out string remoteAnswer))
                    {
                        waitingForAnswer = false;
#if UNITY_MAGICLEAP || UNITY_ANDROID
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
#if UNITY_MAGICLEAP || UNITY_ANDROID
            permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
            permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
            permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

            Disconnect(true);
#endif
        }

        public void SendMessageOnDataChannel()
        {
            string message = messageInput.text;
            if (string.IsNullOrEmpty(message))
                return;
#if UNITY_MAGICLEAP || UNITY_ANDROID
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
            messageInput.text = "";
        }

        public void SetVideoSource(int sourceDropdownIndex)
        {
            selectedFlag = (MLCamera.ConnectFlag)sourceDropdownIndex;
        }

        public void SetVideoResolutionCamOnly(int sizeDropdownIndex)
        {
            selectedVideoSize = (VideoSize)sizeDropdownIndex;

            switch (selectedVideoSize)
            {
                case VideoSize._720p:
                    captureWidth = 1280;
                    captureHeight = 720;
                    break;
                case VideoSize._1080p:
                    captureWidth = 1920;
                    captureHeight = 1080;
                    break;
                case VideoSize._1440p:
                    captureWidth = 2560;
                    captureHeight = 1440;
                    break;
                case VideoSize._2160p:
                    captureWidth = 3840;
                    captureHeight = 2160;
                    break;
            }
        }

        public void SetVideoResolutionMR(int sizeDropdownIndex)
        {
            // only the 4x3 MRQuality values can be streamed over webrtc when using any output format
            selectedMRQuality = (MLCamera.MRQuality)(sizeDropdownIndex + 4);
        }

        public void SendBinaryMessageOnDataChannel()
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
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
            webRequestManager.HttpGet(serverURI + "/offers", (AsyncOperation asyncOp) =>
            {
#if UNITY_MAGICLEAP || UNITY_ANDROID
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
            webRequestManager.HttpPost(serverURI + "/post_offer/" + localId, FormatSdpOffer("offer", sendSdp), (AsyncOperation ao) =>
            {
                remoteStatusText.text = "Waiting for answer...";
                waitingForAnswer = true;
            });
        }

        private void OnConnectionLocalAnswerCreated(MLWebRTC.PeerConnection connection, string sendAnswer)
        {
            remoteStatusText.text = "Sending answer to an offer...";
            webRequestManager.HttpPost(serverURI + "/post_answer/" + localId + "/" + remoteId, FormatSdpOffer("answer", sendAnswer));
        }

        private void OnConnectionLocalIceCandidateFound(MLWebRTC.PeerConnection connection, MLWebRTC.IceCandidate iceCandidate)
        {
            remoteStatusText.text = "Sending ice candidate...";
            webRequestManager.HttpPost(serverURI + "/post_ice/" + localId, FormatIceCandidate(iceCandidate));
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
                webRequestManager.HttpPost(serverURI + "/consume_ices/" + remoteId, "", (AsyncOperation asyncOp) =>
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

#if UNITY_MAGICLEAP || UNITY_ANDROID
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

        private void OnConnectionTrackAdded(List<MLWebRTC.MediaStream> mediaStreams, MLWebRTC.MediaStream.Track addedTrack)
        {
            remoteStatusText.text = $"Adding {addedTrack.TrackType} track.";
            if (remoteMediaStream == null)
            {
                remoteMediaStream = mediaStreams[0];
            }

            switch (addedTrack.TrackType)
            {
                // if the incoming track is audio, set the audio sink to this track.
                case MLWebRTC.MediaStream.Track.Type.Audio:
                    remoteAudioSinkBehavior.AudioSink.SetStream(remoteMediaStream);
                    remoteAudioSinkBehavior.gameObject.SetActive(true);
                    remoteAudioSinkBehavior.AudioSink.SetCacheSize((uint)audioCacheSizeSlider.value);
                    break;

                // if the incoming track is video, set the video sink to this track.
                case MLWebRTC.MediaStream.Track.Type.Video:
                    remoteVideoSinkBehavior.VideoSink.SetStream(remoteMediaStream);
                    remoteVideoSinkBehavior.gameObject.SetActive(true);
                    break;
            }
        }

        private void OnConnectionTrackRemoved(List<MLWebRTC.MediaStream> mediaStream, MLWebRTC.MediaStream.Track removedTrack)
        {
            remoteStatusText.text = $"Removed {removedTrack.TrackType} track.";

            switch (removedTrack.TrackType)
            {
                case MLWebRTC.MediaStream.Track.Type.Audio:
                    remoteAudioSinkBehavior.AudioSink.SetStream(null);
                    remoteAudioSinkBehavior.gameObject.SetActive(false);
                    break;

                case MLWebRTC.MediaStream.Track.Type.Video:
                    remoteVideoSinkBehavior.VideoSink.SetStream(null);
                    remoteVideoSinkBehavior.gameObject.SetActive(false);
                    break;
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
            serverInput.gameObject.SetActive(true);
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
            if (obj == null)
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
#if UNITY_MAGICLEAP || UNITY_ANDROID
            localMediaStream.ActiveAudioTrack?.SetEnabled(on);
            localAudioStatus.text = on ? "On" : "Off";
#endif
        }

        public void ToggleRemoteAudio(bool on)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            remoteAudioSinkBehavior.AudioSink.Stream?.ActiveAudioTrack?.SetEnabled(on);
            remoteAudioStatus.text = on ? "On" : "Off";
#endif
        }

        public void ToggleLocalVideo(bool on)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            localMediaStream.ActiveVideoTrack?.SetEnabled(on);
            localVideoStatus.text = on ? "On" : "Off";
#endif
        }

        public void ToggleRemoteVideo(bool on)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            remoteVideoSinkBehavior.VideoSink.Stream?.ActiveVideoTrack?.SetEnabled(on);
            remoteVideoStatus.text = on ? "On" : "Off";
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
            StartCoroutine(DisconnectCoroutine(onDestroy));
        }

        private IEnumerator DisconnectCoroutine(bool onDestroy = false)
        {
#if UNITY_MAGICLEAP || UNITY_ANDROID
            if (connection == null)
            {
                yield break;
            }

            webRequestManager.HttpPost(serverURI + "/logout/" + localId, string.Empty);

            if (localVideoSource != null)
            {
                localVideoSource.DestroyLocal();

                yield return new WaitWhile(() => localVideoSource.IsCapturing);

                if (mlCamera != null)
                {
                    mlCamera.Disconnect();
                }
            }

            yield return null;

            if (dataChannel != null)
            {
                dataChannel.OnClosed -= OnDataChannelClosed;
                dataChannel.OnOpened -= OnDataChannelOpened;
                dataChannel.OnMessageText -= OnDataChannelTextMessage;
                dataChannel.OnMessageBinary -= OnDataChannelBinaryMessage;
                dataChannel = null;
            }

            UnsubscribeFromConnection(connection);
            connection.Destroy();
            connection = null;

            remoteMediaStream = null;
            waitingForAnswer = false;
            waitingForAnswerGetRequest = false;

            localMediaStream.DestroyLocal();
            localMediaStream = null;

            if (!onDestroy)
            {
                connectButton.gameObject.SetActive(true);
                serverInput.gameObject.SetActive(true);
                localVideoSinkBehavior.gameObject.SetActive(false);
                remoteStatusText.text = "Disconnected";
                localStatusText.text = "";
                remoteVideoSinkBehavior.VideoSink.SetStream(null);
                remoteAudioSinkBehavior.gameObject.SetActive(false);
                remoteVideoSinkBehavior.gameObject.SetActive(false);
                disconnectUI.SetActive(false);
                messageUI.SetActive(false);
                dataChannelText.text = "";
                localVideoSourceDropdown.interactable = true;
                localVideoSizeDropdownRGB.interactable = true;
                localVideoSizeDropdownMR.interactable = true;
            }

            remoteId = "";
            localId = "";
#else
            yield break;
#endif
        }

        public void SetServerInputValue(string serverInputValue)
        {
            connectButton.gameObject.SetActive(!string.IsNullOrEmpty(serverInputValue));
        }

        private void OnPermissionDenied(string permission)
        {
#if UNITY_ANDROID
            MLPluginLog.Error($"{permission} denied, example won't function.");
#endif
        }

        private void OnPermissionGranted(string permission)
        {
            grantedPermissions.Add(permission);
            if (grantedPermissions.Count == requiredPermissions.Length)
            {
                StartAfterPermissions();
            }
        }
    }
}


