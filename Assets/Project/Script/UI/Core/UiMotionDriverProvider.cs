using UnityEngine;

namespace PowerMath.UI.Core
{
    [DisallowMultipleComponent]
    public sealed class UiMotionDriverProvider : MonoBehaviour
    {
        [Header("Motion")]
        [Tooltip("Shared UI motion starting values. A Resources fallback supports legacy scenes during the pilot.")]
        [SerializeField] private UiMotionProfileDefinition profile;
        [SerializeField] private bool reducedMotion;

        private LeanTweenUiDriver _driver;
        private bool _ownsRuntimeProfile;

        public IUiMotionDriver Driver
        {
            get
            {
                EnsureInitialized();
                return _driver;
            }
        }

        public UiMotionProfileDefinition Profile
        {
            get
            {
                EnsureInitialized();
                return profile;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            EnsureInitialized();
            _driver.SetReducedMotion(value);
        }

        private void OnDestroy()
        {
            _driver?.Dispose();
            _driver = null;
            if (_ownsRuntimeProfile && profile != null)
            {
                Destroy(profile);
            }
            profile = null;
        }

        private void EnsureInitialized()
        {
            if (_driver != null)
            {
                return;
            }

            if (profile == null)
            {
                profile = Resources.Load<UiMotionProfileDefinition>(
                    "UiMotionProfile");
            }
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    UiMotionProfileDefinition>();
                _ownsRuntimeProfile = true;
                Debug.LogWarning(
                    "UiMotionProfile is not authored. Runtime starting values " +
                    "are active for the ADR-014 pilot.",
                    this);
            }

            _driver = new LeanTweenUiDriver(this, reducedMotion);
        }
    }
}
