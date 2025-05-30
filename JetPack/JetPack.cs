using System.Collections;
using System.Collections.Generic;
using easyInputs;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class JetPack : MonoBehaviourPunCallbacks
{
    public EasyHand TriggerHand;
    public ParticleSystem[] particleSystems;
    public GameObject[] Enable;
    public GameObject[] Disable;
    public bool Test = false;
    public string GorillaPlayer = "GorillaPlayer";
    public Rigidbody MonkeyPlayer;
    public float strength = 10f;
    public Transform headTransform;

    private bool isShooting;

    private void Start()
    {
        if (MonkeyPlayer == null)
        {
            GameObject gplayer = GameObject.Find(GorillaPlayer);
            if (gplayer != null)
            {
                MonkeyPlayer = gplayer.GetComponent<Rigidbody>();
            }
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        bool triggerHeld = Test ? Input.GetMouseButton(0) : EasyInputs.GetTriggerButtonDown(TriggerHand);

        if (triggerHeld && !isShooting)
        {
            photonView.RPC(nameof(StartShooting), RpcTarget.AllBuffered);
        }
        else if (!triggerHeld && isShooting)
        {
            photonView.RPC(nameof(StopShooting), RpcTarget.AllBuffered);
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine || !isShooting || MonkeyPlayer == null || headTransform == null) return;
        Vector3 flightDirection = headTransform.up * 0.7f + headTransform.forward * 0.3f;
        MonkeyPlayer.AddForce(flightDirection.normalized * strength * 0.5f, ForceMode.VelocityChange);
    }


    [PunRPC]
    public void StartShooting()
    {
        isShooting = true;
        SetGameObjects(Enable, true);
        SetGameObjects(Disable, false);

        foreach (var ps in particleSystems)
        {
            if (ps != null && !ps.isPlaying)
                ps.Play();
        }
    }

    [PunRPC]
    public void StopShooting()
    {
        isShooting = false;
        SetGameObjects(Enable, false);
        SetGameObjects(Disable, true);

        foreach (var ps in particleSystems)
        {
            if (ps != null && ps.isPlaying)
                ps.Stop();
        }
    }

    private void SetGameObjects(GameObject[] objects, bool state)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}
