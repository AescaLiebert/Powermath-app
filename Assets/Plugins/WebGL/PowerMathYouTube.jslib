mergeInto(LibraryManager.library, {
  PowerMathYouTubeShow: function (receiverPtr, videoIdPtr, generation) {
    var receiver = UTF8ToString(receiverPtr);
    var videoId = UTF8ToString(videoIdPtr);
    var state = window.PowerMathYouTubeState || (window.PowerMathYouTubeState = {});
    // A player belongs to one question. Late iframe events must never inherit
    // another question's receiver or generation.
    var request = { receiver: receiver, generation: generation, completed: false };
    state.request = request;
    if (state.player && state.player.destroy) state.player.destroy();
    state.player = null;
    state.receiver = receiver;
    state.generation = generation;
    state.answerMode = false;

    var overlay = document.getElementById('powermath-youtube-overlay');
    if (!overlay) {
      overlay = document.createElement('div');
      overlay.id = 'powermath-youtube-overlay';
      overlay.style.position = 'fixed';
      overlay.style.zIndex = '2147483646';
      overlay.style.background = '#050816';
      overlay.style.display = 'none';
      overlay.style.alignItems = 'center';
      overlay.style.justifyContent = 'center';
      overlay.style.pointerEvents = 'auto';
      overlay.style.transition = 'left 180ms ease, top 180ms ease, width 180ms ease, height 180ms ease, opacity 180ms ease';
      document.body.appendChild(overlay);
    }
    overlay.textContent = '';
    var mount = document.createElement('div');
    mount.id = 'powermath-youtube-player';
    mount.style.width = '100%';
    mount.style.height = '100%';
    overlay.appendChild(mount);

    var align = function () {
      if (!Module.canvas || overlay.style.display === 'none') return;
      var rect = Module.canvas.getBoundingClientRect();
      if (state.answerMode) {
        var narrow = rect.width < 820 || rect.height > rect.width;
        var dockWidth = narrow
          ? Math.max(200, rect.width - 24)
          : Math.max(280, Math.min(760, rect.width * 0.40));
        var dockHeight = Math.min(dockWidth * 9 / 16,
          rect.height * (narrow ? 0.38 : 0.58));
        overlay.style.left = (rect.left + (narrow ? 12 : Math.max(12, rect.width * 0.04))) + 'px';
        overlay.style.top = (rect.top + (narrow ? 12 : Math.max(12, rect.height * 0.12))) + 'px';
        overlay.style.width = dockWidth + 'px';
        overlay.style.height = dockHeight + 'px';
        return;
      }
      var insetX = Math.max(12, rect.width * 0.08);
      var insetY = Math.max(70, rect.height * 0.16);
      overlay.style.left = (rect.left + insetX) + 'px';
      overlay.style.top = (rect.top + insetY) + 'px';
      overlay.style.width = Math.max(0, rect.width - insetX * 2) + 'px';
      overlay.style.height = Math.max(0, rect.height - insetY * 2) + 'px';
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
          onStateChange: function (event) {
            if (state.request !== request || request.completed) return;
            if (event.data === YT.PlayerState.ENDED) {
              request.completed = true;
              SendMessage(request.receiver, 'OnYouTubeEnded', String(request.generation));
            }
          },
          onError: function (event) {
            if (state.request !== request || request.completed) return;
            request.completed = true;
            SendMessage(request.receiver, 'OnYouTubeError', String(request.generation) + '|' + event.data);
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
    overlay.style.opacity = '0.92';
    if (state.align) state.align();
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
