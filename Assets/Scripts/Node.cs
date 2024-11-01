using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    //To determine whether the space can be filled with portion or not
    public bool isUsable;

    public GameObject portion;

    public Node(bool _isUsable, GameObject _portion)
    {
        this.isUsable = _isUsable;
        this.portion = _portion;
    }
}
