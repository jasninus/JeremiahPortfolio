using UnityEngine;
using UnityEngine.Networking;

public class BaseClient : NetworkBehaviour
{
    protected BaseHost host;

    [SerializeField] protected PlayerSceneInitializer owner;

    public BaseHost Host
    {
        set => host = value;
    }

    public bool IsLocalNotServer => !owner.isServer && owner.isLocalPlayer;

    private void Start()
    {
        if (!owner.isLocalPlayer)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }
    }

    [Command]
    protected virtual void CmdSendDebug(string message)
    {
        host.RpcReceiveDebug(message);
    }

    [Command]
    protected virtual void CmdSendByte(byte num)
    {
        host.RpcReceiveByte(num);
    }

    [Command]
    protected virtual void CmdSendString(string message)
    {
        host.RpcReceiveString(message);
    }

    [Command]
    protected virtual void CmdSendInt(int num)
    {
        host.RpcReceiveInt(num);
    }

    [Command]
    protected virtual void CmdSendFloat(float num)
    {
        host.RpcReceiveFloat(num);
    }

    [Command]
    protected virtual void CmdSendVector3(Vector3 vector)
    {
        host.RpcReceiveVector3(vector);
    }

    [ClientRpc]
    public void RpcReceiveDebug(string message)
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }

        Debug.Log("Client: " + message);
    }

    [ClientRpc]
    public virtual void RpcReceiveInt(int num)
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }

        Debug.Log("Client received int: " + num);
    }

    [ClientRpc]
    public virtual void RpcReceiveVector3(Vector3 vector)
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }

        Debug.Log("Client received Vector3: " + vector);
    }

    [ClientRpc]
    public virtual void RpcReceiveString(string message)
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }

        Debug.Log("Client received string: " + message);
    }

    [ClientRpc]
    public virtual void RpcReceiveByte(byte num)
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }

        Debug.Log("Client received int: " + num);
    }

    [ClientRpc]
    public virtual void RpcReceiveFloat(float num)
    {
        if (!owner.isLocalPlayer)
        {
            return;
        }

        Debug.Log("Client received int: " + num);
    }
}