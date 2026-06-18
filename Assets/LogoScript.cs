using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoScript : MonoBehaviour
{
    private Animator animator;
    private static bool hasPlayedStartAnimation = false;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (!hasPlayedStartAnimation)
        {
            animator.SetTrigger("StartAnimation");
            hasPlayedStartAnimation = true;
            StartCoroutine(PlayStartThenIdle());
        }
        else
        {
            animator.SetTrigger("IdleAnimation");
        }
    }

    private IEnumerator PlayStartThenIdle()
    {
        yield return new WaitForSeconds(GetAnimationLength("LogoStart"));

        animator.SetTrigger("IdleAnimation");
    }

    private float GetAnimationLength(string animationName)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animationName)
                return clip.length;
        }
        return 1f; // Default fallback
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
