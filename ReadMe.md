# ♣️ Poker AI Assistant (Roboflow)
A Meta Quest application that uses computer vision and Poker Odds API to calculate poker hand strenght in real time.

<img src="https://github.com/user-attachments/assets/77c3c56a-8e14-46db-b135-75bf267cb436" alt="Poker AI Assistant" width="540px">

## 🔎 Overview
* This project uses computer vision to scan poker game cards and runs simulation engine to calculate hand equity in real-time.
* It provides instant, data-driven insights direcly in your field of view, helping players make mathematically optimal decisions. 

---

## 🏢 Architecture
<img width="640" height="330" alt="Screenshot 2025-09-21 210133" src="https://github.com/user-attachments/assets/d43c6a1a-086c-4fbd-acf5-b05ff4dbac38" />

### Applications/Packages
* [Unity](https://unity.com/) 6000.0.55f1
* [Meta XR SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657)
* [Meta PCA Sample](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples)
* [Roboflow Inference](https://github.com/roboflow/inference)
* [Poker-Odds NodeJs](https://github.com/cookpete/poker-odds) -> Serverfile under ./backend/server.js

---

## 🚀 Project Setup

### 1. Clone this Repository
```bash
git clone https://github.com/nigelhartm/MetaPCARoboflow.git
````
---

### 2. Setup Roboflow Inference Server (required)

👉 [https://github.com/roboflow/inference](https://github.com/roboflow/inference)

To run AI inference, you need to start a local Roboflow Inference Server on your computer (PC or Mac).
This project does not run the model directly on the Meta Quest or on-device. Instead, it sends images from Unity or your headset to the inference server running on your desktop.
For CUDA I recommend this link https://developer.nvidia.com/cuda-downloads

---

### 3. Setup Node.js Backend (required)
* Initialize Node.js environment in the backend folder:
```bash
npm install
````
* Start the server:
```bash
node server.js
````
* The Node.js backend handles communication between Roboflow inference and your Unity/Meta Quest app.

---

## 4. Roboflow API Key Setup 🔑 

* Create a file Assets/Secrets/`APIKeys.cs` with your API key:

```csharp
public static class APIKeys
{
    public const string RF_API_KEY = "your-roboflow-api-key";
}
```
---

## 5. Run Unity Sample

* Open `Main.unity` in Unity.
* Update the IP addresses in `RoboflowCaller.cs` to match your local inference servers:

```csharp
[SerializeField] private string LOCAL_SERVER_IP_ADDRESS = "http://YOUR_COMPUTER_IP:9001";        // Main scene
[SerializeField] private string LOCAL_SERVER_POKER_IP_ADDRESS = "http://YOUR_COMPUTER_IP:3000"; // PokerOdd scene
```
* Build the project for Android (XR Plugin Management > Oculus).
* Deploy and run on Meta Quest with permissions for camera and local network access.

> :warning: **Server not running**: Don't forget that the server need to be started before and the first call takes up to a minute to download the model before!

---

## 📝 Project Overview
### Unity Objects
* WebCamTextureManager - Use the Passthrough Camera of Meta Quest
* EnvironmentRaycastManager - Using Meta’s Depth API to get the distance to objects
* RoboflowCaller - Entire Application logic
* BuildingBlocks - Meta XR blocks for easy integration & project setup.
* HandActivator - Turn on Hand GUI if watching on inner hand

### Possbile Problems
* Internet Connection - Ensure your app has a secure and stable internet connection.
* IP Address - Verify that the IP address in your “RoboflowCaller” object is correct.
* Local Inference - Make sure your Docker and Roboflow Local Inference are running. The first time you start, the model may take a few minutes to download.
* Poker Odds Server - Confirm that the Node.js Poker Odds Server is active and running.
* Permissions - On Meta Quest, double-check that all necessary permissions are enabled in the settings.

---

## Relevant Sources & Opportunities

* [SensAI Kits GitHub](https://github.com/XRBootcamp/SensAIKits) - Main hub for all XR AI kits
* [SensAI Hackademy](https://www.sensaihackademy.com) - Early access program for courses and toolkits
* [SensAI Hack](https://sensaihack.com) - Upcoming hackathons where you can use the kits

---

## Resources & Credits
PokerAssets: https://github.com/zardtomcat <br>
Icons: <a href="https://www.flaticon.com/free-icons/poker" title="poker icons">Freepik - Flaticon</a>

---

## 📄 License
MIT – Free to use, modify and learn from.
