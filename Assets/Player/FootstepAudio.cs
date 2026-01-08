using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    public AudioClip[] footstepClips;
    public AudioSource footstepSource;
    public float walkStepRate = 0.5f;
    public float runStepRate = 0.3f;

    private PlayerController playerMove;
    private float stepTimer;
    private int lastClipIndex = -1;

    void Start()
    {
        playerMove = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (playerMove == null || footstepSource == null || footstepClips.Length == 0)
            return;

        if (playerMove.IsGrounded && playerMove.IsMoving)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = Input.GetKey(KeyCode.LeftShift) ? runStepRate : walkStepRate;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        int index;
        do
        {
            index = Random.Range(0, footstepClips.Length);
        } while (index == lastClipIndex && footstepClips.Length > 1);

        lastClipIndex = index;
        footstepSource.clip = footstepClips[index];
        footstepSource.Play();
    }
}
