using System.Collections;
using System;
using PowerMath.Localization;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PowerMath.PlayerLifecycle
{
    public sealed class PlayerLifecycleRuntime : MonoBehaviour
    {
        private static PlayerLifecycleRuntime _instance;
        public static IPlayerLifecycleCommands Commands { get; private set; }
        private bool _savingLocale;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Commands = null;
            _instance = new GameObject("Player Lifecycle").AddComponent<PlayerLifecycleRuntime>();
            DontDestroyOnLoad(_instance.gameObject);
        }

        public static void Configure(GameApiSettings settings)
        {

            Commands = new ReleaseCheckedLifecycleCommands(settings, new DirectFirestoreLifecycleCommands(settings));
#if UNITY_EDITOR
            if (settings.UseEditorSampleStudent) Commands = new EditorLifecycleCommands();
#endif
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(BindDocuments());
        private IEnumerator BindDocuments()
        {
            yield return null;
            foreach (var document in FindObjectsByType<UIDocument>())
            {
                var root = document.rootVisualElement;
                if (root == null) continue;
                PowerMath.UI.Core.StatusToastOverlay.Attach(root);
                if (document.GetComponent<LocalizedDocument>() != null) continue;
                var binding = document.gameObject.AddComponent<LocalizedDocument>();
                binding.Bind(root);
                var character = document.gameObject.AddComponent<CharacterPresentationBinding>();
                character.Bind(root);
            }
        }

        public static void SetLocaleAndSave(string locale, Label status = null)
        {
            if (_instance != null)
                _instance.StartCoroutine(_instance.ChangeLocale(locale, status));
            else
                LocalizationService.SetLocale(locale);
        }

        private IEnumerator ChangeLocale(string locale, Label status = null)
        {
            if (_savingLocale) yield break;
            LocalizationService.SetLocale(locale);
            PowerMath.Diagnostics.AppLog.Info("Lifecycle", $"Locale set to '{locale}'.");
            var store = PlayerSessionStore.Instance;
            if (store == null || !store.IsReady || Commands == null) yield break;
            _savingLocale = true;
            string playerId = store.Snapshot.playerId;
            var command = new PlayerLifecycleCommand
            {
                kind = PlayerLifecycleCommandKind.SetLocale, value = locale,
                playerId = playerId, expectedRevision = store.Snapshot.revision,
                operationId = Guid.NewGuid().ToString("N")
            };
            string savingText = LocalizationService.Get("common.saving");
            if (status != null) status.text = savingText;
            PowerMath.UI.Core.StatusMessageService.ShowInfo(savingText);
            PlayerSnapshot saved = null;
            yield return Commands.Execute(command, result => saved = result, _ => { });
            if (store.IsReady && store.Snapshot.playerId == playerId)
            {
                if (saved != null && saved.revision >= store.Snapshot.revision)
                {
                    store.TryHydrate(new BootstrapResponse { player = saved, schemaVersion = saved.schemaVersion, remembered = store.IsRemembered });
                    string savedText = LocalizationService.Get("common.saved");
                    if (status != null) status.text = savedText;
                    PowerMath.UI.Core.StatusMessageService.ShowSuccess(savedText);
                }
                else
                {
                    string errorText = LocalizationService.Get("errors.save");
                    if (status != null) status.text = errorText;
                    PowerMath.UI.Core.StatusMessageService.ShowError(errorText);
                }
            }
            _savingLocale = false;
        }

        public static void ApplyAccountLocale(PlayerSnapshot player)
        {
            if (PlayerLifecyclePolicy.IsLocale(player?.preferences?.locale))
                LocalizationService.SetLocale(player.preferences.locale);
        }
    }
}

