using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using Meta.XR;
using System.Linq;
using UnityEngine.Networking;
using PokerOddsJson;

// Handles webcam streaming, sending frames to Roboflow, receiving detections and rendering tracked objects
public class RoboflowCaller : MonoBehaviour {
    [Header("Scene References")]
    [SerializeField] private WebCamTextureManager webCamTextureManager;     // Scene Reference
    [SerializeField] private EnvironmentRaycastManager envRaycastManager;   // Scene Reference
    [SerializeField] private GameObject centerEyeAnchor;                    // Scene Reference
    [SerializeField] private Transform leftHandAnchor;                      // Scene Reference
    [SerializeField] private GameObject markerPrefab;                       // Asset Reference

    // Internals
    private Texture2D texture2D = null; // Buffer Texture
    private List<string> rfClassNames = new List<string> {"10C", "10D", "10H", "10S","2C", "2D", "2H", "2S","3C", "3D", "3H", "3S","4C", "4D", "4H", "4S", "5C", "5D", "5H", "5S", "6C", "6D", "6H", "6S", "7C", "7D", "7H", "7S", "8C", "8D", "8H", "8S", "9C", "9D", "9H", "9S", "AC", "AD", "AH", "AS", "JC", "JD", "JH", "JS", "KC", "KD", "KH", "KS", "QC", "QD", "QH", "QS"}; // Roboflow Class Names
    private List<string> thisTimeInHand = new List<string>();           // Tracks cards currently in hand
    private String[] HandCards = new String[2];                         // Cards in hand 
    private List<string> FieldCards = new List<string>();               // Cards on the field
    private Dictionary<int, RoboflowObject> _activeMarkerMap = new();   // Runtime pool

    private const int targetWidth = 1280;   // Target width for resized images
    private const int targetHeight = 960;   // Target height for resized images

    [Header("Roboflow API Configuration")]
    [SerializeField] private string RF_MODEL = "playing-cards-ow27d/4";                             // Model name for Roboflow
    [SerializeField] private string LOCAL_SERVER_IP_ADDRESS = "http://192.168.0.220:9001";          // Local server URL for Roboflow
    [SerializeField] private string LOCAL_SERVER_POKER_IP_ADDRESS = "http://192.168.0.220:3000";    // Local server URL for Poker-Odds
    [SerializeField] private float minConfidence = 0.65f; // Detection confidence threshold
    private RoboflowInferenceClient client; // API client

    [Header("Card Suit")]
    [SerializeField] private Texture2D clubTexture;     // Asset Reference
    [SerializeField] private Texture2D diamondTexture;  // Asset Reference
    [SerializeField] private Texture2D heartTexture;    // Asset Reference
    [SerializeField] private Texture2D spadeTexture;    // Asset Reference

    [Header("Icons")]
    [SerializeField] private Texture2D kingIcon;        // Asset Reference
    [SerializeField] private Texture2D queenIcon;       // Asset Reference
    [SerializeField] private Texture2D jackIcon;        // Asset Reference
    [SerializeField] private Texture2D emptyIcon;       // Asset Reference

    [Header("Hand UI")]
    [SerializeField] private RawImage[] cardNumber;                         // Scene Reference (2)
    [SerializeField] private RawImage[] cardSuit;                           // Scene Reference (2)
    [SerializeField] private TMPro.TextMeshProUGUI[] cardText;                // Scene Reference (2)
    [SerializeField] private Slider handStrengthSlider;                     // Scene Reference
    [SerializeField] private TMPro.TextMeshProUGUI handStrengthSliderText;  // Scene Reference

    private void Start() {
        client = new RoboflowInferenceClient(APIKeys.RF_API_KEY, LOCAL_SERVER_IP_ADDRESS);
        BuildObjectPool();
        StartCoroutine(initCoroutine());
    }

    // Sets up the object pool for Roboflow objects.
    private void BuildObjectPool() {
        // Build marker pool dynamically
        for (var i = 0; i < rfClassNames.Count; i++) {
            var instance = Instantiate(markerPrefab, Vector3.zero, Quaternion.identity);
            var rfObject = instance.GetComponent<RoboflowObject>();
            rfObject.Init(i); // Initialize with class name and ID
            _activeMarkerMap[rfObject.classID] = rfObject;
            // Assign suit texture to child RawImage
            var rawImage = instance.GetComponentInChildren<RawImage>();
            if (rawImage != null) {
                rawImage.texture = GetSuitTexture(rfClassNames[i]);
            }
        }
    }

