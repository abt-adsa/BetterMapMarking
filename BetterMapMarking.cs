using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMProOld;
using System.Linq;

namespace BetterMapMarking
{
    [BepInPlugin("com.abt-adsa.bettermapmarking", "More Map Markers", "1.0.0")]
    public class BetterMapMarking : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo($"Plugin {Info.Metadata.GUID} is loaded!");
            Harmony.CreateAndPatchAll(typeof(MarkerPatches));
        }
    }

    [HarmonyPatch]
    public static class MarkerPatches
    {
        private const int NewMarkerLimit = 999;

        [HarmonyPatch(typeof(MapMarkerMenu), "UpdateAmounts")]
        [HarmonyPrefix]
        static bool UpdateAmounts_Patch(MapMarkerMenu __instance)
        {
            var enabledColour = new Color(1f, 1f, 1f, 1f);
            var disabledColour = new Color(0.5f, 0.5f, 0.5f, 1f);

            var amounts = (TMProOld.TextMeshPro[])AccessTools.Field(typeof(MapMarkerMenu), "amounts").GetValue(__instance);
            var markers = (Animator[])AccessTools.Field(typeof(MapMarkerMenu), "markers").GetValue(__instance);
            var getMarkerListMethod = AccessTools.Method(typeof(MapMarkerMenu), "GetMarkerList");

            for (int i = 0; i < amounts.Length; i++)
            {
                List<Vector2> markerList = (List<Vector2>)getMarkerListMethod.Invoke(__instance, new object[] { i });

                int placedCount = markerList.Count;
                TMProOld.TextMeshPro textMeshPro = amounts[i];
                textMeshPro.text = placedCount.ToString();

                SpriteRenderer componentInChildren = ((Component)(object)markers[i]).GetComponentInChildren<SpriteRenderer>();

                if (placedCount < NewMarkerLimit)
                {
                    componentInChildren.color = enabledColour;
                    textMeshPro.color = enabledColour;
                }
                else
                {
                    componentInChildren.color = disabledColour;
                    textMeshPro.color = disabledColour;
                }
            }
            return false;
        }

        private static IEnumerable<CodeInstruction> ReplaceNineWithNewLimit(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 9)
                {
                    codes[i].opcode = OpCodes.Ldc_I4;
                    codes[i].operand = NewMarkerLimit;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPatch(typeof(MapMarkerMenu), "PlaceMarker")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PlaceMarker_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceNineWithNewLimit(instructions);
        }

        [HarmonyPatch(typeof(MapMarkerMenu), "Open")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Open_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceNineWithNewLimit(instructions);
        }

        [HarmonyPatch(typeof(GameMap), "OnAwake")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnAwake_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ReplaceNineWithNewLimit(instructions);
        }
    }
}
