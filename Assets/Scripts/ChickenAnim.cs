using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickenAnim : MonoBehaviour
{
    public Animator animator;
    public AnimationClip jumpClip;
    void Start()
    {
        animator = FindObjectOfType<Animator>();
    }

    public void Idle()
    {
        animator.SetBool("Idle", true);
        animator.SetBool("Jump", false);
        animator.SetBool("Died", false);
    }
    public void Jump()
    {
        animator.SetBool("Jump", true);
        animator.SetBool("Idle", false);
    }
    public void Died()
    {
        animator.SetBool("Died", true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
