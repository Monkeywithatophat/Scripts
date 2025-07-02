using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sex : MonoBehaviour
{
    public float FuckTaps;
    public GameObject Cum;
    public GameObject Wet;
    public AudioClip CumSound;
    public AudioSource audioSource;

    void Update()
    {
        if (FuckTaps > 20)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PhotonView.Get(this).RPC("NetworkMySperm", RpcTarget.All);
            }
            else
            {
                PhotonView.Get(this).RPC("NetworkMySpermDone", RpcTarget.All);
            }
        }

        if (FuckTaps > 10)
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                PhotonView.Get(this).RPC("NetworkHerSperm", RpcTarget.All);
            }
            else
            {
                PhotonView.Get(this).RPC("NetworkHerSpermDone", RpcTarget.All);
            }
        }

        if (FuckTaps > 30)
        {
            FuckTaps = 0;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Penis"))
        {
            FuckTaps += 1;
        }
    }

    [PunRPC]
    public void NetworkMySperm()
    {
        Cum.SetActive(true);
        audioSource.PlayOneShot(CumSound);
    }

    [PunRPC]
    public void NetworkMySpermDone()
    {
        Cum.SetActive(false);
    }

    [PunRPC]
    public void NetworkHerSperm()
    {
        Wet.SetActive(true);
    }

    [PunRPC]
    public void NetworkHerSpermDone()
    {
        Wet.SetActive(false);
    }
}
