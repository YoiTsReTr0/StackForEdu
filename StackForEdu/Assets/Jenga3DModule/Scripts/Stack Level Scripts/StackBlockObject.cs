using UnityEngine;
using TMPro;

public class StackBlockObject : MonoBehaviour
{
    #region Public data containers

    [HideInInspector] public MeshRenderer ObjMeshRenderer;
    public TextMeshProUGUI MasteryLevelText;

    #endregion

    private Rigidbody rBody;

    private void Awake()
    {
        rBody = GetComponent<Rigidbody>();
        ObjMeshRenderer = GetComponent<MeshRenderer>();
    }

    public void TestMyStackVoid()
    {
        rBody.useGravity = true;
        rBody.isKinematic = false;
    }


    public void ResetMyStackVoid()
    {
        rBody.useGravity = false;
        rBody.isKinematic = true;
    }
}