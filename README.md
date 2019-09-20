# ECIKParentUnlocker
A plugin for Emotion Creators to allow reparenting any IK target in H nodes

## Installation
To install, place ECIKParentUnlocker.dll in your BepInEx plugins directory.
I recommend putting it in a folder on its own with my name for organization
(the pre-built .zip has the plugin inside that folder).

## Building
If you want to build it yourself, I use the project in VS2017. It relies on the following
file structure in the directory _above_ the repo root:

- ECIKParentUnlocker/ (the top-level repo directory)
  - ECIKParentUnlocker/
    - etc
  - ECIKParentUnlocker.sln
  - etc
- lib/ (in the same directory as the repo folder, _not_ inside)
  - BepInEx/
    - 0Harmony.dll
    - BepInEx.dll
  - Emotion Creators/ (all of these can be found in _EC install folder_/EmotionCreators_Data/Managed/)
    - Assembly-CSharp.dll
    - Assembly-CSharp-firstpass.dll
    - IL.dll
    - TextMeshPro-1.0.55.56.0b12.dll
    - UniRX.dll
    - UnityEngine.dll
    - UnityEngine.CoreModule.dll
    - UnityEngine.UI.dll