    // Helper to get the texture based on the suit in the card name
    private Texture2D GetSuitTexture(string cardName) {
        if (string.IsNullOrEmpty(cardName)) return null;
        char suitChar = cardName[cardName.Length - 1]; // last char: C,D,H,S
        switch (char.ToUpper(suitChar)) {
            case 'C': return clubTexture;
            case 'D': return diamondTexture;
            case 'H': return heartTexture;
            case 'S': return spadeTexture;
            default: return null;
        }
    }

    private void updateTexture2D() {
        texture2D.SetPixels(webCamTextureManager.WebCamTexture.GetPixels());
        texture2D.Apply();
    }

    // Initializes the webcam texture and sets it to the image display.
    private IEnumerator initCoroutine() {
        // Wait for webcam to be ready
        while (webCamTextureManager.WebCamTexture == null)
            yield return null;
        texture2D = new Texture2D(webCamTextureManager.WebCamTexture.width, webCamTextureManager.WebCamTexture.height, TextureFormat.RGB24, false);
        updateTexture2D();
        StartCoroutine(callRoboflow());
    }

    private IEnumerator callRoboflow() {
        while (true) {
            if (texture2D == null) {
                yield return null;
                continue;
            }
            updateTexture2D();

            byte[] png = texture2D.EncodeToPNG();
            string base64Image = Convert.ToBase64String(png);
            var image = new InferenceRequestImage("base64", base64Image);

            bool isDone = false;
            // Call Roboflow and wait for completion
            yield return StartCoroutine(client.InferObjectDetection(
                new ObjectDetectionInferenceRequest(RF_MODEL, image),
                response => { OnResponse(response); isDone = true; },
                error => { Debug.Log(error); isDone = true; }
            ));

            yield return new WaitUntil(() => isDone);
        }
    }

    // Callback for successful inference response.
    private void OnResponse(ObjectDetectionInferenceResponse response) {
        if (response.Predictions != null && response.Predictions.Count > 0) {
            foreach (var pred in response.Predictions)
                Debug.Log($"Detected {pred.Class} at ({pred.X},{pred.Y}) confidence: {pred.Confidence}");
            renderDetections(response.Predictions);
        }
    }

    // Returns an existing marker for a given class ID.
    private RoboflowObject checkForExistingMarker(int classID) {
        _activeMarkerMap.TryGetValue(classID, out var marker);
        return marker;
    }

    public void renderDetections(List<ObjectDetectionPrediction> predictions) {
        thisTimeInHand.Clear();

        // Dictionary to store accumulated positions per class
        Dictionary<string, List<Vector3>> classPositions = new();

        foreach (var prediction in predictions) {
            if (prediction.Confidence < minConfidence)
                continue;
            RoboflowObject marker = checkForExistingMarker(prediction.Class_Id);
            if (marker == null)
                continue;
            Vector3? worldPos = GetWorldPoint(prediction);
            if (worldPos == null)
                continue;
            // Store position in dictionary
            if (!classPositions.ContainsKey(prediction.Class))
                classPositions[prediction.Class] = new List<Vector3>();
            classPositions[prediction.Class].Add(worldPos.Value);
        }

        // Step 2: For each class, take average position
        foreach (var kvp in classPositions) {
            string className = kvp.Key;
            List<Vector3> positions = kvp.Value;
            Vector3 avgPos = positions.Aggregate(Vector3.zero, (acc, p) => acc + p) / positions.Count;

            RoboflowObject marker = checkForExistingMarker(rfClassNames.IndexOf(className));
            if (marker == null)
                continue;

            // Check if card is in hand (close to left hand)
            float distanceToHand = Vector3.Distance(avgPos, leftHandAnchor.position);
            if (distanceToHand < 0.25f) {
                marker.gameObject.SetActive(false);
                thisTimeInHand.Add(className);
                if (FieldCards.Contains(className)) {
                    FieldCards.Remove(className);
                    StartCoroutine(EvaluateHand());
                }
            }
            else {
                if (positions.Count >= 2) {
                    // Just track on first view -> Because of tracking delay
                    if (!FieldCards.Contains(className) && FieldCards.Count < 5 && !HandCards.Contains(className)) {
                        marker.SuccesfullyTracked(avgPos + Vector3.up * 0.1f, centerEyeAnchor.transform.position);
                        marker.SetDebugText(className.Substring(0, className.Length - 1));
                        FieldCards.Add(className);
                        StartCoroutine(EvaluateHand());
                    }
                }
            }

        }

        // Step 3: Handle hand cards logic
        if (thisTimeInHand.Count == 2) {
            if (!((HandCards[0] == thisTimeInHand[0] && HandCards[1] == thisTimeInHand[1]) ||
                  (HandCards[1] == thisTimeInHand[0] && HandCards[0] == thisTimeInHand[1]))) {
                HandCards[0] = thisTimeInHand[0];
                HandCards[1] = thisTimeInHand[1];
                SetHandCards(HandCards);
                StartCoroutine(EvaluateHand());
            }
        }
        thisTimeInHand.Clear();
    }

