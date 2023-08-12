using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 5;
    [SerializeField] private float time = 1f;
    float timer;
    Vector3 pos;
    [SerializeField] private LayerMask walls, leader, follower;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = transform.position + transform.forward * speed * Time.deltaTime;

        timer = timer + 1 * Time.deltaTime;
        if (timer > time)
        {
            Destroy(this.gameObject);
        }
    }
    public void Move(Vector3 pos, Vector3 dir)
    {
        transform.position = pos;
        transform.forward = dir;
    }
    private void OnCollisionEnter(Collision other) 
    {
        if(other.gameObject.layer == walls)
            Destroy(gameObject);
        if(other.gameObject.layer == leader || other.gameObject.layer == follower)
            Destroy(gameObject);

    }
}