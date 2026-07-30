# Android SDK setup (local machine)

Installed under `~/Android` for Unity Android builds of Flying Fox.

| Component | Path |
|-----------|------|
| **JDK 17** | `/home/oem/Android/jdk/jdk-17.0.20+8` |
| **Android SDK** | `/home/oem/Android/Sdk` |
| **NDK** | `/home/oem/Android/Sdk/ndk/27.2.12479018` |
| **adb** | `/home/oem/Android/Sdk/platform-tools/adb` |

## Installed packages

- platform-tools 37.0.0
- platforms android-34, android-35
- build-tools 34.0.0, 35.0.0
- NDK 27.2.12479018
- cmdline-tools latest

## Shell

```bash
source ~/Android/env.sh
# or open a new terminal (sourced from ~/.bashrc)
adb version
```

## Unity Editor

1. Open **Edit → Preferences → External Tools** (Linux: **Edit → Preferences**).
2. Uncheck **Android SDK Tools Installed with Unity** (if present) and set:
   - **JDK**: `/home/oem/Android/jdk/jdk-17.0.20+8`
   - **Android SDK**: `/home/oem/Android/Sdk`
   - **Android NDK**: `/home/oem/Android/Sdk/ndk/27.2.12479018`
3. Install **Android Build Support** module for Unity 6000.0.79f1 via Unity Hub if you have not already (includes Android Player export).
4. **File → Build Settings → Android → Switch Platform**.

Also install via Hub if missing: **Android Build Support**, **OpenJDK**, **Android SDK & NDK Tools** — or keep using this custom SDK path as above.

## China sideload APK (all CN tablets / phones, no Google Play)

```bash
cd ~/flying-fox/unity
source ~/Android/env.sh
./Tools/build.sh android-china
# → Builds/Android-China/FlyingFox-China.apk
```

Full install / distribution notes: [`docs/ANDROID_CHINA_SIDELOAD.md`](../docs/ANDROID_CHINA_SIDELOAD.md).

## Note

Flying Fox v1 targets Steam / desktop first. The **China Android** channel is a
**generic sideload-only APK** (ARM64 + ARMv7) for Chinese phones and tablets,
explicitly **excluded from Google Play**.
