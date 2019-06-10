using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedLoader : MonoBehaviour
{
    [SerializeField] float RotationVel;
    Transform myTransform;
    void Awake()
    {
        myTransform = GetComponent<Transform>();
    }
    void Update()
    {
        myTransform.rotation *= Quaternion.Euler(0,0,Time.deltaTime*RotationVel);
    }
}
