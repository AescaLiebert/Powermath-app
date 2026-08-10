mergeInto(LibraryManager.library, {
  PowerMathNotifyUnityReady: function () {
    window.dispatchEvent(new CustomEvent("powermath-unity-ready"));
  },

  PowerMathRequestReauthentication: function () {
    window.dispatchEvent(new CustomEvent("powermath-unity-reauthentication-required"));
  }
});
