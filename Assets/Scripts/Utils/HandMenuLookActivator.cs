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

        // Vector from hand to eye
        Vector3 handToEye = (centerEyeAnchor.position - handTransform.position).normalized;

        // Hand's forward direction (assuming Y axis points toward the palm/finger)
        Vector3 handForward = -handTransform.up; // Use .forward if Y is not correct

        // Calculate angle between hand forward and handToEye vector
        float angle = Vector3.Angle(handForward, handToEye);

        // Debug: Show angle in console
        // Debug.Log("Angle: " + angle);

        // Activate menu if angle is small enough (you're looking at your hand)
        handMenu.SetActive(angle <= activationAngle);
    }
}
