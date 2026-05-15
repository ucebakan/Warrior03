using UnityEngine;

public class PlayerAttackSFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Attack Clips")]
    [SerializeField] private AudioClip attack1Clip;
    [SerializeField] private AudioClip attack2Clip;
    [SerializeField] private AudioClip attack3Clip;

    [Header("Random Attack Clips")]
    [SerializeField] private AudioClip[] randomAttackClips;

    [Header("Audio Settings")]
    [SerializeField][Range(0f, 1f)] private float volume = 0.85f;
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [Header("Spam Protection")]
    [SerializeField] private bool useCooldown = true;
    [SerializeField] private float minTimeBetweenSounds = 0.05f;

    private float lastPlayTime;
    private float defaultPitch = 1f;

    private void Awake()
    {
        SetupAudioSource();
    }

    private void Reset()
    {
        SetupAudioSource();
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        minPitch = Mathf.Max(0.1f, minPitch);
        maxPitch = Mathf.Max(minPitch, maxPitch);
        minTimeBetweenSounds = Mathf.Max(0f, minTimeBetweenSounds);
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        defaultPitch = audioSource.pitch;
    }

    public void PlayAttack1SFX()
    {
        PlayClip(attack1Clip);
    }

    public void PlayAttack2SFX()
    {
        PlayClip(attack2Clip);
    }

    public void PlayAttack3SFX()
    {
        PlayClip(attack3Clip);
    }

    public void PlayAttack1Sound()
    {
        PlayAttack1SFX();
    }

    public void PlayAttack2Sound()
    {
        PlayAttack2SFX();
    }

    public void PlayAttack3Sound()
    {
        PlayAttack3SFX();
    }

    public void Attack1SFX()
    {
        PlayAttack1SFX();
    }

    public void Attack2SFX()
    {
        PlayAttack2SFX();
    }

    public void Attack3SFX()
    {
        PlayAttack3SFX();
    }

    public void PlayAttackSFX()
    {
        PlayRandomAttackSFX();
    }

    public void PlayAttackSfx()
    {
        PlayRandomAttackSFX();
    }

    public void PlayAttackSound()
    {
        PlayRandomAttackSFX();
    }

    public void AttackSFX()
    {
        PlayRandomAttackSFX();
    }

    public void PlaySwingSFX()
    {
        PlayRandomAttackSFX();
    }

    public void PlaySwingSound()
    {
        PlayRandomAttackSFX();
    }

    public void PlayWeaponSwing()
    {
        PlayRandomAttackSFX();
    }

    public void PlayWeaponSwingSFX()
    {
        PlayRandomAttackSFX();
    }

    public void PlayRandomAttackSFX()
    {
        AudioClip selectedClip = GetRandomAttackClip();

        if (selectedClip != null)
        {
            PlayClip(selectedClip);
            return;
        }

        if (attack1Clip != null)
        {
            PlayClip(attack1Clip);
        }
    }

    private AudioClip GetRandomAttackClip()
    {
        if (randomAttackClips == null || randomAttackClips.Length == 0)
        {
            return null;
        }

        int validClipCount = 0;

        for (int i = 0; i < randomAttackClips.Length; i++)
        {
            if (randomAttackClips[i] != null)
            {
                validClipCount++;
            }
        }

        if (validClipCount <= 0)
        {
            return null;
        }

        int randomValidIndex = Random.Range(0, validClipCount);
        int currentValidIndex = 0;

        for (int i = 0; i < randomAttackClips.Length; i++)
        {
            if (randomAttackClips[i] == null)
            {
                continue;
            }

            if (currentValidIndex == randomValidIndex)
            {
                return randomAttackClips[i];
            }

            currentValidIndex++;
        }

        return null;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            SetupAudioSource();
        }

        if (audioSource == null)
        {
            Debug.LogWarning("PlayerAttackSFX could not find or create an AudioSource.", this);
            return;
        }

        if (useCooldown && Time.time - lastPlayTime < minTimeBetweenSounds)
        {
            return;
        }

        lastPlayTime = Time.time;

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }
        else
        {
            audioSource.pitch = defaultPitch;
        }

        audioSource.PlayOneShot(clip, volume);
    }

    [ContextMenu("Test Attack 1 SFX")]
    private void DebugTestAttack1SFX()
    {
        PlayAttack1SFX();
    }

    [ContextMenu("Test Attack 2 SFX")]
    private void DebugTestAttack2SFX()
    {
        PlayAttack2SFX();
    }

    [ContextMenu("Test Attack 3 SFX")]
    private void DebugTestAttack3SFX()
    {
        PlayAttack3SFX();
    }

    [ContextMenu("Test Random Attack SFX")]
    private void DebugTestRandomAttackSFX()
    {
        PlayRandomAttackSFX();
    }
}