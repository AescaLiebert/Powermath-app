mergeInto(LibraryManager.library, {
  PowerMathYouTubeShow: function (receiverPtr, videoIdPtr, generation) {
    var receiver = UTF8ToString(receiverPtr);
    var videoId = UTF8ToString(videoIdPtr);
    var state = window.PowerMathYouTubeState || (window.PowerMathYouTubeState = {});
    state.receiver = receiver;
    state.generation = generation;
    state.answerMode = false;

    var canvas = Module.canvas;
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
      var mount = document.createElement('div');
      mount.id = 'powermath-youtube-player';
      mount.style.width = '100%';
      mount.style.height = '100%';
      overlay.appendChild(mount);
      document.body.appendChild(overlay);
    }

    var align = function () {
      if (!Module.canvas || overlay.style.display === 'none') return;
      var rect = Module.canvas.getBoundingClientRect();
      if (state.answerMode) {
        var dockWidth = Math.max(280, Math.min(760, rect.width * 0.40));
        var dockHeight = dockWidth * 9 / 16;
        overlay.style.left = (rect.left + Math.max(12, rect.width * 0.04)) + 'px';
        overlay.style.top = (rect.top + Math.max(12, rect.height * 0.12)) + 'px';
        overlay.style.width = dockWidth + 'px';
        overlay.style.height = Math.min(dockHeight, rect.height * 0.58) + 'px';
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
    overlay.style.pointerEvents = 'auto';
    overlay.style.opacity = '1';
    align();

    var createOrLoad = function () {
      if (state.player && state.player.loadVideoById) {
        state.player.loadVideoById(videoId);
        return;
      }
      state.player = new YT.Player('powermath-youtube-player', {
        width: '100%',
        height: '100%',
        videoId: videoId,
        playerVars: { autoplay: 1, controls: 1, rel: 0, playsinline: 1 },
        events: {
          onStateChange: function (event) {
            if (event.data === YT.PlayerState.ENDED)
              SendMessage(state.receiver, 'OnYouTubeEnded', String(state.generation));
          },
          onError: function (event) {
            SendMessage(state.receiver, 'OnYouTubeError', String(state.generation) + '|' + event.data);
          }
        }
      });
    };

    if (window.YT && window.YT.Player) {
      createOrLoad();
    } else {
      state.readyCallbacks = state.readyCallbacks || [];
      state.readyCallbacks.push(createOrLoad);
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
        document.head.appendChild(tag);
      }
    }
  },

  PowerMathYouTubeSetAnswerMode: function () {
    var state = window.PowerMathYouTubeState;
    var overlay = document.getElementById('powermath-youtube-overlay');
    if (!state || !overlay) return;
    state.answerMode = true;
    overlay.style.pointerEvents = 'none';
    overlay.style.opacity = '0.92';
    if (state.align) state.align();
  },

  PowerMathYouTubeHide: function () {
    var state = window.PowerMathYouTubeState;
    var overlay = document.getElementById('powermath-youtube-overlay');
    if (state && state.player && state.player.stopVideo) state.player.stopVideo();
    if (state) state.answerMode = false;
    if (overlay) {
      overlay.style.display = 'none';
      overlay.style.pointerEvents = 'auto';
      overlay.style.opacity = '1';
    }
  }
});
