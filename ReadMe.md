# Poker Assistant

## Preview
https://github.com/user-attachments/assets/6c041819-7b06-4d1a-9434-8603f22ca982

## Architecture
<img width="640" height="330" alt="Screenshot 2025-09-21 210133" src="https://github.com/user-attachments/assets/d43c6a1a-086c-4fbd-acf5-b05ff4dbac38" />

## Applications/Packages
[Unity](https://unity.com/) 6000.0.55f1
[Meta XR SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657)
[Meta PCA Sample](https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples)
[Roboflow Inference](https://github.com/roboflow/inference)
[Poker-Odds NodeJs](https://github.com/cookpete/poker-odds) -> Serverfile under ./backend/server.js

## Setup
Follow the instructions on https://github.com/nigelhartm/MetaPCARoboflow and take care to include the correct IP Adress of your local Server (Computer) and that the Meta Quest is in the same Wifi network.
Additional to the local Roboflow inference server there is a NodeJs Backend which need to be setup. Which is basically just starting the NodeJS server file, after initializing the nodejs environment.

## Unity Objects
WebCamTextureManager - Use the Passthrough Camera of Meta Quest
EnvironmentRaycastManager - Using Meta’s Depth API to get the distance to objects
RoboflowCaller - Entire Application logic
BuildingBlocks - Meta XR blocks for easy integration & project setup.
HandActivator - Turn on Hand GUI if watching on inner hand

## Possbile Problems
Internet Connection - Ensure your app has a secure and stable internet connection.
IP Address - Verify that the IP address in your “RoboflowCaller” object is correct.
Local Inference - Make sure your Docker and Roboflow Local Inference are running. The first time you start, the model may take a few minutes to download.
Poker Odds Server - Confirm that the Node.js Poker Odds Server is active and running.
Permissions - On Meta Quest, double-check that all necessary permissions are enabled in the settings.

## Resources
PokerAssets: https://github.com/zardtomcat
Icons: <a href="https://www.flaticon.com/free-icons/poker" title="poker icons">Freepik - Flaticon</a>
