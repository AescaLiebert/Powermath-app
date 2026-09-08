mergeInto(LibraryManager.library, {
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
