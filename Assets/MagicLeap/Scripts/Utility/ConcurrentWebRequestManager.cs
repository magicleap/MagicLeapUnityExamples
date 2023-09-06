// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2021-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MagicLeap
{
    /// <summary>
    /// For servers that can only handle concurrent requests, this class maintains a queue to send only 1 request at a time.
    /// Call UpdateWebRequests() every frame to check the status of ongoing requests and submit a new one from the pending queue.
    /// </summary>
    public class ConcurrentWebRequestManager
    {
        private Queue<UnityWebRequest> pendingWebRequests = new Queue<UnityWebRequest>();
        private Dictionary<UnityWebRequest, Action<AsyncOperation>> webRequestsToOnCompletedEvent = new Dictionary<UnityWebRequest, Action<AsyncOperation>>();
        private UnityWebRequest lastWebRequest = null;
        private UnityWebRequestAsyncOperation lastWebRequestAsyncOp = null;
        private bool lastWebRequestCompleted = true;

        public void HttpPost(string url, string data, Action<AsyncOperation> onCompleted = null)
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
                request = UnityWebRequest.PostWwwForm(url, data);
            }

            pendingWebRequests.Enqueue(request);
            webRequestsToOnCompletedEvent.Add(request, onCompleted);
        }

        public void HttpGet(string url, Action<AsyncOperation> onCompleted = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            pendingWebRequests.Enqueue(request);
            webRequestsToOnCompletedEvent.Add(request, onCompleted);
        }

        public void UpdateWebRequests()
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
                    UnityWebRequestAsyncOperation webRequestAsyncOp = asyncOp as UnityWebRequestAsyncOperation;
                    if (webRequestAsyncOp.webRequest.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"MLWebRTCExample.Http{webRequestAsyncOp.webRequest.method}({webRequestAsyncOp.webRequest.url}) failed, Reason : {webRequestAsyncOp.webRequest.error}");
                    }
                    webRequestsToOnCompletedEvent[lastWebRequest]?.Invoke(asyncOp);
                    webRequestsToOnCompletedEvent.Remove(lastWebRequest);
                    lastWebRequestCompleted = true;
                };
            }
        }
    }
}
