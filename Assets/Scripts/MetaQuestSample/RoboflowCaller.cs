using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using Meta.XR;
using System.Linq;
using UnityEngine.Networking;

/// <summary>
/// Handles webcam streaming, sending frames to Roboflow,
/// receiving detections, and rendering tracked objects in 3D space.
/// </summary>
public class RoboflowCaller : MonoBehaviour
{
    [Header("Camera & Streaming")]
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    private Texture2D texture2D = null; // Used for sending frames to Roboflow
    private bool isStreaming = false; // Streaming toggle

    [Header("3D Scene References")]
    [SerializeField] private EnvironmentRaycastManager envRaycastManager;
    [SerializeField] private GameObject CenterEyeAnchor;
    [SerializeField] private Transform leftHandAnchor;

    [Header("Tracked Marker Objects")]
    [SerializeField] private GameObject _markerPrefab; // Prefabs to instantiate
    private List<string> rfClassNames = new List<string> {
            "10C", "10D", "10H", "10S",
            "2C", "2D", "2H", "2S",
            "3C", "3D", "3H", "3S",
            "4C", "4D", "4H", "4S",
            "5C", "5D", "5H", "5S",
            "6C", "6D", "6H", "6S",
            "7C", "7D", "7H", "7S",
            "8C", "8D", "8H", "8S",
            "9C", "9D", "9H", "9S",
            "AC", "AD", "AH", "AS",
            "JC", "JD", "JH", "JS",
            "KC", "KD", "KH", "KS",
            "QC", "QD", "QH", "QS"}; // Names of classes for
    private List<string> thisTimeInHand = new List<string>(); // Tracks cards currently in hand
    private String[] HandCards = new String[2]; // Cards in hand 
    private List<string> FieldCards = new List<string>(); // Cards on the field
    private Dictionary<int, RoboflowObject> _activeMarkerMap = new(); // runtime pool
    [SerializeField] private float minConfidence = 0.8f; // Detection confidence threshold

    [Header("Roboflow API Configuration")]
    [SerializeField] private string RF_MODEL = "xraihack_bears-fndxs/2"; // Model name for Roboflow
    [SerializeField] private string LOCAL_SERVER_IP_ADDRESS = "http://192.168.0.220:9001"; // Local server URL for Roboflow
    [SerializeField] private string LOCAL_SERVER_POKER_IP_ADDRESS = "http://192.168.0.220:3000";
    private RoboflowInferenceClient client; // API client

    private Texture2D result; // Texture for resized images
    private const int targetWidth = 1280; // Target width for resized images
    private const int targetHeight = 960; // Target height for resized images

    [Header("Card Suit Textures")]
    [SerializeField] private Texture2D clubTexture;
    [SerializeField] private Texture2D diamondTexture;
    [SerializeField] private Texture2D heartTexture;
    [SerializeField] private Texture2D spadeTexture;

    [SerializeField] private Slider handStrengthSlider;

    [SerializeField] private TMPro.TextMeshProUGUI handText;

    public int cardClassToID(string cardClass)
    {
        return rfClassNames.IndexOf(cardClass);
    }

    private void Start()
    {
        // Initialize Roboflow client with local server URL
        client = new RoboflowInferenceClient(APIKeys.RF_API_KEY, LOCAL_SERVER_IP_ADDRESS);
        BuildObjectPool();

        result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        StartCoroutine(initCoroutine());
    }

