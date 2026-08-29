mergeInto(LibraryManager.library, {
  PowerMathNotifyUnityReady: function () {
    window.dispatchEvent(new CustomEvent("powermath-unity-ready"));
  },

  PowerMathRequestReauthentication: function () {
    window.dispatchEvent(new CustomEvent("powermath-unity-reauthentication-required"));
  },

  PowerMathHardReload: function () {
    if (typeof window !== "undefined") {
      window.location.reload(true);
    }
  },

  PowerMathPurgeCacheAndReload: function () {
    try {
      // 1. Delete Unity IndexedDB cache if available
      if (typeof window !== "undefined" && window.indexedDB) {
        window.indexedDB.deleteDatabase("UnityCache");
      }

      // 2. Unregister active service workers
      if (typeof navigator !== "undefined" && "serviceWorker" in navigator) {
        navigator.serviceWorker.getRegistrations().then(function (registrations) {
          for (var i = 0; i < registrations.length; i++) {
            registrations[i].unregister();
          }
        });
      }

      // 3. Clear Cache Storage API
      if (typeof window !== "undefined" && window.caches) {
        caches.keys().then(function (names) {
          for (var i = 0; i < names.length; i++) {
            caches.delete(names[i]);
          }
        });
      }
    } catch (e) {
      console.warn("Error purging WebGL cache:", e);
    }

    // 4. Force hard reload
    setTimeout(function () {
      if (typeof window !== "undefined") {
        window.location.reload(true);
      }
    }, 150);
  }
});