    private void SetHandCards(String[] cards)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cardSuit[i].texture = GetSuitTexture(cards[i]);
            string rank = cards[i].Substring(0, cards[i].Length - 1);

            switch (rank)
            {
                case "J":
                    cardText[i].SetText("");
                    cardNumber[i].texture = jackIcon;
                    break;
                case "Q":
                    cardText[i].SetText("");
                    cardNumber[i].texture = queenIcon;
                    break;
                case "K":
                    cardText[i].SetText("");
                    cardNumber[i].texture = kingIcon;
                    break;
                case "A":
                    cardText[i].SetText("A");
                    cardNumber[i].texture = emptyIcon;
                    break;
                default:
                    cardText[i].SetText(rank);
                    cardNumber[i].texture = emptyIcon;
                    break;
            }
        }
    }

    private Vector3? GetWorldPoint(ObjectDetectionPrediction prediction) {
        float halfWidth = targetWidth * 0.5f;
        float halfHeight = targetHeight * 0.5f;

        // Convert detection to world space
        float adjustedCenterX = prediction.X - halfWidth;
        float adjustedCenterY = prediction.Y - halfHeight;
        float perX = (adjustedCenterX + halfWidth) / targetWidth;
        float perY = (adjustedCenterY + halfHeight) / targetHeight;
        Vector2 centerPixel = new Vector2(perX * PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.Eye).Resolution.x, (1.0f - perY) * PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.Eye).Resolution.y);
        Ray centerRay = PassthroughCameraUtils.ScreenPointToRayInWorld(
            webCamTextureManager.Eye,
            new Vector2Int(Mathf.RoundToInt(centerPixel.x), Mathf.RoundToInt(centerPixel.y))
        );

        if (!envRaycastManager.Raycast(centerRay, out var centerHit))
            return null;

        return centerHit.point;
    }

    // Coroutine to evaluate the hand
    public IEnumerator EvaluateHand() {
        // Convert hand and board to API format
        List<string> apiHand = new List<string> {
            Format.RoboflowCardToNodeJsCard(HandCards[0]),
            Format.RoboflowCardToNodeJsCard(HandCards[1])
        };

        List<string> apiBoard = new List<string>();
        foreach (var card in FieldCards) {
            apiBoard.Add(Format.RoboflowCardToNodeJsCard(card));
        }

        // Create JSON payload
        string jsonData = JsonUtility.ToJson(new HandRequest {
            hand = apiHand.ToArray(),
            board = apiBoard.ToArray()
        });

        // Send POST request
        using (UnityWebRequest request = new UnityWebRequest(LOCAL_SERVER_POKER_IP_ADDRESS+ "/eval_hand", "POST")) {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError("Error Hand Cards: " + request.error);
            }
            else {
                // Parse response
                HandResponse response = JsonUtility.FromJson<HandResponse>(request.downloadHandler.text);
                handStrengthSlider.value = response.normalizedStrength * 1.3f; // slight increase to because low numbers are good already
                handStrengthSliderText.SetText(((int)(response.normalizedStrength * 100)).ToString());
                Debug.Log("Normalized Hand Strength: " + response.normalizedStrength);
            }
        }
    }
}