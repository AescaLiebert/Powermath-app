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
    state.receiver = receiver;
    state.generation = generation;
    state.answerMode = false;

    var completeWithError = function (reason) {
      if (state.request !== request || request.completed) return;
      request.completed = true;
      if (state.startupTimer) window.clearTimeout(state.startupTimer);
      state.startupTimer = null;
      SendMessage(request.receiver, 'OnYouTubeError', String(request.generation) + '|' + reason);
    };

    var overlay = document.getElementById('powermath-youtube-overlay');
    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'powermath-youtube-overlay';
      overlay.style.position = 'fixed';
      overlay.style.zIndex = '2147483646';
      overlay.style.background = '#000';
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
      document.body.appendChild(overlay);
    }
    overlay.textContent = '';
    var mount = document.createElement('div');
    mount.id = 'powermath-youtube-player';
    mount.style.width = '100%';
    mount.style.height = '100%';
    mount.style.userSelect = 'none';
    mount.style.webkitUserSelect = 'none';
    mount.style.webkitTapHighlightColor = 'transparent';
    mount.style.outline = 'none';
    overlay.appendChild(mount);

    var align = function () {
      if (!Module.canvas || overlay.style.display === 'none') return;
      var rect = Module.canvas.getBoundingClientRect();
      overlay.style.left = rect.left + 'px';
      overlay.style.top = rect.top + 'px';
      overlay.style.width = Math.max(0, rect.width) + 'px';
      overlay.style.height = Math.max(0, rect.height) + 'px';
    };
    state.align = align;
    window.removeEventListener('resize', state.previousAlign || function () {});
    state.previousAlign = align;
    window.addEventListener('resize', align);
    overlay.style.display = 'flex';
    // The assessment owns playback. Disabling hit testing prevents pause,
    // seeking and provider settings while retaining the actual YouTube iframe.
    overlay.style.pointerEvents = 'none';
    overlay.style.opacity = '1';
    align();

    var createOrLoad = function () {
      if (state.request !== request) return;
      state.player = new YT.Player('powermath-youtube-player', {
        width: '100%',
        height: '100%',
        videoId: videoId,
        playerVars: {
          autoplay: 1, controls: 0, disablekb: 1, fs: 0,
          rel: 0, playsinline: 1, origin: window.location.origin
        },
        events: {
          onReady: function (event) {
            if (state.request !== request || request.completed) return;
            var iframe = event.target.getIframe ? event.target.getIframe() : null;
            if (iframe) {
              iframe.tabIndex = -1;
              iframe.draggable = false;
              iframe.style.border = '0';
              iframe.style.outline = 'none';
              iframe.style.pointerEvents = 'none';
              iframe.style.userSelect = 'none';
              iframe.style.webkitUserSelect = 'none';
              iframe.style.webkitTapHighlightColor = 'transparent';
            }
            if (state.startupTimer) window.clearTimeout(state.startupTimer);
            state.startupTimer = window.setTimeout(function () {
              completeWithError('start-timeout');
            }, 12000);
            event.target.playVideo();
          },
          onStateChange: function (event) {
            if (state.request !== request || request.completed) return;
            if (event.data === YT.PlayerState.PLAYING && state.startupTimer) {
              window.clearTimeout(state.startupTimer);
              state.startupTimer = null;
            }
            if (event.data === YT.PlayerState.ENDED) {
              request.completed = true;
              if (state.startupTimer) window.clearTimeout(state.startupTimer);
              state.startupTimer = null;
              SendMessage(request.receiver, 'OnYouTubeEnded', String(request.generation));
            }
          },
          onError: function (event) {
            completeWithError(event.data);
          },
          onAutoplayBlocked: function () {
            completeWithError('autoplay-blocked');
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
          SendMessage(active.receiver, 'OnYouTubeError', String(active.generation) + '|api-load');
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
  },

  PowerMathYouTubeHide: function () {
    var state = window.PowerMathYouTubeState;
    var overlay = document.getElementById('powermath-youtube-overlay');
    if (state) {
      state.request = null;
      state.readyCallbacks = [];
      if (state.player && state.player.destroy) state.player.destroy();
      state.player = null;
      state.answerMode = false;
      if (state.startupTimer) window.clearTimeout(state.startupTimer);
      state.startupTimer = null;
      if (state.previousAlign) window.removeEventListener('resize', state.previousAlign);
      state.previousAlign = null;
    }
    if (overlay) {
      overlay.style.display = 'none';
      overlay.style.pointerEvents = 'auto';
      overlay.style.opacity = '1';
    }
  }
});
