using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public abstract class Item : NetworkBehaviour
{
    public abstract void Use();
}
