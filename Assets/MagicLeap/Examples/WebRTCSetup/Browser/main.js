'use strict';

const hostInput = document.getElementById('host');
const startButton = document.getElementById('startButton');
const hangupButton = document.getElementById('hangupButton');
hangupButton.disabled = true;
startButton.addEventListener('click', start);
hangupButton.addEventListener('click', hangup);

const localVideo = document.getElementById('localVideo');
const remoteVideo = document.getElementById('remoteVideo');

const localAudioEnabled = document.querySelector('#localAudioEnabled');
const localVideoEnabled = document.querySelector('#localVideoEnabled');
const remoteAudioEnabled = document.querySelector('#remoteAudioEnabled');
const remoteVideoEnabled = document.querySelector('#remoteVideoEnabled');
localAudioEnabled.addEventListener('change', updateStreams);
localVideoEnabled.addEventListener('change', updateStreams);
remoteAudioEnabled.addEventListener('change', updateStreams);
remoteVideoEnabled.addEventListener('change', updateStreams);

const chat = document.getElementById('chat');
const sendMessage = document.getElementById('sendMessage');
const sendButton = document.getElementById('sendButton');
const sendBinaryButton = document.getElementById('sendBinaryButton');

sendMessage.disabled = true;
sendButton.disabled = true;
sendBinaryButton.disabled = true;

let host;
let server;
let localId;

let localStream;
let pc;

let dataChannel;

let interval;

const mediaConstraints = {
  audio: true,
  video: true
}

const offerOptions = {
  offerToReceiveAudio: 1,
  offerToReceiveVideo: 1
};


async function start() {
  try {
    const stream = await navigator.mediaDevices.getUserMedia(mediaConstraints);
    console.log('Received local stream');
    localVideo.srcObject = stream;
    localStream = stream;
  } catch (e) {
    alert(`getUserMedia() error: ${e.name}`);
  }

  updateStreams();

  host = hostInput.value;
  server = 'http://' + host + ':8080';

  let response = await fetch(server + '/login', { method: 'POST' });
  localId = await response.text();
  console.log('Logged in as ' + localId);

  let config = {
    'iceServers': [
      { 'url' : 'stun:stun.l.google.com:19302' },
      { 'url' : 'stun:' + host + ':3478' },
      { 'url' : 'turn:' + host + ':3478', credential: 'foo', username: 'bar' },
    ]
  }
  pc = new RTCPeerConnection(config);

  pc.addEventListener("iceconnectionstatechange", async function(ev) {
    switch(pc.iceConnectionState) {
      case "disconnected":
      case "failed":
      case "closed":
        hangup();
        break;
    }
  });

  pc.addEventListener('icecandidate', async function(ev) {
    if (ev.candidate) {
      console.log("Posting ICE candidate");
      await fetch(server + '/post_ice/' + localId, { method: 'POST', body: JSON.stringify(ev.candidate.toJSON()) });
    }
  });

  pc.addEventListener('track', function(ev) {
    console.log('Track received');
    remoteVideo.srcObject = ev.streams[0];
    updateStreams();
  });

  localStream.getTracks().forEach(track => pc.addTrack(track, localStream));

  response = await fetch(server + '/offers');
  let offers = await response.json();
  console.log('Got offers: ' + Object.keys(offers).length);

  if (Object.keys(offers).length > 0) {
    pc.ondatachannel = function(ev) {
      dataChannel = ev.channel;
      setupDataChannel();
    };

    for (let remote_id in offers) {
      pc.remote_id = remote_id;
      console.log('Connecting to ' + remote_id);
      let offer = new RTCSessionDescription(offers[remote_id]);
      await pc.setRemoteDescription(offer);
      let answer = await pc.createAnswer();
      await pc.setLocalDescription(answer);
      console.log('Posting answer to ' + remote_id);
      await fetch(server + '/post_answer/' + localId + '/' + remote_id, { method: 'POST', body: JSON.stringify(answer.toJSON()) });
      break;
    }
  } else {
    dataChannel = pc.createDataChannel("testChannel");
    setupDataChannel();

    const desc = await pc.createOffer(offerOptions);
    await pc.setLocalDescription(desc);
    console.log('Posting offer');
    await fetch(server + '/post_offer/' + localId, { method: 'POST', body: JSON.stringify(desc.toJSON()) });
  }

  interval = setInterval(update, 1000);

  hostInput.disabled = true;
  startButton.disabled = true;
  hangupButton.disabled = false;
}

