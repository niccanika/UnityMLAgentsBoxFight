using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class movement : MonoBehaviour
{
    private Animator Anim;
    public GameObject Player1;
    void Start()
    {
        Anim= GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) == true)
        {
            Anim.SetBool("left", true);
        }

        if (Input.GetKey(KeyCode.LeftShift) == true)
        {
            Anim.SetBool("right", true);
        }
    }
}
