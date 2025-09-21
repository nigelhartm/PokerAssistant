using UnityEngine;

// Represents a single object detected via Roboflow object detection.
public class RoboflowObject : MonoBehaviour {
    [SerializeField] private TMPro.TextMeshProUGUI labelText;   // Reference to label
    public int classID = 0;                                     // Roboflow class id

    // Resets this object to its initial state
    public void Init(int classId) {
        gameObject.SetActive(false);
        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;
        classID = classId;
    }
    
    // Sets the debug label text
    public void SetDebugText(string text) {
        labelText.text = text;
    }

    // Call if detected and updated.
    public void SuccesfullyTracked(Vector3 position, Vector3 CameraPosition) {
        gameObject.transform.position = position;
        gameObject.SetActive(true);
    }
}