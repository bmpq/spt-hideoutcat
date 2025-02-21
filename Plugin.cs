using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DG.Tweening;
using EFT;
using hideoutcat;
using UnityEngine;

[BepInPlugin("com.tarkin.hideoutcat", "hideoutcat", "1.0.0.0")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Log;

    private void Start()
    {
        Log = base.Logger;

        InitConfiguration();

        new PatchHideoutAwake().Enable();
    }

    private void InitConfiguration()
    {
    }
}