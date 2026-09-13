mergeInto(LibraryManager.library, {
  PowerMathYouTubeShow: function (receiverPtr, videoIdPtr, generation) {
    var receiver = UTF8ToString(receiverPtr);
    var videoId = UTF8ToString(videoIdPtr);
    var state = window.PowerMathYouTubeState || (window.PowerMathYouTubeState = {});
    if (state.startupTimer) window.clearTimeout(state.startupTimer);
    state.startupTimer = null;
    // A player belongs to one question. Late iframe events must never inherit
    // another question's receiver or generation.
    var request = { receiver: receiver, generation: generation, completed: false };
    state.request = request;
    if (state.player && state.player.destroy) state.player.destroy();
    state.player = null;
    state.iframe = null;
    state.receiver = receiver;
    state.generation = generation;
    state.answerMode = false;
    state.awaitingGesture = false;

    var resumeUnityAudio = function () {
      try {
        var contexts = [];
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) contexts.push(WEBAudio.audioContext);
        if (typeof Module !== 'undefined') {
          if (Module.audioContext) contexts.push(Module.audioContext);
          if (Module.WEBAudio && Module.WEBAudio.audioContext) contexts.push(Module.WEBAudio.audioContext);
        }
        if (typeof AL !== 'undefined' && AL.currentCtx && AL.currentCtx.audioCtx) contexts.push(AL.currentCtx.audioCtx);
        if (window.unityAudioContext) contexts.push(window.unityAudioContext);

        for (var i = 0; i < contexts.length; i++) {
          var ctx = contexts[i];
          if (ctx && ctx.state !== 'running' && typeof ctx.resume === 'function') {
            ctx.resume();
          }
        }
      } catch (e) {}
    };
    state.resumeUnityAudio = resumeUnityAudio;

    if (!window.__powerMathAudioGestureInstalled) {
      window.__powerMathAudioGestureInstalled = true;
      var gestureEvents = ['pointerdown', 'touchstart', 'mousedown', 'keydown', 'click'];
      var onAudioGesture = function () {
        resumeUnityAudio();
      };
      for (var g = 0; g < gestureEvents.length; g++) {
        if (window.addEventListener) {
          window.addEventListener(gestureEvents[g], onAudioGesture, { capture: true, passive: true });
        }
        if (typeof document !== 'undefined' && document && document.addEventListener) {
          document.addEventListener(gestureEvents[g], onAudioGesture, { capture: true, passive: true });
        }
      }
    }

    var focusUnityCanvas = function () {
      var canvas = (typeof Module !== 'undefined' && Module.canvas) ||
        document.getElementById('unity-canvas');
      if (canvas && canvas.focus) {
        try {
          canvas.focus({ preventScroll: true });
        } catch (error) {
          canvas.focus();
        }
      }
      resumeUnityAudio();
    };

    var sendToUnity = function (targetRequest, method, payload) {
      if (typeof SendMessage !== 'function') return false;
      if (typeof Module !== 'undefined' && (Module.ABORT || Module.runtimeExited)) return false;
      try {
        SendMessage(targetRequest.receiver, method, payload);
        return true;
      } catch (error) {
        if (typeof console !== 'undefined' && console.warn) {
          console.warn('PowerMath ignored a late YouTube callback.', error);
        }
        return false;
      }
    };

    var completeWithError = function (reason) {
      if (state.request !== request || request.completed) return;
      request.completed = true;
      if (state.startupTimer) window.clearTimeout(state.startupTimer);
      state.startupTimer = null;
      var failedOverlay = document.getElementById('powermath-youtube-overlay');
      if (failedOverlay) {
        failedOverlay.style.display = 'none';
        failedOverlay.style.pointerEvents = 'none';
      }
      focusUnityCanvas();
      sendToUnity(request, 'OnYouTubeError', String(request.generation) + '|' + reason);
    };

    var overlay = document.getElementById('powermath-youtube-overlay');
    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'powermath-youtube-overlay';
      overlay.style.position = 'fixed';
      overlay.style.zIndex = '2147483646';
      overlay.style.background = '#000';
      overlay.style.overflow = 'hidden';
      overlay.style.display = 'none';
      overlay.style.alignItems = 'center';
      overlay.style.justifyContent = 'center';
      overlay.style.pointerEvents = 'auto';
      overlay.style.userSelect = 'none';
      overlay.style.webkitUserSelect = 'none';
      overlay.style.webkitTapHighlightColor = 'transparent';
      overlay.style.outline = 'none';
      overlay.onselectstart = function () { return false; };
      overlay.ondragstart = function () { return false; };
      var overlayHost = document.getElementById('unity-container') || document.body;
      overlayHost.appendChild(overlay);
    } else {
      var currentHost = document.getElementById('unity-container') || document.body;
      if (overlay.parentNode !== currentHost) currentHost.appendChild(overlay);
    }
    overlay.textContent = '';

    // Enforce 720p (1280x720) viewport so YouTube's adaptive bitrate (ABR)
    // streams in 720p instead of 1080p/1440p, preventing browser tab memory spikes.
    var scaler = document.createElement('div');
    scaler.id = 'powermath-youtube-scaler';
    scaler.style.width = '1280px';
    scaler.style.height = '720px';
    scaler.style.flexShrink = '0';
    scaler.style.position = 'relative';
    scaler.style.display = 'flex';
    scaler.style.alignItems = 'center';
    scaler.style.justifyContent = 'center';
    scaler.style.transformOrigin = 'center center';
    scaler.style.userSelect = 'none';
    scaler.style.webkitUserSelect = 'none';
    scaler.style.webkitTapHighlightColor = 'transparent';
    scaler.style.outline = 'none';
    overlay.appendChild(scaler);

    var mount = document.createElement('div');
    mount.id = 'powermath-youtube-player';
    mount.style.width = '100%';
    mount.style.height = '100%';
    mount.style.userSelect = 'none';
    mount.style.webkitUserSelect = 'none';
    mount.style.webkitTapHighlightColor = 'transparent';
    mount.style.outline = 'none';
    scaler.appendChild(mount);

    var align = function () {
      if (!Module.canvas || overlay.style.display === 'none') return;
      var rect = Module.canvas.getBoundingClientRect();
      var canvasWidth = Math.max(0, rect.width);
      var canvasHeight = Math.max(0, rect.height);
      overlay.style.left = rect.left + 'px';
      overlay.style.top = rect.top + 'px';
      overlay.style.width = canvasWidth + 'px';
      overlay.style.height = canvasHeight + 'px';

      var scalerEl = document.getElementById('powermath-youtube-scaler');
      if (scalerEl) {
        var baseWidth = 1280;
        var baseHeight = 720;
        var scale = Math.min(canvasWidth / baseWidth, canvasHeight / baseHeight);
        if (scale <= 0 || !isFinite(scale)) scale = 1;
        scalerEl.style.transform = 'scale(' + scale + ')';
      }
    };
    state.align = align;
    window.removeEventListener('resize', state.previousAlign || function () {});
    state.previousAlign = align;
    window.addEventListener('resize', align);
    overlay.style.display = 'flex';
    // The native player must remain tappable until playback starts. Safari on
    // iOS can reject the delayed playVideo() call because it no longer belongs
    // to the original Attack gesture.
    overlay.style.pointerEvents = 'auto';
    overlay.style.opacity = '1';
    align();

    var setPlayerInteraction = function (enabled) {
      overlay.style.pointerEvents = enabled ? 'auto' : 'none';
      if (state.iframe) state.iframe.style.pointerEvents = enabled ? 'auto' : 'none';
    };

    var createOrLoad = function () {
      if (state.request !== request) return;
      state.player = new YT.Player('powermath-youtube-player', {
        width: '100%',
        height: '100%',
        videoId: videoId,
        playerVars: {
          autoplay: 1, controls: 1, disablekb: 1, fs: 0,
          rel: 0, playsinline: 1, mute: 0, origin: window.location.origin,
          vq: 'hd720'
        },
        events: {
          onReady: function (event) {
            if (state.request !== request || request.completed) return;
            if (event.target.setPlaybackQuality) {
              try {
                event.target.setPlaybackQuality('hd720');
              } catch (e) {}
            }
            var iframe = event.target.getIframe ? event.target.getIframe() : null;
            if (iframe) {
              state.iframe = iframe;
              iframe.tabIndex = -1;
              iframe.draggable = false;
              iframe.style.border = '0';
              iframe.style.outline = 'none';
              iframe.style.pointerEvents = 'auto';
              iframe.style.userSelect = 'none';
              iframe.style.webkitUserSelect = 'none';
              iframe.style.webkitTapHighlightColor = 'transparent';
              if (iframe.setAttribute) {
                iframe.setAttribute('allow', 'autoplay; encrypted-media; picture-in-picture');
                iframe.setAttribute('referrerpolicy', 'strict-origin-when-cross-origin');
              }
            }
            if (state.startupTimer) window.clearTimeout(state.startupTimer);
            state.startupTimer = window.setTimeout(function () {
              completeWithError('start-timeout');
            }, 12000);
            if (event.target.unMute) event.target.unMute();
            if (event.target.setVolume) event.target.setVolume(100);
            event.target.playVideo();
          },
          onStateChange: function (event) {
            if (state.request !== request || request.completed) return;
            if (event.data === YT.PlayerState.PLAYING && state.startupTimer) {
              window.clearTimeout(state.startupTimer);
              state.startupTimer = null;
            }
            if (event.data === YT.PlayerState.PLAYING) {
              try {
                if (event.target.isMuted && event.target.isMuted()) {
                  event.target.unMute();
                }
                if (event.target.getVolume && event.target.getVolume() < 100) {
                  event.target.setVolume(100);
                }
              } catch (e) {}
              state.awaitingGesture = false;
              setPlayerInteraction(false);
              focusUnityCanvas();
            }
            if (event.data === YT.PlayerState.ENDED) {
              request.completed = true;
              if (state.startupTimer) window.clearTimeout(state.startupTimer);
              state.startupTimer = null;
              overlay.style.display = 'none';
              overlay.style.pointerEvents = 'none';
              try {
                if (event.target.pauseVideo) event.target.pauseVideo();
              } catch (e) {}
              focusUnityCanvas();
              sendToUnity(request, 'OnYouTubeEnded', String(request.generation));
            }
            if (typeof YT.PlayerState.PAUSED !== 'undefined' &&
                event.data === YT.PlayerState.PAUSED) {
              if (state.startupTimer) window.clearTimeout(state.startupTimer);
              state.startupTimer = window.setTimeout(function () {
                completeWithError('playback-stalled');
              }, 12000);
              var pausedPlayer = event.target || state.player;
              if (pausedPlayer && pausedPlayer.playVideo) pausedPlayer.playVideo();
            }
          },
          onError: function (event) {
            completeWithError(event.data);
          },
          onAutoplayBlocked: function () {
            if (state.request !== request || request.completed) return;
            if (state.startupTimer) window.clearTimeout(state.startupTimer);
            state.startupTimer = null;
            state.awaitingGesture = true;
            setPlayerInteraction(true);
            state.startupTimer = window.setTimeout(function () {
              completeWithError('gesture-timeout');
            }, 30000);
          }
        }
      });
    };

    if (window.YT && window.YT.Player) {
      createOrLoad();
    } else {
      state.readyCallbacks = state.readyCallbacks || [];
      // Only the latest request may mount after the shared API finishes loading.
      state.readyCallbacks = [createOrLoad];
      if (!state.apiRequested) {
        state.apiRequested = true;
        var previousReady = window.onYouTubeIframeAPIReady;
        window.onYouTubeIframeAPIReady = function () {
          if (previousReady) previousReady();
          var callbacks = state.readyCallbacks.splice(0);
          callbacks.forEach(function (callback) { callback(); });
        };
        var tag = document.createElement('script');
        tag.src = 'https://www.youtube.com/iframe_api';
        tag.onerror = function () {
          state.apiRequested = false;
          state.readyCallbacks = [];
          var active = state.request;
          if (!active || active.completed) return;
          active.completed = true;
          var failedOverlay = document.getElementById('powermath-youtube-overlay');
          if (failedOverlay) {
            failedOverlay.style.display = 'none';
            failedOverlay.style.pointerEvents = 'none';
          }
          focusUnityCanvas();
          sendToUnity(active, 'OnYouTubeError', String(active.generation) + '|api-load');
        };
        document.head.appendChild(tag);
      }
    }
  },

  PowerMathYouTubeSetAnswerMode: function () {
    var state = window.PowerMathYouTubeState;
    var overlay = document.getElementById('powermath-youtube-overlay');
    if (!state || !state.request || !overlay) return;
    state.answerMode = true;
    overlay.style.pointerEvents = 'none';
    // Unity owns the centered answer and result states. Keeping the cross-origin
    // iframe visible would place it above that UI and would violate YouTube's
    // no-overlay requirement.
    overlay.style.display = 'none';
    if (state.player) {
      try {
        if (state.player.pauseVideo) state.player.pauseVideo();
        if (state.player.stopVideo) state.player.stopVideo();
      } catch (e) {}
    }
    if (state.resumeUnityAudio) {
      state.resumeUnityAudio();
    }
  },

  PowerMathYouTubeHide: function () {
    var state = window.PowerMathYouTubeState;
    var overlay = document.getElementById('powermath-youtube-overlay');
    if (state) {
      if (state.player) {
        try {
          if (state.player.pauseVideo) state.player.pauseVideo();
          if (state.player.stopVideo) state.player.stopVideo();
        } catch (e) {}
      }
      if (state.resumeUnityAudio) {
        state.resumeUnityAudio();
      }
      state.request = null;
      state.readyCallbacks = [];
      if (state.player && state.player.destroy) {
        try {
          state.player.destroy();
        } catch (e) {}
      }
      state.player = null;
      state.iframe = null;
      state.answerMode = false;
      state.awaitingGesture = false;
      if (state.startupTimer) window.clearTimeout(state.startupTimer);
      state.startupTimer = null;
      if (state.previousAlign) window.removeEventListener('resize', state.previousAlign);
      state.previousAlign = null;
    }
    if (overlay) {
      overlay.style.display = 'none';
      overlay.style.pointerEvents = 'none';
      overlay.style.opacity = '1';
    }
  }
});
