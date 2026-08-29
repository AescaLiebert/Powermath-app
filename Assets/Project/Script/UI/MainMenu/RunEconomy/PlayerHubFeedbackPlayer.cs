using System;
using System.Collections.Generic;
using PowerMath.UI.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu
{
    public sealed class PlayerHubFeedbackPlayer : IDisposable
    {
        private readonly PlayerHubView _view;
        private readonly PlayerHubJuiceProfileDefinition _profile;
        private readonly AudioSource _audio;
        private readonly IUiMotionDriver _motionDriver;
        private readonly List<UiMotionHandle> _tweens =
            new List<UiMotionHandle>();
        private readonly VisualElement[] _particles;
        private readonly StyleScale _authoredAuraScale;
        private readonly StyleFloat _authoredAuraOpacity;
        private readonly StyleTranslate _authoredUpgradeTranslate;
        private UiMotionHandle _idleTween;
        private int _milestoneRevision;

        public PlayerHubFeedbackPlayer(
            PlayerHubView view,
            PlayerHubJuiceProfileDefinition profile,
            AudioSource audio,
            IUiMotionDriver motionDriver)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _profile = profile ?? ScriptableObject.CreateInstance<
                PlayerHubJuiceProfileDefinition>();
            _audio = audio;
            _motionDriver = motionDriver ??
                throw new ArgumentNullException(nameof(motionDriver));
            _authoredAuraScale = _view.WeaponAura.style.scale;
            _authoredAuraOpacity = _view.WeaponAura.style.opacity;
            _authoredUpgradeTranslate = _view.UpgradeButton.style.translate;
            _particles = new VisualElement[_profile.ParticleCount];
            for (int index = 0; index < _particles.Length; index++)
            {
                var particle = new VisualElement { pickingMode = PickingMode.Ignore };
                particle.AddToClassList("player-hub-particle");
                _view.ParticleLayer.Add(particle);
                _particles[index] = particle;
            }
        }

        public void Dispose()
        {
            StopIdle();
            foreach (UiMotionHandle tween in _tweens)
                tween?.Cancel();
            _tweens.Clear();
            ResetVisuals();
        }

        public void StartIdle()
        {
            StopIdle();
            if (_motionDriver.ReducedMotion) return;
            _idleTween = _motionDriver.Tween(
                _view.WeaponAura,
                UiMotionChannel.Ambient,
                _profile.IdleSeconds,
                UiMotionEasing.InOutSine,
                value =>
                {
                    float scale = Mathf.Lerp(0.9f, 1.04f, value);
                    _view.WeaponAura.style.scale = new Scale(
                        new Vector3(scale, scale, 1f));
                    _view.WeaponAura.style.opacity = Mathf.Lerp(0.35f, 0.8f, value);
                },
                loopPingPong: true);
        }

        public void StopIdle()
        {
            _idleTween?.Cancel();
            _idleTween = null;
            _view.WeaponAura.style.scale = _authoredAuraScale;
            _view.WeaponAura.style.opacity = _authoredAuraOpacity;
        }

        public void PlayWeaponPress()
        {
            Pulse(_view.WeaponIcon, 1f, 0.88f, _profile.PressSeconds);
        }

        public void PlayInsufficient()
        {
            PlayClip(_profile.FailureClip);
            if (_motionDriver.ReducedMotion)
            {
                Pulse(_view.NextCard, 1f, 1.04f, _profile.FailureSeconds);
                return;
            }
            Tween(_view.UpgradeButton, _profile.FailureSeconds, value =>
            {
                float x = Mathf.Sin(value * Mathf.PI * 6f) * (1f - value) * 10f;
                _view.UpgradeButton.style.translate = new Translate(x, 0f);
            }, () => _view.UpgradeButton.style.translate =
                _authoredUpgradeTranslate);
        }

        public void PlayWeaponSuccess(bool milestone, string milestoneName)
        {
            PlayClip(milestone ? _profile.MilestoneClip : _profile.UpgradeSuccessClip);
            Pulse(_view.WeaponIcon, 0.84f, milestone ? 1.32f : 1.2f,
                _profile.SuccessSeconds);
            Pulse(_view.CurrentCard, 0.96f, 1.05f, _profile.SuccessSeconds);
            Pulse(_view.NextCard, 0.96f, 1.08f, _profile.SuccessSeconds);
            Burst(milestone ? 1.5f : 1f);
            if (milestone) ShowMilestone(milestoneName);
        }

        public void PlayPetPressed()
        {
            Pulse(_view.PetPreviewIcon, 1f, 0.9f, _profile.PressSeconds);
        }

        public void PlayPetSuccess()
        {
            PlayClip(_profile.PetEquipClip);
            Pulse(_view.PetPreviewIcon, 0.88f, 1.16f, _profile.SuccessSeconds);
            Pulse(_view.EquippedPet, 0.76f, 1.2f, _profile.SuccessSeconds);
        }

        public void PlayPetFailure()
        {
            PlayClip(_profile.FailureClip);
            Pulse(_view.PetPreview, 0.98f, 1.03f, _profile.FailureSeconds);
        }

        private void Pulse(
            VisualElement target,
            float from,
            float peak,
            float duration)
        {
            if (target == null) return;
            float resolvedDuration = _motionDriver.ReducedMotion
                ? 0.12f
                : duration;
            Tween(target, resolvedDuration, value =>
            {
                float scale = value < 0.45f
                    ? Mathf.Lerp(from, peak, value / 0.45f)
                    : Mathf.Lerp(peak, 1f, (value - 0.45f) / 0.55f);
                target.style.scale = new Scale(new Vector3(scale, scale, 1f));
            }, () => target.style.scale = new Scale(Vector3.one));
        }

        private void Burst(float strength)
        {
            if (_motionDriver.ReducedMotion) return;
            for (int index = 0; index < _particles.Length; index++)
            {
                VisualElement particle = _particles[index];
                float angle = Mathf.PI * 2f * index / _particles.Length;
                Vector2 destination = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)) * 92f * strength;
                particle.style.opacity = 1f;
                particle.style.translate = new Translate(0f, 0f);
                Tween(particle, _profile.SuccessSeconds * strength, value =>
                {
                    Vector2 position = Vector2.LerpUnclamped(
                        Vector2.zero,
                        destination,
                        1f - (1f - value) * (1f - value));
                    particle.style.translate = new Translate(position.x, position.y);
                    particle.style.opacity = 1f - value;
                }, () => particle.style.opacity = 0f);
            }
        }

        private void ShowMilestone(string milestoneName)
        {
            int revision = ++_milestoneRevision;
            _view.MilestoneName.text = string.IsNullOrWhiteSpace(milestoneName)
                ? "NEW WEAPON FORM"
                : milestoneName.ToUpperInvariant();
            _view.Milestone.EnableInClassList("is-visible", true);
            _view.Milestone.schedule.Execute(() =>
            {
                if (revision == _milestoneRevision)
                    _view.Milestone.EnableInClassList("is-visible", false);
            }).StartingIn((long)(_profile.MilestoneSeconds * 1000f));
        }

        private void Tween(
            VisualElement target,
            float duration,
            Action<float> update,
            Action complete = null)
        {
            UiMotionHandle tween = null;
            _tweens.RemoveAll(candidate =>
                candidate == null || !candidate.IsActive);
            tween = _motionDriver.Tween(
                target,
                UiMotionChannel.Feedback,
                Mathf.Max(0.01f, duration),
                UiMotionEasing.OutBack,
                update,
                () =>
                {
                    _tweens.Remove(tween);
                    complete?.Invoke();
                });
            if (tween.IsActive)
                _tweens.Add(tween);
        }

        private void ResetVisuals()
        {
            _view.WeaponAura.style.scale = _authoredAuraScale;
            _view.WeaponAura.style.opacity = _authoredAuraOpacity;
            _view.WeaponIcon.style.scale = new Scale(Vector3.one);
            _view.NextCard.style.scale = new Scale(Vector3.one);
            _view.CurrentCard.style.scale = new Scale(Vector3.one);
            _view.UpgradeButton.style.translate = _authoredUpgradeTranslate;
            _view.PetPreviewIcon.style.scale = new Scale(Vector3.one);
            _view.EquippedPet.style.scale = new Scale(Vector3.one);
            _view.PetPreview.style.scale = new Scale(Vector3.one);
            _view.Milestone.EnableInClassList("is-visible", false);
            for (int index = 0; index < _particles.Length; index++)
            {
                _particles[index].style.opacity = 0f;
                _particles[index].style.translate = new Translate(0f, 0f);
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }
    }
}
