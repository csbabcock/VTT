using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// Handles audio playback (Single Responsibility)
    /// </summary>
    public class AudioHandler : IAudioHandler
    {
        private readonly CharacterController _controller;
        private readonly AudioClip _landingClip;
        private readonly AudioClip[] _footstepClips;
        private readonly float _volume;

        public AudioHandler(
            CharacterController controller,
            AudioClip landingClip,
            AudioClip[] footstepClips,
            float volume)
        {
            _controller = controller;
            _landingClip = landingClip;
            _footstepClips = footstepClips;
            _volume = volume;
        }

        public void PlayFootstep()
        {
            if (_footstepClips != null && _footstepClips.Length > 0)
            {
                var index = Random.Range(0, _footstepClips.Length);
                AudioSource.PlayClipAtPoint(
                    _footstepClips[index],
                    _controller.transform.TransformPoint(_controller.center),
                    _volume
                );
            }
        }

        public void PlayLanding()
        {
            if (_landingClip != null)
            {
                AudioSource.PlayClipAtPoint(
                    _landingClip,
                    _controller.transform.TransformPoint(_controller.center),
                    _volume
                );
            }
        }
    }
}

