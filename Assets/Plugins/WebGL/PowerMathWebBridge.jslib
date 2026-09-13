mergeInto(LibraryManager.library, {
  PowerMathTryExitFullscreen: function () {
    try {
      if (!document.fullscreenElement && !document.webkitFullscreenElement) return 1;
      var exitFullscreen = document.exitFullscreen || document.webkitExitFullscreen;
      var promise = exitFullscreen ? exitFullscreen.call(document) : null;
      if (promise && promise.catch) promise.catch(function () {});
      return 1;
    } catch (error) {
      console.warn("PowerMath fullscreen exit failed", error);
      return 0;
    }
  },
  PowerMathNotifyUnityReady: function () {
    window.dispatchEvent(new CustomEvent("powermath-unity-ready"));
  },

  PowerMathRequestReauthentication: function () {
    window.dispatchEvent(new CustomEvent("powermath-unity-reauthentication-required"));
  },

  PowerMathHardReload: function () {
    if (typeof window !== "undefined") {
      window.location.reload();
    }
  },

  PowerMathTryEnterLandscapeFullscreen: function () {
    if (typeof window === "undefined" || typeof document === "undefined") return 0;

    var displayModeFullscreen = window.matchMedia &&
      window.matchMedia("(display-mode: fullscreen)").matches;
    var displayModeStandalone = window.matchMedia &&
      window.matchMedia("(display-mode: standalone)").matches;
    var iosStandalone = window.navigator && window.navigator.standalone === true;

    if (document.fullscreenElement || document.webkitFullscreenElement ||
        displayModeFullscreen || displayModeStandalone || iosStandalone ||
        window.__powerMathFullscreenPending) {
      return 0;
    }

    var canvas = Module.canvas || document.getElementById("unity-canvas");
    if (!canvas) return 0;

    // The YouTube question layer is hosted by unity-container. Making that
    // container fullscreen keeps browser media in the same fullscreen tree.
    var fullscreenTarget = document.getElementById("unity-container") || canvas;
    var requestFullscreen = fullscreenTarget.requestFullscreen || fullscreenTarget.webkitRequestFullscreen;
    if (!requestFullscreen) return 0;

    window.__powerMathFullscreenPending = true;

    var lockLandscape = function () {
      if (window.screen && screen.orientation && screen.orientation.lock) {
        var lockResult = screen.orientation.lock("landscape");
        if (lockResult && lockResult.catch) lockResult.catch(function () {});
      }
    };

    try {
      var requestResult = requestFullscreen.call(fullscreenTarget);
      if (requestResult && requestResult.then) {
        requestResult.then(lockLandscape).catch(function () {}).then(function () {
          window.__powerMathFullscreenPending = false;
        });
      } else {
        lockLandscape();
        window.__powerMathFullscreenPending = false;
      }
      return 1;
    } catch (error) {
      window.__powerMathFullscreenPending = false;
      return 0;
    }
  },

  PowerMathPurgeCacheAndReload: function (targetVersion) {
    var target = UTF8ToString(targetVersion);
    var key = "powermath.reload." + window.location.pathname;
    try {
      if (sessionStorage.getItem(key) === target) return;
      sessionStorage.setItem(key, target);
    } catch (e) {
      // Without a durable loop guard, leave the update screen for manual reload.
      return;
    }
    // Only application-owned Cache Storage entries may be cleared. PlayerPrefs,
    // Unity's shared IndexedDB cache and unrelated service workers are preserved.
    var cleanup = window.caches ? caches.keys().then(function (names) {
      return Promise.all(names.filter(function (name) {
        return name.indexOf("powermath-build-") === 0;
      }).map(function (name) { return caches.delete(name); }));
    }) : Promise.resolve();
    Promise.race([cleanup.catch(function () {}), new Promise(function (resolve) {
      setTimeout(resolve, 3000);
    })]).then(function () {
      var url = new URL(window.location.href);
      url.searchParams.set("powermathBuild", target);
      window.location.replace(url.toString());
    });
  }
});