async function hangup() {
  clearInterval(interval);

  if (pc) {
    pc.close();
    pc = null;
  }

  localStream.getTracks().forEach(function(track) {
    track.stop();
  });

  localStream = null;

  let response = await fetch(server + '/logout/' + localId, { method: 'POST' });

  hostInput.disabled = false;
  startButton.disabled = false;
  hangupButton.disabled = true;
  sendMessage.disabled = true;
  sendButton.disabled = true;
  sendBinaryButton.disabled = true;
  chat.textContent = "";
}

sendButton.addEventListener('click', async function (ev) {
  if (dataChannel && dataChannel.readyState == "open") {
    chat.textContent += "You: " + sendMessage.value + "\n";
    dataChannel.send(sendMessage.value);
  }
});

sendBinaryButton.addEventListener('click', async function (ev) {
  if (dataChannel && dataChannel.readyState == "open") {
    // 5 integers of 4 bytes each
    var buffer = new ArrayBuffer(5 * 4);
    var bufferView = new Int32Array(buffer);
    for (var i = 0; i < 5; i++) {
      bufferView[i] = Math.floor(Math.random() * 101);
    }
    chat.textContent += "You (binary): " + bufferView.toString() + "\n";
    dataChannel.send(buffer);
  }
});

function setupDataChannel() {
  if (dataChannel) {
    dataChannel.onopen = function() {
      console.log("Data channel openend")
      sendMessage.disabled = false;
      sendButton.disabled = false;
      sendBinaryButton.disabled = false;
    }
    dataChannel.onclose = function() {
      console.log("Data channel closed")
      sendMessage.disabled = true;
      sendButton.disabled = true;
      sendBinaryButton.disabled = true;
    }
    dataChannel.onmessage = function(ev) {
      if (ev.data instanceof ArrayBuffer) {
        var bufferView = new Int32Array(ev.data);
        chat.textContent += "Peer (binary): " + bufferView.toString() + "\n";
      } else if (typeof(ev.data) == "string") {
        chat.textContent += "Peer: " + ev.data + "\n";
      } else {
        console.log("recieved data channel message of unexpected type %s" + typeof(ev.data));
        chat.textContent += "Peer: " + ev.data + "\n";
      }
    }
  }
}

async function update() {
  try {
    if (!pc.remote_id) {
      let response = await fetch(server + '/answer/' + localId);
      let answer = await response.json();
      if (answer.id) {
        pc.remote_id = answer.id;
        let desc = new RTCSessionDescription(answer.answer);
        await pc.setRemoteDescription(desc);
      }
    }
    if (pc.remote_id) {
      let response = await fetch(server + '/consume_ices/' + pc.remote_id, { method: 'POST' });
      let ices = (await response.json()).ices;
      for (let ice of ices) {
        console.log('Adding remote ICE')
        pc.addIceCandidate(ice);
      }
    }
  } catch (e) {
    console.error(e)
  }
}

function updateStreams() {
  console.log('Updating local stream: ' + localAudioEnabled.checked + ' ' + localVideoEnabled.checked);
  console.log('Updating remote stream: ' + remoteAudioEnabled.checked + ' ' + remoteVideoEnabled.checked);
  if (localStream) {
    localStream.getAudioTracks().forEach(track => track.enabled = localAudioEnabled.checked)
    localStream.getVideoTracks().forEach(track => track.enabled = localVideoEnabled.checked)
  }
  if (remoteVideo.srcObject) {
    remoteVideo.srcObject.getAudioTracks().forEach(track => track.enabled = remoteAudioEnabled.checked)
    remoteVideo.srcObject.getVideoTracks().forEach(track => track.enabled = remoteVideoEnabled.checked)
  }
};

localVideo.addEventListener('loadedmetadata', function() {
  console.log(`Local video videoWidth: ${this.videoWidth}px,  videoHeight: ${this.videoHeight}px`);
});

remoteVideo.addEventListener('loadedmetadata', function() {
  console.log(`Remote video videoWidth: ${this.videoWidth}px,  videoHeight: ${this.videoHeight}px`);
});
