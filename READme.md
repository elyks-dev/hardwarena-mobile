# HARDWARENA MOBILE

HardWarena Mobile is a Unity 6000 Android application that serves as a virtual gamified hardware laboratory for Computer System Servicing (CSS). This repository contains the complete Unity project, including assets, scenes, scripts, Firebase integration, and project settings.

## Requirements

Before opening the project, install the following:

- Unity Hub
- Unity Editor **6000.5.4f1**
- Android Build Support for Unity, including:
  - Android SDK & NDK Tools
  - OpenJDK

## Clone the Project

Open Terminal and clone the repository.

```bash
git clone https://github.com/elyks-dev/hardwarena-mobile.git
cd hardwarena-mobile
```

## Open the Project in Unity

1. Open **Unity Hub**.
2. Click **Add**.
3. Select the cloned `hardwarena-mobile` folder.
4. Open the project using **Unity 6000.5.4f1**.

> **Note:** The first time the project opens, Unity will automatically generate the `Library`, `Logs`, and `Temp` folders. This may take several minutes.

## Firebase Setup

After the project finishes importing:

1. In Unity, go to **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
2. Wait until the message **Resolution Succeeded** appears.

This downloads and configures the required Firebase Android dependencies.

## Build for Android

1. Go to **File → Build Settings**.
2. Select **Android** as the target platform.
3. Click **Switch Platform** if needed.
4. Connect an Android device or create an emulator.
5. Click **Build** or **Build and Run**.

## Git Workflow

Before starting work:

```bash
git pull origin main
```

After making changes:

```bash
git add .
git commit -m "Describe your changes"
git push origin main
```

## Project Structure

```text
Assets/              Unity assets, scripts, scenes, prefabs, and Firebase files
Packages/            Unity package dependencies
ProjectSettings/     Unity project configuration
firestore.rules      Firebase Firestore security rules
.gitignore           Git ignore rules for Unity-generated files
```

## Notes

- Do **not** commit the `Library`, `Logs`, `Temp`, `build`, or `.utmp` folders.
- Unity will regenerate ignored folders automatically when the project is opened.
- If Firebase dependencies are missing after pulling the project, run **Force Resolve** again from the External Dependency Manager.