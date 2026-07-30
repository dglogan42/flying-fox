# Flying Fox — China sideload APK (all tablets / phones)

Generic **sideload-only** Android build for **mainland China devices** that ship
**without Google Play** / GMS. One fat APK for phones and tablets across OEMs.

| | |
|--|--|
| **Package** | `com.flyingfox.china` |
| **Launcher name** | 飞狐 Flying Fox |
| **Artifact** | `FlyingFox-China.apk` (**APK only** — not a Play App Bundle) |
| **Defines** | `FF_CHINA_SIDELOAD`, `FF_NO_GPLAY` |
| **ABIs** | **ARM64 + ARMv7** (covers modern + older devices) |
| **min / target SDK** | 24 / 34 (Android 7.0+) |
| **Store** | **Excluded from Google Play** — USB, LAN, private link, OEM file share only |

Designed for pure AOSP / China OEM ROMs (no GMS dependency).

## Target devices

Works the same on (non-exhaustive):

| OEM / line | Examples |
|------------|----------|
| Lenovo | Xiaoxin / 小新 tablets & phones |
| Huawei / Honor | MatePad, Nova (no GMS models) |
| Xiaomi / Redmi / POCO | Pad, Note, etc. |
| Oppo / Realme / OnePlus | Pad, Reno, etc. |
| Vivo / iQOO | Pad, X series |
| Other | Any ARM Android 7+ without Play |

Touch-first; gamepad optional if the device has one.

## Why not Google Play

- Most CN retail units have **no Play Store** and unreliable or no GMS  
- Distribution is **sideload** or Chinese app stores (this package is store-agnostic)  
- Game is **offline-first**: local saves only, no Play Billing / Games / Login  

If a Google Play build is ever needed, add a **separate** target (AAB + different
application id, **without** `FF_CHINA_SIDELOAD`).

## Prerequisites

1. Unity 6 LTS + **Android Build Support**
2. Android SDK / NDK — see [`unity/ANDROID_SDK.md`](../unity/ANDROID_SDK.md)
3. External Tools paths (JDK / SDK / NDK) set in Unity Preferences

## Build

```bash
cd unity
chmod +x Tools/build.sh
source ~/Android/env.sh   # if using local SDK under ~/Android

./Tools/build.sh android-china
# aliases: china | china-sideload | cn | android
# legacy:  android-xiaoxin | xiaoxin-pro
```

Output:

```
unity/Builds/Android-China/FlyingFox-China.apk
unity/Builds/Android-China/build-info.txt
```

Editor menu: **Flying Fox → Build → Android China Sideload APK (no Google Play)**

### What the build applies

- `buildAppBundle = false` (APK, not AAB)  
- Package `com.flyingfox.china`  
- IL2CPP, **ARM64 | ARMv7**  
- Defines `FF_CHINA_SIDELOAD;FF_NO_GPLAY`  
- Platform: `ChinaSideloadPlatformServices` (`PlatformId = china-sideload`)  

## Install (any Chinese phone / tablet)

1. **Settings → Security / Apps** → allow **Install unknown apps** for Files / browser / USB.  
2. Copy `FlyingFox-China.apk` to the device (USB, WeChat, LAN, cloud link, …).  
3. Open the APK → **Install**.  
4. Launch **飞狐 Flying Fox**.

### adb

```bash
source ~/Android/env.sh
adb devices
adb install -r unity/Builds/Android-China/FlyingFox-China.apk
```

## Distribution rules

| Do | Don’t |
|----|--------|
| Ship this **APK** via private download / USB / team share | Upload `com.flyingfox.china` to **Google Play** |
| Keep package id stable for updates | Rely on Play App Signing |
| Stay offline / optional CN store later | Bundle Play Billing or GMS-only plugins |

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Install blocked | Enable unknown sources for the installer app |
| `INSTALL_FAILED_NO_MATCHING_ABIS` | Rebuild with ARM64+ARMv7 (default for this channel) |
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | Uninstall old build or bump versionCode |
| Huawei / EMUI install warning | Expected for sideloads; allow this once |
| Disk full (IL2CPP) | Free several GB under `/home` before build |

## Related

- [BUILD_PIPELINE.md](BUILD_PIPELINE.md)  
- [unity/ANDROID_SDK.md](../unity/ANDROID_SDK.md)  
- Legacy note: [ANDROID_XIAOXIN.md](ANDROID_XIAOXIN.md) redirects here  
