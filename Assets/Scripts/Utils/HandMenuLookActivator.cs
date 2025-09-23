using UnityEngine;

public class HandMenuLookActivator : MonoBehaviour
{
    [Header("References")]
    public Transform handTransform;         // The VR hand transform
    public Transform centerEyeAnchor;       // The VR camera / head
    public GameObject handMenu;             // The GUI to activate

    private float activationAngle = 70f;     // Max angle to look at hand to activate menu

    void Update()
    {
        if (!handTransform || !centerEyeAnchor || !handMenu)
            return;

        Vector3 handToEye = (centerEyeAnchor.position - handTransform.position).normalized;
        Vector3 handForward = -handTransform.up;
        float angle = Vector3.Angle(handForward, handToEye);
        handMenu.SetActive(angle <= activationAngle);
    }
}