    /// <summary>
    /// Sets up the object pool for Roboflow objects.
    /// </summary>
    private void BuildObjectPool()
    {
        // Build marker pool dynamically
        for (var i = 0; i < rfClassNames.Count; i++)
        {
            var instance = Instantiate(_markerPrefab, Vector3.zero, Quaternion.identity);
            var rfObject = instance.GetComponent<RoboflowObject>();
            rfObject.Init(rfClassNames[i], i); // Initialize with class name and ID
            _activeMarkerMap[rfObject.ClassID] = rfObject;

            // --- NEW: assign suit texture to child RawImage ---
            var rawImage = instance.GetComponentInChildren<RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = GetSuitTexture(rfClassNames[i]);
            }
        }
    }

    // Helper to get the texture based on the suit in the card name
    private Texture2D GetSuitTexture(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return null;
        char suitChar = cardName[cardName.Length - 1]; // last char: C,D,H,S
        switch (char.ToUpper(suitChar))
        {
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

    /// <summary>
    /// Initializes the webcam texture and sets it to the image display.
    /// </summary>
    private IEnumerator initCoroutine()
    {
        Debug.Log("initCoroutine started");

        // Wait for webcam to be ready
        while (webCamTextureManager.WebCamTexture == null)
            yield return null;

        texture2D = new Texture2D(webCamTextureManager.WebCamTexture.width, webCamTextureManager.WebCamTexture.height, TextureFormat.RGB24, false);
        updateTexture2D();
        isStreaming = true;
        StartCoroutine(callRoboflow());
    }

    private IEnumerator callRoboflow()
    {
        while (isStreaming)
        {
            if (texture2D == null)
            {
                yield return null;
                continue;
            }
            updateTexture2D();

            //byte[] png = resizeTexture(texture2D, targetWidth, targetHeight).EncodeToPNG();
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

    /// <summary>
    /// Callback for successful inference response.
    /// </summary>
    private void OnResponse(ObjectDetectionInferenceResponse response)
    {
        if (response.Predictions != null && response.Predictions.Count > 0)
        {
            foreach (var pred in response.Predictions)
                Debug.Log($"Detected {pred.Class} at ({pred.X},{pred.Y}) confidence: {pred.Confidence}");
            renderDetections(response.Predictions);
        }
        else
        {
            Debug.Log("No predictions found.");
        }
    }

    /// <summary>
    /// Returns an existing marker for a given class ID.
    /// </summary>
    private RoboflowObject checkForExistingMarker(int classID)
    {
        _activeMarkerMap.TryGetValue(classID, out var marker);
        return marker;
    }

    public void renderDetections(List<ObjectDetectionPrediction> predictions)
    {
        thisTimeInHand.Clear();

        // Dictionary to store accumulated positions per class
        Dictionary<string, List<Vector3>> classPositions = new();

        foreach (var prediction in predictions)
        {
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
        foreach (var kvp in classPositions)
        {
            string className = kvp.Key;
            List<Vector3> positions = kvp.Value;
            Vector3 avgPos = positions.Aggregate(Vector3.zero, (acc, p) => acc + p) / positions.Count;

            RoboflowObject marker = checkForExistingMarker(cardClassToID(className));
            if (marker == null)
                continue;

            // Check if card is in hand (close to left hand)
            float distanceToHand = Vector3.Distance(avgPos, leftHandAnchor.position);
            if (distanceToHand < 0.25f)
            {
                marker.Disable();
                thisTimeInHand.Add(className);
                if (FieldCards.Contains(className))
                {
                    FieldCards.Remove(className);
                    StartCoroutine(EvaluateHand());
                }
            }
            else
            {
                if (positions.Count >= 2)
                {
                    // Just track on first view -> Because of tracking delay
                    if (!FieldCards.Contains(className) && FieldCards.Count < 5 && !HandCards.Contains(className))
                    {
                        marker.SuccesfullyTracked(avgPos + Vector3.up * 0.1f, CenterEyeAnchor.transform.position);
                        marker.SetDebugText(className.Substring(0, className.Length - 1));
                        FieldCards.Add(className);
                        StartCoroutine(EvaluateHand());
                    }
                }
            }

        }

        // Step 3: Handle hand cards logic
        if (thisTimeInHand.Count == 2)
        {
            if (!((HandCards[0] == thisTimeInHand[0] && HandCards[1] == thisTimeInHand[1]) ||
                  (HandCards[1] == thisTimeInHand[0] && HandCards[0] == thisTimeInHand[1])))
            {
                HandCards[0] = thisTimeInHand[0];
                HandCards[1] = thisTimeInHand[1];
                handText.text = $"{HandCards[0]}\n{HandCards[1]}\n";
                StartCoroutine(EvaluateHand());
            }
        }

        thisTimeInHand.Clear();
    }

    private Vector3? GetWorldPoint(ObjectDetectionPrediction prediction)
    {
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

    // Convert Unity card format to API format
    public static string roboflowCardToNodeJsCard(string card)
    {
        if (string.IsNullOrEmpty(card) || card.Length < 2)
            throw new ArgumentException("Invalid card format ###" + card);

        string rank = card.Substring(0, card.Length - 1); // all except last char
        char suit = char.ToLower(card[card.Length - 1]);  // last char to lowercase

        // Map Unity ranks to API ranks
        switch (rank)
        {
            case "10": rank = "T"; break;
            case "J": rank = "J"; break;
            case "Q": rank = "Q"; break;
            case "K": rank = "K"; break;
            case "A": rank = "A"; break;
            default: rank = rank; break; // "2"-"9" stay the same
        }

        return rank + suit;
    }

    // Coroutine to evaluate the hand
    public IEnumerator EvaluateHand()
    {
        // Convert hand and board to API format
        List<string> apiHand = new List<string>
        {
            roboflowCardToNodeJsCard(HandCards[0]),
            roboflowCardToNodeJsCard(HandCards[1])
        };

        List<string> apiBoard = new List<string>();
        foreach (var card in FieldCards)
        {
            apiBoard.Add(roboflowCardToNodeJsCard(card));
        }

        // Create JSON payload
        string jsonData = JsonUtility.ToJson(new HandRequest
        {
            hand = apiHand.ToArray(),
            board = apiBoard.ToArray()
        });

        // Send POST request
        using (UnityWebRequest request = new UnityWebRequest(LOCAL_SERVER_POKER_IP_ADDRESS+ "/eval_hand", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error Hand Cards: " + request.error);
            }
            else
            {
                // Parse response
                HandResponse response = JsonUtility.FromJson<HandResponse>(request.downloadHandler.text);
                //handText.text += $"{response.normalizedStrength:F2}";
                handStrengthSlider.value = response.normalizedStrength;
                Debug.Log("Normalized Hand Strength: " + response.normalizedStrength);
            }
        }
    }

    // JSON helper classes
    [System.Serializable]
    public class HandRequest
    {
        public string[] hand;
        public string[] board;
    }

    [System.Serializable]
    public class HandResponse
    {
        public string[] hand;
        public int simulations;
        public HandChance[] distribution;
        public float strengthScore;
        public float normalizedStrength;
        public float visualStrength;
    }

    [System.Serializable]
    public class HandChance
    {
        public string name;
        public float probability;
    }
}