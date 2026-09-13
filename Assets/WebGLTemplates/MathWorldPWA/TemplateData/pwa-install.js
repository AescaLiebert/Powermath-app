(function () {
  "use strict";

  var deferredInstallPrompt = null;
  var promptElement = null;

  function isAndroidBrowser() {
    return /Android/i.test(window.navigator.userAgent || "");
  }

  function isInstalled() {
    var installedDisplayMode = window.matchMedia && (
      window.matchMedia("(display-mode: standalone)").matches ||
      window.matchMedia("(display-mode: fullscreen)").matches ||
      window.matchMedia("(display-mode: minimal-ui)").matches
    );
    return installedDisplayMode || window.navigator.standalone === true;
  }

  function hidePrompt() {
    if (promptElement) promptElement.hidden = true;
  }

  function showPrompt() {
    if (promptElement && deferredInstallPrompt && isAndroidBrowser() && !isInstalled()) {
      promptElement.hidden = false;
    }
  }

  window.addEventListener("DOMContentLoaded", function () {
    promptElement = document.getElementById("pwa-install-prompt");
    var installButton = document.getElementById("pwa-install-button");
    var dismissButton = document.getElementById("pwa-install-dismiss");

    if (dismissButton) dismissButton.addEventListener("click", hidePrompt);
    if (installButton) installButton.addEventListener("click", async function () {
      if (!deferredInstallPrompt) return;
      var installPrompt = deferredInstallPrompt;
      deferredInstallPrompt = null;
      hidePrompt();
      try {
        await installPrompt.prompt();
        await installPrompt.userChoice;
      } catch (error) {
        // Browser policy can withdraw eligibility; keep gameplay unaffected.
        console.warn("PowerMath install prompt was unavailable.", error);
      }
    });

    showPrompt();
  });

  window.addEventListener("beforeinstallprompt", function (event) {
    event.preventDefault();
    deferredInstallPrompt = event;
    showPrompt();
  });

  window.addEventListener("appinstalled", function () {
    deferredInstallPrompt = null;
    hidePrompt();
  });
})();
